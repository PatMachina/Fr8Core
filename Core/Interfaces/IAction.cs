﻿using System.Collections.Generic;
using Data.Entities;
using Data.Interfaces;

namespace Core.Interfaces
{
    public interface IAction
    {
        IEnumerable<TViewModel> GetAllActions<TViewModel>();
        IEnumerable<ActionRegistrationDO> GetAvailableActions(IDockyardAccountDO curAccount);
        bool SaveOrUpdateAction(ActionDO currentActionDo);
        void Register(string ActionType, string PluginRegistration, string Version);
    }
}