using System;
using TMPro;
using UnityEngine;

public class MessageBoxManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;

    public event Action OnOpened;
    public event Action TurnBackToGame;

    private Action _requestedAction;
    private RectTransform _rect;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        EventManager.Instance.OnGameEnd += OnGameEnd;
        EventManager.Instance.OnStartSweeping += OnGameEnd;
        gameObject.SetActive(false);
    }
    
    private void OnEnable() => OnOpened!.Invoke();

    private void Update()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnCancel();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnConfirm();
            return;
        }

        if (Input.GetMouseButtonDown(0) &&
            RectTransformUtility.RectangleContainsScreenPoint(_rect, Input.mousePosition) is false)
        {
            OnCancel();
        }
#elif UNITY_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnCancel();
            return;
        }
        
        if (Input.touchCount == 0) return;
        
        Touch touch = Input.GetTouch(0);
        
        if (touch.phase == TouchPhase.Began &&
            RectTransformUtility.RectangleContainsScreenPoint(_rect, touch.position) is false)
        {
            OnCancel();
        }
#endif
    }
    
    public void MessageBoxOpen(string message, Action requestedAct)
    {
        messageText.text = message;
        _requestedAction = requestedAct;
        gameObject.SetActive(true);
    }
    
    public void OnConfirm()
    {
        _requestedAction?.Invoke();
        gameObject.SetActive(false);
        TurnBackToGame!.Invoke();
    }

    public void OnCancel()
    {
        gameObject.SetActive(false);
        TurnBackToGame!.Invoke();
    }

    private void OnGameEnd()
        => gameObject.SetActive(false);
}