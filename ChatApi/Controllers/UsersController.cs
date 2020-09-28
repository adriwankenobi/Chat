using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ChatData.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ChatApi.Controllers.Common;

namespace ChatApi.Controllers
{

    [Produces("application/json")]
    [Route("api/users")]
    public class UsersController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly FabricClient fabricClient;
        private readonly StatelessServiceContext serviceContext;

        public UsersController(HttpClient httpClient, StatelessServiceContext context, FabricClient fabricClient)
        {
            this.fabricClient = fabricClient;
            this.httpClient = httpClient;
            this.serviceContext = context;
        }

        // Register and/or login
        // PUT: api/users?id=id
        [HttpPut("")]
        public async Task<IActionResult> Put([FromQuery] string id, [FromBody] UserData user)
        {
            if (String.IsNullOrEmpty(id))
            {
                // Register
                if (String.IsNullOrEmpty(user.Name))
                {
                    return new BadRequestResult();
                }

                user = new UserData(user.Name);
            }
            else
            {
                // Login
                string proxyUrlLogin = PartitionHelper.GetProxyUrl(this.serviceContext, $"{HttpHelper.USERS_API}/{id}", id);

                using (HttpResponseMessage response = await this.httpClient.GetAsync(proxyUrlLogin))
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        return this.StatusCode((int)response.StatusCode);
                    }
                    user = JsonConvert.DeserializeObject<UserData>(await response.Content.ReadAsStringAsync());
                }

                user.LastLoginAt = DateTime.Now;
            }

            // Set as connected user
            string remoteAddress = $"{this.HttpContext.Connection.RemoteIpAddress}:{this.HttpContext.Connection.RemotePort}";
            string proxyUrl = PartitionHelper.GetProxyUrl(this.serviceContext, HttpHelper.CONNECTED_USERS_API, user.Id);
            StringContent putContent = HttpHelper.GetJSONContent(new ConnectedUserData(user.Id, user.Name, remoteAddress));

            using (HttpResponseMessage response = await this.httpClient.PutAsync(proxyUrl, putContent))
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return this.StatusCode((int)response.StatusCode);
                }
            }

            // Save user
            proxyUrl = PartitionHelper.GetProxyUrl(this.serviceContext, HttpHelper.USERS_API, user.Id);
            putContent = HttpHelper.GetJSONContent(user);

            using (HttpResponseMessage response = await this.httpClient.PutAsync(proxyUrl, putContent))
            {
                return new ContentResult()
                {
                    StatusCode = (int)response.StatusCode,
                    Content = await response.Content.ReadAsStringAsync()
                };
            }
        }

        // Logout
        // DELETE: api/users?id=id
        [HttpDelete("")]
        public async Task<IActionResult> Delete([FromQuery] string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                return new BadRequestResult();
            }

            // Logout
            // Set as not connected user
            string proxyUrl = PartitionHelper.GetProxyUrl(this.serviceContext, $"{HttpHelper.CONNECTED_USERS_API}/{id}", id);

            using (HttpResponseMessage response = await this.httpClient.DeleteAsync(proxyUrl))
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return this.StatusCode((int)response.StatusCode);
                }
            }

            return new OkResult();
        }
    }
}