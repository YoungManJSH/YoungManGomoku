using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.SingletoneManager;

namespace YoungManGomoku_WebServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class GomokuIngameController : ControllerBase
    {
        ApplicationDBContext _context;

        private readonly ILogger<GomokuIngameController> _logger;
        private readonly ServerManager _serverManager;

        public GomokuIngameController(ILogger<GomokuIngameController> logger, ApplicationDBContext context, ServerManager serverManager)
        {
            _logger = logger;
            _context = context;
            _serverManager = serverManager;
        }
    }
}
