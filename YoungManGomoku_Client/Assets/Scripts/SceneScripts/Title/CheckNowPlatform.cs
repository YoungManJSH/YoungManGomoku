using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckNowPlatform : MonoBehaviour
{
    // 게임 시작 시 현재 플랫폼에 대응하는 씬으로 전환
    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SceneManager.LoadScene("TitleScene - Android");
#elif UNITY_STANDALONE || UNITY_EDITOR
        SceneManager.LoadScene("TitleScene - PC");
#endif
    }
}
