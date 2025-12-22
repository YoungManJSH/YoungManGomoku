using UnityEngine;
using YoungManGomoku_Protocol.TypeEnum.InGame;

/// <summary> 2인용 플레이 착수 제어 클래스 </summary>
public sealed class StoneMoverSingle : StoneMover
{
    private GameObject _blackPreview;
    private GameObject _whitePreview;

    protected override void OnAwake()
    {
        EventManager.Instance.OnGameStart += () => enabled = true;
        OnStoneMove += isBlack
            => nowPreview = isBlack ? _blackPreview : _whitePreview;
    }

    private void OnDisable()
    {
        _blackPreview.SetActive(false);
        _whitePreview.SetActive(false);
    }
    
    public override void MoveConfirmed()
    {
#if UNITY_ANDROID
        if (isBlackTurn && _blackPreview.activeSelf)
        {
            _blackPreview.SetActive(false);
            MoveStone(prevCoord);
        }
        else if (isBlackTurn is false && _whitePreview.activeSelf)
        {
            _whitePreview.SetActive(false);
            MoveStone(prevCoord);
        }
#endif
    }

    protected override void MessageBoxClosed() => enabled = true;
    
    protected override void CreatePreview()
    {
        _blackPreview = Instantiate(blackStone, blackParent);
        _blackPreview.GetComponent<SpriteRenderer>().color = previewColor;
        _blackPreview.name = "Black Preview";
        _blackPreview.SetActive(false);

        _whitePreview = Instantiate(whiteStone, whiteParent);
        _whitePreview.GetComponent<SpriteRenderer>().color = previewColor;
        _whitePreview.name = "White Preview";
        _whitePreview.SetActive(false);
    }
}