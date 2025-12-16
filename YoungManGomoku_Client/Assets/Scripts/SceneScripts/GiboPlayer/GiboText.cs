using System;
using TMPro;
using UnityEngine;

public class GiboText : MonoBehaviour
{
    [SerializeField] private GiboBoardManager boardManager;
    [SerializeField] private TextMeshProUGUI dateTimeText;
    
    private TextMeshProUGUI _myText;

    private void Awake()
    {
        _myText = GetComponent<TextMeshProUGUI>();
        dateTimeText.text = String.Empty;
        boardManager.OnReadFailed += () => _myText.text = "기보 불러오기 실패";
        boardManager.OnReadSucceed += OnReadSucceed;
        boardManager.OnTurnChanged += OnTurnChanged; 
    }

    private void OnTurnChanged(int turn)
        => _myText.text = $"{turn}수 / {boardManager.LastTurn}수 {boardManager.Result}";

    private void OnReadSucceed()
    {
        OnTurnChanged(0);
        dateTimeText.text = boardManager.GiboDateTime.ToString("yyyy-MM-dd HH:mm");
    }
}
