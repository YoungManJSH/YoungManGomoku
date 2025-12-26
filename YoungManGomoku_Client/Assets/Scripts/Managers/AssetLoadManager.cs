using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Video;

public class AssetLoadManager : MonoBehaviour
{
    public static AssetLoadManager Instance { get; private set; }
    
    [SerializeField] private List<AssetReference> audioReferences;
    [SerializeField] private List<AssetReference> videoReferences;
    [SerializeField] private List<AssetReference> spriteReferences;


    private Dictionary<string, AudioClip> audioLibrary;
    private Dictionary<string, VideoClip> videoLibrary;
    private Dictionary<string, Sprite> spriteLibrary;

    private void Awake()
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
        
        audioLibrary = new Dictionary<string, AudioClip>();
        videoLibrary = new Dictionary<string, VideoClip>();
        spriteLibrary = new Dictionary<string, Sprite>();
    }

    /// <summary>
    /// 
    /// </summary>
    public async Task LoadAssetsAsync()
    {
        audioLibrary.Clear();
        videoLibrary.Clear();
        spriteLibrary.Clear();

        // Audio
        foreach (var reference in audioReferences)
        {
            var handle = reference.LoadAssetAsync<AudioClip>();
            await handle.Task;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                audioLibrary[handle.Result.name] = handle.Result;
            }
        }

        // Video
        foreach (var reference in videoReferences)
        {
            var handle = reference.LoadAssetAsync<VideoClip>();
            await handle.Task;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                videoLibrary[handle.Result.name] = handle.Result;
            }
        }

        // Sprite
        foreach (var reference in spriteReferences)
        {
            var handle = reference.LoadAssetAsync<Sprite>();
            await handle.Task;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                spriteLibrary[handle.Result.name] = handle.Result;
            }
        }
    }


    
    public AudioClip GetAudioClip(string key)
        =>  audioLibrary[key];
    
    public VideoClip GetVideoClip(string key)
        =>  videoLibrary[key];
    
    public Sprite GetSprite(string key)
        =>  spriteLibrary[key];
}
