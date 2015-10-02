﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StructureMap;
using Core.Interfaces;
using Data.Entities;
using Data.Infrastructure;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.ManifestSchemas;
using Data.States;
using Data.States.Templates;
using PluginBase.BaseClasses;
using PluginBase.Infrastructure;
using pluginDockyardCore.Interfaces;

namespace pluginDockyardCore.Actions
{
    public class FilterUsingRunTimeData_v1 : BasePluginAction
    {

        public FilterUsingRunTimeData_v1()
        {
        }

        /// <summary>
        /// Action processing infrastructure.
        /// </summary>
        public async Task<PayloadDTO> Execute(ActionDataPackageDTO curActionDataPackage)
        {
            var curActionDTO = curActionDataPackage.ActionDTO;
            var curPayloadDTO = await GetProcessPayload(curActionDataPackage.PayloadDTO.ProcessId);

            ActionDO curAction = AutoMapper.Mapper.Map<ActionDO>(curActionDTO);
            var controlsMS = _action.GetControlsManifest(curAction);
            
            FieldDefinitionDTO filterPaneControl = controlsMS.Controls.FirstOrDefault(x => x.Type == "filterPane");
            if (filterPaneControl == null)
            {
                throw new ApplicationException("No control found with Type == \"filterPane\"");
            }

            var valuesCrate = curPayloadDTO.CrateStorageDTO()
                .CrateDTO
                .Where(x => x.ManifestType == CrateManifests.STANDARD_PAYLOAD_MANIFEST_NAME
                    && x.Label == "DocuSign Envelope Data")
                .FirstOrDefault();

            // Prepare envelope data.
            var curValues = JsonConvert.DeserializeObject<List<FieldDTO>>(valuesCrate.Contents);

            // Evaluate criteria using Contents json body of found Crate.
            var result = Evaluate(filterPaneControl.Value, curPayloadDTO.ProcessId, curValues);

            return curPayloadDTO;
        }

        private bool Evaluate(string criteria, int processId, IEnumerable<FieldDTO> values)
            {
            if (criteria == null)
                throw new ArgumentNullException("criteria");
            if (criteria == string.Empty)
                throw new ArgumentException("criteria is empty", "criteria");
            if (values == null)
                throw new ArgumentNullException("envelopeData");

            return Filter(criteria, processId, values.AsQueryable()).Any();
        }

        private IQueryable<FieldDTO> Filter(string criteria,
            int processId, IQueryable<FieldDTO> values)
        {
            if (criteria == null)
                throw new ArgumentNullException("criteria");
            if (criteria == string.Empty)
                throw new ArgumentException("criteria is empty", "criteria");
            if (values == null)
                throw new ArgumentNullException("envelopeData");

            var filterDataDTO = JsonConvert.DeserializeObject<FilterDataDTO>(criteria);
            if (filterDataDTO.ExecutionType == FilterExecutionType.WithoutFilter)
            {
                return values;
            }
            else
            {
                EventManager.CriteriaEvaluationStarted(processId);

                var filterExpression = ParseCriteriaExpression(filterDataDTO.Conditions, values);
                var results = values.Provider.CreateQuery<FieldDTO>(filterExpression);

                return results;
            }
        }

        private Expression ParseCriteriaExpression(
            IEnumerable<FilterConditionDTO> conditions,
            IQueryable<FieldDTO> queryableData)
        {
            var curType = typeof(FieldDTO);

            Expression criteriaExpression = null;
            var pe = Expression.Parameter(curType, "p");

            foreach (var condition in conditions)
            {
                var namePropInfo = curType.GetProperty("Key");
                var valuePropInfo = curType.GetProperty("Value");

                var nameLeftExpr = Expression.Property(pe, namePropInfo);
                var nameRightExpr = Expression.Constant(condition.Field);
                var nameExpression = Expression.Equal(nameLeftExpr, nameRightExpr);

                var valueLeftExpr = Expression.Property(pe, valuePropInfo);
                var valueRightExpr = Expression.Constant(condition.Value);


                var op = condition.Operator;
                Expression criterionExpression;

                switch (op)
                {
                    case "eq":
                        criterionExpression = Expression.Equal(valueLeftExpr, valueRightExpr);
                        break;
                    case "neq":
                        criterionExpression = Expression.NotEqual(valueLeftExpr, valueRightExpr);
                        break;
                    case "gt":
                        criterionExpression = Expression.GreaterThan(valueLeftExpr, valueRightExpr);
                        break;
                    case "gte":
                        criterionExpression = Expression.GreaterThanOrEqual(valueLeftExpr, valueRightExpr);
                        break;
                    case "lt":
                        criterionExpression = Expression.LessThan(valueLeftExpr, valueRightExpr);
                        break;
                    case "lte":
                        criterionExpression = Expression.LessThanOrEqual(valueLeftExpr, valueRightExpr);
                        break;
                    default:
                        throw new NotSupportedException(string.Format("Not supported operator: {0}", op));
                }

                if (criteriaExpression == null)
                {
                    criteriaExpression = Expression.And(nameExpression, criterionExpression);
                }
                else
                {
                    criteriaExpression = Expression.AndAlso(criteriaExpression, Expression.And(nameExpression, criterionExpression));
                }
            }

            if (criteriaExpression == null)
            {
                criteriaExpression = Expression.Constant(true);
            }

            var whereCallExpression = Expression.Call(
                typeof (Queryable),
                "Where",
                new[] { curType },
                queryableData.Expression,
                Expression.Lambda<Func<FieldDTO, bool>>(criteriaExpression, new[] { pe })
            );

            return whereCallExpression;
        }

