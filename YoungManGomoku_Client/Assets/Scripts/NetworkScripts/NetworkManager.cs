using Newtonsoft.Json; // JsonUtil은 클라 안에서만 쓰세요, 통신에는 너무 구리다
using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ClientToServer;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_Protocol.TypeEnum.InGame;

// OnRequestFailed 등록된 함수에 인자로 넘겨주는 접속 에러 정보들
public struct RequestError
{
    /*
    HTTP Error Code 의미
    400 : 잘못된 요청 (클라가 이상하게 보냄)
    401 : 인증 만료 (재로그인) 
    403 : 권한 없음 (접근 차단)
    409 : 상태 충돌 (중복요청 or 이미 진행중인 요청)
    503 : 서버 과부하
     */
    public long StatusCode; // HTTP Error Code
    public UnityWebRequest.Result Result; // 클라서버 통신이 안되면 ConnectionError, 서버에서 배드리퀘스트 혹은 컨플릭 등의 응답이 오면 ProtocolError
    public string Message;      // 에러 메시지 "Bad Request" or "Conflict" 등의 응답
    public string ResponseBody; // 서버로부터 보내져온 메인 데이터 or 배드 리퀘스트나 컨플릭트 등의 응답 시 같이 보내져온 설명 문자열
}

// 모든 유니티 클라이언트에서 보내오는 모든 데이터를 전부 신뢰시킴
// 당연히 보안적으로 개 쓰레기니까 개발 도중 테스트 편이성을 위해서만 잠깐 쓰자
class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true; // 무조건 신뢰
    }
}


/*
로그인할 때 인증 토큰(UID같은거)을 보냄
게임 시작 시점과 게임 결과 시점 등 요청할 때마다 토큰을 같이 보내서 인증
웹서버라서 매 요청마다 토큰이 필요함
*/
public class NetworkManager : MonoBehaviour
{
	/* 나중에 바꿀 예정
	 * "https://localhost:5001" (서버와 클라이언트가 동일 컴퓨터인 경우)
     * 기존값 : "https://192.168.200.156:5001" (학원 자습실 컴퓨터 공유기 로컬망)
     * 강찬구 집 데스크탑 : "https://115.21.221.6:5001"
     * 김재환 집 노트북 : "https://115.126.216.245:5001"
     * 김재환 AWS EC2 인스턴스 : "https://15.164.163.249:5001"
     * */
	[SerializeField] private const string baseURL = "https://192.168.200.156:5001"; //"https://localhost:5001";

    // Server로 무언가의 요청을 했을 때 Connection Error 등 여러 사유로 요청 실패시 호출되는 이벤트
    public event Action<RequestError> OnRequestFailed;

    public static NetworkManager Instance { get; private set; }

    
    // Execution Order -3 : 이 Instance는 가장 먼저 등록돼 있어야 함 
    private void Awake()
    {
	    if (Instance != null) Destroy(Instance);
	    
	    Instance = this;
    }

    private void OnDestroy() => Instance = null;

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
	public async Awaitable<SC_MatchResultDTO> RegisterMatchingRequest(string idToken, int timeOutSeconds = 0)
        => await RequestPostServer<SC_MatchResultDTO>("Matching/Register", $"\"{idToken}\"", timeOutSeconds, "Match Register");


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
	public async Awaitable<SC_ResponseStringDTO> CancelMatchingRequest(string idToken, int timeOutSeconds = 0)
        => await RequestPostServer<SC_ResponseStringDTO>("Matching/Cancel", $"\"{idToken}\"", timeOutSeconds, "Match Cancel");


