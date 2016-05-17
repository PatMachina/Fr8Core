﻿using Microsoft.Owin;
using Owin;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using Data.Infrastructure.AutoMapper;
using terminalFr8Core.Actions;
using terminalFr8Core.Activities;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using TerminalBase.Services;

[assembly: OwinStartup(typeof(terminalFr8Core.Startup))]

namespace terminalFr8Core
{
    public class Startup: BaseConfiguration
    {
        public void Configuration(IAppBuilder app)
        {
            Configuration(app, false);
        }

        public void Configuration(IAppBuilder app, bool selfHost)
        {
            Hub.StructureMap.StructureMapBootStrapper.ConfigureDependencies(Hub.StructureMap.StructureMapBootStrapper.DependencyType.LIVE);
            DataAutoMapperBootStrapper.ConfigureAutoMapper();
            TerminalBootstrapper.ConfigureLive();
            ConfigureProject(selfHost, null);
            RoutesConfig.Register(_configuration);
            ConfigureFormatters();

            app.UseWebApi(_configuration);

            if (!selfHost)
            {
                StartHosting("terminalAzure");
            }
        }


        protected override void RegisterActivities()
        {
            ActivityStore.RegisterActivity<AddPayloadManually_v1>(AddPayloadManually_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<AppBuilder_v1>(AppBuilder_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<Build_Message_v1>(Build_Message_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<BuildQuery_v1>(BuildQuery_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<ConnectToSql_v1>(ConnectToSql_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<ConvertCrates_v1>(ConvertCrates_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<ConvertRelatedFieldsIntoTable_v1>(ConvertRelatedFieldsIntoTable_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<ExecuteSql_v1>(ExecuteSql_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<ExtractTableField_v1>(ExtractTableField_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<FilterObjectListByIncomingMessage_v1>(FilterObjectListByIncomingMessage_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<FindObjects_Solution_v1>(FindObjects_Solution_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<GetDataFromFr8Warehouse_v1>(GetDataFromFr8Warehouse_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<GetFileFromFr8Store_v1>(GetFileFromFr8Store_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<Loop_v1>(Loop_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<ManagePlan_v1>(ManagePlan_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<MapFields_v1>(MapFields_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<Monitor_Fr8_Events_v1>(Monitor_Fr8_Events_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<QueryFr8Warehouse_v1>(QueryFr8Warehouse_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<SaveToFr8Warehouse_v1>(SaveToFr8Warehouse_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<SearchFr8Warehouse_v1>(SearchFr8Warehouse_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<Select_Fr8_Object_v1>(Select_Fr8_Object_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<SetDelay_v1>(SetDelay_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<Show_Report_Onscreen_v1>(Show_Report_Onscreen_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<StoreFile_v1>(StoreFile_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<TestAndBranch_v1>(TestAndBranch_v1.ActivityTemplateDTO);
            ActivityStore.RegisterActivity<TestIncomingData_v1>(TestIncomingData_v1.ActivityTemplateDTO);
        }

        public override ICollection<Type> GetControllerTypes(IAssembliesResolver assembliesResolver)
        {
            return new Type[] {
                    typeof(Controllers.ActivityController),
                    typeof(Controllers.EventController),
                    typeof(Controllers.TerminalController)
                };
        }
    }
}
