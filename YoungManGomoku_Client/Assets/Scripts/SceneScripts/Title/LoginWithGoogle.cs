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


    private string imageUrl;
    private bool isGoogleSignInInitialized;

    private void Awake()
    {
        isGoogleSignInInitialized = false;
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

        GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(task =>
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

            auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(authTask =>
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
        string deviceId = SystemInfo.deviceUniqueIdentifier;

        var loginResult = await GetComponent<NetworkManager>().LoginRequest(deviceId);

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

        // 해피 패스
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        var accountRegisterDTO = new CS_AccountRegisterDTO()
        {
            IdToken = deviceId,
            IsGuest = true,
            UserNickname = userNickname,
        };
        
        var registerResult = await GetComponent<NetworkManager>().RegisterAccountRequest(accountRegisterDTO);

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

    private string CheckImageUrl(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            return url;
        }

        return imageUrl;
    }
}