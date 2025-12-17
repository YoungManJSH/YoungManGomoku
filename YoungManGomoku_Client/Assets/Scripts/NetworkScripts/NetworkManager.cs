using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ClientToServer;
using Newtonsoft.Json; // JsonUtil은 클라 안에서만 쓰세요, 통신에는 너무 구리다

// OnRequestFailed 등록된 함수에 인자로 넘겨주는 접속 에러 정보들
public struct RequestError
{
    public long StatusCode; // HTTP Error Code
    public UnityWebRequest.Result Result;
    public string Message; // 클라에서 발생한 에러 메시지
    public string ResponseBody; // 서버로부터 보내져온 메인 데이터
}

// 로그인할 때 인증 토큰(UID같은거)을 보냄
// 게임 시작 시점과 게임 결과 시점 등 요청할 때마다 토큰을 같이 보내서 인증
// 웹서버라서 매 요청마다 토큰이 필요함

public class NetworkManager : MonoBehaviour
{
	// 나중에 바꿀 예정
	[SerializeField] private const string baseURL = "https://localhost:5001";

    // Server로 무언가의 요청을 했을 때 Connection Error 등 여러 사유로 요청 실패시 호출되는 이벤트
    public event Action<RequestError> OnRequestFailed;

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
    /// <param name="timeOutSeconds">
    /// 웹서버로부터 지정된 시간까지 응답이 없다면 Connection Error를 띄움
    /// 0이나 음수 값 설정 시 Connection Error 없이 무한 응답 대기
    /// </param>
    /// <returns>
    /// null : 이미 존재하는 ID Token이라 계정 등록 실패
    /// not null : 회원가입 성공, 계정 정보 받아옴
    /// </returns>
    public async Awaitable<PlayerData> RegisterAccountRequest(CS_AccountRegisterDTO registerUserDTO, int timeOutSeconds = 0)
	{
		if (registerUserDTO == null)
		{
			Debug.Log($"CS_AccountRegisterDTO : Unknown User!");
			return null;
		}
		// Debug.Log($"Account Register User DTO : {JsonUtility.ToJson(registerUserDTO)}");
		// Debug.Log($"Account Register User DTO : {JsonConvert.SerializeObject(registerUserDTO)}");
		return await RequestPostServer<PlayerData>("Account/Register", JsonConvert.SerializeObject(registerUserDTO), timeOutSeconds, "Account Register Success");
    }

	/// <summary>
	/// 웹 서버로 해당하는 ID Token의 데이터에 따라 로그인 요청
	/// </summary>
	/// <param name="idToken"> 계정 인증용 ID Token. 구글 계정인 경우 GoogleSignInUser.IdToken 사용 </param>
	/// <param name="timeOutSeconds">
	/// 웹서버로부터 지정된 시간까지 응답이 없다면 Connection Error를 띄움
	/// 0이나 음수 값 설정 시 Connection Error 없이 무한 응답 대기
	/// </param>
	/// <returns>
	/// null : 서버 터짐
	/// </returns>
	public async Awaitable<PlayerData> LoginRequest(string idToken, int timeOutSeconds = 0)
        => await RequestPostServer<PlayerData>("Account/Login", $"\"{idToken}\"", timeOutSeconds, "Login Success");

	/// <summary>
	/// 웹 서버에 ID Token으로 매칭 등록
	/// </summary>
	/// <param name="idToken"> 계정 인증용 ID Token. 구글 계정인 경우 GoogleSignInUser.IdToken 사용 </param>
	/// <param name="timeOutSeconds">
	/// 웹서버로부터 지정된 시간까지 응답이 없다면 Connection Error를 띄움
	/// 0이나 음수 값 설정 시 Connection Error 없이 무한 응답 대기
	/// </param>
	/// <returns>
	/// null : 서버 터짐
	/// </returns>
	public async Awaitable<PlayerData> RegisterMatchingRequest(string idToken, int timeOutSeconds = 0)
        => await RequestPostServer<PlayerData>("Matching/Register", $"\"{idToken}\"", timeOutSeconds, "Match Register");


