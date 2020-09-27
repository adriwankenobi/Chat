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

        // GET: api/rooms
        [HttpGet("")]
        public async Task<IActionResult> Get()
        {
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
     
                    result.AddRange(JsonConvert.DeserializeObject<List<RoomData>>(await response.Content.ReadAsStringAsync()).Where(r => !r.IsDeleted));
                }
            }

            if (result.Count <= 0)
            {
                return new NoContentResult();
            }

            return this.Json(result);
        }

        // PUT: api/rooms
        [HttpPut("")]
        public async Task<IActionResult> Put([FromBody] RoomData room)
        {
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

        // DELETE: api/rooms/id
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                return new BadRequestResult();
            }

            var result = await RoomHelper.RoomExists(this.serviceContext, this.httpClient, this.StatusCode, id);
            if (result.Result != null)
            {
                return result.Result;
            }

            result.Room.IsDeleted = true;

            string proxyUrl = PartitionHelper.GetProxyUrl(this.serviceContext, $"{HttpHelper.ROOMS_API}", id);
            StringContent putContent = HttpHelper.GetJSONContent(result.Room);

            using (HttpResponseMessage response = await this.httpClient.PutAsync(proxyUrl, putContent))
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