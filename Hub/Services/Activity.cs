﻿using AutoMapper;
using Data.Constants;
using Data.Entities;
using Data.Infrastructure;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Newtonsoft.Json;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Control;
using Data.Crates;
using Data.Repositories.Plan;
using Data.Infrastructure.StructureMap;
using Data.States;
using Hub.Infrastructure;
using Hub.Interfaces;
using Hub.Managers;
using Hub.Managers.APIManagers.Transmitters.Restful;
using Hub.Managers.APIManagers.Transmitters.Terminal;
using Utilities.Interfaces;

namespace Hub.Services
{
    public class Activity : IActivity
    {
        private readonly ICrateManager _crate;
        private readonly IAuthorization _authorizationToken;
        private readonly ISecurityServices _security;
        private readonly IActivityTemplate _activityTemplate;
        private readonly IRouteNode _routeNode;
        private readonly AsyncMultiLock _configureLock = new AsyncMultiLock();

        public Activity(ICrateManager crate, IAuthorization authorizationToken, ISecurityServices security, IActivityTemplate activityTemplate, IRouteNode routeNode)
        {
            _crate = crate;
            _authorizationToken = authorizationToken;
            _security = security;
            _activityTemplate = activityTemplate;
            _routeNode = routeNode;
        }

        public IEnumerable<TViewModel> GetAllActivities<TViewModel>()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                return uow.PlanRepository.GetActivityQueryUncached().Select(Mapper.Map<TViewModel>);
            }
        }

        public ActivityDO SaveOrUpdateActivity(IUnitOfWork uow, ActivityDO submittedActivityData)
        {
            SaveAndUpdateActivity(uow, submittedActivityData, new List<ActivityDO>());
            uow.SaveChanges();
          
            var result = uow.PlanRepository.GetById<ActivityDO>(submittedActivityData.Id);
            return result;
        }

        private void UpdateActivityProperties(IUnitOfWork uow, ActivityDO submittedActivity)
        {
            var existingAction = uow.PlanRepository.GetById<ActivityDO>(submittedActivity.Id);

            if (existingAction == null)
            {
                throw new Exception("Action was not found");
            }

            UpdateActivityProperties(existingAction, submittedActivity);
            uow.SaveChanges();
        }

        private static void UpdateActivityProperties(ActivityDO existingActivity, ActivityDO submittedActivity)
        {
            // it is unlikely that we have scenarios when activity template can be changed after activity was created
            //existingActivity.ActivityTemplateId = submittedActivity.ActivityTemplateId;

            existingActivity.Label = submittedActivity.Label;
            existingActivity.CrateStorage = submittedActivity.CrateStorage;
            existingActivity.Ordering = submittedActivity.Ordering;
        }

        private static void RestoreSystemProperties(ActivityDO existingActivity, ActivityDO submittedActivity)
        {
            submittedActivity.AuthorizationTokenId = existingActivity.AuthorizationTokenId;
        }

        private void SaveAndUpdateActivity(IUnitOfWork uow, ActivityDO submittedActiviy, List<ActivityDO> pendingConfiguration)
        {
            RouteTreeHelper.Visit(submittedActiviy, x =>
            {
                var activity = (ActivityDO)x;

                if (activity.Id == Guid.Empty)
                {
                    activity.Id = Guid.NewGuid();
                }
            });

            RouteNodeDO route;
            RouteNodeDO originalAction;
            if (submittedActiviy.ParentRouteNodeId != null)
            {
                route = uow.PlanRepository.Reload<RouteNodeDO>(submittedActiviy.ParentRouteNodeId);
                originalAction = route.ChildNodes.FirstOrDefault(x => x.Id == submittedActiviy.Id);
            }
            else
            {
                originalAction = uow.PlanRepository.Reload<RouteNodeDO>(submittedActiviy.Id);
                route = originalAction.ParentRouteNode;
            }


            if (originalAction != null)
            {
                route.ChildNodes.Remove(originalAction);

                var originalActions = RouteTreeHelper.Linearize(originalAction)
                    .ToDictionary(x => x.Id, x => (ActivityDO)x);

                foreach (var submitted in RouteTreeHelper.Linearize(submittedActiviy))
                {
                    ActivityDO existingActivity;

                    if (!originalActions.TryGetValue(submitted.Id, out existingActivity))
                    {
                        pendingConfiguration.Add((ActivityDO)submitted);
                    }
                    else
                    {
                        RestoreSystemProperties(existingActivity, (ActivityDO)submitted);
                    }
                }
            }
            else
            {
                pendingConfiguration.AddRange(RouteTreeHelper.Linearize(submittedActiviy).OfType<ActivityDO>());
            }

            if (submittedActiviy.Ordering <= 0)
            {
                route.AddChildWithDefaultOrdering(submittedActiviy);
            }
            else
            {
                route.ChildNodes.Add(submittedActiviy);
            }
        }

        public ActivityDO GetById(IUnitOfWork uow, Guid id)
        {
            return uow.PlanRepository.GetById<ActivityDO>(id);
        }

        public async Task<RouteNodeDO> CreateAndConfigure(IUnitOfWork uow, string userId, int actionTemplateId, string label = null, int? order = null, Guid? parentNodeId = null, bool createRoute = false, Guid? authorizationTokenId = null)
        {
            if (parentNodeId != null && createRoute)
            {
                throw new ArgumentException("Parent node id can't be set together with create route flag");
            }

            if (parentNodeId == null && !createRoute)
            {
                throw new ArgumentException("Either Parent node id or create route flag must be set");
            }

            // to avoid null pointer exception while creating parent node if label is null 
            if (label == null)
            {
                label = userId + "_" + actionTemplateId.ToString();
            }

            RouteNodeDO parentNode;
            PlanDO plan = null;

            if (createRoute)
            {
                plan = ObjectFactory.GetInstance<IPlan>().Create(uow, label);

                plan.ChildNodes.Add(parentNode = new SubrouteDO
                {
                    StartingSubroute = true,
                    Name = label + " #1"
                });
            }
            else
            {
                parentNode = uow.PlanRepository.GetById<RouteNodeDO>(parentNodeId);

                if (parentNode is PlanDO)
                {
                    if (((PlanDO)parentNode).StartingSubroute == null)
                    {
                        parentNode.ChildNodes.Add(parentNode = new SubrouteDO
                        {
                            StartingSubroute = true,
                            Name = label + " #1"
                        });
                    }
                    else
                    {
                        parentNode = ((PlanDO)parentNode).StartingSubroute;
                    }

                }
            }

            var activity = new ActivityDO
            {
                Id = Guid.NewGuid(),
                ActivityTemplateId = actionTemplateId,
                Label = label,
                CrateStorage = _crate.EmptyStorageAsStr(),
                AuthorizationTokenId = authorizationTokenId
            };

            parentNode.AddChild(activity, order);

            uow.SaveChanges();

            await ConfigureSingleActivity(uow, userId, activity);

            if (createRoute)
            {
                return plan;
            }

            return activity;
        }

        private async Task<ActivityDO> CallActivityConfigure(IUnitOfWork uow, string userId, ActivityDO curActivityDO)
        {
            var plan = curActivityDO.RootRouteNode as PlanDO;

            if (plan?.RouteState == RouteState.Deleted)
            {
                var message = "Cannot configure activity when plan is deleted";
                


                EventManager.TerminalConfigureFailed(
                   _activityTemplate.GetTerminalUrl(curActivityDO.ActivityTemplateId),
                    JsonConvert.SerializeObject(Mapper.Map<ActivityDTO>(curActivityDO)),
                    message,
                    curActivityDO.Id.ToString()
                    );

                throw new ApplicationException(message);
            }

            var tempActionDTO = Mapper.Map<ActivityDTO>(curActivityDO);

            if (!_authorizationToken.ValidateAuthenticationNeeded(uow, userId, tempActionDTO))
            {
                curActivityDO = Mapper.Map<ActivityDO>(tempActionDTO);

                try
                {
                    tempActionDTO = await CallTerminalActivityAsync<ActivityDTO>(uow, "configure", curActivityDO, Guid.Empty);
                }
                catch (ArgumentException e)
                {
                    EventManager.TerminalConfigureFailed("<no terminal url>", JsonConvert.SerializeObject(Mapper.Map<ActivityDTO>(curActivityDO)), e.Message, curActivityDO.Id.ToString());
                    throw;
                }
                catch (RestfulServiceException e)
                {
                    // terminal requested token invalidation
                    if (e.StatusCode == 419)
                    {
                        _authorizationToken.InvalidateToken(uow, userId, tempActionDTO);
                    }
                    else
                    {
                        JsonSerializerSettings settings = new JsonSerializerSettings
                        {
                            PreserveReferencesHandling = PreserveReferencesHandling.Objects
                        };
                        var endpoint = _activityTemplate.GetTerminalUrl(curActivityDO.ActivityTemplateId) ?? "<no terminal url>";
                        EventManager.TerminalConfigureFailed(endpoint, JsonConvert.SerializeObject(Mapper.Map<ActivityDTO>(curActivityDO), settings), e.Message, curActivityDO.Id.ToString());
                        throw;
                    }
                }
                catch (Exception e)
                {
                    JsonSerializerSettings settings = new JsonSerializerSettings
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    };

                    var endpoint = _activityTemplate.GetTerminalUrl(curActivityDO.ActivityTemplateId) ?? "<no terminal url>";
                    EventManager.TerminalConfigureFailed(endpoint, JsonConvert.SerializeObject(Mapper.Map<ActivityDTO>(curActivityDO), settings), e.Message, curActivityDO.Id.ToString());
                    throw;
                }
            }

            return Mapper.Map<ActivityDO>(tempActionDTO);
        }

        private async Task<ActivityDO> ConfigureSingleActivity(IUnitOfWork uow, string userId, ActivityDO curActivityDO)
        {
            curActivityDO = await CallActivityConfigure(uow, userId, curActivityDO);

            UpdateActivityProperties(uow, curActivityDO);

            return curActivityDO;
        }

        public async Task<ActivityDTO> Configure(IUnitOfWork uow,
            string userId, ActivityDO curActivityDO, bool saveResult = true)
        {
            if (curActivityDO == null)
            {
                throw new ArgumentNullException(nameof(curActivityDO));
            }

            using (await _configureLock.Lock(curActivityDO.Id))
            {
                curActivityDO = await CallActivityConfigure(uow, userId, curActivityDO);

                if (saveResult)
                {
                    //save the received action as quickly as possible
                    curActivityDO = SaveOrUpdateActivity(uow, curActivityDO);
                    return Mapper.Map<ActivityDTO>(curActivityDO);
                }
            }

            return Mapper.Map<ActivityDTO>(curActivityDO);
        }

        public ActivityDO MapFromDTO(ActivityDTO curActivityDTO)
        {
            ActivityDO submittedActivity = Mapper.Map<ActivityDO>(curActivityDTO);
            return submittedActivity;
        }

        public void Delete(Guid id)
        {
            //we are using Kludge solution for now
            //https://maginot.atlassian.net/wiki/display/SH/Action+Deletion+and+Reordering

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {

                var curAction = uow.PlanRepository.GetById<ActivityDO>(id);
                if (curAction == null)
                {
                    throw new InvalidOperationException("Unknown RouteNode with id: " + id);
                }

                var downStreamActivities = _routeNode.GetDownstreamActivities(uow, curAction).OfType<ActivityDO>();
                //we should clear values of configuration controls
                var directChildren = curAction.GetDescendants().OfType<ActivityDO>();

                //there is no sense of updating children of action being deleted. 
                foreach (var downStreamActivity in downStreamActivities.Except(directChildren))
                {
                    var currentActivity = downStreamActivity;

                    using (var crateStorage = _crate.UpdateStorage(() => currentActivity.CrateStorage))
                    {
                        bool hasChanges = false;
                        foreach (var configurationControls in crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>())
                        {
                            foreach (IResettable resettable in configurationControls.Controls)
                            {
                                resettable.Reset();
                                hasChanges = true;
                            }
                        }

                        if (!hasChanges)
                        {
                            crateStorage.DiscardChanges();
                        }
                    }
                }

                curAction.RemoveFromParent();

                uow.SaveChanges();
            }
        }

        public async Task PrepareToExecute(ActivityDO curActivity, ActivityState curActionState, ContainerDO curContainerDO, IUnitOfWork uow)
        {
            EventManager.ActionStarted(curActivity);

            var payload = await Run(uow, curActivity, curActionState, curContainerDO);

            if (payload != null)
            {
                using (var crateStorage = _crate.UpdateStorage(() => curContainerDO.CrateStorage))
                {
                    crateStorage.Replace(_crate.FromDto(payload.CrateStorage));
                }
            }

            uow.SaveChanges();
        }

        // Maxim Kostyrkin: this should be refactored once the TO-DO snippet below is redesigned
        public async Task<PayloadDTO> Run(IUnitOfWork uow, ActivityDO curActivityDO, ActivityState curActionState, ContainerDO curContainerDO)
        {
            if (curActivityDO == null)
            {
                throw new ArgumentNullException("curActivityDO");
            }

            try
            {
                var actionName = curActionState == ActivityState.InitialRun ? "Run" : "ExecuteChildActivities";
                EventManager.ActivityRunRequested(curActivityDO, curContainerDO);

                var payloadDTO = await CallTerminalActivityAsync<PayloadDTO>(uow, actionName, curActivityDO, curContainerDO.Id);

                EventManager.ActivityResponseReceived(curActivityDO, ActivityResponse.RequestSuspend);

                return payloadDTO;

            }
            catch (ArgumentException e)
            {
                EventManager.TerminalRunFailed("<no terminal url>", JsonConvert.SerializeObject(Mapper.Map<ActivityDTO>(curActivityDO)), e.Message, curActivityDO.Id.ToString());
                throw;
            }
            catch (Exception e)
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                };

                var endpoint = _activityTemplate.GetTerminalUrl(curActivityDO.ActivityTemplateId) ?? "<no terminal url>";
                EventManager.TerminalRunFailed(endpoint, JsonConvert.SerializeObject(Mapper.Map<ActivityDTO>(curActivityDO), settings), e.Message, curActivityDO.Id.ToString());
                throw;
            }
        }
       
        public async Task<ActivityDTO> Activate(ActivityDO curActivityDO)
        {
            try
            {
                //if this action contains nested actions, do not pass them to avoid 
                // circular reference error during JSON serialization (FR-1769)
                //curActivityDO = Mapper.Map<ActivityDO>(curActivityDO); // this doesn't clone activity

                curActivityDO = (ActivityDO)curActivityDO.Clone();

                using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    var result = await CallTerminalActivityAsync<ActivityDTO>(uow, "activate", curActivityDO, Guid.Empty);

                    EventManager.ActionActivated(curActivityDO);
                    return result;
                }
            }
            catch (ArgumentException)
            {
                EventManager.TerminalActionActivationFailed("<no terminal url>", JsonConvert.SerializeObject(Mapper.Map<ActivityDTO>(curActivityDO)), curActivityDO.Id.ToString());
                throw;
            }
            catch
            {
                EventManager.TerminalActionActivationFailed(_activityTemplate.GetTerminalUrl(curActivityDO.ActivityTemplateId) ?? "<no terminal url>", JsonConvert.SerializeObject(Mapper.Map<ActivityDTO>(curActivityDO)), curActivityDO.Id.ToString());
                throw;
            }
        }

        public async Task<ActivityDTO> Deactivate(ActivityDO curActivityDO)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                return await CallTerminalActivityAsync<ActivityDTO>(uow, "deactivate", curActivityDO, Guid.Empty);
            }
        }
       
        private Task<TResult> CallTerminalActivityAsync<TResult>(IUnitOfWork uow, string activityName, ActivityDO curActivityDO, Guid containerId, string curDocumentationSupport = null)
        {
            if (activityName == null) throw new ArgumentNullException("activityName");
            if (curActivityDO == null) throw new ArgumentNullException("curActivityDO");

            var dto = Mapper.Map<ActivityDO, ActivityDTO>(curActivityDO);

            var fr8DataDTO = new Fr8DataDTO
            {
                ContainerId = containerId,
                ActivityDTO = dto
            };

            if (curDocumentationSupport != null)
            {
                dto.Documentation = curDocumentationSupport;
            }

            _authorizationToken.PrepareAuthToken(uow, dto);

            EventManager.ActionDispatched(curActivityDO, containerId);

            if (containerId != Guid.Empty)
            {
                var containerDO = uow.ContainerRepository.GetByKey(containerId);
                EventManager.ContainerSent(containerDO, curActivityDO);
                var reponse = ObjectFactory.GetInstance<ITerminalTransmitter>()
                    .CallActivityAsync<TResult>(activityName, fr8DataDTO, containerId.ToString());
                EventManager.ContainerReceived(containerDO, curActivityDO);
                return reponse;
            }
            return ObjectFactory.GetInstance<ITerminalTransmitter>().CallActivityAsync<TResult>(activityName, fr8DataDTO, containerId.ToString());
        }
        //This method finds and returns single SolutionPageDTO that holds some documentation of Activities that is obtained from a solution by aame
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="activityDTO"></param>
        /// <param name="isSolution">This parameter controls the access level: if it is a solution case
        /// we allow calls without CurrentAccount; if it is not - we need a User to get the list of available activities</param>
        /// <returns>Task<SolutionPageDTO/> or Task<ActivityResponceDTO/></returns>
        public async Task<T> GetActivityDocumentation<T>(ActivityDTO activityDTO, bool isSolution = false)
        {
            //activityResponce can be either of type SolutoinPageDTO or ActivityRepsonceDTO
            T activityResponce;
            var userId = Guid.NewGuid().ToString();
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var allActivityTemplates = ObjectFactory.GetInstance<IEnumerable<ActivityTemplateDTO>>();
                if (isSolution)
                    //Get the list of all actions that are solutions from database
                    allActivityTemplates = _routeNode.GetSolutions(uow);
                else
                {
                    var curUser = _security.GetCurrentAccount(uow);
                    userId = curUser.Id;
                    allActivityTemplates = _routeNode.GetAvailableActivities(uow, curUser);
                }
                //find the activity by the provided name
                var curActivityTerminalDTO = allActivityTemplates.Single(a => a.Name == activityDTO.ActivityTemplate.Name);
                //prepare an Activity object to be sent to Activity in a Terminal
                //IMPORTANT: this object will not be hold in the database
                //It is used to transfer data
                //as ActivityDTO is the first mean of communication between The Hub and Terminals
                var curActivityDTO = new ActivityDTO
                {
                    Id = Guid.NewGuid(),
                    Label = curActivityTerminalDTO.Label,
                    ActivityTemplate = curActivityTerminalDTO,
                    AuthToken = new AuthorizationTokenDTO
                    {
                        UserId = null
                    },
                    Documentation = activityDTO.Documentation
                };
                activityResponce = await GetDocumentation<T>(curActivityDTO);
                //Add log to the database
                if (!isSolution) {
                    var curActivityDo = Mapper.Map<ActivityDO>(activityDTO);
                    EventManager.ActivityResponseReceived(curActivityDo, ActivityResponse.ShowDocumentation);
                }
                    
            }
            return activityResponce;
        }

        private async Task<T> GetDocumentation<T>(ActivityDTO curActivityDTO)
        {
            //Put a method name so that HandleFr8Request could find correct method in the terminal Action
            var actionName = "documentation";
            curActivityDTO.Documentation = curActivityDTO.Documentation;
            var curContainerId = Guid.Empty;
            var fr8Data = new Fr8DataDTO
            {
                ActivityDTO = curActivityDTO
            };
            //Call the terminal
            return await ObjectFactory.GetInstance<ITerminalTransmitter>().CallActivityAsync<T>(actionName, fr8Data, curContainerId.ToString());
        }

        public List<string> GetSolutionList(string terminalName)
        {
            var solutionNameList = new List<string>();
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curActivities = uow.ActivityTemplateRepository.GetAll()
                    .Where(a => a.Terminal.Name == terminalName
                        && a.Category == ActivityCategory.Solution)
                        .ToList();
                solutionNameList.AddRange(curActivities.Select(activity => activity.Name));
            }
            return solutionNameList;
        }
    }
}