	/// <summary>
	/// 웹 서버에 ID Token으로 등록한 매칭 취소
	/// </summary>
	/// <param name="idToken"> 계정 인증용 ID Token. 구글 계정인 경우 GoogleSignInUser.IdToken 사용 </param>
	/// <param name="timeOutSeconds">
	/// 웹서버로부터 지정된 시간까지 응답이 없다면 Connection Error를 띄움
	/// 0이나 음수 값 설정 시 Connection Error 없이 무한 응답 대기
	/// </param>
	/// <returns>
	/// null : 서버 터짐
	/// </returns>
	public async Awaitable<PlayerData> CancelMatchingRequest(string idToken, int timeOutSeconds = 0)
        => await RequestPostServer<PlayerData>("Matching/Cancel", $"\"{idToken}\"", timeOutSeconds, "Match Cancel");


    // 앞으로 네트워크 매니저의 중추를 담당할 함수들. Open되어있지는 않음.
    // API들은 전부 이 함수들을 Wrapping해 사용할 것
    private async Awaitable<RecvData> RequestPostServer<RecvData>(string serverURL, string sendJsonString, int timeOutSeconds = 0, string successAnnounce = "=== Request Success! ===")
        => await RequestServer<RecvData>(serverURL, "POST", sendJsonString, timeOutSeconds, successAnnounce);

    // 서버가 살았는지 아닌지 테스트하는 용도, 핑을 그냥 던져봄. 문자열 퐁이 돌아올 거임.
    private async Awaitable<RecvData> RequestPing<RecvData>(int timeOutSeconds = 10, string successAnnounce = "=== Ping Pong Success! ===")
        => await RequestServer<RecvData>("Ping", "GET", "\"\"", timeOutSeconds, successAnnounce);

    private async Awaitable<RecvData> RequestServer<RecvData>(string serverURL, string method, string sendJsonString, int timeOutSeconds = 0, string successAnnounce = "=== Request Success! ===")
	{
        UnityWebRequest uwr = new UnityWebRequest($"{baseURL}/{serverURL}", method);
    
        if (string.IsNullOrEmpty(sendJsonString) == false && method != "GET")
        {
			Debug.Log($"Request URL: {baseURL}/{serverURL}\nSend Json Data : {sendJsonString}");
			uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(sendJsonString));
        }
        else
        {
            Debug.Log($"\"Get\" Or Empty Send String : {Encoding.UTF8.GetBytes(sendJsonString).ToString()}");
        }

		uwr.downloadHandler = new DownloadHandlerBuffer();

        uwr.SetRequestHeader("Content-Type", "application/json");

        // timeout 지정된 값 만큼 초를 대기하고, 이 시간초 이후에도 응답 없으면 ConnectionError
        // 0 이하의 값인 경우 timeout 등록을 하지 않고 그냥 무한 응답 대기
        if (timeOutSeconds > 0) uwr.timeout = timeOutSeconds;

        await uwr.SendWebRequest();

        if (uwr.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log($"{uwr.result} URL: {baseURL}/{serverURL}\n{sendJsonString}");

            Debug.LogError($"Error: {uwr.error} / Code: {uwr.responseCode}\nBody: {uwr.downloadHandler.text}");

            OnRequestFailed?.Invoke(new RequestError
            {
                StatusCode = uwr.responseCode,
                Result = uwr.result,
                Message = uwr.error,
                ResponseBody = uwr.downloadHandler.text
            });

            return default(RecvData);
        }

		string responseJson = uwr.downloadHandler.text;
        Debug.Log($"{successAnnounce}\n{responseJson}");

		return JsonConvert.DeserializeObject<RecvData>(responseJson);
		// JsonUtility는 Dictionary와 Property 인식이 불가능하니 주의
		//return JsonUtility.FromJson<RecvData>(responseJson);
    }
}