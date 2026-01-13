using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoadManager
{
    public enum SceneType
    {
        LoadingScene, TitleScene,
        LobbyScene, ShopScene,
        IngameScene, GiboPlayScene
    }

    private static readonly Dictionary<SceneType, string> SceneMap;

    static SceneLoadManager()
    {
        SceneMap = new Dictionary<SceneType, string>()
        {
            {SceneType.LoadingScene, "LoadingScene"},
            {SceneType.IngameScene, "IngameScene"},
            {SceneType.GiboPlayScene, "GiboPlayScene"},
        };
        
#if UNITY_STANDALONE || UNITY_EDITOR
        SceneMap.Add(SceneType.TitleScene, "TitleScene - PC");
        SceneMap.Add(SceneType.LobbyScene, "LobbyScene - PC");
        SceneMap.Add(SceneType.ShopScene, "ShopScene - PC");
#elif UNITY_ANDROID
        SceneMap.Add(SceneType.TitleScene, "TitleScene - Android");
        SceneMap.Add(SceneType.LobbyScene, "LobbyScene - Android");
        SceneMap.Add(SceneType.ShopScene, "ShopScene - Android");
#endif
    }

    /// <summary>지정한 씬을 seconds 후 로드, seconds 미입력 시 즉시 로드</summary>
    /// <param name="sceneType">로드할 씬의 종류</param>
    /// <param name="seconds">대기 시간, 미입력하거나 음수이면 즉시 로드</param>
    public static async Awaitable LoadScene(SceneType sceneType, float seconds = 0f)
    {
        try
        {
            // 0 이하일 때 동기 실행을 보장하기 위한 조건문
            if (seconds > 0f)
                await Awaitable.WaitForSecondsAsync(seconds);

            SceneManager.LoadScene(SceneMap[sceneType]);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in Load Scene: {e}");
        }
    }

    /// <summary>seconds 후 클라이언트를 종료, seconds 미입력 시 즉시 종료</summary>
    /// <param name="seconds">대기 시간, 미입력하거나 음수이면 즉시 종료</param>
    public static async Awaitable CloseGame(float seconds = 0f)
    {
        try
        {
            if (seconds > 0f)
                await Awaitable.WaitForSecondsAsync(seconds);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in Closing Game: {e}");
        }
    }
}
