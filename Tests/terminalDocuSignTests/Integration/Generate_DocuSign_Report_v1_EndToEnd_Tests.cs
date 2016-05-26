﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fr8Data.Constants;
using Fr8Data.Control;
using Fr8Data.Crates;
using Fr8Data.DataTransferObjects;
using Fr8Data.Manifests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using HealthMonitor.Utility;

namespace terminalDocuSignTests.Integration
{
    [Explicit]
    [Category("terminalDocuSignTests.Integration")]
    public class Generate_DocuSign_Report_v1_EndToEnd_Tests : BaseHubIntegrationTest
    {
        public override string TerminalName
        {
            get { return "terminalDocuSign"; }
        }

        [Test]
        public async Task Generate_DocuSign_Report_EndToEnd()
        {
            try
            {
                await RevokeTokens();

                // Create Solution plan & initial configuration.
                var plan = await CreateSolution();
                var solution = ExtractSolution(plan.Plan);
                solution = await EnsureSolutionAuthenticated(solution);

                var crateStorage = Crate.FromDto(solution.CrateStorage);
                ValidateCrateStructure(crateStorage);
                ValidateConfigurationControls(crateStorage);
                var planConfigure = await GetPlanByActivity(solution.Id);
                ValidatePlanCategory(planConfigure.Plan);
                await SaveActivity(solution);

                // FollowUp configuration.
                MockSolutionFollowUpConfigurationData(solution);
                solution = await ConfigureActivity(solution);

                crateStorage = Crate.FromDto(solution.CrateStorage);
                ValidateCrateStructure(crateStorage);
                ValidateConfigurationControls(crateStorage);
                ValidateChildrenActivities(solution);
                ValidateSolutionOperationalState(crateStorage);
                var planFollowup = await GetPlanByActivity(solution.Id);
                ValidatePlanName(planFollowup.Plan, crateStorage);
                await SaveActivity(solution);

                // Execute plan.
                var container = await ExecutePlan(plan.Plan);
                ValidateContainer(container);


                // Extract container payload.
                var payload = await ExtractContainerPayload(container);
                ValidateContainerPayload(payload);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<PlanDTO> CreateSolution()
        {
            var solutionCreateUrl = _baseUrl + "plans/createSolution?solutionName=Generate_DocuSign_Report";
            
            var plan = await HttpPostAsync<string, PlanDTO>(solutionCreateUrl, null);

            return plan;
        }

        private ActivityDTO ExtractSolution(PlanFullDTO plan)
        {
            var solution = plan.SubPlans
                .FirstOrDefault()
                .Activities
                .FirstOrDefault();

            return solution;
        }

        private async Task<ActivityDTO> EnsureSolutionAuthenticated(ActivityDTO solution)
        {
            var crateStorage = Crate.FromDto(solution.CrateStorage);
            var stAuthCrate = crateStorage.CratesOfType<StandardAuthenticationCM>().FirstOrDefault();
            var defaultDocuSignAuthTokenExists = (stAuthCrate == null);

            if (!defaultDocuSignAuthTokenExists)
            {
                var creds = GetDocuSignCredentials();
                creds.Terminal = solution.ActivityTemplate.Terminal;

                var token = await HttpPostAsync<CredentialsDTO, JObject>(
                    _baseUrl + "authentication/token", creds
                );      

                Assert.AreNotEqual(
                    token["error"].ToString(),
                    "Unable to authenticate in DocuSign service, invalid login name or password.", "DocuSign auth error. Perhaps max number of tokens is exceeded."
                );

                Assert.AreEqual(
                    false,
                    string.IsNullOrEmpty(token["authTokenId"].Value<string>()),
                    "AuthTokenId is missing in API response."
                );

                var tokenGuid = Guid.Parse(token["authTokenId"].Value<string>());

                var applyToken = new ManageAuthToken_Apply()
                {
                    ActivityId = solution.Id,
                    AuthTokenId = tokenGuid,
                    IsMain = true
                };

                await HttpPostAsync<ManageAuthToken_Apply[], string>(
                    _baseUrl + "ManageAuthToken/apply",
                    new ManageAuthToken_Apply[]
                    {
                        applyToken
                    }
                );

                solution = await HttpPostAsync<ActivityDTO, ActivityDTO>(
                    _baseUrl + "activities/configure?id=" + solution.Id,
                    solution
                );
            }

            return solution;
        }

        private void ValidateCrateStructure(ICrateStorage crateStorage)
        {
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count());
            Assert.AreEqual(1, crateStorage.CratesOfType<FieldDescriptionsCM>().Count());
            Assert.AreEqual("Queryable Criteria", crateStorage.CratesOfType<FieldDescriptionsCM>().Single().Label);
        }

        private void ValidateConfigurationControls(ICrateStorage crateStorage)
        {
            var controls = crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single();

            Assert.AreEqual(ControlTypes.TextArea, controls.Controls[0].Type);
            Assert.AreEqual(ControlTypes.QueryBuilder, controls.Controls[1].Type);
            Assert.AreEqual("QueryBuilder", controls.Controls[1].Name);
            Assert.AreEqual(ControlTypes.Button, controls.Controls[2].Type);
            Assert.AreEqual("Continue", controls.Controls[2].Name);
        }

        private void MockSolutionFollowUpConfigurationData(ActivityDTO solution)
        {
            using (var updater = Crate.UpdateStorage(() => solution.CrateStorage))
            {
                var controls = updater.CrateContentsOfType<StandardConfigurationControlsCM>().Single();

                // Set QueryBuilder's value.
                var queryBuilder = controls.FindByName<QueryBuilder>("QueryBuilder");

                var criteria = new List<FilterConditionDTO>()
                {
                    new FilterConditionDTO()
                    {
                        Field = "Folder",
                        Operator = "eq",
                        Value = "Sent Items"
                    },
                    new FilterConditionDTO()
                    {
                        Field = "Status",
                        Operator = "eq",
                        Value = "Sent"
                    },
                    new FilterConditionDTO()
                    {
                        Field = "CreateDate",
                        Operator = "gt",
                        Value = DateTime.Today.AddDays(-2).ToString("dd-MM-yyyy")
                    }
                };

                queryBuilder.Value = JsonConvert.SerializeObject(criteria);

                // Set Continue button Clicked = true.
                var continueButton = controls.FindByName<Button>("Continue");
                continueButton.Clicked = true;
            }
        }

        private void ValidateChildrenActivities(ActivityDTO solution)
        {
            Assert.AreEqual(1, solution.ChildrenActivities.Length);
            Assert.AreEqual("QueryFr8Warehouse", solution.ChildrenActivities[0].ActivityTemplate.Name);
        }

        private void ValidateSolutionOperationalState(ICrateStorage crateStorage)
        {
            Assert.AreEqual(1, crateStorage.CratesOfType<OperationalStateCM>().Count());

            var state = crateStorage.CrateContentsOfType<OperationalStateCM>().Single();
            Assert.AreEqual(
                ActivityResponse.ExecuteClientActivity.ToString(),
                state.CurrentActivityResponse.Type
            );
            Assert.AreEqual(
                "RunImmediately",
                state.CurrentClientActivityName
            );
        }

        private void ValidateContainer(ContainerDTO container)
        {
            //Activity Responses should be resetted
            Assert.AreEqual(null, container.CurrentActivityResponse);
            Assert.AreEqual("ShowTableReport", container.CurrentClientActivityName);
        }

        private void ValidateContainerPayload(PayloadDTO payload)
        {
            var crateStorage = Crate.FromDto(payload.CrateStorage);
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardPayloadDataCM>().Count(x => x.Label == "Sql Query Result"));
        }

