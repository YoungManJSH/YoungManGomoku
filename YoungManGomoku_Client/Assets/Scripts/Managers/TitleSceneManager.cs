using UnityEngine;
using UnityEngine.Video;
using DG.Tweening;
using UnityEngine.UI;

public class TitleSceneManager : MonoBehaviour
{
    private const string VIDEO_NAME = "타이틀시네마틱";
    private const string AUDIO_NAME = "whoosh-super-cape-390707";
    
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource audioSource;
    
    [SerializeField] private GameObject openingVideo;
    [SerializeField] private GameObject gameTitle;
    [SerializeField] private GameObject whiteEffect;
    [SerializeField] private GameObject gameStartButton;
    
    private void OnEnable()
    {
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    private void OnDisable()
    {
        videoPlayer.loopPointReached -= OnVideoEnd;
    }
    
    private void Start()
    {
        videoPlayer.clip = AssetLoadManager.Instance.GetVideoClip(VIDEO_NAME);
        audioSource.clip = AssetLoadManager.Instance.GetAudioClip(AUDIO_NAME);
        videoPlayer.Play();
    }

    private void OnVideoEnd(VideoPlayer _)
    {
        openingVideo.SetActive(false);
        gameTitle.SetActive(true);
        whiteEffect.GetComponent<Image>().DOFade(0f, 1.5f).SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                whiteEffect.SetActive(false);
                gameStartButton.SetActive(true);
            });
        audioSource.Play();
    }
}
