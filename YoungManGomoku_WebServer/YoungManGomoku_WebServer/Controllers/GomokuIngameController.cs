using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.SingletoneManager;
using YoungManGomoku_Protocol.ClientToServer;

namespace YoungManGomoku_WebServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class GomokuIngameController : ControllerBase
    {
        ApplicationDBContext _context;

        private readonly ILogger<GomokuIngameController> _logger;
        private readonly ServerManager _serverManager;
        private readonly GameRoomManager _gameRoomManager;

        public GomokuIngameController(ILogger<GomokuIngameController> logger, ApplicationDBContext context, ServerManager serverManager, GameRoomManager gameRoomManager)
        {
            _logger = logger;
            _context = context;
            _serverManager = serverManager;
            _gameRoomManager = gameRoomManager;
        }
        
		[HttpPost("PlaceStone")]
		public IActionResult PlaceStone([FromBody] CS_PlaceStoneDTO dto)
		{
			ulong uid = _serverManager.GetPlayerUID(dto.IDToken);

			if (!_gameRoomManager.TryGetRoomByPlayer(uid, out GameRoom room))
				return BadRequest("Not in game");

			PlaceStoneResult result = room.PlaceStone(uid, dto.X, dto.Y);
			return Ok(result);
		}
	}
}
