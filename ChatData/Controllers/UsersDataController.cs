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
    public class UsersDataController : Controller
    {
        private readonly IReliableStateManager stateManager;
        private const string USERS = "users";

        public UsersDataController(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        // GET api/UsersData/id
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            IReliableDictionary<string, UserData> usersDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, UserData>>(USERS);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                if (!await usersDict.ContainsKeyAsync(tx, id))
                {
                    return new NoContentResult();
                }

                var result = await usersDict.TryGetValueAsync(tx, id);
                return this.Json(result.Value);
            }
        }

        // PUT api/UsersData
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] UserData user)
        {
            IReliableDictionary<string, UserData> usersDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, UserData>>(USERS);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                await usersDict.AddOrUpdateAsync(tx, user.Id, user, (key, oldvalue) => user);
                await tx.CommitAsync();
            }

            return this.Json(user);
        }
    }
}