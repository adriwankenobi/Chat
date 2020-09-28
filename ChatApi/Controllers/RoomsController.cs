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
    [Route("api/rooms")]
    public class RoomsController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly FabricClient fabricClient;
        private readonly StatelessServiceContext serviceContext;

        public RoomsController(HttpClient httpClient, StatelessServiceContext context, FabricClient fabricClient)
        {
            this.fabricClient = fabricClient;
            this.httpClient = httpClient;
            this.serviceContext = context;
        }

        // List all rooms
        // GET: api/rooms?id=id
        [HttpGet("")]
        public async Task<IActionResult> Get([FromQuery] string id)
        {
            var res = await UserHelper.IsUserAuthorized(this.serviceContext, this.httpClient, id);
            if (res.Result != null)
            {
                return res.Result;
            }

            Uri proxyAddress = PartitionHelper.GetProxyAddress(this.serviceContext);
            ServicePartitionList partitions = await PartitionHelper.GetAllPartitions(this.serviceContext, this.fabricClient);

            List<RoomData> result = new List<RoomData>();

            foreach (Partition partition in partitions)
            {
                string proxyUrl = PartitionHelper.GetProxyUrl(proxyAddress, HttpHelper.ROOMS_API, ((Int64RangePartitionInformation)partition.PartitionInformation).LowKey);

                using (HttpResponseMessage response = await this.httpClient.GetAsync(proxyUrl))
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        continue;
                    }
     
                    result.AddRange(JsonConvert.DeserializeObject<List<RoomData>>(await response.Content.ReadAsStringAsync()));
                }
            }

            if (result.Count <= 0)
            {
                return new NoContentResult();
            }

            return this.Json(result);
        }

        // Create a room
        // PUT: api/rooms?id=id
        [HttpPut("")]
        public async Task<IActionResult> Put([FromQuery] string id, [FromBody] RoomData room)
        {
            var res = await UserHelper.IsUserAuthorized(this.serviceContext, this.httpClient, id);
            if (res.Result != null)
            {
                return res.Result;
            }

            if (String.IsNullOrEmpty(room.Name))
            {
                return new BadRequestResult();
            }

            room = new RoomData(room.Name);

            string proxyUrl = PartitionHelper.GetProxyUrl(this.serviceContext, HttpHelper.ROOMS_API, room.Id);
            StringContent putContent = HttpHelper.GetJSONContent(room);

            using (HttpResponseMessage response = await this.httpClient.PutAsync(proxyUrl, putContent))
            {
                return new ContentResult()
                {
                    StatusCode = (int)response.StatusCode,
                    Content = await response.Content.ReadAsStringAsync()
                };
            }
        }

        // Join a room
        // PUT: api/rooms/roomId?id=id
        [HttpPut("{roomId}")]
        public async Task<IActionResult> Put([FromQuery] string id, string roomId)
        {
            var res = await UserHelper.IsUserAuthorized(this.serviceContext, this.httpClient, id);
            if (res.Result != null)
            {
                return res.Result;
            }

            if (String.IsNullOrEmpty(roomId))
            {
                return new BadRequestResult();
            }

            var result = await RoomHelper.RoomExists(this.serviceContext, this.httpClient, this.StatusCode, roomId);
            if (result.Result != null)
            {
                return result.Result;
            }

            if (result.Room.Members.Count >= 20)
            {
                return new ContentResult()
                {
                    StatusCode = (int)System.Net.HttpStatusCode.OK,
                    Content = "Room is full"
                };
            }

            // Add user to the room
            result.Room.Members.Add(id);

            string proxyUrl = PartitionHelper.GetProxyUrl(this.serviceContext, HttpHelper.ROOMS_API, roomId);
            StringContent putContent = HttpHelper.GetJSONContent(result.Room);

            using (HttpResponseMessage response = await this.httpClient.PutAsync(proxyUrl, putContent))
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return this.StatusCode((int)response.StatusCode);
                }
            }

            // Add room to user list
            proxyUrl = PartitionHelper.GetProxyUrl(this.serviceContext, $"{HttpHelper.USERS_API}/{id}", id);

            UserData user;
            using (HttpResponseMessage response = await this.httpClient.GetAsync(proxyUrl))
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return this.StatusCode((int)response.StatusCode);
                }
                user = JsonConvert.DeserializeObject<UserData>(await response.Content.ReadAsStringAsync());
            }

            user.Rooms.Add(roomId);

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

        // Leave a room
        // DELETE: api/rooms/roomId?id=id
        [HttpDelete("{roomId}")]
        public async Task<IActionResult> Delete([FromQuery] string id, string roomId)
        {
            var res = await UserHelper.IsUserAuthorized(this.serviceContext, this.httpClient, id);
            if (res.Result != null)
            {
                return res.Result;
            }

            if (String.IsNullOrEmpty(roomId))
            {
                return new BadRequestResult();
            }

            var result = await RoomHelper.RoomExists(this.serviceContext, this.httpClient, this.StatusCode, roomId);
            if (result.Result != null)
            {
                return result.Result;
            }

            // Remove user from the room
            result.Room.Members.Remove(id);

            string proxyUrl = PartitionHelper.GetProxyUrl(this.serviceContext, HttpHelper.ROOMS_API, roomId);
            StringContent putContent = HttpHelper.GetJSONContent(result.Room);

            using (HttpResponseMessage response = await this.httpClient.PutAsync(proxyUrl, putContent))
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return this.StatusCode((int)response.StatusCode);
                }
            }

            // Remove room from user list
            proxyUrl = PartitionHelper.GetProxyUrl(this.serviceContext, $"{HttpHelper.USERS_API}/{id}", id);

            UserData user;
            using (HttpResponseMessage response = await this.httpClient.GetAsync(proxyUrl))
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return this.StatusCode((int)response.StatusCode);
                }
                user = JsonConvert.DeserializeObject<UserData>(await response.Content.ReadAsStringAsync());
            }

            user.Rooms.Remove(roomId);

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
    }
}