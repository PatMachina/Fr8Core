﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.Interfaces;
using Data.Entities;
using Data.Exceptions;
using Data.Infrastructure;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using System.Data.Entity;
using StructureMap;
using System.Data;
using Newtonsoft.Json;

namespace Core.Services
{
    public class ProcessTemplate : IProcessTemplate
    {
        private readonly IProcess _process;
        private readonly DockyardAccount _dockyardAccount;
        private readonly IAction _action;
        private readonly ICrate _crate;

        public object itemsToRemove { get; private set; }

        public ProcessTemplate()
        {
            _process = ObjectFactory.GetInstance<IProcess>();
            _dockyardAccount = ObjectFactory.GetInstance<DockyardAccount>();
            _action = ObjectFactory.GetInstance<IAction>();
            _crate = ObjectFactory.GetInstance<ICrate>();
        }

        public IList<ProcessTemplateDO> GetForUser(string userId, bool isAdmin = false, int? id = null)
        {
            if (userId == null)
                throw new ApplicationException("UserId must not be null");

            using (var unitOfWork = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var queryableRepo = unitOfWork.ProcessTemplateRepository.GetQuery()
                    .Include("SubscribedDocuSignTemplates")
                    .Include("SubscribedExternalEvents");

                if (isAdmin)
                {
                    return (id == null ? queryableRepo : queryableRepo.Where(pt => pt.Id == id)).ToList();
                }

                return (id == null
                    ? queryableRepo.Where(pt => pt.DockyardAccount.Id == userId)
                    : queryableRepo.Where(pt => pt.Id == id && pt.DockyardAccount.Id == userId)).ToList();
            }
        }

        public int CreateOrUpdate(IUnitOfWork uow, ProcessTemplateDO ptdo, bool updateChildEntities)
        {
            var creating = ptdo.Id == 0;
            if (creating)
            {
                ptdo.ProcessTemplateState = ProcessTemplateState.Inactive;
                uow.ProcessTemplateRepository.Add(ptdo);
            }
            else
            {
                var curProcessTemplate = uow.ProcessTemplateRepository.GetByKey(ptdo.Id);
                if (curProcessTemplate == null)
                    throw new EntityNotFoundException();
                curProcessTemplate.Name = ptdo.Name;
                curProcessTemplate.Description = ptdo.Description;

                //
                if (updateChildEntities)
                {
                    //Update DocuSign template registration
                    MakeCollectionEqual(uow, curProcessTemplate.SubscribedDocuSignTemplates,
                        ptdo.SubscribedDocuSignTemplates);

                    //Update DocuSign trigger registration
                    MakeCollectionEqual(uow, curProcessTemplate.SubscribedExternalEvents,
                        ptdo.SubscribedExternalEvents);
                }
            }
            uow.SaveChanges();
            return ptdo.Id;
        }

        /// <summary>
        /// The function add/removes items on the current collection 
        /// so that they match the items on the new collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collectionToUpdate"></param>
        /// <param name="sourceCollection"></param>
        public void MakeCollectionEqual<T>(IUnitOfWork uow, IList<T> collectionToUpdate, IList<T> sourceCollection) where T : class
        {
            List<T> itemsToAdd = new List<T>();
            List<T> itemsToRemove = new List<T>();
            bool found;

            foreach (T entity in collectionToUpdate)
            {
                found = false;
                foreach (T entityToCompare in sourceCollection)
                {
                    if (((IEquatable<T>)entity).Equals(entityToCompare))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    itemsToRemove.Add(entity);
                }
            }
            itemsToRemove.ForEach(e => uow.Db.Entry(e).State = EntityState.Deleted);
            
            foreach (T entity in sourceCollection)
            {
                found = false;
                foreach (T entityToCompare in collectionToUpdate)
                {
                    if (((IEquatable<T>)entity).Equals(entityToCompare))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    itemsToAdd.Add(entity);
                }
            }
            itemsToAdd.ForEach(i => collectionToUpdate.Add(i));


            //identify deleted items and remove them from the collection
            //collectionToUpdate.Except(sourceCollection).ToList().ForEach(s => collectionToUpdate.Remove(s));
            //identify added items and add them to the collection
            //sourceCollection.Except(collectionToUpdate).ToList().ForEach(s => collectionToUpdate.Add(s));
        }

