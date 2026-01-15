using System;
using UnityEngine;

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
        try
        {
            matchMakePanel.SetActive(true);

            if (playerDataFromWebServer == null)
            {
                return;
            }

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
    
    public void CancelMatchMaking()
    {
        matchMakePanel.SetActive(false);

        if (playerDataFromWebServer == null)
        {
            return;
        }

        NetworkManager.Instance.CancelMatchingRequest(playerDataFromWebServer.IDToken).Cancel();
    }
}
