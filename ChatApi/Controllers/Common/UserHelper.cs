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

        public static async Task<IActionResult> IsUserAuthorized(ServiceContext context, HttpClient httpClient, string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                return new UnauthorizedResult();
            }

            string proxyUrl = PartitionHelper.GetProxyUrl(context, $"{HttpHelper.CONNECTED_USERS_API}/{id}", id);
            using (HttpResponseMessage response = await httpClient.GetAsync(proxyUrl))
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return new UnauthorizedResult();
                }
            }

            return null;
        }

    }
}