    /// <summary> 게임 시작 요청 </summary>
	public async Awaitable<SC_ResponseStringDTO> RequestGameStartAnnounce(string idToken, int timeOutSeconds = 0)
	=> await RequestPostServer<SC_ResponseStringDTO>("GomokuIngame/GameStart", $"\"{idToken}\"", timeOutSeconds, "Gomoku Ingame : Game Start");

    
	/// <summary> 타이머 검증 요청, RequestPlaceStone과 함께 보내는 요청 </summary>
	/// <returns>
	/// <para> 클라이언트 타이머 승인: false, 서버 타이머 데이터 혹은 이상 감지값 </para>
	/// <para> 클라이언트 타이머 반려: true, 서버 타이머 데이터 </para>
	/// </returns>
	public async Awaitable<TimerSyncData> RequestTimerSynchro(CS_RequestTimerSynchroDTO requestSynchroDTO, int timeOutSeconds = 0)
		=> await RequestPostServer<TimerSyncData>("GomokuIngame/TimerSynchronize", JsonConvert.SerializeObject(requestSynchroDTO), timeOutSeconds, "Gomoku Ingame : Timer Synchronize Success");
	
	/// <summary> 착수 요청 </summary>
	/// <param name="placeStoneDTO"> IdToken, 착수 위치, 본인 타이머 정보 </param>
	/// <param name="timeOutSeconds"> Connection Error 한계 시간, 0 이하이면 무한 대기 </param>
	/// <returns>
	/// <para> null : 서버 터짐 </para>
	/// <para> not null : 상대방 착수 정보 (롱 폴링으로 송신됨) </para>
	/// </returns>
	public async Awaitable<SC_OpponentPlaceStoneDTO> RequestPlaceStone(CS_PlaceStoneDTO placeStoneDTO, int timeOutSeconds = 0)
	=> await RequestPostServer<SC_OpponentPlaceStoneDTO>("GomokuIngame/PlaceStone", JsonConvert.SerializeObject(placeStoneDTO), timeOutSeconds, "Gomoku Ingame : Place Stone Success");
	
	/// <summary> 내 턴을 진행하고 있는 동안 응답 대기용으로 보낼 요청 </summary>
	/// <returns> 게임 종료 상황 발생 시 해당 enum값 수신, 그밖에는 None </returns>
	public async Awaitable<SC_WaitEventDTO> RequestWaitForEvent(string idToken, int timeOutSeconds = 0)
		=> await RequestPostServer<SC_WaitEventDTO>("GomokuIngame/WaitForEvent", $"\"{idToken}\"", timeOutSeconds, "Gomoku Ingame : Request Wait For Event Success");
	
    // 무르기 요청, 항복, 초읽기 구매
    public async Awaitable<SC_ResponseStringDTO> RequestIngameAction(CS_InGameRequestDTO ingameReqDTO, int timeOutSeconds = 0)
    => await RequestPostServer<SC_ResponseStringDTO>("GomokuIngame/Request", JsonConvert.SerializeObject(ingameReqDTO), timeOutSeconds, "Gomoku Ingame : In Game Request Success");


	/// <summary>
	/// 서버로부터 상대의 무르기 요청 이벤트가 온 경우, 상대의 무르기 요청에 대한 승인이나 거절 여부를 담아 서버로 전송
	/// </summary>
	/// <param name="takebackPermitDTO">  IdToken, 상대의 무르기를 승인할 것인지 거부할 것인지 여부의 bool 변수 </param>
	/// <param name="timeOutSeconds"> Connection Error 한계 시간, 0 이하이면 무한 대기 </param>
	/// <returns> 단순 성공 응답 문자열 </returns>
	public async Awaitable<SC_ResponseStringDTO> RequestTakeBackPermit(CS_TakeBackPermitDTO takebackPermitDTO, int timeOutSeconds = 0)
	=> await RequestPostServer<SC_ResponseStringDTO>("GomokuIngame/TakeBackPermit", JsonConvert.SerializeObject(takebackPermitDTO), timeOutSeconds, "Gomoku Ingame : Send Takeback Permit Success");



	/// <summary> 서버가 응답이 이상하거나 클라가 이상한 등 아무튼 클라의 접속을 끊어버리고 서버의 관리에서 죽여버리고 싶을 때(로그아웃) </summary>
	/// <returns> 게임 종료 상황 발생 시 해당 enum값 수신, 그밖에는 None </returns>
	public async Awaitable<SC_ResponseStringDTO> RequestCloseSession(string idToken, int timeOutSeconds = 0)
		=> await RequestPostServer<SC_ResponseStringDTO>("Session/Close", $"\"{idToken}\"", timeOutSeconds, "Session Close");




