﻿using Newtonsoft.Json;

namespace MyLab.AsyncProcessor.Api.DataModel
{
    public class ProcessingError
    {
        [JsonProperty("bizMgs")]
        public string BizMessage { get; set; }
        [JsonProperty("techMgs")]
        public string TechMessage { get; set; }
        [JsonProperty("techInfo")]
        public string TechInfo { get; set; }
    }
}