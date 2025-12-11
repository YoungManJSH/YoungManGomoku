using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.Data.DatabaseContext;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.Source;
using YoungManGomoku_Protocol.ClientToServer;
using YoungManGomoku_Protocol.Source.TypeEnum;

namespace YoungManGomoku_WebServer.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class AccountController : ControllerBase
	{
		ApplicationDBContext _context;

		private readonly ILogger<AccountController> _logger;

		public AccountController(ILogger<AccountController> logger, ApplicationDBContext context)
		{
			_logger = logger;
			_context = context;
		}

        // POST
        [HttpPost("Guest/Register")]
        public PlayerData RegisterGuestAccountData([FromBody] string idToken)
		{
			PlayerProfile playerProfile = new PlayerProfile("Guest");
            playerProfile.AuthToken = idToken;
			_context.PlayerProfileTable.Add(playerProfile);

			PlayerBattleRecord playerRecord = new PlayerBattleRecord(playerProfile.UID);
			_context.PlayerGomokuRecordTable.Add(playerRecord);

			_context.SaveChanges();

            return ServerManager.GetPlayerData(playerProfile.UID);
        }

		[HttpPost("GoogleAccount/Register")]
        public PlayerData RegisterGoogleAccountData([FromBody] CS_GoogleAccountRegisterDTO googleLoginUserData)
		{
			PlayerProfile playerProfile = new PlayerProfile(googleLoginUserData.UserNickname);
            playerProfile.AuthToken = googleLoginUserData.IdToken;
			_context.PlayerProfileTable.Add(playerProfile);

			PlayerBattleRecord playerRecord = new PlayerBattleRecord(playerProfile.UID);
            _context.PlayerGomokuRecordTable.Add(playerRecord);

            _context.SaveChanges();

			return ServerManager.GetPlayerData(playerProfile.UID);
		}

        // Login이 Get이면 토큰이 URL에 노출되서 보안상 위험하지 않을까?
        [HttpPost("Guest/Login")] 
        public PlayerData LoginGuestAccountData([FromBody] string idToken)
        {
            PlayerProfile findProfile = _context.PlayerProfileTable.Where(x => x.AuthToken == idToken).FirstOrDefault();

            if (findProfile == null || findProfile.AuthLevel == AuthLevel.Ban) return null;

            PlayerBattleRecord findRecord = _context.PlayerGomokuRecordTable.Where(x => x.UID == findProfile.UID).FirstOrDefault();

			if (findRecord == null)
			{
				// 생성 한 후 DB Context Change로 쿼리를 날려야 해서 오래 걸린다.
				// 애초에 여기 들어오면 사실상 Assert인건데 테스트로 DB에 행 값을 만들다 말았을 수도 있음
				// InitPlayerRecord(findProfile.UID);
                return null;
			}

            findProfile.LastLoginDate = DateTime.Now;
            ServerManager.PlayerProfiles.TryAdd(findProfile.UID, findProfile);
            ServerManager.PlayerRecords.TryAdd(findRecord.UID, findRecord);

            _context.SaveChanges();

            return ServerManager.GetPlayerData(findProfile.UID);
        }

        [HttpPost("GoogleAccount/Login")]
        public PlayerData LoginGoogleAccountData([FromBody] string IdToken)
        {
            PlayerProfile findProfile = _context.PlayerProfileTable.Where(x => x.AuthToken == IdToken).FirstOrDefault();

            if (findProfile == null || findProfile.AuthLevel == AuthLevel.Ban) return null;

            PlayerBattleRecord findRecord = _context.PlayerGomokuRecordTable.Where(x => x.UID == findProfile.UID).FirstOrDefault();

            if (findRecord == null)
            {
                // InitPlayerRecord(findProfile.UID);
                return null;
            }

            findProfile.LastLoginDate = DateTime.Now;
			ServerManager.PlayerProfiles.TryAdd(findProfile.UID, findProfile);
			ServerManager.PlayerRecords.TryAdd(findRecord.UID, findRecord);

            _context.SaveChanges();

            return ServerManager.GetPlayerData(findProfile.UID);
        }


        
		[HttpGet]
		public IEnumerable<PlayerData> Get()
        {
            int size = 5;
            PlayerData[] datas = new PlayerData[size];
            

            int i = 0;
            // 안전한 스냅샷?
            // 잡히는거 size개까지 그냥 가져옴
            foreach (PlayerProfile profile in ServerManager.PlayerProfiles.Values.Take(size).ToArray()) 
            {
                datas[i++] = ServerManager.GetPlayerData(profile.UID);
            }

            return datas;
        }
    }
}
