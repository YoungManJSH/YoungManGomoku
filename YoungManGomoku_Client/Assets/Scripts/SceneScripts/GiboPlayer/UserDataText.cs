using TMPro;
using UnityEngine;

public class UserDataText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI blackName;
    [SerializeField] private TextMeshProUGUI blackRecord;
    [SerializeField] private TextMeshProUGUI whiteName;
    [SerializeField] private TextMeshProUGUI whiteRecord;
    [SerializeField] private GiboBoardManager boardManager;

    private void Awake() => boardManager.OnReadSucceed += OnReadSucceed;

    private void OnReadSucceed()
    {
        blackName.text = boardManager.BlackData.name;
        whiteName.text = boardManager.WhiteData.name;
        blackRecord.text = $"{boardManager.BlackData.win}승 {boardManager.BlackData.draw}무 {boardManager.BlackData.lose}패 ({boardManager.BlackData.rating:F1}pt)";
        whiteRecord.text = $"{boardManager.WhiteData.win}승 {boardManager.WhiteData.draw}무 {boardManager.WhiteData.lose}패 ({boardManager.WhiteData.rating:F1}pt)";
    }
}
