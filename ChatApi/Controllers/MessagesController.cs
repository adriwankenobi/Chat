using System;
using System.Fabric;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ChatApi.Controllers.Common;
using ChatData.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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

        // GET: api/rooms/roomId?id=id
        [HttpGet("{roomId}")]
        public async Task<IActionResult> Get([FromQuery] string id, string roomId)
        {
            var res = await UserHelper.IsUserAuthorized(this.serviceContext, this.httpClient, id);
            if (res != null)
            {
                return res;
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

            if (!result.Room.Members.Contains(id))
            {
                return new UnauthorizedResult();
            }

            string proxyUrl = PartitionHelper.GetProxyUrl(this.serviceContext, $"{HttpHelper.MESSAGES_API}/{roomId}", roomId);

            using (HttpResponseMessage response = await this.httpClient.GetAsync(proxyUrl))
            {
                return new ContentResult()
                {
                    StatusCode = (int)response.StatusCode,
                    Content = await response.Content.ReadAsStringAsync()
                };
            }

            
        }

        // POST: api/rooms/roomId?id=id
        [HttpPost("{roomId}")]
        public async Task<IActionResult> Post([FromQuery] string id, string roomId, [FromBody] MessageData msg)
        {
            var res = await UserHelper.IsUserAuthorized(this.serviceContext, this.httpClient, id);
            if (res != null)
            {
                return res;
            }

            if (String.IsNullOrEmpty(roomId) || (!String.IsNullOrEmpty(msg.RoomId) && roomId != msg.RoomId) ||
                String.IsNullOrEmpty(msg.Content))
            {
                return new BadRequestResult();
            }

            var result = await RoomHelper.RoomExists(this.serviceContext, this.httpClient, this.StatusCode, roomId);
            if (result.Result != null)
            {
                return result.Result;
            }

            if (!result.Room.Members.Contains(id))
            {
                return new UnauthorizedResult();
            }

            msg = new MessageData(id, roomId, msg.Content);

            string proxyUrl = PartitionHelper.GetProxyUrl(this.serviceContext, HttpHelper.MESSAGES_API, roomId);
            StringContent postContent = HttpHelper.GetJSONContent(msg);

            using (HttpResponseMessage response = await this.httpClient.PostAsync(proxyUrl, postContent))
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return this.StatusCode((int)response.StatusCode);
                }
            }

            // Broadcast message to online members
            foreach (string member in result.Room.Members)
            {
                proxyUrl = PartitionHelper.GetProxyUrl(this.serviceContext, $"{HttpHelper.CONNECTED_USERS_API}/{member}", member);

                using (HttpResponseMessage response = await httpClient.GetAsync(proxyUrl))
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        continue;
                    }
                    var user = JsonConvert.DeserializeObject<ConnectedUserData>(await response.Content.ReadAsStringAsync());

                    // TODO: Send message to connected user
                    Console.WriteLine($"Sending message to: {user.Ip}");
                }
            }

            return new ContentResult()
            {
                StatusCode = (int)System.Net.HttpStatusCode.OK,
                Content = JsonConvert.SerializeObject(msg, Formatting.Indented)
            };
        }
    }
}