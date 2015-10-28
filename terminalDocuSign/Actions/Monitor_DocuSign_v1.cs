﻿﻿using AutoMapper;
using Data.Entities;
using TerminalBase.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using TerminalBase;
using DocuSign.Integrations.Client;
using terminalDocuSign.DataTransferObjects;
using terminalDocuSign.Infrastructure;
using terminalDocuSign.Services;
using TerminalBase.BaseClasses;

namespace terminalDocuSign.Actions
{
    public class Monitor_DocuSign_v1 : BasePluginAction
    {
        DocuSignManager _docuSignManager = new DocuSignManager();

        public async Task<ActionDTO> Configure(ActionDTO curActionDTO)
        {
            if (NeedsAuthentication(curActionDTO))
            {
                throw new ApplicationException("No AuthToken provided.");
            }

            return await ProcessConfigurationRequest(curActionDTO, x => ConfigurationEvaluator(x));
        }

        public ConfigurationRequestType ConfigurationEvaluator(ActionDTO curActionDTO)
        {
            CrateStorageDTO curCrates = curActionDTO.CrateStorage;

            if (curCrates.CrateDTO.Count == 0)
            {
                return ConfigurationRequestType.Initial;
            }

            return ConfigurationRequestType.Followup;
        }

        protected CrateDTO PackCrate_DocuSignTemplateNames(DocuSignAuthDTO authDTO)
        {
            var template = new DocuSignTemplate();

            var templates = template.GetTemplates(authDTO.Email, authDTO.ApiPassword);
            var fields = templates.Select(x => new FieldDTO() { Key = x.Name, Value = x.Id }).ToArray();
            var createDesignTimeFields = Crate.CreateDesignTimeFieldsCrate(
                "Available Templates",
                fields);
            return createDesignTimeFields;
        }

        private string GetSelectedTemplateId(ActionDTO curActionDTO)
        {
            var controlsCrates = Crate.GetCratesByManifestType(CrateManifests.STANDARD_CONF_CONTROLS_NANIFEST_NAME,
                curActionDTO.CrateStorage);
            var curDocuSignTemplateId = Crate.GetElementByKey(controlsCrates, key: "Selected_DocuSign_Template",
                keyFieldName: "name")
                .Select(e => (string)e["value"])
                .FirstOrDefault(s => !string.IsNullOrEmpty(s));
            return curDocuSignTemplateId;
        }

        public object Activate(ActionDTO curDataPackage)
        {
            DocuSignAccount docuSignAccount = new DocuSignAccount();
            ConnectProfile connectProfile = docuSignAccount.GetDocuSignConnectProfiles();
            if (Int32.Parse(connectProfile.totalRecords) > 0)
            {
                return "Not Yet Implemented"; // Will be changed when implementation is plumbed in.
            }
            else
            {
                return "Fail";
            }
        }

        public object Deactivate(ActionDTO curDataPackage)
        {
            DocuSignAccount docuSignAccount = new DocuSignAccount();
            ConnectProfile connectProfile = docuSignAccount.GetDocuSignConnectProfiles();
            if (Int32.Parse(connectProfile.totalRecords) > 0)
            {
                return "Not Yet Implemented"; // Will be changed when implementation is plumbed in.
            }
            else
            {
                return "Fail";
            }
        }

        public async Task<PayloadDTO> Run(ActionDTO actionDto)
        {
            if (NeedsAuthentication(actionDto))
            {
                throw new ApplicationException("No AuthToken provided.");
            }

            var processPayload = await GetProcessPayload(actionDto.ProcessId);

            // Extract envelope id from the payload Crate
            string envelopeId = GetEnvelopeId(processPayload);

            // Make sure that it exists
            if (String.IsNullOrEmpty(envelopeId))
                throw new PluginCodedException(PluginErrorCode.PAYLOAD_DATA_MISSING, "EnvelopeId");

            //Create a field
            var fields = new List<FieldDTO>()
            {
                new FieldDTO()
                {
                    Key = "EnvelopeId",
                    Value = envelopeId
                }
            };

            var cratePayload = Crate.Create(
                "DocuSign Envelope Payload Data",
                JsonConvert.SerializeObject(fields),
                CrateManifests.STANDARD_PAYLOAD_MANIFEST_NAME,
                CrateManifests.STANDARD_PAYLOAD_MANIFEST_ID
                );

            processPayload.UpdateCrateStorageDTO(new List<CrateDTO>() { cratePayload });

            return processPayload;
        }

