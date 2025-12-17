using System;
using UnityEngine;
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
        
        var data = await GetComponent<NetworkManager>().RegisterMatchingRequest(playerDataFromWebServer.IDToken);
    }
    
    public async void CancleMatchMaking()
    {
        matchMakePanel.SetActive(false);
        
        if (playerDataFromWebServer == null)
        {
            return;
        }
        
        var data = await GetComponent<NetworkManager>().CancelMatchingRequest(playerDataFromWebServer.IDToken);
    }
}
