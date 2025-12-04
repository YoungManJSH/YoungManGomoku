using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyUIController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void MoveSceneToLobby()
    {
        SceneManager.LoadScene("ShopScene");
    }
}