        public void Delete(IUnitOfWork uow, int id)
        {
            var curProcessTemplate = uow.ProcessTemplateRepository.GetByKey(id);
            if (curProcessTemplate == null)
            {
                throw new EntityNotFoundException<ProcessTemplateDO>(id);
            }
            uow.ProcessTemplateRepository.Remove(curProcessTemplate);
        }

        public void LaunchProcess(IUnitOfWork uow, ProcessTemplateDO curProcessTemplate, CrateDTO curEventData)
        {
            if (curProcessTemplate == null)
                throw new EntityNotFoundException(curProcessTemplate);

            if (curProcessTemplate.ProcessTemplateState != ProcessTemplateState.Inactive)
            {
                _process.Launch(curProcessTemplate, curEventData);

                //todo: what does this do?
                //ProcessDO launchedProcess = uow.ProcessRepository.FindOne(
                //    process =>
                //        process.Name.Equals(curProcessTemplate.Name) && process.EnvelopeId.Equals(envelopeIdField.Value) &&
                //        process.ProcessState == ProcessState.Executing);
                //EventManager.ProcessLaunched(launchedProcess);
            }
        }

        public IList<ProcessNodeTemplateDO> GetProcessNodeTemplates(ProcessTemplateDO curProcessTemplateDO)
        {
            using (var unitOfWork = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var queryableRepo = unitOfWork.ProcessTemplateRepository.GetQuery()
                    .Include("ProcessNodeTemplates")
                    .Where(x => x.Id == curProcessTemplateDO.Id);

                return queryableRepo.SelectMany<ProcessTemplateDO, ProcessNodeTemplateDO>(x => x.ProcessNodeTemplates)
                    .ToList();
            }
        }

        public IList<ProcessTemplateDO> GetMatchingProcessTemplates(string userId, EventReportMS curEventReport)
        {
            List<ProcessTemplateDO> processTemplateSubscribers = new List<ProcessTemplateDO>();
            if (String.IsNullOrEmpty(userId))
                throw new ArgumentNullException("Parameter UserId is null");
            if (curEventReport == null)
                throw new ArgumentNullException("Parameter Standard Event Report is null");

            //1. Query all ProcessTemplateDO that are Active
            //2. are associated with the determined DockyardAccount
            //3. their first Activity has a Crate of  Class "Standard Event Subscriptions" which has inside of it an event name that matches the event name 
            //in the Crate of Class "Standard Event Reports" which was passed in.
            var subscribingProcessTemplates = _dockyardAccount.GetActiveProcessTemplates(userId).ToList();
                
            //3. Get ActivityDO
            foreach (var processTemplateDO in subscribingProcessTemplates)
            {
                //get the 1st activity
                var actionDO = GetFirstActivity(processTemplateDO) as ActionDO;

                //Get the CrateStorage
                if (actionDO != null && actionDO.CrateStorage != "")
                {
                    //Loop each CrateDTO in CrateStorage
                    IEnumerable<CrateDTO> eventSubscriptionCrates = _action.GetCratesByManifestType("Standard Event Subscriptions", actionDO.CrateStorageDTO());
                    foreach (var curEventSubscription in eventSubscriptionCrates)
                    {
                        //Parse CrateDTO to EventReportMS and compare Event name then add the ProcessTemplate to the results
                        EventSubscriptionMS subscriptionsList = _crate.GetContents<EventSubscriptionMS>(curEventSubscription);

                        if (subscriptionsList != null && subscriptionsList.Subscriptions
                            .Where(events => events.ToLower().Contains(curEventReport.EventNames.ToLower().Trim()))
                            .Any())//check event names if its subscribing
                            processTemplateSubscribers.Add(processTemplateDO);
                    }
                }
            }
            return processTemplateSubscribers;
        }

        public ActivityDO GetFirstActivity(ProcessTemplateDO curProcessTemplateDO)
        {
            ActivityDO activityDO = null;
            List<ActionListDO> actionLists = curProcessTemplateDO.ProcessNodeTemplates.SelectMany(s => s.ActionLists).ToList();
            if (actionLists.Count > 1)
            {
                throw new Exception("Multiple ActionList found in ProcessTemplateDO");
            }
            else if (actionLists.Count > 0)
            {
                ActionListDO actionListDO = actionLists[0];
                activityDO = actionListDO.Activities.OrderBy(o => o.Ordering).FirstOrDefault();
            }

            return activityDO;
        }
    }
}