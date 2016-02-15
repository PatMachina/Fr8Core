﻿using Data.Control;
using Data.Crates;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using HealthMonitor.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using terminalFr8CoreTests.Fixtures;
using Hub.Managers;
using Hub.Managers.APIManagers.Transmitters.Restful;

namespace terminalFr8CoreTests.Integration
{
    [Explicit]
    public class ConnectToSql_v1_Tests : BaseTerminalIntegrationTest
    {
        public override string TerminalName
        {
            get { return "terminalFr8Core"; }
        }

        private void AssertConfigureControls(StandardConfigurationControlsCM control)
        {
            Assert.AreEqual(1, control.Controls.Count);

            // Assert that first control is a TextBox 
            // with Label == "SQL Connection String"
            // with Name == "ConnectionString"
            Assert.IsTrue(control.Controls[0] is TextBox);
            Assert.AreEqual("SQL Connection String", control.Controls[0].Label);
            Assert.AreEqual("ConnectionString", control.Controls[0].Name);
        }

        private void AssertErrorControls(StandardConfigurationControlsCM control)
        {
            Assert.AreEqual(2, control.Controls.Count);

            Assert.IsTrue(control.Controls[0] is TextBlock);
            Assert.AreEqual("Connection String", control.Controls[0].Label);
            Assert.AreEqual("ConnectionString", control.Controls[0].Name);
            
            Assert.IsTrue(control.Controls[1] is TextBlock);
            Assert.AreEqual("Unexpected error", control.Controls[1].Label);
            Assert.AreEqual("ErrorLabel", control.Controls[1].Name);
        }

        private void AssertFollowUpCrateTypes(CrateStorage crateStorage)
        {
            Assert.AreEqual(4, crateStorage.Count);
            Assert.AreEqual(3, crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Count());
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count());
            
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Count(x => x.Label == "Sql Table Definitions"));
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Count(x => x.Label == "Sql Column Types"));
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardDesignTimeFieldsCM>().Count(x => x.Label == "Sql Connection String"));
        }

        private void AssertConfigureCrate(CrateStorage crateStorage)
        {
            Assert.AreEqual(1, crateStorage.Count);
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count());

            AssertConfigureControls(crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single());
        }

        private Crate CreateConnectionStringCrate()
        {
            var control = UtilitiesTesting.Fixtures.FixtureData.TestConnectionString2();
            control.Name = "ConnectionString";
            control.Label = "Connection String";

            return PackControlsCrate(control);
        }

        private Crate CreateWrongConnectionStringCrate()
        {
            var control = UtilitiesTesting.Fixtures.FixtureData.TestConnectionString2();
            control.Name = "ConnectionString";
            control.Label = "Connection String";
            control.Value = "Wrong connection string";

            return PackControlsCrate(control);
        }

        private Crate<StandardConfigurationControlsCM> PackControlsCrate(params ControlDefinitionDTO[] controlsList)
        {
            return Crate<StandardConfigurationControlsCM>.FromContent("Configuration_Controls", new StandardConfigurationControlsCM(controlsList));
        }

        /// <summary>
        /// Validate correct crate-storage structure in initial configuration response.
        /// </summary>
        [Test]
        public async Task ConnectToSql_Initial_Configuration_Check_Crate_Structure()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var requestActionDTO = FixtureData.ConnectToSql_InitialConfiguration_Fr8DataDTO();

            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    requestActionDTO
                );

            Assert.NotNull(responseActionDTO);
            Assert.NotNull(responseActionDTO.CrateStorage);
            Assert.NotNull(responseActionDTO.CrateStorage.Crates);

            var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);
            AssertConfigureCrate(crateStorage);
        }
        

        /// <summary>
        /// Validate correct crate-storage structure in follow-up configuration response with error connetcion string
        /// </summary>
        [Test]
        public async Task ConnectToSql_FollowUp_Configuration_No_Connection_String_Check_Crate_Structure()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var dataDTO = FixtureData.ConnectToSql_InitialConfiguration_Fr8DataDTO();

            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    dataDTO
                );
            dataDTO.ActivityDTO = responseActionDTO;
            responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    dataDTO
                );

            Assert.NotNull(responseActionDTO);
            Assert.NotNull(responseActionDTO.CrateStorage);
            Assert.NotNull(responseActionDTO.CrateStorage.Crates);

            var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);

            AssertConfigureCrate(crateStorage);
        }        

        /// <summary>
        /// Validate correct crate-storage structure in follow-up configuration response 
        /// </summary>
        [Test]
        public async Task ConnectToSql_FollowUp_Configuration_Wrong_ConnetcioString_Check_Crate_Structure()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var dataDTO = FixtureData.ConnectToSql_InitialConfiguration_Fr8DataDTO();

            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    dataDTO
                );            

            using (var updater = Crate.UpdateStorage(responseActionDTO))
            {
                updater.CrateStorage.RemoveByLabel("Configuration_Controls");
                updater.CrateStorage.Add(CreateWrongConnectionStringCrate());
            }
            dataDTO.ActivityDTO = responseActionDTO;
            responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    dataDTO
                );

            Assert.NotNull(responseActionDTO);
            Assert.NotNull(responseActionDTO.CrateStorage);
            Assert.NotNull(responseActionDTO.CrateStorage.Crates);

            var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);

            Assert.AreEqual(1, crateStorage.Count);
            Assert.AreEqual(1, crateStorage.CratesOfType<StandardConfigurationControlsCM>().Count());
            AssertErrorControls(crateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single());
        }

        /// <summary>
        /// Validate correct crate-storage structure in follow-up configuration response 
        /// </summary>
        [Test]
        public async Task ConnectToSql_FollowUp_Configuration_Check_Crate_Structure()
        {
            var configureUrl = GetTerminalConfigureUrl();

            var dataDTO = FixtureData.ConnectToSql_InitialConfiguration_Fr8DataDTO();

            var responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    dataDTO
                );

            using (var updater = Crate.UpdateStorage(responseActionDTO))
            {
                updater.CrateStorage.RemoveByLabel("Configuration_Controls");
                updater.CrateStorage.Add(CreateConnectionStringCrate());
            }
            dataDTO.ActivityDTO = responseActionDTO;
            responseActionDTO =
                await HttpPostAsync<Fr8DataDTO, ActivityDTO>(
                    configureUrl,
                    dataDTO
                );

            Assert.NotNull(responseActionDTO);
            Assert.NotNull(responseActionDTO.CrateStorage);
            Assert.NotNull(responseActionDTO.CrateStorage.Crates);

            var crateStorage = Crate.FromDto(responseActionDTO.CrateStorage);

            AssertFollowUpCrateTypes(crateStorage);            
        }

        /// <summary>
        /// Test run-time for action Run().
        /// </summary>
        [Test]
        public async Task ConnectToSql_Run()
        {
            var runUrl = GetTerminalRunUrl();

            var dataDTO = FixtureData.ConnectToSql_InitialConfiguration_Fr8DataDTO();

            AddOperationalStateCrate(dataDTO, new OperationalStateCM());

            AddPayloadCrate(
               dataDTO,
               new EventReportCM()
           );

            var responsePayloadDTO =
                await HttpPostAsync<Fr8DataDTO, PayloadDTO>(runUrl, dataDTO);

            Assert.NotNull(responsePayloadDTO);

            var crateStorage = Crate.GetStorage(responsePayloadDTO);
            Assert.AreEqual(2, crateStorage.Count);
        }
    }
}
