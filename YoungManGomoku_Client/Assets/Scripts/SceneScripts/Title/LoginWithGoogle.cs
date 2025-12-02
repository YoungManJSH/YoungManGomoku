using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;
using Google;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Firebase.Auth;
using UnityEngine.UI;
using UnityEngine.Networking;

public class LoginWithGoogle : MonoBehaviour
{
    [Header("Google API")]
    private string googleAPI = "73345955248-rkt1qojvprclr1g2es3p878bv6d0c7tc.apps.googleusercontent.com";

    //private GoogleSignInConfiguration configuration;

    [Header("Firebase Auth")] 
    private FirebaseAuth auth;
    private FirebaseUser user;

    [Header("UI References")] 
    [SerializeField] private TextMeshProUGUI username;

    [SerializeField] private TextMeshProUGUI userEmail;

    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject userPanel;
    [SerializeField] private Image userProfilePic;

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

    public void Login()
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

                username.text = user.DisplayName;
                userEmail.text = user.Email;

                loginPanel.SetActive(false);
                userPanel.SetActive(true);

                StartCoroutine(LoadImage(CheckImageUrl(user.PhotoUrl?.ToString())));
            });
        });

#elif UNITY_STANDALONE || UNITY_EDITOR
        //Debug.Log("pc환경입니다. 익명 로그인 실행");

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

    private IEnumerator LoadImage(string imageUri)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUri);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            userProfilePic.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
            Debug.Log("Image loaded successfully.");
        }
        else
        {
            Debug.LogError("Error loading profile image: " + www.error);
        }
    }

    // User SignOut From Firebase First Then again Sign IN With Google
    public void SignOut()
    {
        GoogleSignIn.DefaultInstance.SignOut();
        loginPanel.SetActive(true);
        userPanel.SetActive(false);
    }
}