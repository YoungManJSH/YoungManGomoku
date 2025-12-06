using TMPro;
using UnityEngine;

public class ProgressText : MonoBehaviour
{
    private TextMeshProUGUI progressText;

    private void Awake()
    {
        progressText = GetComponent<TextMeshProUGUI>();
    }
    
    private void Start()
    {
        GameManager.Instance.BoardInform.OnTurnChanged += OnTurnChanged;
    }
    
    private void OnTurnChanged(int turn)
    {
        progressText.text = $"{turn}수 진행 중";
    }
}
