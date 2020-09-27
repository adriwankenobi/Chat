using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ChatData.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ChatApi.Controllers
{

    [Produces("application/json")]
    [Route("api/rooms")]
    public class RoomsDataController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly FabricClient fabricClient;
        private readonly StatelessServiceContext serviceContext;

        public RoomsDataController(HttpClient httpClient, StatelessServiceContext context, FabricClient fabricClient)
        {
            this.fabricClient = fabricClient;
            this.httpClient = httpClient;
            this.serviceContext = context;
        }

        // GET: api/rooms
        [HttpGet("")]
        public async Task<IActionResult> Get()
        {
            Uri serviceName = ChatApi.GetChatDataServiceName(this.serviceContext);
            Uri proxyAddress = this.GetProxyAddress(serviceName);

            ServicePartitionList partitions = await this.fabricClient.QueryManager.GetPartitionListAsync(serviceName);

            List<RoomData> result = new List<RoomData>();

            foreach (Partition partition in partitions)
            {
                string proxyUrl =
                    $"{proxyAddress}/api/RoomsData?PartitionKey={((Int64RangePartitionInformation)partition.PartitionInformation).LowKey}&PartitionKind=Int64Range";

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

            Uri serviceName = ChatApi.GetChatDataServiceName(this.serviceContext);
            Uri proxyAddress = this.GetProxyAddress(serviceName);
            long partitionKey = this.GetPartitionKey(room.Id);
            string proxyUrl = $"{proxyAddress}/api/RoomsData?PartitionKey={partitionKey}&PartitionKind=Int64Range";

            StringContent putContent = new StringContent(JsonConvert.SerializeObject(room, Formatting.Indented), Encoding.UTF8, "application/json");
            putContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

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

            Uri serviceName = ChatApi.GetChatDataServiceName(this.serviceContext);
            Uri proxyAddress = this.GetProxyAddress(serviceName);
            long partitionKey = this.GetPartitionKey(id);
            string proxyUrl = $"{proxyAddress}/api/RoomsData/{id}?PartitionKey={partitionKey}&PartitionKind=Int64Range";

            RoomData room;
            using (HttpResponseMessage response = await this.httpClient.GetAsync(proxyUrl))
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        return new NotFoundResult();
                    }
                    return this.StatusCode((int)response.StatusCode);
                }

                room = JsonConvert.DeserializeObject<RoomData>(await response.Content.ReadAsStringAsync());
            }

            if (room.IsDeleted)
            {
                return new NotFoundResult();
            }

            room.IsDeleted = true;

            proxyUrl = $"{proxyAddress}/api/RoomsData?PartitionKey={partitionKey}&PartitionKind=Int64Range";

            StringContent putContent = new StringContent(JsonConvert.SerializeObject(room, Formatting.Indented), Encoding.UTF8, "application/json");
            putContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using (HttpResponseMessage response = await this.httpClient.PutAsync(proxyUrl, putContent))
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return this.StatusCode((int)response.StatusCode);
                }
            }

            return new OkResult();
        }


        /// <summary>
        /// Constructs a reverse proxy URL for a given service.
        /// Example: http://localhost:19081/Chat/ChatData/
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        private Uri GetProxyAddress(Uri serviceName)
        {
            return new Uri($"http://localhost:19081{serviceName.AbsolutePath}");
        }

        /// <summary>
        /// Creates a partition key from the given name.
        /// Uses the zero-based numeric position in the alphabet of the first letter of the name (0-25).
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private long GetPartitionKey(string name)
        {
            return Char.ToUpper(name.First()) - 'A';
        }
    }
}