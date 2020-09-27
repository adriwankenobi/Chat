using System;
using System.Fabric;
using System.Net.Http;
using System.Threading.Tasks;
using ChatApi.Controllers.Common;
using ChatData.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatApi.Controllers
{

    [Produces("application/json")]
    [Route("api/rooms")]
    public class MessagesController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly FabricClient fabricClient;
        private readonly StatelessServiceContext serviceContext;

        public MessagesController(HttpClient httpClient, StatelessServiceContext context, FabricClient fabricClient)
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

            var result = await RoomHelper.RoomExists(this.serviceContext, this.httpClient, this.StatusCode, id);
            if (result.Result != null)
            {
                return result.Result;
            }

            string proxyUrl = PartitionHelper.GetProxyUrl(this.serviceContext, $"{HttpHelper.MESSAGES_API}/{id}", id);

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

            var result = await RoomHelper.RoomExists(this.serviceContext, this.httpClient, this.StatusCode, id);
            if (result.Result != null)
            {
                return result.Result;
            }

            msg = new MessageData(msg.SenderId, id, msg.Content);

            string proxyUrl = PartitionHelper.GetProxyUrl(this.serviceContext, $"{HttpHelper.MESSAGES_API}", id);
            StringContent postContent = HttpHelper.GetJSONContent(msg);

            using (HttpResponseMessage response = await this.httpClient.PostAsync(proxyUrl, postContent))
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