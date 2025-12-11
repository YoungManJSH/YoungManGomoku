using TMPro;
using UnityEngine;

public class CountDown : MonoBehaviour
{
    [SerializeField] private float minScale;
    [SerializeField] private EventManager eventManager;
    
    private TextMeshProUGUI _countDownText;
    private RectTransform _textTransform;

    private void Awake()
    {
        _countDownText = GetComponent<TextMeshProUGUI>();
        _textTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        _ = StartCountDown();
    }

    private async Awaitable StartCountDown()
    {
        for (int count = 3; count > 0; --count)
        {
            await ScaleAnimating(count.ToString());
        }

        await ScaleAnimating("Start!!");
        gameObject.SetActive(false);

        eventManager.StartGame();
    }
    
    private async Awaitable ScaleAnimating(string text)
    {
        _countDownText.text = text;

        float second = 0f;
        
        while (second < 1f)
        {
            second += Time.deltaTime;
            
            _textTransform.localScale = Mathf.Lerp(minScale, 1f, 1f - Mathf.Cos(second * Mathf.PI)) * Vector3.one;
            await Awaitable.NextFrameAsync();
        }
    }
}
