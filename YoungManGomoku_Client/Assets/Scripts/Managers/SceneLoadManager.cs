using System.Collections.Generic;
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
    
    public static void LoadScene(SceneType sceneType)
        => SceneManager.LoadScene(SceneMap[sceneType]);
}
