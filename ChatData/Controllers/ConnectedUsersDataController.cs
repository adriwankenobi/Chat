using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using ChatData.Models;

namespace ChatData.Controllers
{

    [Route("api/[controller]")]
    public class ConnectedUsersDataController : Controller
    {
        private readonly IReliableStateManager stateManager;
        private const string CONNECTED_USERS = "connected_users";

        public ConnectedUsersDataController(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        // PUT api/ConnectedUserData
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] ConnectedUserData user)
        {
            IReliableDictionary<string, ConnectedUserData> usersDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, ConnectedUserData>>(CONNECTED_USERS);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                await usersDict.AddOrUpdateAsync(tx, user.Id, user, (key, oldvalue) => user);
                await tx.CommitAsync();
            }

            return this.Json(user);
        }

        // DELETE api/ConnectedUserData
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            IReliableDictionary<string, ConnectedUserData> usersDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, ConnectedUserData>>(CONNECTED_USERS);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                await usersDict.TryRemoveAsync(tx, id);
                await tx.CommitAsync();
            }

            return new OkResult();
        }

        // GET api/ConnectedUserData
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            IReliableDictionary<string, ConnectedUserData> usersDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, ConnectedUserData>>(CONNECTED_USERS);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                var result = await usersDict.TryGetValueAsync(tx, id);
                if (!result.HasValue)
                {
                    return new NoContentResult();
                }
                return this.Json(result.Value);
            }
        }
    }
}