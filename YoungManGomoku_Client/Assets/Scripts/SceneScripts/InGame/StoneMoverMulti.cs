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

    private void OnDisable() => NowPreview.SetActive(false);

    public override void MoveConfirmed()
    {
#if UNITY_ANDROID
        if (NowPreview.activeSelf)
        {
            NowPreview.SetActive(false);
            MoveStone(PrevCoord);
        }
#endif
    }
    
    protected override void MessageBoxClosed() => enabled = _isPlayerTurn;
    
    protected override void CreatePreview()
    {
        NowPreview = GameManager.Instance.IsPlayerBlack ?
            Instantiate(blackStone, BlackParent) : Instantiate(whiteStone, WhiteParent);
        
        NowPreview.GetComponent<SpriteRenderer>().color = PreviewColor;
        NowPreview.name = "Preview";
        NowPreview.SetActive(false);
    }
}
