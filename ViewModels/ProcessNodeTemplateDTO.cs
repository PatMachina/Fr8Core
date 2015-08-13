﻿using Newtonsoft.Json.Linq;

namespace Web.ViewModels
{
    /// <summary>
    /// Data transfer object for ProcessNodeTemplateDO entity.
    /// </summary>
    public class ProcessNodeTemplateDTO
    {
        public int Id { get; set; }

        public int? ProcessTemplateId { get; set; }

        public string Name { get; set; }

        public JToken TransitionKey { get; set; }
    }
}