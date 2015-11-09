﻿using System;
using System.Web.Http;
using Data.Interfaces.DataTransferObjects;
using AutoMapper;
using Data.Entities;
using Newtonsoft.Json;
using System.Reflection;
using TerminalBase.BaseClasses;
using System.Collections.Generic;
using Data.States;
using Utilities.Configuration.Azure;
using System.Web.Http.Description;
using Data.Interfaces.Manifests;

namespace terminalSalesforce.Controllers
{
    [RoutePrefix("terminals")]
    public class TerminalController : ApiController
    {
        [HttpGet]
        [Route("discover")]
        [ResponseType(typeof(StandardFr8TerminalCM))]
        public IHttpActionResult Get()
        {
            var terminal = new TerminalDO()
            {
                Name = "terminalSendGrid",
                TerminalStatus = TerminalStatus.Active,
                Endpoint = CloudConfigurationManager.GetSetting("TerminalEndpoint"),
                Version = "1"
            };

            var action = new ActivityTemplateDO()
            {
                Name = "SendEmailViaSendGrid",
                Label = "Send Email Vie Send Grid",
                Version = "1",
                Tags = "Notifier",
                Terminal = terminal,
                AuthenticationType = AuthenticationType.None,
                Category = ActivityCategory.Forwarders,
                MinPaneWidth = 330
            };

            var actionList = new List<ActivityTemplateDO>()
            {
                action
            };

            StandardFr8TerminalCM curStandardFr8TerminalCM = new StandardFr8TerminalCM()
            {
                Definition = terminal,
                Actions = actionList
            };
            return Json(curStandardFr8TerminalCM);
        }
    }
}