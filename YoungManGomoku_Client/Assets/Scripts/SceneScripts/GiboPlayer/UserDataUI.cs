using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserDataUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI blackName;
    [SerializeField] private TextMeshProUGUI blackRecord;
    [SerializeField] private Image blackProfile;
    [SerializeField] private TextMeshProUGUI whiteName;
    [SerializeField] private TextMeshProUGUI whiteRecord;
    [SerializeField] private Image whiteProfile;
    [SerializeField] private ProfileImages profileImages;
    [SerializeField] private GiboBoardManager boardManager;

    private void Awake() => boardManager.OnReadSucceed += OnReadSucceed;

    private void OnReadSucceed()
    {
        blackName.text = boardManager.BlackData.name;
        whiteName.text = boardManager.WhiteData.name;
        blackRecord.text = $"({boardManager.BlackData.rating:F1}pt)\n{boardManager.BlackData.win}승 {boardManager.BlackData.draw}무 {boardManager.BlackData.lose}패";
        whiteRecord.text = $"({boardManager.WhiteData.rating:F1}pt)\n{boardManager.WhiteData.win}승 {boardManager.WhiteData.draw}무 {boardManager.WhiteData.lose}패";
        blackProfile.sprite = profileImages[boardManager.BlackData.imageNum];
        whiteProfile.sprite = profileImages[boardManager.WhiteData.imageNum];
    }
}
