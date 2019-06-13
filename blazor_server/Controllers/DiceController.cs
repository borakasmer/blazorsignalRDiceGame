using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace blazor_server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiceController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }
    }
    public class DiceHub : Hub
    {
        static ConcurrentDictionary<string, string> clientList = new ConcurrentDictionary<string, string>();
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("GetConnectionId", this.Context.ConnectionId);
        }
        public async Task<bool> AddList(string userName, string connectionId)
        {
            var result = clientList.TryAdd(userName, connectionId);
            if (result)
            {
                await GetUser(userName);
            }
            return result;
        }

        public async Task GetUser(string userName)
        {
            if (clientList.Any(cl => cl.Key != userName))
            {
                var player2 = clientList.First(cl => cl.Key != userName);
                var player1 = clientList.First(cl => cl.Key == userName);

                await Clients.Client(player1.Value).SendAsync("FetchUser", player2.Key, player2.Value);
                await Clients.Client(player2.Value).SendAsync("FetchUser", player1.Key, player1.Value);

                clientList.TryRemove(player1.Key, out _);
                clientList.TryRemove(player2.Key, out _);
            }
        }

        public async Task SenDice(string connectionID, string userName, int diceOne, int diceTwo)
        {
            await Clients.Client(connectionID).SendAsync("GetDice", userName, diceOne, diceTwo);
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;
            string userName = clientList.Where(entry => entry.Value == connectionId)
              .Select(entry => entry.Key).FirstOrDefault();
            clientList.TryRemove(userName, out _);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
