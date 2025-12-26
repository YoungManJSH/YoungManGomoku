using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.TypeEnum.InGame;

public class LobbySceneManager : MonoBehaviour
{
    public static LobbySceneManager Instance { get; private set; }
    
    [SerializeField] private GameObject matchMakePanel;
    
    private PlayerDataFromWebServer playerDataFromWebServer;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (PlayerDataFromWebServer.Instance == null)
        {
            return;
        }

        playerDataFromWebServer = PlayerDataFromWebServer.Instance;
    }

    public async void MatchMaking()
    {
        matchMakePanel.SetActive(true);

        if (playerDataFromWebServer == null)
        {
            return;
        }
        
        var matchData = await GetComponent<NetworkManager>().RegisterMatchingRequest(playerDataFromWebServer.IDToken);

        if (matchData.MatchingSuccess is false)
            return;

        if (matchData == null)
        {
            matchMakePanel.SetActive(false);
            return;
        }
        
        PlayerDataFromWebServer.Instance.CompleteMatchFromWebServer(matchData);
        Destroy(SoundManager.instance.gameObject);
        SceneManager.LoadScene("InGameScene");
    }
    
    public async void CancelMatchMaking()
    {
        matchMakePanel.SetActive(false);
        
        if (playerDataFromWebServer == null)
        {
            return;
        }
        
        await GetComponent<NetworkManager>().CancelMatchingRequest(playerDataFromWebServer.IDToken);
    }
}
