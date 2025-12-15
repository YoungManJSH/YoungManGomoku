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
		[HttpPost("Register")]
        public PlayerData RegisterAccountData([FromBody] CS_AccountRegisterDTO registerUserData)
		{
            // 이미 이 ID토큰을 가지고 있는 회원이 있다면 중복 회원가입을 막는다
            if (_context.PlayerAccountTable.Any(x => x.AuthToken == registerUserData.IdToken))
                return null;
            
			PlayerAccount playerAccount = new PlayerAccount(registerUserData.IdToken, registerUserData.UserNickname);
            playerAccount.AuthToken = registerUserData.IdToken;
			_context.PlayerAccountTable.Add(playerAccount);
            _context.SaveChanges();
            //ServerManager.playerAccount.TryAdd(playerAccount.UID, playerAccount);
            //ServerManager.PlayerRecords.TryAdd(playerRecord.UID, playerRecord);

            return ServerManager.GetPlayerData(playerAccount.UID);
		}

        // Login이 Get이면 토큰이 URL에 노출되서 보안상 위험하지 않을까?
        [HttpPost("Login")] 
        public PlayerData LoginGuestAccountData([FromBody] string idToken)
        {
            PlayerAccount findProfile = _context.PlayerAccountTable.Where(x => x.AuthToken == idToken).FirstOrDefault();

            if (findProfile == null || findProfile.AuthLevel == AuthLevel.Ban) return null;

            PlayerBattleRecord findRecord = _context.PlayerGomokuBattleRecordTable.Where(x => x.UID == findProfile.UID).FirstOrDefault();

            // 생성 한 후 DB Context Change로 쿼리를 날려야 해서 오래 걸린다.
            // 애초에 여기 들어오면 사실상 Assert이긴 하다.
            // 그런데 DB측에서 독단적 테스트로 DB에 행 값을 만들다 말았을 수도 있어서 Assert는 위험하니 return null로 방어적 코딩
            if (findRecord == null) return null;
			

            findProfile.LastLoginDate = DateTime.Now;
            //ServerManager.PlayerProfiles.TryAdd(findProfile.UID, findProfile);
            //ServerManager.PlayerRecords.TryAdd(findRecord.UID, findRecord);

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
            //foreach (PlayerAccount profile in ServerManager.PlayerDatas.Values.Take(size).ToArray()) 
            {
                //datas[i++] = ServerManager.GetPlayerData(PlayerAccount.UID);
            }

            return datas;
        }
    }
}
