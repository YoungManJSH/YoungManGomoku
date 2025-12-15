using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyUIController : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject replayPanel;
    [SerializeField] private GameObject matchMakingPanel;

    // 키보드 esc를 누를 때, 메치메이킹 취소 / 리플레이 창 제거 / 메뉴 창 온오프 기능을 넣음.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (matchMakingPanel.activeSelf)
            {
                matchMakingPanel.SetActive(false);
                return;
            }

            if (replayPanel.activeSelf)
            {
                replayPanel.SetActive(false);
                return;
            }

            bool isActive = menuPanel.activeSelf;
            menuPanel.SetActive(!isActive);
        }
    }
    
    public void MoveSceneToShop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SceneManager.LoadScene("ShopScene - Android");
#elif UNITY_STANDALONE || UNITY_EDITOR
        SceneManager.LoadScene("ShopScene - PC");
#endif
    }

    public void OpenCloseReplayPanel()
    {
        bool isActive = replayPanel.activeSelf;
        replayPanel.SetActive(!isActive);
    }
}