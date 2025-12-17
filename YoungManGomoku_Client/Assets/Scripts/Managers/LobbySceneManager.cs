using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using YoungManGomoku_Protocol;

public class LobbySceneManager : MonoBehaviour
{
    [SerializeField] private GameObject matchMakePanel;
    
    private PlayerDataFromWebServer playerDataFromWebServer;

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

        if (matchData != null)
        {
            PlayerDataFromWebServer.Instance.CompleteMatchFromWebServer(matchData);
            SceneManager.LoadScene("InGameScene");
        }
    }
    
    public async void CancleMatchMaking()
    {
        matchMakePanel.SetActive(false);
        
        if (playerDataFromWebServer == null)
        {
            return;
        }
        
        await GetComponent<NetworkManager>().CancelMatchingRequest(playerDataFromWebServer.IDToken);
    }
}
