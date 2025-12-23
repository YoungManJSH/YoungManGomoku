using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Firebase.Extensions;
using Google;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Firebase.Auth;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ClientToServer;

public class LoginWithGoogle : MonoBehaviour
{
    [Header("Google API")]
    private string googleAPI = "73345955248-rkt1qojvprclr1g2es3p878bv6d0c7tc.apps.googleusercontent.com";

    //private GoogleSignInConfiguration configuration;

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
    [SerializeField] private TextMeshProUGUI platformText;


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
//        platformText.text = "뭘까요?";
#if UNITY_ANDROID && !UNITY_EDITOR || True
        platformText.text = "안드로이드";
        if (!isGoogleSignInInitialized)
        {
            platformText.text = "70줄 까지 실행";
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                RequestIdToken = true,
                WebClientId = googleAPI,
                RequestEmail = true
            };
            
            isGoogleSignInInitialized = true;
        }

        platformText.text = "80줄 까지 실행";
        GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogWarning("Google sign-in was canceled.");
                    platformText.text = "87줄 까지 실행";
                return;
            }

            if (task.IsFaulted)
            {
                Debug.LogError("Google sign-in encountered an error: " + task.Exception);
platformText.text = "94줄 까지 실행";
                return;
            }

            platformText.text = "98줄 까지 실행";

            GoogleSignInUser googleUser = task.Result;

            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

            auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread( async authTask =>
            {
                platformText.text = "106줄 까지 실행";
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
                platformText.text = "118줄 까지 실행";
                user = auth.CurrentUser;
                platformText.text = "완료";
                
                // 파이어베이스 인증 해피패스
                
                platformText.text = "웹서버 통신 시작";
                isLoginProcessing = true;
                var loginResult = await GetComponent<NetworkManager>().LoginRequest(user.UserId);
                isLoginProcessing = false;
        
                if (loginResult == null)
                {
                    platformText.text = "서버에 아이디 없음";
                    var accountRegisterDTO = new CS_AccountRegisterDTO()
                    {
                        IdToken = user.UserId,
                        IsGuest = false,
                        UserNickname = user.DisplayName,
                    };
        
                    platformText.text = "서버에 등록중";
                    isRegisterProcessing = true;
                    var registerResult = await GetComponent<NetworkManager>().RegisterAccountRequest(accountRegisterDTO);
                    isRegisterProcessing = false;
        
                    if (registerResult == null)
                    {
                        platformText.text = "등록 실패";
                        disconnectWarning.SetActive(true);
                    }
                    else
                    {
                        platformText.text = "등록 성공";
                        PlayerDataFromWebServer.Instance.SetIDToken(user.UserId);
                        PlayerDataFromWebServer.Instance.CompleteLoginFromWebServer(registerResult);
                        MoveToLobbyScene();
                    }
                }
                else
                {
                    PlayerDataFromWebServer.Instance.SetIDToken(user.UserId);
                    PlayerDataFromWebServer.Instance.CompleteLoginFromWebServer(loginResult);
                    MoveToLobbyScene();
                    //Debug.Log("로그인 성공!");
                }

        
                //username.text = user.DisplayName;
                //userEmail.text = user.Email;

                //loginPanel.SetActive(false);
                //userPanel.SetActive(true);

                //StartCoroutine(LoadImage(CheckImageUrl(user.PhotoUrl?.ToString())));
                //var result = await GetComponent<NetworkManager>().GoogleAccountRegisterRequest(deviceId);
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
            MoveToLobbyScene();
            //Debug.Log("로그인 성공!");
        }
#endif
    }

    // pc 환경에서만 실행되는 코드
    // 닉네임을 입력받고 해당 닉네임을 웹통신으로 보내준다.
    // TODO : 새로 만들어질 DTO를 이용해서 데이터 담아서 보내기
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
            MoveToLobbyScene();
        }
    }

    private void MoveToLobbyScene()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SceneManager.LoadScene("LobbyScene - Android");
#elif UNITY_STANDALONE || UNITY_EDITOR
        SceneManager.LoadScene("LobbyScene - PC");
#endif
    }
}