        /// <summary>
        /// Configure infrastructure.
        /// </summary>
        public async Task<ActionDTO> Configure(ActionDTO curActionDataPackageDTO)
        {
            return await ProcessConfigurationRequest(curActionDataPackageDTO, ConfigurationEvaluator);
        }

        private CrateDTO CreateControlsCrate()
        {
            var fieldFilterPane = new FilterPaneFieldDefinitionDTO()
            {
                Label = "Execute Actions If:",
                Name = "Selected_Filter",
                Required = true,
                Source = new FieldSourceDTO
                {
                    Label = "Queryable Criteria",
                    ManifestType = CrateManifests.DESIGNTIME_FIELDS_MANIFEST_NAME
                }
            };

            return PackControlsCrate(fieldFilterPane);
        }

        /// <summary>
        /// Looks for first Create with Id == "Standard Design-Time" among all upcoming Actions.
        /// </summary>
        protected override async Task<ActionDTO> InitialConfigurationResponse(ActionDTO curActionDTO)
        {
            if (curActionDTO.Id > 0)
            {
                //this conversion from actiondto to Action should be moved back to the controller edge
                var curUpstreamFields =
                    (await GetDesignTimeFields(curActionDTO.Id, GetCrateDirection.Upstream))
                    .Fields
                    .ToArray();

                //2) Pack the merged fields into a new crate that can be used to populate the dropdownlistbox
                CrateDTO queryFieldsCrate = _crate.CreateDesignTimeFieldsCrate(
                    "Queryable Criteria", curUpstreamFields);
                    
                //build a controls crate to render the pane
                CrateDTO configurationControlsCrate = CreateControlsCrate();

                var crateStrorageDTO = AssembleCrateStorage(queryFieldsCrate, configurationControlsCrate);
                curActionDTO.CrateStorage = crateStrorageDTO;
            }
            else
            {
                throw new ArgumentException(
                    "Configuration requires the submission of an Action that has a real ActionId");
            }

            return curActionDTO;
        }

        /// <summary>
        /// ConfigurationEvaluator always returns Initial,
        /// since Initial and FollowUp phases are the same for current action.
        /// </summary>
        private ConfigurationRequestType ConfigurationEvaluator(ActionDTO curActionDataPackageDTO)
        {
            if (curActionDataPackageDTO.CrateStorage == null
                && curActionDataPackageDTO.CrateStorage.CrateDTO == null)
            {
                return ConfigurationRequestType.Initial;
            }

            var hasControlsCrate = curActionDataPackageDTO.CrateStorage.CrateDTO
                .Any(x => x.ManifestType == CrateManifests.STANDARD_CONF_CONTROLS_NANIFEST_NAME
                    && x.Label == "Configuration_Controls");

            var hasQueryFieldsCrate = curActionDataPackageDTO.CrateStorage.CrateDTO
                .Any(x => x.ManifestType == CrateManifests.DESIGNTIME_FIELDS_MANIFEST_NAME
                    && x.Label == "Queryable Criteria");

            if (hasControlsCrate && hasQueryFieldsCrate)
            {
                return ConfigurationRequestType.Followup;
            }
            else
            {
                return ConfigurationRequestType.Initial;
            }
        }
    }
}
