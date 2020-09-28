using ChatData.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Fabric;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChatApi.Controllers.Common
{
    public static class UserHelper
    {

        public class ConnectedUser
        {
            public IActionResult Result { get; set; }
            public ConnectedUserData User { get; set; }
        }

        public static async Task<ConnectedUser> IsUserAuthorized(ServiceContext context, HttpClient httpClient, string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                new ConnectedUser() { Result = new UnauthorizedResult() };
            }

            ConnectedUserData user;
            string proxyUrl = PartitionHelper.GetProxyUrl(context, $"{HttpHelper.CONNECTED_USERS_API}/{id}", id);
            using (HttpResponseMessage response = await httpClient.GetAsync(proxyUrl))
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    new ConnectedUser() { Result = new UnauthorizedResult() };
                }

                user = JsonConvert.DeserializeObject<ConnectedUserData>(await response.Content.ReadAsStringAsync());
            }

            return new ConnectedUser() { User = user }; ;
        }

    }
}
