using System;
using UnityEngine;
using YoungManGomoku_Protocol.TypeEnum.InGame;

/// <summary> 2인용 플레이 착수 제어 클래스 </summary>
public sealed class StoneMoverSingle : StoneMover
{
    public event Action<bool> OnStoneMove;
    
    private GameObject _blackPreview;
    private GameObject _whitePreview;
    private GameObject _nowPreview;

    protected override void OnAwake()
        => EventManager.Instance.OnGameStart += () => enabled = true;

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

    protected override void InputProcessing()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;
        
        if (TryGetBoardCoord(worldPos, out var coord) is false ||
            boardInform[coord.row, coord.col] != StoneColorType.Empty ||
            forbiddenCoords.Contains(coord))
        {
            _nowPreview.SetActive(false);
            prevCoord = coord;
            return;
        }

        if (Input.GetMouseButtonDown(0) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Space))
        {
            _nowPreview.SetActive(false);
            prevCoord = (-1, -1);
            MoveStone(coord);
            return;
        }
            
        if (coord != prevCoord)
        {
            prevCoord = coord;
            UpdatePreview(coord);
        }
        
#elif UNITY_ANDROID
        if (Input.touchCount == 0) return;
        
        Touch touch = Input.GetTouch(0);
        
        if (touch.phase is TouchPhase.Began or TouchPhase.Moved)
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(touch.position);
            worldPos.z = 0;

            if (TryGetBoardCoord(worldPos, out var coord))
            {
                if (coord == prevCoord) return;

                prevCoord = coord;
                
                if (boardInform[coord.row, coord.col] != StoneColorType.Empty ||
                    forbiddenCoords.Contains(coord))
                {
                    _nowPreview.SetActive(false);
                    return;
                }

                UpdatePreview(coord);
            }
        }
#endif
    }

    protected override void MessageBoxClosed() => enabled = true;

    protected override void MoveStone((int row, int col) coord)
    {
        Vector3 position = spriteRenderer.bounds.min + new Vector3(firstLineWorld + cellSizeWorld * coord.col,
            firstLineWorld + cellSizeWorld * (Board.MaxCoord - coord.row), 0f);

        if (boardInform.TryMoveStone(coord.row, coord.col))
        {
            PlaceStone(position);
            audioSource.Play();
            if (isBlackTurn) ClearForbiddenMarks();
            isBlackTurn = boardInform.NowTurn % 2 == 0;
            _nowPreview = isBlackTurn ? _blackPreview : _whitePreview;
            OnStoneMove?.Invoke(isBlackTurn);
            return;
        }

        // 금수로 인한 착수 실패
        PlaceForbiddenMark(position);
        audioSource.PlayOneShot(deniedSound);
        forbiddenCoords.Add(coord);
    }
    
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
    
    protected override void UpdatePreview((int row, int col) coord)
    {
        _nowPreview.transform.position = spriteRenderer.bounds.min +
                                         new Vector3(firstLineWorld + cellSizeWorld * coord.col,
                                            firstLineWorld + cellSizeWorld * (Board.MaxCoord - coord.row), 0f);
        
        _nowPreview.SetActive(true);
    }
}