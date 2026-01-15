using UnityEngine;

/// <summary> 2인용 플레이 착수 제어 클래스 </summary>
public sealed class StoneMoverSingle : StoneMover
{
    private GameObject _blackPreview;
    private GameObject _whitePreview;

    protected override void OnAwake()
    {
        EventManager.Instance.OnGameStart += () => enabled = true;
        OnStoneMove += OnTurnChanged;
    }

    private void OnDisable()
    {
        _blackPreview.SetActive(false);
        _whitePreview.SetActive(false);
    }

    protected override void MessageBoxClosed() => enabled = true;
    
    protected override void CreatePreview()
    {
        _blackPreview = Instantiate(blackStone, BlackParent).gameObject;
        _blackPreview.GetComponent<SpriteRenderer>().color = PreviewColor;
        _blackPreview.name = "Black Preview";
        _blackPreview.SetActive(false);

        _whitePreview = Instantiate(whiteStone, WhiteParent).gameObject;
        _whitePreview.GetComponent<SpriteRenderer>().color = PreviewColor;
        _whitePreview.name = "White Preview";
        _whitePreview.SetActive(false);
    }

    private void OnTurnChanged(bool isBlack)
    {
        NowPreview = isBlack ? _blackPreview : _whitePreview;
#if UNITY_ANDROID
        confirmButton.ButtonImageChange(isBlack);
#endif
    }
}