using System;
using UnityEngine;

public class LobbySceneManager : MonoBehaviour
{
    public static LobbySceneManager Instance { get; private set; }
    
    [SerializeField] private GameObject matchMakePanel;
    
    private PlayerDataFromWebServer playerDataFromWebServer;

    private bool isMatchMaking;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        isMatchMaking = false;
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
        try
        {
            if (playerDataFromWebServer == null)
            {
                matchMakePanel.SetActive(true);
                return;
            }
            
            matchMakePanel.SetActive(true);
            isMatchMaking = true;

            var matchData =
                await NetworkManager.Instance.RegisterMatchingRequest(playerDataFromWebServer.IDToken);

            if (matchData == null || matchData.MatchingSuccess is false)
            {
                matchMakePanel.SetActive(false);
                return;
            }

            PlayerDataFromWebServer.Instance.CompleteMatchFromWebServer(matchData);
            SceneLoadManager.LoadScene(SceneLoadManager.SceneType.IngameScene).Cancel();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in MatchMaking Request: {e}");
            matchMakePanel.SetActive(false);
        }
    }
    
    public async void CancelMatchMaking()
    {
        if (playerDataFromWebServer == null)
        {
            matchMakePanel.SetActive(false);
            return;
        }

        if (isMatchMaking == false)
        {
            return;
        }
        
        isMatchMaking = false;
        await NetworkManager.Instance.CancelMatchingRequest(playerDataFromWebServer.IDToken);
        
        matchMakePanel.SetActive(false);
    }
}
