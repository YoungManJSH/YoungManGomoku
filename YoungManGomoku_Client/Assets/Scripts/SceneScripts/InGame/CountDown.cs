using System;
using TMPro;
using UnityEngine;

public class CountDown : MonoBehaviour
{
    [SerializeField] private float minScale;
    [SerializeField] private AudioClip countSound;
    [SerializeField] private AudioClip startSound;
    
    private TextMeshProUGUI _countDownText;
    private RectTransform _textTransform;
    private AudioSource _audioSource;

    private void Awake()
    {
        _countDownText = GetComponent<TextMeshProUGUI>();
        _textTransform = GetComponent<RectTransform>();
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        StartCountDown().Cancel();
    }

    private async Awaitable StartCountDown()
    {
        for (int count = 3; count > 0; --count)
        {
            await ScaleAnimating(count.ToString(), countSound);
        }
        
        await ScaleAnimating("Start!!", startSound);
        _countDownText.text = String.Empty;
        Destroy(gameObject, 2f);
        
        EventManager.Instance.StartGame();
    }

    private async Awaitable ScaleAnimating(string text, AudioClip clip)
    {
        _countDownText.text = text;
        bool isSoundPlayed = false;

        float second = 0f;
        
        while (second < 1f)
        {
            second += Time.deltaTime;
            
            if (isSoundPlayed is false && second >= 0.2f)
            {
                _audioSource.PlayOneShot(clip);
                isSoundPlayed = true;
            }
            
            _textTransform.localScale = Mathf.Lerp(minScale, 1f, 1f - Mathf.Cos(second * Mathf.PI)) * Vector3.one;
            await Awaitable.NextFrameAsync();
        }
    }
}
