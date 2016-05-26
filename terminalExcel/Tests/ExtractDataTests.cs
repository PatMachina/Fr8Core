﻿using AutoMapper;
using NUnit.Framework;
using StructureMap;
using Data.Entities;
using Data.Interfaces;
using Fr8Data.DataTransferObjects;
using Hub.Interfaces;
using Hub.Managers;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;
using terminalExcel.Actions;
using TerminalBase.Infrastructure;

namespace terminalExcel.PluginExcelTests
{
    [TestFixture]
    [Category("terminalExcel")]
    public class ExtractDataTests : BaseTest
    {
        public const string ExcelTestServerUrl = "ExcelTestServerUrl";

        public const string filesCommand = "files";

        private IActivity _activity;
        private ICrateManager _crate;
        private FixtureData _fixtureData;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            TerminalBootstrapper.ConfigureTest();

            _fixtureData = new FixtureData(ObjectFactory.GetInstance<IUnitOfWork>());
            _activity = ObjectFactory.GetInstance<IActivity>();
            _crate = ObjectFactory.GetInstance<ICrateManager>();
        }

        [TearDown]
        public void Cleanup()
        {

        }

        [Test]
        public void ConfigEvaluatorInitialResponse_Test()
        {
            
            var curActionDTO = new ActivityDTO();
            var curActivityDO = Mapper.Map<ActivityDO>(curActionDTO);
            var result = new Load_Excel_File_v1().ConfigurationEvaluator(curActivityDO);

            Assert.AreEqual(result, TerminalBase.Infrastructure.ConfigurationRequestType.Initial);
        }

        //[Test]
        //[ExpectedException]
        //public void ConfigEvaluatorFollowupResponseThrowsException_Test()
        //{
        //    var curActionDTO = new ActionDTO()
        //    {
        //        CrateStorage = new CrateStorageDTO()
        //        {
        //            CrateDTO = new System.Collections.Generic.List<CrateDTO>(),
        //        },
        //    };
        //    StandardConfigurationControlsMS confControlsMS = new StandardConfigurationControlsMS()
        //    {
        //        Controls = new List<ControlDefinitionDTO>()
        //        {
        //            new ControlDefinitionDTO("select_file", true, "1", "select file"),
        //            new ControlDefinitionDTO("select_file", true, "2", "select file"),
        //        },
        //    };
        //    curActionDTO.CrateStorage.CrateDTO.Add(new CrateDTO()
        //    {
        //        Contents = JsonConvert.SerializeObject(confControlsMS),
        //        ManifestType = CrateManifests.STANDARD_CONF_CONTROLS_NANIFEST_NAME,
        //    });
        //    //Mock<ICrate> crateMock = new Mock<ICrate>();
        //    //crateMock.Setup(a => a.GetElementByKey<int>(It.IsAny<IEnumerable<CrateDTO>>(), It.IsAny<int>(), It.IsAny<string>())).Returns(() => new List<JObject>() { new JObject(), new JObject() });

        //    //ActionDO activityDO = new FixtureData(uow).TestAction3();
        //    //var controller = new ActionController(crateMock.Object);


        //    var result = new ExtractData_v1().ConfigurationEvaluator(curActionDTO);

        //    Assert.AreNotEqual(result, PluginBase.Infrastructure.ConfigurationRequestType.Followup);
        //}

        //[Test]
        //public void ConfigEvaluatorFollowupResponse_Test()
        //{
        //    var curActionDTO = new ActionDTO()
        //    {
        //        CrateStorage = new CrateStorageDTO()
        //        {
        //            CrateDTO = new System.Collections.Generic.List<CrateDTO>(),
        //        },
        //    };
        //    StandardConfigurationControlsMS confControlsMS = new StandardConfigurationControlsMS()
        //    {
        //        Controls = new List<ControlDefinitionDTO>()
        //        {
        //            new ControlDefinitionDTO("select_file", true, "1", "select file"),
        //        },
        //    };
        //    curActionDTO.CrateStorage.CrateDTO.Add(new CrateDTO()
        //        {
        //            Contents = JsonConvert.SerializeObject(confControlsMS),
        //            ManifestType = CrateManifests.STANDARD_CONF_CONTROLS_NANIFEST_NAME,
        //        });
        //    //Mock<ICrate> crateMock = new Mock<ICrate>();
        //    //crateMock.Setup(a => a.GetElementByKey<int>(It.IsAny<IEnumerable<CrateDTO>>(), It.IsAny<int>(), It.IsAny<string>())).Returns(() => new List<JObject>() { new JObject(), new JObject() });

        //    //ActionDO activityDO = new FixtureData(uow).TestAction3();
        //    //var controller = new ActionController(crateMock.Object);


        //    var result = new ExtractData_v1().ConfigurationEvaluator(curActionDTO);

        //    Assert.AreEqual(result, PluginBase.Infrastructure.ConfigurationRequestType.Followup);
        //}
    }
}