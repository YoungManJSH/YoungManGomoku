using System;
using UnityEngine;
using UnityEngine.UI;

public class GiboBoardManager : MonoBehaviour
{
    [SerializeField] private BoardImageData boardData;

    private Image _boardImage;
    private (int row, int col)[] _recordData;
    private DateTime _giboDateTime;
    private BasicPlayerData _blackData;
    private BasicPlayerData _whiteData;
    private string _result;
    
    private void Awake()
    {
        _boardImage = GetComponent<Image>();
        _boardImage.sprite = BoardGenerator.GenerateBoard(boardData);
        
        if (GiboFileManager.TryReadGiboFile(out _recordData, out _giboDateTime,
                out _blackData, out _whiteData, out _result))
        {
            Debug.Log($"기보 날짜 : {_giboDateTime}");
            Debug.Log($"흑인 정보 : {_blackData.name}: {_blackData.win}승 {_blackData.draw}무 {_blackData.lose}패 {_blackData.rating}pt");
            Debug.Log($"백인 정보 : {_whiteData.name}: {_whiteData.win}승 {_whiteData.draw}무 {_whiteData.lose}패 {_whiteData.rating}pt");
            Debug.Log($"{_recordData.Length}수 {_result}");

            bool isBlack = true;
            foreach (var (row, col) in _recordData)
            {
                Debug.Log($"({row},{col}) {(isBlack?'흑':'백')} 착수");
                isBlack = !isBlack;
            }
        }
        else
        {
            Debug.LogWarning("기보 파일 읽기 실패");
        }
    }
}
