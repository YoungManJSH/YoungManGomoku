using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReplayListMessageBox : MonoBehaviour
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI message;
    [SerializeField] private LobbyUIController lobbyUIController;

    private RectTransform _rect;
    private CancellationTokenSource _cts;
    private bool _respond;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
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
        // 뒤로 가기 소프트키 입력 시 취소 처리
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnCancel();
            return;
        }
        
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

    /* 책임 분리가 무너지는 방식이지만 마무리 단계이므로 구현 편의에 집중
     * 이런 게 많아지면 이벤트 구조 같은 걸로 처리하는 게 바람직할 듯? */
    private void OnEnable() => lobbyUIController.enabled = false;
    private void OnDisable() => lobbyUIController.enabled = true;
    
    public async Awaitable<bool> OpenMessageBox(string messageText)
    {
        _cts = new CancellationTokenSource();
        _respond = false;
        
        message.text = messageText;
        gameObject.SetActive(true);

        try { await Awaitable.WaitForSecondsAsync(60f, _cts.Token); }
        catch (OperationCanceledException) { }
        
        gameObject.SetActive(false);
        _cts.Dispose();
        _cts = null;
        
        return _respond;
    }
    
    private void OnConfirm()
    {
        _respond = true;
        _cts.Cancel();
    }

    private void OnCancel() => _cts.Cancel();
}