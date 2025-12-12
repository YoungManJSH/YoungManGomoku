using Firebase.Auth;
using Google;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ClientToServer;
using YoungManGomoku_Protocol.Source;

// 로그인할 때 인증 토큰(UID같은거)을 보냄
// 게임 시작 시점과 게임 결과 시점 등 요청할 때마다 토큰을 같이 보내서 인증
// 웹서버라서 매 요청마다 토큰이 필요함

public class NetworkManager : MonoBehaviour
{
	// 나중에 바꿀 예정
	[SerializeField] private const string baseURL = "https://localhost:44331";

    // Server로 무언가의 요청을 했을 때 Connection Error 등 여러 사유로 요청 실패시 호출되는 이벤트
    public event Action OnRequestFailed;


    // static singletone class로 만들고 싶다면 awake 함수 파서 만들면 됨

    // GoogleSignInUser는 구글 어카운트 정보가 다 들어 있어서 무겁다.
    // 따라서 Json으로 변환하면 string이 무지막지하게 길어질 것이다.
    // -> 꼭 필요한 데이터 string IdToken, NickName 2가지만 DTO로 빼서 넘겨주도록 하자.

    /// <summary>
    /// CS_AccountRegisterDTO(회원가입을 위해 필요한 데이터)를 조립해서 웹 서버로 회원 가입 요청
    /// </summary>
    /// <param name="registerUserDTO"> 
    /// 회원 가입을 위한 데이터 : 유저 닉네임, ID Token(인증서), guest 여부. 
    /// 구글 계정인 경우 GoogleSignInUser의 IdToken과 DisplayName 사용
    /// </param>
    /// <returns>
    /// null : 이미 존재하는 ID Token이라 계정 등록 실패
    /// not null : 회원가입 성공, 계정 정보 받아옴
    /// </returns>
    public async Awaitable<PlayerData> RegisterAccountRequest(CS_AccountRegisterDTO registerUserDTO)
	{
		if (registerUserDTO == null)
		{
			Debug.Log($"Unknow User!");
			return null;
		}
        return await RequestPostServer<PlayerData>("/Account/Register", JsonUtility.ToJson(registerUserDTO), "Register Success");
    }

	/// <summary>
	/// 웹 서버로 해당하는 ID Token의 데이터에 따라 로그인 요청
	/// </summary>
	/// <param name="idToken"> 계정 인증용 ID Token. 구글 계정인 경우 GoogleSignInUser.IdToken 사용 </param>
	/// <returns>
	/// null : 해당하는 ID Token에 맞는 계정 탐색에 실패 (계정이 없음, 회원가입 필요)
	/// not null : 로그인 성공, 해당 계정 플레이어 데이터를 return
	/// </returns>
	public async Awaitable<PlayerData> LoginRequest(string idToken)
        => await RequestPostServer<PlayerData>("/Account/Login", $"\"{idToken}\"", "Login Success");

    // 앞으로 네트워크 매니저의 중추를 담당할 함수들. Open되어있지는 않음.
    // API들은 전부 이 함수들을 Wrapping해 사용할 것
    private async Awaitable<RecvData> RequestPostServer<RecvData>(string serverURL, string sendJsonString, string successAnnounce = "=== Request Success! ===")
        => await RequestServer<RecvData>(serverURL, "POST", sendJsonString, successAnnounce);
    
    private async Awaitable<RecvData> RequestServer<RecvData>(string serverURL, string method, string sendJsonString, string successAnnounce = "=== Request Success! ===")
	{
        UnityWebRequest uwr = new UnityWebRequest($"{baseURL}/{serverURL}", method);
        uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(sendJsonString));
        uwr.downloadHandler = new DownloadHandlerBuffer();

        uwr.SetRequestHeader("Content-Type", "application/json");

        await uwr.SendWebRequest();

        if (uwr.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(uwr.error);
            OnRequestFailed?.Invoke();
            return default(RecvData);
        }
        string responseJson = uwr.downloadHandler.text;
        Debug.Log($"{successAnnounce}\n{responseJson}");
		return JsonUtility.FromJson<RecvData>(responseJson);
    }

    // 리팩토링 도중 소멸한 API들
    [Obsolete("이 메서드는 제거되었습니다. RegisterAccountRequest(CS_AccountRegisterDTO registerUserDTO)를 사용하세요.", true)]
    public async Awaitable<PlayerData> GuestAccountRegisterRequest(string idToken) 
        => await RequestPostServer<PlayerData>("/Account/Register", $"\"{idToken}\"", "Register Success");

    [Obsolete("이 메서드는 제거되었습니다. RegisterAccountRequest(CS_AccountRegisterDTO registerUserDTO)를 사용하세요.", true)]
    public async Awaitable<PlayerData> GoogleAccountRegisterRequest(CS_AccountRegisterDTO GoogleLoginUserDTO)
        => await RegisterAccountRequest(GoogleLoginUserDTO);

    [Obsolete("이 메서드는 제거되었습니다. LoginRequest(string idToken)를 사용하세요.", true)]
    public async Awaitable<PlayerData> GuestLoginRequest(string idToken)
        => await RequestPostServer<PlayerData>("/Account/Login", $"\"{idToken}\"", "Login Success");

    [Obsolete("이 메서드는 제거되었습니다. LoginRequest(string idToken)를 사용하세요.", true)]
    public async Awaitable<PlayerData> GoogleLoginRequest(string idToken)
        => await RequestPostServer<PlayerData>("/Account/Login", $"\"{idToken}\"", "Login Success");
}
