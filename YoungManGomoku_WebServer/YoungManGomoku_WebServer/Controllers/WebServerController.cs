using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoungManGomoku_Protocol.Source;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.Data.DatabaseContext;    // DB Context

namespace YoungManGomoku_WebServer.Controllers
{
    // REST (REpresentational State Transfer) API

    // CRUD (Create Read Update Delete)

    // Controller 반환값
    // Null 반환 시 -> 204 Response (No Context)
    [Route("[controller]")]
    [ApiController]
    public class WebServerController : ControllerBase
    {
		ApplicationDBContext _context;

		private readonly ILogger<WebServerController> _logger;

		public WebServerController(ILogger<WebServerController> logger, ApplicationDBContext context)
		{
			_logger = logger;
			_context = context;
		}

		// Create
		// 서버에서 DB에 뭔갈 추가하길 요청
		/*
		[HttpPost]
        public PlayerData AddPlayerData([FromBody] PlayerData playerData)
        {
            _context.PlayerDatas.Add(playerData);
            _context.SaveChanges();

            return playerData;
        }
        */

		// Read
		// 서버에서 DB를 읽고 데이터를 반환
		// 대부분의 Send-Recv는 서버 컨트롤 혹은 DB 값 변경이니까 이걸로 돌아갈 듯
		// GET이 없으면 사이트도 안 열린다
        /*
		[HttpGet]
        public List<PlayerProfile> GetPlayerDatas()
        {
            List<PlayerProfile> results = _context.PlayerProfileTable.OrderByDescending(item => item.Nickname).ToList();
            return results;
        }

        [HttpGet("{id}")]
		public PlayerProfile GetPlayerDataByUID(ulong uid)
        {
            return _context.PlayerProfileTable.Where(item => item.UID == uid).FirstOrDefault();
        }
        */
		/*
		// foreach를 돌릴 수 있는 형태로 배열 return
        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
         */

		// Update
		// DB의 데이터를 갱신
		/*
        [HttpPut]
        public bool UpdatePlayerData([FromBody] PlayerData playerData)
        {
            var findData = _context.PlayerDatas.Where(x => x.UID == playerData.UID).FirstOrDefault();
            
            if( findData != null) return false;
            
            // 찾은 데이터의 이름과 설명을 갱신
            findData.Nickname = playerData.Nickname;

            _context.SaveChanges();

            return true;
        }
        */

		// Delete
		// 데이터 삭제
		/*
        [HttpDelete("{id}")]
        public bool DeletePlayerData(int uid)
        {
            var findData = _context.PlayerDatas.Where(x => x.UID == uid).FirstOrDefault();

            if (findData != null) return false;

            // 그냥 테스트용. 우리 게임은 회원 탈퇴 따위를 기획하지 않았어요
            // _context.PlayerDatas.Remove(findData);
            _context.SaveChanges();

            return true;
        }
        */
	}
}