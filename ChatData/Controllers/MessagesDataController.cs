using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using ChatData.Models;
using System.Linq;

namespace ChatData.Controllers
{

    [Route("api/[controller]")]
    public class MessagesDataController : Controller
    {
        private readonly IReliableStateManager stateManager;
        private const string MESSAGES = "messages";

        public MessagesDataController(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        // GET api/MessagesData/id
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            IReliableDictionary<string, List<MessageData>> msgsDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, List<MessageData>>>(MESSAGES);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                if (!await msgsDict.ContainsKeyAsync(tx, id))
                {
                    return new NoContentResult();
                }

                var result = await msgsDict.TryGetValueAsync(tx, id);
                if (result.Value.Count <= 0)
                {
                    return new NoContentResult();
                }

                return this.Json(result.Value);
            }
        }

        // POST api/MessagesData
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] MessageData msg)
        {
            IReliableDictionary<string, List<MessageData>> msgsDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, List<MessageData>>>(MESSAGES);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                await msgsDict.AddOrUpdateAsync(tx, msg.RoomId, new List<MessageData>().Append(msg).ToList(), (key, oldvalue) => oldvalue.Append(msg).ToList());
                await tx.CommitAsync();
                return this.Json(msg);
            }
        }
    }
}