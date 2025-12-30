using DG.Tweening;
using UnityEngine;

public class NicknameWarning : MonoBehaviour
{
    private void OnEnable()
    {
        GetComponent<RectTransform>().DOShakeAnchorPos(
            duration: 1f,          // 흔들리는 총 시간
            strength: new Vector2(20f, 0f), // 좌우 흔들림 강도
            vibrato: 10,           // 진동 횟수
            randomness: 0f,
            fadeOut: true          // 점점 줄어드는 효과
        ).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }
}
