using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Site.Models;

namespace Site.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
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

            var res = await result.Content.ReadAsAsync<FunctionCallResult>();

            return new StatusResult
            {
                State = (res.Result == "open" ? State.Open : State.Closed).ToString().ToLower()
            };
        }

        [HttpPut, Route("toggle")]
        public async Task<ToggleResult> TogglePort()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            var arg = new KeyValuePair<string, string>("args", "togglePort");
            var result = await
               client.PostAsync($"https://api.particle.io/v1/devices/{_conf.Value.DeviceId}/functions?access_token={_conf.Value.AccessToken}", new FormUrlEncodedContent(new[] { arg }));
            return new ToggleResult { Result = true };
        }
    }

    public class StatusResult
    {
        public string State { get; set; }
    }

    public class ToggleResult
    {
        public Boolean Result { get; set; }
    }

    public enum State
    {
        Open,
        Closed
    }
}