	/// <summary>
	/// 웹 서버에 하트 비트 요청 (접속 여부 확인)
	/// 서버에서는 클라의 마지막 요청 시간과 하트비트가 날아온 시간차를 비교해 유효한 연결인지 계산
	/// </summary>
	/// <param name="idToken"> 플레이어의 idToken </param>
	/// <param name="timeOutSeconds"> 대기 한계시간, 이 값을 넘으면 Connection Error, 0 이하면 무한 대기 </param>
	public async Awaitable<string> RequestHeartbeat(string idToken, int timeOutSeconds = 10)
		=> await RequestPostServer<string>("Heartbeat", $"\"{idToken}\"", timeOutSeconds, "=== Heart Beat Success ===");


	// 서버가 살았는지 아닌지 테스트하는 용도의 함수, 핑을 그냥 던져봄. 서버가 살았으면 return으로 문자열 pong이 돌아올 것.
	// 응답만 해주는 테스트용 함수기 때문에 별도의 서버 동작이 이루어지진 않음.
	public async Awaitable<string> RequestPing(int timeOutSeconds = 10, string successAnnounce = "=== Ping Pong Success! ===")
        => await RequestServer<string>("Ping", "GET", "\"\"", timeOutSeconds, successAnnounce);



    // 앞으로 네트워크 매니저의 중추를 담당할 함수들. Open되어있지는 않음.
    // API들은 전부 이 함수들을 Wrapping해 사용할 것
    private async Awaitable<RecvData> RequestPostServer<RecvData>(string serverURL, string sendJsonString, int timeOutSeconds = 0, string successAnnounce = "=== Request Success! ===")
        => await RequestServer<RecvData>(serverURL, "POST", sendJsonString, timeOutSeconds, successAnnounce);


    private async Awaitable<RecvData> RequestServer<RecvData>(string serverURL, string method, string sendJsonString, int timeOutSeconds = 0, string successAnnounce = "=== Request Success! ===")
	{
        UnityWebRequest uwr = new UnityWebRequest($"{baseURL}/{serverURL}", method);

        // Test 단계에서만 잠깐 쓸 코드, 유니티는 전부 신뢰시켜버리는거라 서버 보안적으로 매우 위험
        uwr.certificateHandler = new BypassCertificate();

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
                /*
                uwr.responseCode 값 의미 정리
                400 : 잘못된 요청 (클라가 이상하게 보냄)
                401 : 인증 만료 (재로그인) (서버에 네 ID Token이 로그인되어있지 않음)
                403 : 권한 없음 (접근 차단)
                409 : 상태 충돌 (중복요청 or 이미 진행중인 요청) (ex. 게임중인데 또 게임함, 매칭중인데 또 매칭요청함)
                503 : 서버 과부하
                 */
                StatusCode = uwr.responseCode,
                Result = uwr.result,  // 클라-서버 간 통신이 안 되면 ConnectionError, 서버에서 배드리퀘스트 혹은 컨플릭트 등의 응답이 오면 ProtocolError
                Message = uwr.error,                    // "Bad Request" or "Conflict" or "Time Out" 등의 응답
                ResponseBody = uwr.downloadHandler.text // 서버가 같이 보내온 에러 설명 문자열
            });

            return default(RecvData);
        }

		string responseJson = uwr.downloadHandler.text;
        Debug.Log($"{successAnnounce}\n{responseJson}");

        /*
        Server에서 Ok() 때리고 빈 응답만 보내져 왔을 때...
        Server 작업 자체는 성공해서 반환해줬지만 온 데이터가 비어있는 상황이다.
        이러면 성공 실패 여부를 Http Code로만 판단해야 한다.
        어지간해선 이 코드로는 안 들어 오는 쪽이 좋다.
        */
        if (string.IsNullOrEmpty(responseJson))
        {
            return default(RecvData);
        }

        return JsonConvert.DeserializeObject<RecvData>(responseJson);
		// JsonUtility는 Dictionary와 Property 인식이 불가능하니 주의
		//return JsonUtility.FromJson<RecvData>(responseJson);
    }
}