        private string GetEnvelopeId(PayloadDTO curPayloadDTO)
        {
            var eventReportCrate = curPayloadDTO.CrateStorageDTO().CrateDTO.SingleOrDefault();
            if (eventReportCrate == null)
            {
                return null;
            }

            var eventReportMS = JsonConvert.DeserializeObject<EventReportCM>(eventReportCrate.Contents);
            var crate = eventReportMS.EventPayload.SingleOrDefault();
            if (crate == null)
            {
                return null;
            }

            var fields = JsonConvert.DeserializeObject<List<FieldDTO>>(crate.Contents);
            if (fields == null || fields.Count == 0) return null;

            var envelopeIdField = fields.SingleOrDefault(f => f.Key == "EnvelopeId");
            if (envelopeIdField == null) return null;

            return envelopeIdField.Value;
        }

        protected override async Task<ActionDTO> InitialConfigurationResponse(ActionDTO curActionDTO)
        {
            var docuSignAuthDTO = JsonConvert
                .DeserializeObject<DocuSignAuthDTO>(curActionDTO.AuthToken.Token);

            if (curActionDTO.CrateStorage == null)
            {
                curActionDTO.CrateStorage = new CrateStorageDTO();
            }

            var crateControls = PackCrate_ConfigurationControls();
            var crateDesignTimeFields = _docuSignManager.PackCrate_DocuSignTemplateNames(docuSignAuthDTO);
            var eventFields = PackCrate_DocuSignEventFields();

            curActionDTO.CrateStorage.CrateDTO.Add(crateControls);
            curActionDTO.CrateStorage.CrateDTO.Add(crateDesignTimeFields);
            curActionDTO.CrateStorage.CrateDTO.Add(eventFields);

            var configurationFields = Crate.GetConfigurationControls(Mapper.Map<ActionDO>(curActionDTO));

            // Remove previously added crate of "Standard Event Subscriptions" schema

            Crate.ReplaceCratesByManifestType(curActionDTO.CrateStorage.CrateDTO,
                CrateManifests.STANDARD_EVENT_SUBSCRIPTIONS_NAME,
                new List<CrateDTO> { PackCrate_EventSubscriptions(configurationFields) });

            return await Task.FromResult<ActionDTO>(curActionDTO);
        }

        protected override Task<ActionDTO> FollowupConfigurationResponse(ActionDTO curActionDTO)
        {
            string curSelectedTemplateId = GetSelectedTemplateId(curActionDTO);

            if (!string.IsNullOrEmpty(curSelectedTemplateId))
            {
                //get the existing DocuSign event fields
                var curEventFieldsCrate = Crate.GetCratesByLabel("DocuSign Event Fields", curActionDTO.CrateStorage).Single();
                var curEventFields = Crate.GetStandardDesignTimeFields(curEventFieldsCrate);

                //set the selected template ID
                curEventFields.Fields.ForEach(field =>
                {
                    if (field.Key.Equals("TemplateId"))
                    {
                        field.Value = curSelectedTemplateId;
                    }
                });


                //update the DocuSign Event Fields with new value
                Crate.ReplaceCratesByLabel(curActionDTO.CrateStorage.CrateDTO, "DocuSign Event Fields",
                    new List<CrateDTO>
                    {
                        Crate.CreateDesignTimeFieldsCrate("DocuSign Event Fields", curEventFields.Fields.ToArray())
                    });

                UpdateSelectedEvents(curActionDTO);
            }

            return Task.FromResult(curActionDTO);
        }

