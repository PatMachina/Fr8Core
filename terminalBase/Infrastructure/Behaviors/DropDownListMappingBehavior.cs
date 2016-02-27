﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Control;
using Data.Crates;

namespace TerminalBase.Infrastructure.Behaviors
{
    public class DropDownListMappingBehavior : BaseControlMappingBehavior<DropDownList>
    {
        public DropDownListMappingBehavior(ICrateStorage crateStorage, string behaviorName) 
            : base(crateStorage, behaviorName)
        {
            BehaviorPrefix = "DropDownListMappingBehavior-";
        }

        public void Append(string name, string labelName,  List<ListItem> items)
        {
            var controlsCM = GetOrCreateStandardConfigurationControlsCM();

            var theName = string.Concat(BehaviorPrefix, name);

            var userDefinedDropDownList = new DropDownList()
            {
                Name = theName,
                Label = labelName,
                ListItems = items
            };

            controlsCM.Controls.Add(userDefinedDropDownList);
        }

        public List<DropDownList> GetValues(ICrateStorage payload = null)
        {
            var controlsCM = GetOrCreateStandardConfigurationControlsCM();

            var dropDownLists = controlsCM
                .Controls.Where(IsBehaviorControl).OfType<DropDownList>();

            foreach (var list in dropDownLists)
            {
                list.Name = GetFieldId(list);
            }

            return dropDownLists.ToList();
        }
    }
 }
