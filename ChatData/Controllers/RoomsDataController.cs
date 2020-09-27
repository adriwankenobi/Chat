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
    public class RoomsDataController : Controller
    {
        private readonly IReliableStateManager stateManager;
        private const string ROOMS = "rooms";

        public RoomsDataController(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        // GET api/RoomsData
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            CancellationToken ct = new CancellationToken();

            IReliableDictionary<string, RoomData> roomsDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, RoomData>>(ROOMS);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                Microsoft.ServiceFabric.Data.IAsyncEnumerable<KeyValuePair<string, RoomData>> list = await roomsDict.CreateEnumerableAsync(tx);

                Microsoft.ServiceFabric.Data.IAsyncEnumerator<KeyValuePair<string, RoomData>> enumerator = list.GetAsyncEnumerator();

                List<RoomData> result = new List<RoomData>();

                while (await enumerator.MoveNextAsync(ct))
                {
                    result.Add(enumerator.Current.Value);
                }

                return this.Json(result);
            }
        }

        // GET api/RoomsData/id
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            IReliableDictionary<string, RoomData> roomsDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, RoomData>>(ROOMS);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                if (!await roomsDict.ContainsKeyAsync(tx, id))
                {
                    return new NoContentResult();
                }

                var result = await roomsDict.TryGetValueAsync(tx, id);
                return this.Json(result.Value);
            }
        }

        // PUT api/RoomsData
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] RoomData room)
        {
            IReliableDictionary<string, RoomData> roomsDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, RoomData>>(ROOMS);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                await roomsDict.AddOrUpdateAsync(tx, room.Id, room, (key, oldvalue) => room);
                await tx.CommitAsync();
            }

            return this.Json(room);
        }
    }
}