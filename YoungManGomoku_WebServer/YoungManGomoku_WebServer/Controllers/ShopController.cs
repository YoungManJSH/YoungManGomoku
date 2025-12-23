using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.SingletoneManager;

namespace YoungManGomoku_WebServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ShopController : ControllerBase
    {
		private  ApplicationDBContext _context;
        private readonly ILogger<ShopController> _logger;
        private readonly ServerManager _serverManager;

        public ShopController(ILogger<ShopController> logger, ApplicationDBContext context, ServerManager serverManager)
        {
            _logger = logger;
            _context = context;
            _serverManager = serverManager;
        }

        // 상점 입장함 (상점 판매 목록 요청)
        [HttpPost("GetItemList")]
        public IActionResult ShopItemAnnounce()
        {
            return Ok("ShopItems");
        }

        // 물건 구매 요청
        [HttpPost("Buy")]
        public IActionResult BuyItem()
        {
            return Ok("ShopItems");
        }
    }
}