        private async Task<PlanDTO> GetPlanByActivity(Guid id)
        {
            var solutionCreateUrl = _baseUrl + "/plans/getByActivity?id=" + id.ToString();
            var plan = await HttpGetAsync<PlanDTO>(solutionCreateUrl);

            return plan;
        }

        private void ValidatePlanCategory(PlanFullDTO plan)
        {
            Assert.AreEqual(plan.Category.Trim().ToLower(), "report");
        }

        private void ValidatePlanName(PlanFullDTO plan, ICrateStorage crateStorage)
        {
            var configurationControls = crateStorage
            .CrateContentsOfType<StandardConfigurationControlsCM>()
            .SingleOrDefault();

            var actionUi = new ActivityUi();
            actionUi.ClonePropertiesFrom(configurationControls);

            var criteria = JsonConvert.DeserializeObject<List<FilterConditionDTO>>(
                actionUi.QueryBuilder.Value
            );

            Assert.AreEqual(plan.Name.Trim().ToLower(), ParseConditionToText(criteria).Trim().ToLower());
        }
    }

    public class ActivityUi : StandardConfigurationControlsCM
    {
        [JsonIgnore]
        public QueryBuilder QueryBuilder { get; set; }

        public ActivityUi()
        {
            Controls = new List<ControlDefinitionDTO>();

            Controls.Add(new TextArea
            {
                IsReadOnly = true,
                Label = "",
                Value = "<p>Search for DocuSign Envelopes where:</p>"
            });

            var filterConditions = new[]
            {
                    new FilterConditionDTO { Field = "Envelope Text", Operator = "eq" },
                    new FilterConditionDTO { Field = "Folder", Operator = "eq" },
                    new FilterConditionDTO { Field = "Status", Operator = "eq" }
                };

            string initialQuery = JsonConvert.SerializeObject(filterConditions);

            Controls.Add((QueryBuilder = new QueryBuilder
            {
                Name = "QueryBuilder",
                Value = initialQuery,
                Source = new FieldSourceDTO
                {
                    Label = "Queryable Criteria",
                    ManifestType = CrateManifestTypes.StandardDesignTimeFields
                }
            }));

            Controls.Add(new Button()
            {
                Label = "Generate Report",
                Name = "Continue",
                Events = new List<ControlEvent>()
                    {
                        new ControlEvent("onClick", "requestConfig")
                    }
            });
        }
    }
}