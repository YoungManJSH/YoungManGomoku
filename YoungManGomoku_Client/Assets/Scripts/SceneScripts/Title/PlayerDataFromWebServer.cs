using UnityEngine;
using YoungManGomoku_Protocol;

public class PlayerDataFromWebServer : MonoBehaviour
{
    public static PlayerDataFromWebServer Instance;

    public string IDToken { get; set; }
    public PlayerData PlayerData { get; set; }
    public OpponentPlayerData OpponentPlayerData { get; set; }

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetIDToken(string idToken)
        => IDToken = idToken;
    
    public void CompleteLoginFromWebServer(PlayerData playerData)
        => PlayerData = playerData;

    public void CompleteMatchFromWebServer(OpponentPlayerData opponentPlayerData)
        => OpponentPlayerData = opponentPlayerData;
}