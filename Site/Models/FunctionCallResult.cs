using System;
using Newtonsoft.Json;

namespace Site.Models
{
   public class FunctionCallResult
   {
      [JsonProperty(PropertyName = "cmd")]
      public string Command { get; set; }

      [JsonProperty(PropertyName = "name")]
      public string Name { get; set; }

      [JsonProperty(PropertyName = "result")]
      public string Result { get; set; }

      [JsonProperty(PropertyName = "coreInfo")]
      public CoreInfo CoreInfo { get; set; }
      
   }

   public class CoreInfo
   {
      [JsonProperty(PropertyName = "last_app")]
      public string LastApp { get; set; }

      [JsonProperty(PropertyName = "last_heard")]
      public DateTimeOffset LastHeard { get; set; }

      [JsonProperty(PropertyName = "connected")]
      public bool Connected { get; set; }

      [JsonProperty(PropertyName = "LastHandShakeAt")]
      public DateTimeOffset LastHandShakeAt { get; set; }

      [JsonProperty(PropertyName = "deviceID")]
      public string DeviceId { get; set; }

      [JsonProperty(PropertyName = "product_id")]
      public string ProductId{ get; set; }
   }
}