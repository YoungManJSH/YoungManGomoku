using UnityEngine;

/// <summary> 멀티플레이 착수 제어 클래스 </summary>
public class StoneMoverMulti : StoneMover
{
    private bool _isPlayerTurn;

    protected override void OnAwake()
    {
        _isPlayerTurn = GameManager.Instance.IsPlayerBlack;
        OnStoneMove += _ =>
        {
            _isPlayerTurn = !_isPlayerTurn;
            enabled = _isPlayerTurn;
        };
    }

    private void OnDisable() => nowPreview.SetActive(false);

    public override void MoveConfirmed()
    {
#if UNITY_ANDROID
        if (nowPreview.activeSelf)
        {
            nowPreview.SetActive(false);
            MoveStone(prevCoord);
        }
#endif
    }
    
    protected override void MessageBoxClosed() => enabled = _isPlayerTurn;
    
    protected override void CreatePreview()
    {
        nowPreview = GameManager.Instance.IsPlayerBlack ?
            Instantiate(blackStone, blackParent) : Instantiate(whiteStone, whiteParent);
        
        nowPreview.GetComponent<SpriteRenderer>().color = previewColor;
        nowPreview.name = "Preview";
        nowPreview.SetActive(false);
    }
}
