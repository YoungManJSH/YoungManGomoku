using UnityEngine;
using UnityEngine.Video;
using DG.Tweening;
using UnityEngine.UI;

public class TitleSceneManager : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource audioSource;
    
    [SerializeField] private GameObject openingVideo;
    [SerializeField] private GameObject gameTitle;
    [SerializeField] private GameObject whiteEffect;
    
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
        videoPlayer.clip = AssetLoadManager.Instance.GetVideoClip("타이틀시네마틱");
        audioSource.clip = AssetLoadManager.Instance.GetAudioClip("whoosh-super-cape-390707");
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
            });
        audioSource.Play();
    }
}
