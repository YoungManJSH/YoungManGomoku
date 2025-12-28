using UnityEngine;

/// <summary> 2인용 플레이 착수 제어 클래스 </summary>
public sealed class StoneMoverSingle : StoneMover
{
    private GameObject _blackPreview;
    private GameObject _whitePreview;

    protected override void OnAwake()
    {
        EventManager.Instance.OnGameStart += () => enabled = true;
        OnStoneMove += isBlack
            => NowPreview = isBlack ? _blackPreview : _whitePreview;
    }

    private void OnDisable()
    {
        _blackPreview.SetActive(false);
        _whitePreview.SetActive(false);
    }
    
    public override void MoveConfirmed()
    {
#if UNITY_ANDROID
        if (_blackPreview.activeSelf)
        {
            _blackPreview.SetActive(false);
            MoveStone(PrevCoord);
        }
        else if (_whitePreview.activeSelf)
        {
            _whitePreview.SetActive(false);
            MoveStone(PrevCoord);
        }
#endif
    }

    protected override void MessageBoxClosed() => enabled = true;
    
    protected override void CreatePreview()
    {
        _blackPreview = Instantiate(blackStone, BlackParent);
        _blackPreview.GetComponent<SpriteRenderer>().color = PreviewColor;
        _blackPreview.name = "Black Preview";
        _blackPreview.SetActive(false);

        _whitePreview = Instantiate(whiteStone, WhiteParent);
        _whitePreview.GetComponent<SpriteRenderer>().color = PreviewColor;
        _whitePreview.name = "White Preview";
        _whitePreview.SetActive(false);
    }
}