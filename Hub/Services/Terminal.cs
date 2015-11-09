﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using StructureMap;
using Data.Entities;
using Data.Interfaces;
using Hub.Interfaces;
using Hub.Managers.APIManagers.Transmitters.Restful;
using Data.Interfaces.Manifests;
using Hub.Managers;

namespace Hub.Services
{
    /// <summary>
    /// File service
    /// </summary>
    public class Terminal : ITerminal
    {
        public IEnumerable<TerminalDO> GetAll()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                return uow.TerminalRepository.GetAll();
            }
        }

        public async Task<IList<string>> RegisterTerminals(string uri)
        {
            var eventReporter = ObjectFactory.GetInstance<EventReporter>();

            var activityTemplateList = await GetAvailableActions(uri);

            List<string> activityTemplateNames = new List<string>(); 
            foreach (var activityTemplate in activityTemplateList)
            {
                try
                {
                    new ActivityTemplate().Register(activityTemplate);
                    activityTemplateNames.Add(activityTemplate.Name);
                }
                catch (Exception ex)
                {
                    eventReporter = ObjectFactory.GetInstance<EventReporter>();
                    eventReporter.ActivityTemplateTerminalRegistrationError(
                        string.Format("Failed to register {0} terminal. Error Message: {1}", activityTemplate.Terminal.Name, ex.Message),
                        ex.GetType().Name);
                }
            }

            return activityTemplateNames;
        }
        

        /// <summary>
        /// Parses the required terminal service URL for the given action by Terminal Name and its version
        /// </summary>
        /// <param name="curTerminalName">Name of the required terminal</param>
        /// <param name="curTerminalVersion">Version of the required terminal</param>
        /// <param name="curActionName">Required action</param>
        /// <returns>Parsed URl to the terminal for its action</returns>
        public string ParseTerminalUrlFor(string curTerminalName, string curTerminalVersion, string curActionName)
        {
            using (IUnitOfWork uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                //get the terminal by name and version
                ITerminalDO curTerminal =
                    uow.TerminalRepository.FindOne(
                        terminal => terminal.Name.Equals(curTerminalName) && terminal.Version.Equals(curTerminalVersion));


                string curTerminalUrl = string.Empty;

                //if there is a valid terminal, prepare the URL with its endpoint and add the given action name
                if (curTerminal != null)
                {
                    curTerminalUrl += @"http://" + curTerminal.Endpoint + "/" + curActionName;
                }

                //return the pugin URL
                return curTerminalUrl;
            }
        }

        private async Task<IList<ActivityTemplateDO>> GetAvailableActions(string uri)
        {
            var restClient = new RestfulServiceClient();
            var standardFr8TerminalCM = await restClient.GetAsync<StandardFr8TerminalCM>(new Uri(uri, UriKind.Absolute));
            return standardFr8TerminalCM.Actions;
        }
    }
}
