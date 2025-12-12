using System;
using UnityEngine;

public class LobbySceneManager : MonoBehaviour
{
    [SerializeField] private GameObject matchMakePanel;
    [SerializeField] private GameObject menuPanel;

    public void MatchMaking()
    {
        matchMakePanel.SetActive(true);
    }
    
    public void CancleMatchMaking()
    {
        matchMakePanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isActive = menuPanel.activeSelf;
            menuPanel.SetActive(!isActive);
        }
    }
}