        /// <summary>
        /// Updates event subscriptions list by user checked check boxes.
        /// </summary>
        /// <remarks>The configuration controls include check boxes used to get the selected DocuSign event subscriptions</remarks>
        private void UpdateSelectedEvents(ActionDTO curActionDTO)
        {
            //get the config controls manifest
            var curConfigControlsCrate = Crate.GetConfigurationControls(Mapper.Map<ActionDO>(curActionDTO));

            //get selected check boxes (i.e. user wanted to subscribe these DocuSign events to monitor for)
            var curSelectedDocuSignEvents =
                curConfigControlsCrate.Controls
                    .Where(configControl => configControl.Type.Equals(ControlTypes.CheckBox) && configControl.Selected)
                    .Select(checkBox => checkBox.Label.Replace(" ", ""));

            //create standard event subscription crate with user selected DocuSign events
            var curEventSubscriptionCrate = Crate.CreateStandardEventSubscriptionsCrate("Standard Event Subscriptions",
                curSelectedDocuSignEvents.ToArray());

            //replace the existing crate with new event subscription crate
            Crate.ReplaceCratesByManifestType(curActionDTO.CrateStorage.CrateDTO,
                CrateManifests.STANDARD_EVENT_SUBSCRIPTIONS_NAME, new List<CrateDTO> {curEventSubscriptionCrate});
        }

        private CrateDTO PackCrate_EventSubscriptions(
            StandardConfigurationControlsCM configurationFields)
        {
            var subscriptions = new List<string>();

            var eventCheckBoxes = configurationFields.Controls
                .Where(x => x.Type == "CheckBox" && x.Name.StartsWith("Event_"));

            foreach (var eventCheckBox in eventCheckBoxes)
            {
                if (eventCheckBox.Selected)
                {
                    subscriptions.Add(eventCheckBox.Label);
                }
            }

            return Crate.CreateStandardEventSubscriptionsCrate(
                "Standard Event Subscriptions",
                subscriptions.ToArray()
                );
        }

        private CrateDTO PackCrate_ConfigurationControls()
        {
            var fieldEnvelopeSent = new CheckBoxControlDefinitionDTO()
            {
                Label = "Envelope Sent",
                Name = "Event_Envelope_Sent",
                Events = new List<ControlEvent>()
                {
                    new ControlEvent("onChange", "requestConfig")
                }
            };

            var fieldEnvelopeReceived = new CheckBoxControlDefinitionDTO()
            {
                Label = "Envelope Received",
                Name = "Event_Envelope_Received",
                Events = new List<ControlEvent>()
                {
                    new ControlEvent("onChange", "requestConfig")
                }
            };

            var fieldRecipientSigned = new CheckBoxControlDefinitionDTO()
            {
                Label = "Recipient Signed",
                Name = "Event_Recipient_Signed",
                Events = new List<ControlEvent>()
                {
                    new ControlEvent("onChange", "requestConfig")
                }
            };

            var fieldEventRecipientSent = new CheckBoxControlDefinitionDTO()
            {
                Label = "Recipient Sent",
                Name = "Event_Recipient_Sent",
                Events = new List<ControlEvent>()
                {
                    new ControlEvent("onChange", "requestConfig")
                }
            };

            return PackControlsCrate(
                _docuSignManager.CreateDocuSignTemplatePicker(true),
                fieldEnvelopeSent,
                fieldEnvelopeReceived,
                fieldRecipientSigned,
                fieldEventRecipientSent);
        }

        private CrateDTO PackCrate_TemplateNames(DocuSignAuthDTO authDTO)
        {
            var template = new DocuSignTemplate();

            var templates = template.GetTemplates(authDTO.Email, authDTO.ApiPassword);
            var fields = templates.Select(x => new FieldDTO() { Key = x.Name, Value = x.Id }).ToArray();
            var createDesignTimeFields = Crate.CreateDesignTimeFieldsCrate(
                "Available Templates",
                fields);
            return createDesignTimeFields;
        }

        private CrateDTO PackCrate_DocuSignEventFields()
        {
            return Crate.CreateDesignTimeFieldsCrate("DocuSign Event Fields",
                new FieldDTO { Key = "EnvelopeId", Value = string.Empty },
                new FieldDTO { Key = "TemplateId", Value = string.Empty });
        }
    }
}