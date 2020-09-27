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
    public class MessagesDataController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly FabricClient fabricClient;
        private readonly StatelessServiceContext serviceContext;

        public MessagesDataController(HttpClient httpClient, StatelessServiceContext context, FabricClient fabricClient)
        {
            this.fabricClient = fabricClient;
            this.httpClient = httpClient;
            this.serviceContext = context;
        }

        // GET: api/rooms/id
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            if (String.IsNullOrEmpty(id))
            {
                return new BadRequestResult();
            }

            Uri serviceName = ChatApi.GetChatDataServiceName(this.serviceContext);
            Uri proxyAddress = this.GetProxyAddress(serviceName);
            long partitionKey = this.GetPartitionKey(id);
            string proxyUrl = $"{proxyAddress}/api/RoomsData/{id}?PartitionKey={partitionKey}&PartitionKind=Int64Range";

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

                RoomData room = JsonConvert.DeserializeObject<RoomData>(await response.Content.ReadAsStringAsync());
                if (room.IsDeleted)
                {
                    return new NotFoundResult();
                }
            }

            proxyUrl = $"{proxyAddress}/api/MessagesData/{id}?PartitionKey={partitionKey}&PartitionKind=Int64Range";

            using (HttpResponseMessage response = await this.httpClient.GetAsync(proxyUrl))
            {
                return new ContentResult()
                {
                    StatusCode = (int)response.StatusCode,
                    Content = await response.Content.ReadAsStringAsync()
                };
            }
        }

        // POST: api/rooms/id
        [HttpPost("{id}")]
        public async Task<IActionResult> Post(string id, [FromBody] MessageData msg)
        {
            if (String.IsNullOrEmpty(id) || (!String.IsNullOrEmpty(msg.RoomId) && id != msg.RoomId) ||
                String.IsNullOrEmpty(msg.SenderId) || String.IsNullOrEmpty(msg.Content))
            {
                return new BadRequestResult();
            }
            
            Uri serviceName = ChatApi.GetChatDataServiceName(this.serviceContext);
            Uri proxyAddress = this.GetProxyAddress(serviceName);
            long partitionKey = this.GetPartitionKey(id);
            string proxyUrl = $"{proxyAddress}/api/RoomsData/{id}?PartitionKey={partitionKey}&PartitionKind=Int64Range";

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

                RoomData room = JsonConvert.DeserializeObject<RoomData>(await response.Content.ReadAsStringAsync());
                if (room.IsDeleted)
                {
                    return new NotFoundResult();
                }
            }

            proxyUrl = $"{proxyAddress}/api/MessagesData?PartitionKey={partitionKey}&PartitionKind=Int64Range";

            msg = new MessageData(msg.SenderId, id, msg.Content);

            StringContent postContent = new StringContent(JsonConvert.SerializeObject(msg, Formatting.Indented), Encoding.UTF8, "application/json");
            postContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using (HttpResponseMessage response = await this.httpClient.PostAsync(proxyUrl, postContent))
            {
                return new ContentResult()
                {
                    StatusCode = (int)response.StatusCode,
                    Content = await response.Content.ReadAsStringAsync()
                };
            }
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