using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GiboMessageController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GiboBoardManager boardManager;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private string readFailedText;
    [SerializeField] private string deleteFailedText;
    [SerializeField] private string simulationFailedText;

    public event Action MessageboxOpened;
    public event Action MessageboxClosed;
    
    private Action _requestedAction;
    private RectTransform _rect;
    
    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
        
        boardManager.OnReadFailed += OnReadFailed;
        boardManager.OnSimulationCompleted += OnSimulationCompleted;
        
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetButtonDown("Submit"))
        {
            OnConfirm();
            return;
        }

        if (Input.GetButtonDown("Cancel"))
        {
            OnCancel();
            return;
        }
        
#if UNITY_STANDALONE || UNITY_EDITOR
        // 메시지 박스 바깥 영역 클릭 시 취소 처리
        if (Input.GetMouseButtonDown(0) &&
            RectTransformUtility.RectangleContainsScreenPoint(_rect, Input.mousePosition) is false)
        {
            OnCancel();
        }
#elif UNITY_ANDROID
        // 터치 입력 없으면 패스
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        // 터치 위치가 메시지 박스 바깥 영역이면 취소 처리
        if (touch.phase == TouchPhase.Began &&
            RectTransformUtility.RectangleContainsScreenPoint(_rect, touch.position) is false)
        {
            OnCancel();
        }
#endif
    }

    private void OnEnable() => MessageboxOpened?.Invoke();
    private void OnDisable() => MessageboxClosed?.Invoke();
    
    public void MessageBoxOpen(string message, Action requestedAct = null)
    {
        messageText.text = message;
        _requestedAction = requestedAct;
        gameObject.SetActive(true);
    }
    
    private void OnConfirm()
    {
        gameObject.SetActive(false);
        _requestedAction?.Invoke();
    }

    private void OnCancel()
    {
        gameObject.SetActive(false);
        _requestedAction = null;
    }

    private void OnReadFailed()
        => MessageBoxOpen(readFailedText, DeleteFile);

    private void OnSimulationCompleted(bool isSuccess)
    {
        if (isSuccess is false)
            MessageBoxOpen(simulationFailedText);
    }

    private void DeleteFile()
    {
        if (GiboFileManager.TryDeleteGiboFile())
            GiboBoardManager.TurnBackToLobby();
        else
            MessageBoxOpen(deleteFailedText, GiboBoardManager.TurnBackToLobby);
    }
}
