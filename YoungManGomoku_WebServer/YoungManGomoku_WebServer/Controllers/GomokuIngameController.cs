using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using YoungManGomoku_WebServer.Data;

namespace YoungManGomoku_WebServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GomokuIngameController : ControllerBase
    {
        ApplicationDBContext _context;

        private readonly ILogger<GomokuIngameController> _logger;

        public GomokuIngameController(ILogger<GomokuIngameController> logger, ApplicationDBContext context)
        {
            _logger = logger;
            _context = context;
        }
    }
}
