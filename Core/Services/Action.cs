﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Core.Interfaces;
using Core.Managers.APIManagers.Transmitters.Plugin;
using Core.Managers.APIManagers.Transmitters.Restful;
using Data.Entities;
using Data.Infrastructure;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Data.Wrappers;
using StructureMap;
using System.Data.Entity;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;

namespace Core.Services
{
    public class Action : IAction
    {
        private IEnvelope _envelope;
        private IAction _action;
        private IDocuSignTemplate _docusignTemplate; //TODO: switch to wrappers
        private Task curAction;
        private IPlugin _plugin;
        private readonly AuthorizationToken _authorizationToken;

        public Action()
        {
            _authorizationToken = new AuthorizationToken();
            _plugin = ObjectFactory.GetInstance<IPlugin>();

        }

        public IEnumerable<TViewModel> GetAllActions<TViewModel>()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                return uow.ActionRepository.GetAll().Select(Mapper.Map<TViewModel>);
            }
        }

        public ActionDO SaveOrUpdateAction(ActionDO submittedActionData)
        {
            ActionDO existingActionDO = null;
            ActionDO curAction;
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                if (submittedActionData.ActivityTemplateId == 0)
                {
                    submittedActionData.ActivityTemplateId = null;
                }

                if (submittedActionData.Id > 0)
                {
                    existingActionDO = uow.ActionRepository.GetByKey(submittedActionData.Id);
                }

                if (submittedActionData.IsTempId)
                {
                    submittedActionData.Id = 0;
                }

                if (existingActionDO != null)
                {
                    existingActionDO.ParentActivity = submittedActionData.ParentActivity;
                    existingActionDO.ParentActivityId = submittedActionData.ParentActivityId;
                    existingActionDO.ActivityTemplateId = submittedActionData.ActivityTemplateId;
                    existingActionDO.Name = submittedActionData.Name;
                    existingActionDO.CrateStorage = submittedActionData.CrateStorage;
                    curAction = existingActionDO;
                }
                else
                {
                    uow.ActionRepository.Add(submittedActionData);
                    curAction = submittedActionData;
                }

                uow.SaveChanges();
                curAction.IsTempId = false;
                return curAction;
            }
        }
        public List<CrateDTO> GetCrates(ActionDO curActionDO)
        {
            return curActionDO.CrateStorageDTO().CrateDTO;
        }

        public ActionDO GetById(int id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                return uow.ActionRepository.GetQuery().Include(i => i.ActivityTemplate).Where(i => i.Id == id).Select(s => s).FirstOrDefault();
            }
        }

        public ActionDTO Configure(ActionDO curActionDO)
        {
            ActivityTemplateDO curActivityTemplate;

            if (curActionDO != null && curActionDO.ActivityTemplateId != 0)
            {
                //fetch this Action's ActivityTemplate
                using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    curActivityTemplate = uow.ActivityTemplateRepository.GetByKey(curActionDO.ActivityTemplateId);

                    if (curActivityTemplate != null)
                    {
                        //convert the Action to a DTO in preparation for serialization and POST to the plugin
                        var curActionDTO = Mapper.Map<ActionDTO>(curActionDO);

                        //convert the ActivityTemplate to a DTO as well
                        ActivityTemplateDTO curActivityTemplateDTO = Mapper.Map<ActivityTemplateDTO>(curActivityTemplate);
                        curActionDTO.ActivityTemplate = curActivityTemplateDTO;

                        // prepare the current plugin URL
                        // TODO: Add logic to use https:// for production

                        string curPluginUrl = "http://" + curActivityTemplate.Plugin.Endpoint + "/actions/configure/";

                        var restClient = new RestfulServiceClient();
                        string actionDTOJSON;
                        try
                        {
                            actionDTOJSON =
                                restClient.PostAsync(new Uri(curPluginUrl, UriKind.Absolute), curActionDTO).Result;
                        }
                        catch (Exception)
                        {
                            EventManager.PluginConfigureFailed(curPluginUrl, JsonConvert.SerializeObject(curActionDTO));
                            throw;
                        }

                        //Converting Received ActionDTO in JSON Format to ActionDTO Object
                        ActionDTO tempActionDTO = JsonConvert.DeserializeObject<ActionDTO>(actionDTOJSON);

                        //Plugin Configure Action Return ActionDTO
                        curActionDO = Mapper.Map<ActionDO>(tempActionDTO);

                        //save the received action as quickly as possible
                        SaveOrUpdateAction(curActionDO);

                        //Returning ActionDTO
                        return tempActionDTO;
                    }

                    else
                    {
                        throw new ArgumentNullException("ActivityTemplateDO");
                    }
                }
            }
            else
            {
                throw new ArgumentNullException("ActivityTemplateDO");
            }
        }

        public ActionDO MapFromDTO(ActionDTO curActionDTO)
        {
            ActionDO submittedAction = AutoMapper.Mapper.Map<ActionDO>(curActionDTO);
            return SaveOrUpdateAction(submittedAction);
        }

        public void Delete(int id)
        {

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curAction = UpdateCurrentActivity(id, uow);
                if (curAction == null)
                {
                    curAction = new ActivityDO { Id = id };
                    uow.ActivityRepository.Attach(curAction);
                }
                uow.ActivityRepository.Remove(curAction);
                uow.SaveChanges();
            }
        }

        /// <summary>
        /// The method checks if the action being deleted is CurrentActivity for its ActionList. 
        /// if it is, sets CurrentActivity to the next Action, or null if it is the last action. 
        /// </summary>
        /// <param name="curActionId">Action Id</param>
        /// <param name="uow">Unit of Work</param>
        /// <returns>Returns the current action (if found) or null if not.</returns>
        public ActivityDO UpdateCurrentActivity(int curActionId, IUnitOfWork uow)
        {
            // Find an ActionList for which the action is set as CurrentActivity
            // Also, get the whole list of actions for this Action List 
            var curActionList = uow.ActionListRepository.GetQuery().Where(al => al.CurrentActivityID == curActionId).Include(al => al.Activities).SingleOrDefault();
            if (curActionList == null) return null;

            // Get current Action
            var curAction = curActionList.Activities.Where(a => a.Id == curActionId).SingleOrDefault();
            if (curAction == null) return null; // Well, who knows...

            // Get ordered list of next Activities 
            var activities = curActionList.Activities.Where(a => a.Ordering > curAction.Ordering).OrderBy(a => a.Ordering);

            //if no next activities, just nullify CurrentActivity 
            if (activities.Count() == 0)
            {
                curActionList.CurrentActivity = null;
            }
            else
            {
                // if there is one, set it as CurrentActivity
                curActionList.CurrentActivity = activities.ToList()[0];
            }

            return curAction;
        }

        public async Task<int> Process(ActionDO curAction, ProcessDO curProcessDO)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                //if status is unstarted, change it to in-process. If status is completed or error, throw an exception.
                if (curAction.ActionState == ActionState.Unstarted || curAction.ActionState == ActionState.InProcess)
                {
                    curAction.ActionState = ActionState.InProcess;
                    try
                    {
                        uow.ActionRepository.Attach(curAction);
                        uow.SaveChanges();
                    }
                    catch(Exception e)
                    {
                        var foo = 3;
                    }
                 

                    EventManager.ActionStarted(curAction);

                    var jsonResult = await Execute(curAction, curProcessDO);

                    //this JSON error check is broken because it triggers on standard success messages, which look like this:
                    //"{\"success\": {\"ErrorCode\": \"0\", \"StatusCode\": \"200\", \"Description\": \"\"}}"


                    //check if the returned JSON is Error
                    //  if (jsonResult.ToLower().Contains("error"))
                    // {
                    //     curAction.ActionState = ActionState.Error;
                    //  }
                    //   else
                    //   {
                    curAction.ActionState = ActionState.Active;
                    //   }

                    uow.ActionRepository.Attach(curAction);
                    uow.SaveChanges();
                }
                else
                {
                    curAction.ActionState = ActionState.Error;
                    uow.ActionRepository.Attach(curAction);
                    uow.SaveChanges();
                    throw new Exception(string.Format("Action ID: {0} status is {1}.", curAction.Id, curAction.ActionState));
                }
            }
            return curAction.ActionState.Value;
        }

        public async Task<string> Execute(ActionDO curActionDO, ProcessDO curProcessDO)
        {
            if (curActionDO == null)
                throw new ArgumentNullException("curActionDO");

            var curActionDTO = Mapper.Map<ActionDTO>(curActionDO);
            var curPayloadDTO = new PayloadDTO(curProcessDO.CrateStorage, curProcessDO.Id);

            //TODO: The plugin transmitter Post Async to get Payload DTO is depriciated. This logic has to be discussed and changed.
            var curPluginClient = ObjectFactory.GetInstance<IPluginTransmitter>();

            //TODO : Cut base Url from PluginDO.Endpoint

            curPluginClient.BaseUri = new Uri(curActionDO.ActivityTemplate.Plugin.Endpoint);

            var jsonResult = await curPluginClient.PostActionAsync(curActionDO.Name, curActionDTO, curPayloadDTO);
            EventManager.ActionDispatched(curActionDTO);

            return jsonResult;
        }



        /// <summary>
        /// Retrieve authorization token
        /// </summary>
        /// <param name="curActionDO"></param>
        /// <returns></returns>
        public string Authenticate(ActionDO curActionDO)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                DockyardAccountDO curDockyardAccountDO = GetAccount(curActionDO);
                var curPlugin = curActionDO.ActivityTemplate.Plugin;
                string curToken = string.Empty;

                if (curDockyardAccountDO != null)
                {
                    curToken = _authorizationToken.GetToken(curDockyardAccountDO.Id, curPlugin.Id);

                    if (!string.IsNullOrEmpty(curToken))
                        return curToken;
                }

                curToken = _authorizationToken.GetPluginToken(curPlugin.Id);
                if (!string.IsNullOrEmpty(curToken))
                    return curToken;
                return _plugin.Authorize();
            }
        }

        /// <summary>
        /// Retrieve account
        /// </summary>
        /// <param name="curActionDO"></param>
        /// <returns></returns>
        public DockyardAccountDO GetAccount(ActionDO curActionDO)
        {
            if (curActionDO.ParentActivity != null
                && curActionDO.ActivityTemplate.AuthenticationType == "OAuth")
            {
                ActionListDO curActionListDO = (ActionListDO)curActionDO.ParentActivity;

                return curActionListDO
                    .Process
                    .ProcessTemplate
                    .DockyardAccount;
            }

            return null;

        }

        public void AddCrate(ActionDO curActionDO, List<CrateDTO> curCrateDTOLists)
        {
            if (curCrateDTOLists == null)
                throw new ArgumentNullException("CrateDTO is null");
            if (curActionDO == null)
                throw new ArgumentNullException("ActionDO is null");

            if (curCrateDTOLists.Count > 0)
            {
                curActionDO.UpdateCrateStorageDTO(curCrateDTOLists);
            }
        }

        public IEnumerable<CrateDTO> GetCratesByManifestType(string curManifestType, CrateStorageDTO curCrateStorageDTO)
        {
            if (String.IsNullOrEmpty(curManifestType))
                throw new ArgumentNullException("Parameter Manifest Type is empty");
            if (curCrateStorageDTO == null)
                throw new ArgumentNullException("Parameter CrateStorageDTO is null.");

            IEnumerable<CrateDTO> crateDTO = null;

            crateDTO = curCrateStorageDTO.CrateDTO.Where(crate => crate.ManifestType == curManifestType);

            return crateDTO;
        }


        public string Activate(ActionDO curActionDO)
        {
            return CallPluginAction(curActionDO, "activate");
        }

        public string Deactivate(ActionDO curActionDO)
        {
            return CallPluginAction(curActionDO, "deactivate");
        }

        private string CallPluginAction(ActionDO curActionDO, string actionName)
        {
            if (curActionDO != null && curActionDO.ActivityTemplateId != 0)
            {
                using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    ActivityTemplateDO curActivityTemplate = curActionDO.ActivityTemplate;
                    if (curActivityTemplate != null)
                    {
                        //convert the Action to a DTO in preparation for serialization and POST to the plugin
                        var curActionDTO = Mapper.Map<ActionDTO>(curActionDO);
                        string curPluginUrl = string.Format("http://{0}/actions/{1}/", curActivityTemplate.Plugin.Endpoint, actionName);
                        var restClient = new RestfulServiceClient();
                        string result;
                        try
                        {
                            result = restClient.PostAsync(new Uri(curPluginUrl, UriKind.Absolute), curActionDTO).Result;
                            EventManager.ActionActivated(curActionDO);
                        }
                        catch (Exception)
                        {
                            EventManager.PluginActionActivationFailed(curPluginUrl, JsonConvert.SerializeObject(curActionDO));
                            throw;
                        }
                        return result;
                    }
                    else
                    {
                        throw new ArgumentNullException("ActivityTemplateDO");
                    }
                }
            }
            else
            {
                throw new ArgumentNullException("curActionDO");
            }

        }
    }
}