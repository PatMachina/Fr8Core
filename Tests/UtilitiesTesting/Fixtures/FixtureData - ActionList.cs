﻿using Data.Entities;
using Data.States;
using System.Collections.Generic;
using Hub.Managers;
using StructureMap;

namespace UtilitiesTesting.Fixtures
{
    public class ActionListDO : List<RouteNodeDO>
    {
        private RouteNodeDO _parentActivity;
        private int? _parentActivityId;

        internal RouteNodeDO ParentActivity
        {
            get
            {
                return _parentActivity;
            }
            set
            {
                _parentActivity = value;
                foreach (var a in this)
                {
                    a.ParentRouteNode = value;
                }
            }
        }


        internal int? ParentActivityId
        {
            get
            {
                return _parentActivityId;
            }
            set
            {
                _parentActivityId = value;
                foreach (var a in this)
                {
                    a.ParentRouteNodeId = value;
                }
            }
        }


        internal int? SubrouteId
        {
            get
            {
                return _parentActivityId;
            }
            set
            {
                _parentActivityId = value;
                foreach (var a in this)
                {
                    a.ParentRouteNodeId = value;
                }
            }
        }

        internal List<RouteNodeDO> Activities
        {
            get { return this; }
            set
            {
                Clear(); 
                AddRange(value);
                foreach (var activityDo in value)
                {
                    activityDo.ParentRouteNodeId = _parentActivityId;
                    activityDo.ParentRouteNode = _parentActivity;
                }
            }
        }
    }

    partial class FixtureData
    {

