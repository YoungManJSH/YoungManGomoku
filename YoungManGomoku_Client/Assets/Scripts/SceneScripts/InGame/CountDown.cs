using TMPro;
using UnityEngine;

public class CountDown : MonoBehaviour
{
    [SerializeField] private float minScale;
    
    private TextMeshProUGUI countDownText;
    private RectTransform textTransform;

    private void Awake()
    {
        countDownText = GetComponent<TextMeshProUGUI>();
        textTransform = GetComponent<RectTransform>();
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

        GameManager.Instance.StartGame();
    }
    
    private async Awaitable ScaleAnimating(string text)
    {
        countDownText.text = text;

        float second = 0f;
        
        while (second < 1f)
        {
            second += Time.deltaTime;
            
            textTransform.localScale = Mathf.Lerp(minScale, 1f, 1f - Mathf.Cos(second * Mathf.PI)) * Vector3.one;
            await Awaitable.NextFrameAsync();
        }
    }
}
