using Firebase.Auth;
using Google;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using YoungManGomoku_Protocol.Source;

// 로그인할 때 인증 토큰(UID같은거)을 보냄
// 게임 시작 시점과 게임 결과 시점 등 요청할 때마다 토큰을 같이 보내서 인증
// 웹서버라서 매 요청마다 토큰이 필요함

public class NetworkManager : MonoBehaviour
{
	// 나중에 바꿀 예정
	[SerializeField] private const string baseURL = "https://localhost:44331/api";
	
	// static singletone class로 만들고 싶다면 awake 함수 파서 만들면 되는데 일단 상의부터

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
		// 사용예시
		/*
		PlayerData res = new PlayerData();
		res.UID = 123;
		res.Nickname = "KCG";

		SendRequest("ranking", "POST", res, (uwr) =>
		{
			// SendRequest의 결과에 따라 처리할 일을 지정
			// TODO
			// 람다로 작성하든 본인이 함수 작성해서 집어넣든 비우든 필요에 따라 알아서 하시오
		}).Cancel();
		*/
	}

	// Update is called once per frame
	void Update()
	{

	}

    // Network Thread 따로 파서 Monobehavior와 Awaitable 없이 돌릴까 고민해봤는데,
    // 일단 클라이언트 팀원들이 이쪽이 익숙할 것 같아서 싱글코어 Awaitable 비동기로 때림

    // 게스트 등록
    // 근데 우리 게스트는 PC판만 쓰기로 합의해서 파이어베이스 게스트는 안 씀
    // 좀 더 지켜봤다가 제거할 수도 있는 코드
    /*
	public async Awaitable GuestRegisterRequest(string token)
	{
		// 파이어베이스로부터 익명 인증 받아봄
		// 분명 더럽게 느릴테니 싹다 await
		AuthResult result = await FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync();
		string idToken = await result.User.TokenAsync(false);

		// 서버로 ID Token 전송
		SendGuestAuthRequest(idToken).Cancel();
	}
	*/

    // POST = Add Data
    // GET = Read Data
    public async Awaitable GuestAccountRegisterRequest(string idToken)
	{
		UnityWebRequest uwr = new UnityWebRequest($"{baseURL}/Account/Guest/Register", "POST");
		uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(idToken));
		uwr.downloadHandler = new DownloadHandlerBuffer();

		await uwr.SendWebRequest();

		if (uwr.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
		{
			Debug.Log(uwr.error);
			return;
		}

		Debug.Log($"Guest Login Success : {uwr.downloadHandler.text}");
	}

    // GoogleSignInUser는 구글 어카운트 정보가 다 들어 있어서 무겁다.
    // 따라서 Json으로 변환하면 string이 무지막지하게 길어질 것이다.
    // -> 꼭 필요한 데이터 string IdToken, NickName 2가지만 DTO로 빼서 넘겨주도록 하자.
    public async Awaitable GoogleAccountRegisterRequest(CS_GoogleAccountRegisterDTO GoogleLoginUserDTO)
	{
		UnityWebRequest uwr = new UnityWebRequest($"{baseURL}/Account/GoogleAccount/Register", "POST");

		if (GoogleLoginUserDTO == null)
		{
			Debug.Log($"Unknow Google Sign User!");
			return;
		}
		
		string jsonStr = JsonUtility.ToJson(GoogleLoginUserDTO);

		uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonStr));
		uwr.downloadHandler = new DownloadHandlerBuffer();

		await uwr.SendWebRequest();

		if (uwr.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
		{
			Debug.Log(uwr.error);
			return;
		}

		Debug.Log($"Google Login Success : {uwr.downloadHandler.text}");
	}

    public async Awaitable<bool> GuestLoginRequest(string idToken)
    {
        UnityWebRequest uwr = new UnityWebRequest($"{baseURL}/Account/Guest/Login", "POST");

        string jsonBody = $"\"{idToken}\"";
        uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        uwr.downloadHandler = new DownloadHandlerBuffer();

        uwr.SetRequestHeader("Content-Type", "application/json");

        await uwr.SendWebRequest();

        if (uwr.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(uwr.error);
            return false;
        }

        // Login 성공 시 true, 실패 시 false를 받아옴
        return bool.Parse(uwr.downloadHandler.text);
    }

    public async Awaitable<bool> GoogleLoginRequest(string idToken)
	{
        UnityWebRequest uwr = new UnityWebRequest($"{baseURL}/Account/GoogleAccount/Login", "POST");

        string jsonBody = $"\"{idToken}\"";
        uwr.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        uwr.downloadHandler = new DownloadHandlerBuffer();

        uwr.SetRequestHeader("Content-Type", "application/json");

        await uwr.SendWebRequest();

        if (uwr.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(uwr.error);
            return false;
        }

        // Login 성공 시 true, 실패 시 false를 받아옴
        return bool.Parse(uwr.downloadHandler.text);
    }


    // json 형태로 표현 가능한 object 변수를 보내고, 어떤 데이터를 요청하고 받아오는 함수 템플릿
    private async Awaitable SendRequest(string url, string method, object sendObj, Action<UnityWebRequest> callback)
	{
		string sendURL = $"{baseURL}/{url}/";

		byte[] jsonBytes = null;

		// 뭘 요청하겠다는 거요?
		if (sendObj != null)
		{
			string jsonStr = JsonUtility.ToJson(sendObj);
			jsonBytes = Encoding.UTF8.GetBytes(jsonStr);
		}

		// 웹서버 요청 데이터 조립
		var uwr = new UnityWebRequest(sendURL, method);
		uwr.uploadHandler = new UploadHandlerRaw(jsonBytes); // 웹서버로 보낼 json 데이터 업로드
		uwr.downloadHandler = new DownloadHandlerBuffer();
		uwr.SetRequestHeader("Content-Type", "application/json");

		// 웹서버에 요청하기
		await uwr.SendWebRequest();

		if (uwr.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
		{
			Debug.Log(uwr.error);
			return;
		}

		// 웹서버의 응답 결과
		// 이걸 잘 써먹어보자, 아니면 콜백에서 관리하던지
		Debug.Log($"Recv Text : {uwr.downloadHandler.text}");
		callback(uwr);
	}

	// 연습용
	/*
	private async Awaitable RecvWebTextureData(string url, string method, object obj)
	{
		string recvURL = $"{baseURL}/{url}/";

		// 텍스쳐로 이미지를 받아봤음
		UnityWebRequest request = UnityWebRequestTexture.GetTexture(recvURL);
		await request.SendWebRequest();

		if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
		{
			Debug.Log($"{recvURL}로부터 Get 실패! : {request.error}");
			return;
		}

		// 데이터를 제대로 받아온 경우의 처리는 여기서

		// ...
		Texture2D img = (request.downloadHandler as DownloadHandlerTexture).texture;
	}
	*/
}