        public static ActionListDO TestActionListHealth1()
        {
            //string envelopeId = "F02C3D55-F6EF-4B2B-B0A0-02BF64CA1E09";
            var containerDO = new ContainerDO
            {
                Id = 1,
                ContainerState = 1,
                Name = "test name",
                RouteId = TestRouteHealthDemo().Id
            };

            using (var updater = ObjectFactory.GetInstance<ICrateManager>().UpdateStorage(() => containerDO.CrateStorage))
            {
                updater.CrateStorage.Add(GetEnvelopeIdCrate());
            }

            return new ActionListDO
            {
//               Id = 88,
//               Name = "list1",
//               ActionListType = ActionListType.Immediate,
//               SubrouteID = 50,
//               CurrentActivity = TestActionHealth1(),
//               Process = processDo
            };
        }
//
        public static ActionListDO TestActionList()
        {
            var curActionListDO = new ActionListDO
            {
//               Id = 1,
//               SubrouteID = 1,
//               Name = "list1",
//               ActionListType = ActionListType.Immediate
            };
            curActionListDO.Activities.Add(TestAction20());
            curActionListDO.Activities.Add(TestAction21());
//
            return curActionListDO;
        }
//
        public static ActionListDO TestActionList2()
        {
            var curActionListDO = new ActionListDO
            {
//               Id = 1,
//               Name = "list1",
//               ActionListType = ActionListType.Immediate
            };
            curActionListDO.Activities.Add(TestAction20());
            curActionListDO.Activities.Add(TestAction21());
//
            return curActionListDO;
        }
//
        public static ActionListDO TestEmptyActionList()
        {
            var curActionListDO = new ActionListDO
            {
//               Id = 4,
//               SubrouteID = 1,
//               Name = "list1",
//               ActionListType = ActionListType.Immediate
            };
            return curActionListDO;
        }
//
        public static ActionListDO TestActionListMedical()
        {
            var curActionListDO = new ActionListDO
            {
//               Id = 4,
//               SubrouteID = 1,
//               Name = "list1",
//               ActionListType = ActionListType.Immediate,                    
            };
            return curActionListDO;
        }
//
        public static ActionListDO TestActionList3()
        {
            return new ActionListDO
            {
//               Id = 2,
//               CurrentActivity = TestAction21(),
//               ActionListState = ActionListState.Inprocess
            };
        }
//
        public static ActionListDO TestActionList4()
        {
            return new ActionListDO
            {
//               Id = 2,
//               CurrentActivity = TestAction21(),
//               ActionListState = ActionListState.Unstarted
            };
        }
//
        public static ActionListDO TestActionList5()
        {
            return new ActionListDO
            {
//               Id = 2,
//               ActionListType = ActionListType.Immediate,
//               CurrentActivity = FixtureData.TestAction6(),
//               ActionListState = ActionListState.Unstarted,
                Activities = new System.Collections.Generic.List<RouteNodeDO>() 
                { 
                    FixtureData.TestAction22(),
                   FixtureData.TestAction7(),
                   FixtureData.TestAction8(null)             
                }
            };
        }
//
        public static ActionListDO TestActionList6()
        {
            ContainerDO containerDO = FixtureData.TestContainer1();
            containerDO.CrateStorage = "";
            return new ActionListDO
            {
//               Id = 2,
//               ActionListType = ActionListType.Immediate,
//               ActionListState = ActionListState.Unstarted,
//               Process = processDO
            };
        }
//
        public static ActionListDO TestActionList7()
        {
            return new ActionListDO
            {
//               Id = 2,
//               CurrentActivity = FixtureData.TestAction6(),
//               ActionListState = ActionListState.Unstarted,
                Activities = new System.Collections.Generic.List<RouteNodeDO>() 
                { 
                    FixtureData.TestAction10(),
                    FixtureData.TestAction7(),
                   FixtureData.TestAction8(null)             
                }
            };
        }
//
      /* public static ActionListDO TestActionList8()
        {
            return new ActionListDO
            {
//               Id = 2,
//               CurrentActivity = FixtureData.TestAction6(),
//               ActionListState = ActionListState.Unstarted,
                Activities = new System.Collections.Generic.List<ActivityDO>() 
                { 
                    FixtureData.TestAction10(),
                    FixtureData.TestAction7(),
                    FixtureData.TestAction8()             
                }
            };
       }*/
//
//		  /<summary>
//		  /Big tree from https://maginot.atlassian.net/wiki/display/SH/Getting+Upstream+and+Downstream+Activities+Lists
//		  /</summary>
//		  /<returns></returns>
//		  public static List<ActionListDO> TreeFromWikiPage()
//		  {
//			  List<ActionListDO> actionLists = new List<ActionListDO>();
//
//			  var activityTempate = new ActivityTemplateDO()
//			  {
//				  Id = 1,
//				  Version = "1"
//			  };
//			  ActionListDO al_1 = new ActionListDO() { Id = 1, Ordering = 1, ActionListType = ActionListType.Immediate, Name = "al_1" };
//			  ActionDO a_23 = new ActionDO() { Id = 23, ActivityTemplate = activityTempate, Name = "a_23" };
//			  al_1.Activities.Add(a_23);
//			  a_23.ParentActivity = al_1;
//
//			  ActionListDO al_43 = new ActionListDO() { Id = 43, Ordering = 2, ActionListType = ActionListType.Immediate, Name = "al_43" };
//			  al_43.ParentActivity = al_1;
//           ActionDO a_44 = new ActionDO() { Id = 44, Ordering = 1, ActivityTemplate = activityTempate, Name = "a_44" };
//			  a_44.ParentActivity = al_43;
//			  al_43.Activities.Add(a_44);
//			  ActionDO a_46 = new ActionDO() { Id = 46, Ordering = 2, ActivityTemplate = activityTempate, Name = "a_46" };
//			  a_46.ParentActivity = al_43;
//			  al_43.Activities.Add(a_46);
//			  ActionDO a_48 = new ActionDO() { Id = 48, Ordering = 3, ActivityTemplate = activityTempate, Name = "a_48" };
//			  a_48.ParentActivity = al_43;
//			  al_43.Activities.Add(a_48);
//
//			  ActionListDO al_52 = new ActionListDO() { Id = 52, Ordering = 3, ActionListType = ActionListType.Immediate, Name = "al_52" };
//			  ActionDO a_53 = new ActionDO() { Id = 53, Ordering = 1, ActivityTemplate = activityTempate, Name = "a_53" };
//			  a_53.ParentActivity = al_52;
//			  al_52.Activities.Add(a_53);
//
//			  ActionListDO al_54 = new ActionListDO() { Id = 54, Ordering = 2, ActionListType = ActionListType.Immediate, Name = "al_54" };
//			  al_54.ParentActivity = al_52;
//
//			  ActionDO a_56 = new ActionDO() { Id = 56, Ordering = 1, ActivityTemplate = activityTempate, Name = "a_56" };
//			  a_56.ParentActivity = al_54;
//			  al_54.Activities.Add(a_56);
//			  ActionDO a_57 = new ActionDO() { Id = 57, Ordering = 2, ActivityTemplate = activityTempate, Name = "a_57" };
//			  a_57.ParentActivity = al_54;
//			  al_54.Activities.Add(a_57);
//			  ActionDO a_58 = new ActionDO() { Id = 58, Ordering = 3, ActivityTemplate = activityTempate, Name = "a_58" };
//			  a_58.ParentActivity = al_54;
//			  al_54.Activities.Add(a_58);
//
//			  ActionDO a_55 = new ActionDO() { Id = 55, Ordering = 3, ActivityTemplate = activityTempate, Name = "a_55" };
//			  a_55.ParentActivity = al_52;
//			  al_52.Activities.Add(a_55);
//
//			  ActionListDO al_59 = new ActionListDO() { Id = 59, Ordering = 4, ActionListType = ActionListType.Immediate, Name = "al_59" };
//			  ActionDO a_60 = new ActionDO() { Id = 60, Ordering = 1, ActivityTemplate = activityTempate, Name = "a_60" };
//			  a_60.ParentActivity = al_59;
//			  al_59.Activities.Add(a_60);
//
//			  ActionListDO al_61 = new ActionListDO() { Id = 61, Ordering = 2, ActionListType = ActionListType.Immediate, Name = "al_61" };
//			  al_61.ParentActivity = al_59;
//			  ActionDO a_63 = new ActionDO() { Id = 63, Ordering = 1, ActivityTemplate = activityTempate, Name = "a_63" };
//			  a_63.ParentActivity = al_61;
//			  al_61.Activities.Add(a_63);
//			  ActionDO a_64 = new ActionDO() { Id = 64, Ordering = 2, ActivityTemplate = activityTempate, Name = "a_64" };
//			  a_64.ParentActivity = al_61;
//			  al_61.Activities.Add(a_64);
//			  ActionDO a_65 = new ActionDO() { Id = 65, Ordering = 3, ActivityTemplate = activityTempate, Name = "a_65" };
//			  a_65.ParentActivity = al_61;
//			  al_61.Activities.Add(a_65);
//
//			  ActionDO a_62 = new ActionDO() { Id = 62, Ordering = 3, ActivityTemplate = activityTempate, Name = "a_62" };
//			  a_62.ParentActivity = al_59;
//			  al_59.Activities.Add(a_62);
//
//			  al_43.ParentActivity = al_1;
//			  al_52.ParentActivity = al_1;
//			  al_59.ParentActivity = al_1;
//
//			  actionLists.Add(al_1);
//			  actionLists.Add(al_43);
//			  actionLists.Add(al_52);
//			  actionLists.Add(al_54);
//			  actionLists.Add(al_59);
//			  actionLists.Add(al_61);
//			  return actionLists;
//		  }
//
        public static List<RouteNodeDO> TestActionList1()
          {
              List<ActionListDO> actionLists = new List<ActionListDO>();
//
              var activityTempate = new ActivityTemplateDO()
              {
                  Id = 1,
                  Version = "1",
                  Plugin = FixtureData.PluginFive(),
                  Name = "Monitor_DocuSign"
              };
           ActionListDO al_1 = new ActionListDO()
           {
                ParentActivityId = 1
           };
             ActionDO a_23 = new ActionDO()
             {
                 Id = 23, 
                 ActivityTemplate = activityTempate, 
                 ActivityTemplateId = activityTempate.Id,
                 Name = "a_23", 
                 CrateStorage = ""
             };
              al_1.Activities.Add(a_23);
             
              actionLists.Add(al_1);
             return new List<RouteNodeDO>() { a_23 };
          }
//
         public static List<RouteNodeDO> TestActionListParentActivityID12()
          {
              List<ActionListDO> actionLists = new List<ActionListDO>();
//
              var activityTempate = new ActivityTemplateDO()
              {
                  Id = 1,
                  Version = "1",
                  Plugin = FixtureData.PluginFive(),
                  Name = "Monitor_DocuSign"
              };
             ActionListDO al_1 = new ActionListDO() { ParentActivityId = 12 };
             ActionDO a_23 = new ActionDO()
             {
                 Id = 23, 
                 ActivityTemplate = activityTempate,
                 ActivityTemplateId = activityTempate.Id,
                 Name = "a_23",
                 CrateStorage = ""
             };
              al_1.Activities.Add(a_23);
             
              actionLists.Add(al_1);
             return new List<RouteNodeDO>() { a_23 };
          }

        // Commented out by Vladimir. There is no ActionLists now. Empty action list has no sense after DO-1214
//        public static ActionListDO TestActionListProcess()
//          {
//           ActionListDO al = new ActionListDO() { Id = 52, Ordering = 3, ActionListType = ActionListType.Immediate, Name = "al" };
//           return al;
//          }
    }
}
