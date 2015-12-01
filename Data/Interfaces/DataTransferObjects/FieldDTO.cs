﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using Newtonsoft.Json;

namespace Data.Interfaces.DataTransferObjects
{
    public class FieldDTO
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("tags")]
        public string Tags { get; set; }

        public FieldDTO()
        {
        }

        public FieldDTO(string key)
        {
            Key = key;
        }

        public FieldDTO(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
