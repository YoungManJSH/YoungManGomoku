using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyUIController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void MoveSceneToLobby()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SceneManager.LoadScene("ShopScene - Android");
#elif UNITY_STANDALONE || UNITY_EDITOR
        SceneManager.LoadScene("ShopScene - PC");
#endif
    }
}
