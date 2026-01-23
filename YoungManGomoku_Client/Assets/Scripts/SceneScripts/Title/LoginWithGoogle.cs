using System.Text.RegularExpressions;
using Firebase.Extensions;
using Google;
using UnityEngine;
using TMPro;
using Firebase.Auth;
using YoungManGomoku_Protocol.ClientToServer;

public class LoginWithGoogle : MonoBehaviour
{
    [Header("Google API")]
    private string googleAPI = "73345955248-rkt1qojvprclr1g2es3p878bv6d0c7tc.apps.googleusercontent.com";

    [Header("Firebase Auth")] 
    private FirebaseAuth auth;
    private FirebaseUser user;

    [Header("PC Register New User")] 
    private const int maxNicknameLength = 7;
    
    [SerializeField] private GameObject PCRegisterUI;

    [SerializeField] private TMP_InputField userNicknameInputField;
    [SerializeField] private TextMeshProUGUI nicknameWarningText;
    [SerializeField] private GameObject disconnectWarning;
    private Regex nicknameRegex = new Regex("^[a-zA-Z0-9가-힣_-]+$");
    
    [Header("Platform Checker")]
    [SerializeField] private TextMeshProUGUI statusCheckText;


    private string imageUrl;
    private bool isGoogleSignInInitialized;
    private bool isLoginProcessing;
    private bool isRegisterProcessing;

    private void Awake()
    {
        isGoogleSignInInitialized = false;
        isLoginProcessing = false;
    }

    private void Start()
    {
        InitFirebase();
    }

    private void InitFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
    }

    public async void Login()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!isGoogleSignInInitialized)
        {
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                RequestIdToken = true,
                WebClientId = googleAPI,
                RequestEmail = true
            };
            
            isGoogleSignInInitialized = true;
        }
        
        await GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogWarning("Google sign-in was canceled.");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.LogError("Google sign-in encountered an error: " + task.Exception);
                return;
            }
            

            GoogleSignInUser googleUser = task.Result;

            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

            auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread( async authTask =>
            {
                if (authTask.IsCanceled)
                {
                    Debug.LogWarning("Firebase auth was canceled.");
                    return;
                }

                if (authTask.IsFaulted)
                {
                    Debug.LogError("Firebase auth failed: " + authTask.Exception);
                    return;
                }
                
                user = auth.CurrentUser;
                
                // 파이어베이스 인증 해피패스
                isLoginProcessing = true;
                var loginResult = await GetComponent<NetworkManager>().LoginRequest(user.UserId);
                isLoginProcessing = false;
        
                if (loginResult == null)
                {
                    var accountRegisterDTO = new CS_AccountRegisterDTO()
                    {
                        IdToken = user.UserId,
                        IsGuest = false,
                        UserNickname = user.DisplayName,
                    };
                    
                    isRegisterProcessing = true;
                    var registerResult = await GetComponent<NetworkManager>().RegisterAccountRequest(accountRegisterDTO);
                    isRegisterProcessing = false;
        
                    if (registerResult == null)
                    {
                        disconnectWarning.SetActive(true);
                    }
                    else
                    {
                        PlayerDataFromWebServer.Instance.SetIDToken(user.UserId);
                        PlayerDataFromWebServer.Instance.CompleteLoginFromWebServer(registerResult);
                        ShopDataManager.Instance.InitializeShopData(await GetComponent<NetworkManager>().RequestShopData(user.UserId));
                        MoveToLobbyScene();
                    }
                }
                else
                {
                    // 로그인 성공!
                    PlayerDataFromWebServer.Instance.SetIDToken(user.UserId);
                    PlayerDataFromWebServer.Instance.CompleteLoginFromWebServer(loginResult);
                    ShopDataManager.Instance.InitializeShopData(await GetComponent<NetworkManager>().RequestShopData(user.UserId));
                    MoveToLobbyScene();
                }

            });
        });

#elif UNITY_STANDALONE || UNITY_EDITOR
        // PC로 접속하면, 게스트로 회원가입, 로그인을 진행함
        // 유니티에서 제공하는 OS별 개별 데이터를 키로 사용하여 DB에 로그인하고 회원가입하도록 동작을 만듦.
        if (isLoginProcessing == true)
        {
            return;
        }
        
        string deviceId = SystemInfo.deviceUniqueIdentifier;

        isLoginProcessing = true;
        var loginResult = await GetComponent<NetworkManager>().LoginRequest(deviceId);
        isLoginProcessing = false;
        
        if (loginResult == null)
        {
            PCRegisterUI.SetActive(true);
        }
        else
        {
            PlayerDataFromWebServer.Instance.SetIDToken(deviceId);
            PlayerDataFromWebServer.Instance.CompleteLoginFromWebServer(loginResult);
            
            // 서버로 상점 데이터 요청 및 대기
            ShopDataManager.Instance.InitializeShopData(await GetComponent<NetworkManager>().RequestShopData(deviceId));
            
            MoveToLobbyScene();
        }
#endif
    }

    // pc 환경에서만 실행되는 코드
    // 닉네임을 입력받고 해당 닉네임을 웹통신으로 보내준다.
    public async void RegisterNewUser()
    {
        string userNickname = userNicknameInputField.text;

        // 닉네임 형식 체크
        if (nicknameRegex.IsMatch(userNickname) == false)
        {
            nicknameWarningText.gameObject.SetActive(true);
            nicknameWarningText.text = "형식에 맞지 않은 닉네임입니다.";
            return;
        }

        // 닉네임 길이 체크
        if (userNickname.Length >= maxNicknameLength)
        {
            nicknameWarningText.gameObject.SetActive(true);
            nicknameWarningText.text = $"닉네임 길이가 너무 깁니다. 최대 {maxNicknameLength - 1}자";
            return;
        }

        // 등록이 진행중인지 체크
        if (isRegisterProcessing == true)
        {
            return;
        }

        // 해피 패스
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        var accountRegisterDTO = new CS_AccountRegisterDTO()
        {
            IdToken = deviceId,
            IsGuest = true,
            UserNickname = userNickname,
        };
        
        isRegisterProcessing = true;
        var registerResult = await GetComponent<NetworkManager>().RegisterAccountRequest(accountRegisterDTO);
        isRegisterProcessing = false;
        
        if (registerResult == null)
        {
            disconnectWarning.SetActive(true);
        }
        else
        {
            PlayerDataFromWebServer.Instance.SetIDToken(deviceId);
            PlayerDataFromWebServer.Instance.CompleteLoginFromWebServer(registerResult);
            
            // 서버로 상점 데이터 요청 및 대기
            ShopDataManager.Instance.InitializeShopData(await GetComponent<NetworkManager>().RequestShopData(deviceId));
            
            MoveToLobbyScene();
        }
    }

    private void MoveToLobbyScene()
        => SceneLoadManager.LoadScene(SceneLoadManager.SceneType.LobbyScene).Cancel();
}