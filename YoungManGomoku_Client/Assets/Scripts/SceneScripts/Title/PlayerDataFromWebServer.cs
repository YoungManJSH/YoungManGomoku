using UnityEngine;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ServerToClient;

public class PlayerDataFromWebServer : MonoBehaviour
{
    public static PlayerDataFromWebServer Instance;

    public string IDToken { get; set; }
    public PlayerData PlayerData { get; set; }
    
    public SC_MatchResultDTO MatchResultDTO { get; set; }

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

    public void CompleteMatchFromWebServer(SC_MatchResultDTO matchResultDTO)
        => MatchResultDTO = matchResultDTO;
}