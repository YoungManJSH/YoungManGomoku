using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography.X509Certificates;
using YoungManGomoku_WebServer.Data;
using YoungManGomoku_WebServer.Data.DatabaseContext;
using YoungManGomoku_Protocol.Source.TypeEnum;

//using Google;
//using Firebase.Auth;

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


			return playerData;
		}

		// POST
		[HttpPut("Register/Guest")]
		internal PlayerProfile CreateGuestAccountData([FromBody] string idToken)
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
			_context.SaveChanges();

			return playerData;
		}

		[HttpPut("Register/GoogleAccount")]
		/*
		internal PlayerProfile CreateGoogleAccountData([FromBody] GoogleSignInUser googleSignInUser)
		{


			PlayerProfile playerData = InitPlayer("google");

			_context.PlayerProfileTable.Add(playerData);
			_context.SaveChanges();

			return playerData;
		}*/
	}
}
