using UnityEngine;
using DG.Tweening;

public class RotateSearchingImage : MonoBehaviour
{
    private Sequence seq;
    
    private void OnEnable()
    {
        seq = DOTween.Sequence();

        seq.Append(transform.DORotate(new Vector3(0, 0, -180), 0.8f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear));

        seq.AppendInterval(0.5f); // 루프마다 딜레이

        seq.SetLoops(-1, LoopType.Restart);
    }

    private void OnDisable()
    {
        if (seq != null && seq.IsActive())
        {
            seq.Kill();
        }
        
        transform.localRotation = Quaternion.identity;
    }
}
