using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Site.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Site.Controllers
{
    [Authorize]
    [Route("api/garage")]
    public class GarageController : Controller
    {
        private readonly IOptions<ParticleConfig> _conf;

        public GarageController(IOptions<ParticleConfig> conf)
        {
            _conf = conf;
        }

        [HttpGet, Route("status")]
        public async Task<StatusResult> Status()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var result = await
               client.GetAsync($"https://api.particle.io/v1/devices/{_conf.Value.DeviceId}/portState?access_token={_conf.Value.AccessToken}");

            var res = JsonConvert.DeserializeObject<FunctionCallResult>(await result.Content.ReadAsStringAsync());

            return new StatusResult
            {
                State = (res.Result == "open" ? State.Open : State.Closed).ToString().ToLower()
            };
        }

        [HttpPut, HttpGet, Route("toggle")]
        public async Task<ToggleResult> TogglePort()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            var arg = new KeyValuePair<string, string>("args", "togglePort");
            var result = await
               client.PostAsync($"https://api.particle.io/v1/devices/{_conf.Value.DeviceId}/functions?access_token={_conf.Value.AccessToken}", new FormUrlEncodedContent(new[] { arg }));
            return new ToggleResult { Result = true, Text = "Jag fixar det" };
        }
    }

    public class StatusResult
    {
        public string State { get; set; }
    }

    public class ToggleResult
    {
        public Boolean Result { get; set; }
        public string Text { get; set; }
    }

    public enum State
    {
        Open,
        Closed
    }
}
