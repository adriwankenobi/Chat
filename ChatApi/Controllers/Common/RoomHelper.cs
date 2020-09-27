using ChatData.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Fabric;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChatApi.Controllers.Common
{
    public static class RoomHelper
    {

        public class RoomResult
        {
            public IActionResult Result { get; set; }
            public RoomData Room { get; set; }
        }

        public static async Task<RoomResult> RoomExists(ServiceContext context, HttpClient httpClient, Func<int, IActionResult> statusCodeFunc, string id)
        {
            string proxyUrl = PartitionHelper.GetProxyUrl(context, $"{HttpHelper.ROOMS_API}/{id}", id);

            RoomData room;
            using (HttpResponseMessage response = await httpClient.GetAsync(proxyUrl))
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        return new RoomResult() { Result = new NotFoundResult() };
                    }
                    return new RoomResult() { Result = statusCodeFunc((int)response.StatusCode) };
                }

                room = JsonConvert.DeserializeObject<RoomData>(await response.Content.ReadAsStringAsync());
                if (room.IsDeleted)
                {
                    return new RoomResult() { Result = new NotFoundResult() };
                }
            }

            return new RoomResult() { Room = room }; ;
        }

    }
}
