using System;
using UnityEngine;

public class LobbySceneManager : MonoBehaviour
{
    [SerializeField] private GameObject matchMakePanel;

    public void MatchMaking()
    {
        matchMakePanel.SetActive(true);
    }
    
    public void CancleMatchMaking()
    {
        matchMakePanel.SetActive(false);
    }
}
