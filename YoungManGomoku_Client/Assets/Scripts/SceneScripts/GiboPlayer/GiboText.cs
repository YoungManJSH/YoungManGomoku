using TMPro;
using UnityEngine;

public class GiboText : MonoBehaviour
{
    [SerializeField] private GiboBoardManager boardManager;
    
    private TextMeshProUGUI _myText;

    private void Awake()
    {
        _myText = GetComponent<TextMeshProUGUI>();
        boardManager.OnReadFailed += () => _myText.text = "기보 불러오기 실패";
        boardManager.OnReadSucceed += () => OnTurnChanged(0);
        boardManager.OnTurnChanged += OnTurnChanged; 
    }

    private void OnTurnChanged(int turn)
        => _myText.text = $"{turn}수 / {boardManager.LastTurn}수 {boardManager.Result}";
}
