//using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // AsNoTracking()
//using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
//using System.Security.Cryptography.X509Certificates;

using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ClientToServer;

using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.Data.DatabaseContext;
using YoungManGomoku_WebServer.Sessions;
using YoungManGomoku_WebServer.SingletoneManager;

namespace YoungManGomoku_WebServer.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class AccountController : ControllerBase
	{
		private ApplicationDBContext _context;
		private readonly ILogger<AccountController> _logger;
        private readonly ServerManager _serverManager;

        public AccountController(ILogger<AccountController> logger, ApplicationDBContext context, ServerManager serverManager)
		{
			_logger = logger;
			_context = context;
            _serverManager = serverManager;
        }

        // POST
        [HttpPost("Register")]
        public IActionResult RegisterAccount([FromBody] CS_AccountRegisterDTO registerUserData)
        {
            _logger.LogTrace($"[{DateTime.Now}] [Account Controller] Register - ID Token : {registerUserData.IdToken}\nName : {registerUserData.UserNickname} / Guest : {registerUserData.IsGuest}\n");

            /*
            이미 이 ID토큰을 가지고 있는 회원이 있다면 중복 회원가입을 막는다
            수정/삭제 시 Tracker에 등록된 엔티티 캐싱
            앞으로 수정/삭제할 의도가 아니라 테이블 단순 조회 시 AsNoTracking()으로 최적화
            변경 사항을 SaveChanges에서 미반영하고 조회 속도가 상승
            */
            if (_context.PlayerAccountTable.AsNoTracking().Any(x => x.AuthToken == registerUserData.IdToken))
            { 
                return Conflict("Already registered Token.");
            }

			// Create Account
			PlayerAccount playerAccount = new PlayerAccount(_serverManager.GenerateUID64(), registerUserData.IdToken, registerUserData.UserNickname);
            playerAccount.AuthToken = registerUserData.IdToken;

            // Database Insert
			_context.PlayerAccountTable.Add(playerAccount);
            _context.SaveChanges();

            // Register Server Memory Session
            while (_serverManager.PlayerDatas.TryAdd(playerAccount.UID
                , new PlayerSession(_serverManager.GenerateUID64(), playerAccount, isConnect: true))
                == false)
            {
                _logger.LogDebug($"[{DateTime.Now}] [Account Controller] Register : PlayerSession already exists. UID={playerAccount.UID}");

                // 일단 무식하게 제거
                while (_serverManager.PlayerDatas.TryRemove(playerAccount.UID, out PlayerSession findRemoveAccount));
            }

            while (_serverManager.UIDByIDToken.TryAdd(playerAccount.AuthToken, playerAccount.UID) == false)
            {
                _logger.LogDebug($"[{DateTime.Now}] [Account Controller] Register : PlayerSession UID-Token Link already exists. UID={playerAccount.UID} / {playerAccount.AuthToken}");

                // 일단 무식하게 제거
                while (_serverManager.UIDByIDToken.TryRemove(playerAccount.AuthToken, out ulong findRemoveAccountUID));
            }

            return Ok(_serverManager.ComposePlayerData(playerAccount.UID));
		}

        // Login이 Get이면 토큰이 URL에 노출되서 보안상 위험하지 않을까?
        [HttpPost("Login")] 
        public IActionResult LoginAccount([FromBody] string idToken)
        {
            // DB Select Where By Token
            PlayerAccount findAccount = _context.PlayerAccountTable.Where(x => x.AuthToken == idToken)
                                .Include(a => a.Money)
                                .Include(a => a.Status)
                                .Include(a => a.Equip)
                                .Include(a => a.GomokuBattleRecord)
                                .Include(a => a.Inventory)
                                .FirstOrDefault();

            if (findAccount == null)
            {
                _logger.LogTrace($"[{DateTime.Now}] [Account Controller] Login : Account DB Find Fail...");
                return Conflict("Can't Find ID Token. Login Failed!");
            }

            _logger.LogTrace($"[{DateTime.Now}] [Account Controller] Login : Account DB Find Success\nLogin : {_serverManager.UserInfo(findAccount.UID)}");

            // DB Column Update
            findAccount.LastLoginDate = DateTime.Now;

            // DB Process
            _context.PlayerAccountTable.Update(findAccount);
            _context.SaveChanges();

            // 접속이 끊긴 유저의 로그아웃 처리가 제대로 되지 않았었던 것 같다.
            // 메모리에 그대로 남아있네...?
            while (_serverManager.PlayerDatas.TryAdd(findAccount.UID
                , new PlayerSession(_serverManager.GenerateUID64(), findAccount, isConnect: true)) == false)
            {
                _logger.LogDebug($"[{DateTime.Now}] [Account Controller] Login : PlayerSession already exists. UID={findAccount.UID}");

                // 키가 없어서 제거 실패할 때까지 일단 무식하게 제거
                while ( _serverManager.PlayerDatas.TryRemove(findAccount.UID, out PlayerSession findRemoveAccount));
            }

            while (_serverManager.UIDByIDToken.TryAdd(findAccount.AuthToken, findAccount.UID) == false)
            {
                _logger.LogDebug($"[{DateTime.Now}] [Account Controller] Login : PlayerSession UID-Token Link already exists. UID={findAccount.UID} / {findAccount.AuthToken}");
                
                // 일단 무식하게 제거
                while (_serverManager.UIDByIDToken.TryRemove(findAccount.AuthToken, out ulong findRemoveAccountUID));
            }

            return Ok(_serverManager.ComposePlayerData(findAccount.UID));
        }
        
		[HttpGet]
		public IEnumerable<PlayerData?> Get()
        {
            int size = 5;
            
            PlayerData?[] datas = new PlayerData[size];
            
            int i = 0;
            // 안전한 스냅샷?
            // 잡히는거 size개까지 그냥 가져옴
            foreach (PlayerSession session in _serverManager.PlayerDatas.Values.Take(size).ToArray()) 
            {
                datas[i++] = _serverManager.ComposePlayerData(session.Account.UID);
            }
            
            return datas;
        }
    }
}
