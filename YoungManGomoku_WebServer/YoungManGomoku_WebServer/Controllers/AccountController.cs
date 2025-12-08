using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using YoungManGomoku_Protocol.Source;
using YoungManGomoku_Protocol.Source.TypeEnum;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.Data.DatabaseContext;

namespace YoungManGomoku_WebServer.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class AccountController : ControllerBase
	{
		UIDGenerator uidGenerator;

		ApplicationDBContext _context;

		private readonly ILogger<AccountController> _logger;

		public AccountController(ILogger<AccountController> logger, ApplicationDBContext context)
		{
			_logger = logger;
			_context = context;
		}

		private PlayerProfile InitPlayer(string nickname)
		{
			PlayerProfile playerData = new PlayerProfile();
			playerData.UID = ServerManager.GenerateUID64();
			playerData.AuthToken = nickname;
			playerData.Nickname = nickname;

			playerData.Rating = 1000;
			playerData.Level = 1;
			playerData.ExperiencePoint = 0;
			playerData.MaxExperiencePoint = 100;
			playerData.AuthLevel = AuthLevel.Common;

			playerData.GameMoney = 0;
			playerData.CashMoney = 0;

			playerData.EquipProfile = ProfileImageType.None;
			playerData.EquipStoneSkin = StoneSkinType.None;
			playerData.EquipBoardSkin = BoardSkinType.None;

			playerData.RegisterDate = DateTime.Now;
            playerData.LastLoginDate = DateTime.Now;

            return playerData;
		}

        private PlayerBattleRecord InitPlayerRecord(ulong UID)
		{
            PlayerBattleRecord playerRecord = new PlayerBattleRecord();

			playerRecord.UID = UID;
            playerRecord.WinCount = 0;
			playerRecord.DrawCount = 0;
			playerRecord.LoseCount = 0;

            return playerRecord;
        }


        // POST
        [HttpPost("Guest/Register")]
		internal PlayerProfile RegisterGuestAccountData([FromBody] string idToken)
		{
			// 참고용 코드, 제거 예정
			/*
			const { idToken } = JSON.parse(event.body);

			// Firebase Admin SDK로 토큰 검증
			const decodedToken = await admin.auth().verifyIdToken(idToken);
			const { uid, email, firebase } = decodedToken;
		   
		    // DynamoDB에서 기존 사용자 조회
		    const existingUser = await getUserByUID(uid);
		    
		    if (existingUser) 
			{
				// Account Linking: 익명 → 회원 전환
				if (!existingUser.email && email) 
				{
					await updateUserToMember(uid, email);
					return 
					{ 
						success: true, 
						isUpgrade: true,
						message: "기존 JWT 토큰으로 계속 사용 가능합니다"
					};
				}
		    } 
			else
			{
				// 신규 사용자 생성
				await createNewUser(uid, email || null);
			}
		
			// JWT 발급 (익명/회원 구분하지 않음)
			const jwt = generateJWT({ uid, email, type: email ? 'user' : 'anonymous' });
		
			return { success: true, jwt, isNewUser: !existingUser };
			*/

			// 파이어베이스 인증 예제
			/*
			 using FirebaseAdmin.Auth;

			[HttpPost]
			public async Task<IActionResult> VerifyUser([FromBody] TokenRequest request)
			{
			    try
			    {
			        FirebaseToken decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken);
			
			        string uid = decoded.Uid;      // Firebase Auth UID
			        var provider = decoded.Claims["firebase"]?["sign_in_provider"]; // anonymous / google.com
			
			        return Ok(new { uid, provider });
			    }
			    catch (Exception ex)
			    {
			        return Unauthorized("Invalid ID Token: " + ex.Message);
			    }
			}
			
			public class TokenRequest
			{
			    public string IdToken { get; set; }
			}

			 */

			PlayerProfile playerData = InitPlayer("Guest");
			playerData.AuthToken = idToken;
			_context.PlayerProfileTable.Add(playerData);


			PlayerBattleRecord playerRecord = InitPlayerRecord(playerData.UID);
			_context.PlayerGomokuRecordTable.Add(playerRecord);


			_context.SaveChanges();

            return playerData;
		}

		[HttpPost("GoogleAccount/Register")]		
		internal PlayerProfile RegisterGoogleAccountData([FromBody] CS_GoogleAccountRegisterDTO googleLoginUserData)
		{
			PlayerProfile playerData = InitPlayer(googleLoginUserData.UserNickname);
            playerData.AuthToken = googleLoginUserData.IdToken;
			_context.PlayerProfileTable.Add(playerData);


			PlayerBattleRecord playerRecord = InitPlayerRecord(playerData.UID);
            _context.PlayerGomokuRecordTable.Add(playerRecord);


            _context.SaveChanges();
			
			return playerData;
		}

        [HttpPost("Guest/Login")]
        internal bool LoginGuestAccountData([FromBody] string idToken)
        {
            PlayerProfile findProfile = _context.PlayerProfileTable.Where(x => x.AuthToken == idToken).FirstOrDefault();

            if (findProfile == null) return false;

            PlayerBattleRecord findRecord = _context.PlayerGomokuRecordTable.Where(x => x.UID == findProfile.UID).FirstOrDefault();

			if (findRecord == null)
			{
				// 생성 한 후 DB Context Change로 쿼리를 날려야 해서 오래 걸린다.
				// 애초에 여기 들어오면 사실상 Assert인건데 테스트로 DB에 행 값을 만들다 말았을 수도 있음
				// 그래서 그냥 false return
				// InitPlayerRecord(findProfile.UID);
                return false;
			}


            findProfile.LastLoginDate = DateTime.Now;
            ServerManager.PlayerProfiles.TryAdd(findProfile.UID, findProfile);
            ServerManager.PlayerRecords.TryAdd(findRecord.UID, findRecord);

            _context.SaveChanges();

            return true;
        }

        [HttpPost("GoogleAccount/Login")]
        internal bool LoginGoogleAccountData([FromBody] string IdToken)
        {
            PlayerProfile findProfile = _context.PlayerProfileTable.Where(x => x.AuthToken == IdToken).FirstOrDefault();

            if (findProfile == null) return false;

            PlayerBattleRecord findRecord = _context.PlayerGomokuRecordTable.Where(x => x.UID == findProfile.UID).FirstOrDefault();

            if (findRecord == null)
            {
                // InitPlayerRecord(findProfile.UID);
                return false;
            }

            findProfile.LastLoginDate = DateTime.Now;
			ServerManager.PlayerProfiles.TryAdd(findProfile.UID, findProfile);
			ServerManager.PlayerRecords.TryAdd(findRecord.UID, findRecord);

            _context.SaveChanges();

            return true;
        }

		[HttpGet]
        internal List<PlayerProfile> GetPlayerDatas()
        {
            List<PlayerProfile> results = _context.PlayerProfileTable.OrderByDescending(item => item.UID).ToList();
            return results;
        }

    }
}
