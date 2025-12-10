using System;
using TMPro;
using UnityEngine;

public class MessageBoxManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;

    public event Action OnMessageBoxOpen;
    public event Action OnMessageBoxClose;

    private Action _requestedAction;
    private bool _isGameEnd;

    private void Awake()
    {
        _isGameEnd = false;
        EventManager.Instance.OnGameEnd += () =>
        {
            _isGameEnd = true;
            gameObject.SetActive(false);
        };
    }

    private void Start() => gameObject.SetActive(false);
    private void OnEnable() => OnMessageBoxOpen!.Invoke();

    private void OnDisable()
    {
        if (_isGameEnd) return;
        OnMessageBoxClose!.Invoke();
    }

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
        }
#elif UNITY_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Back))
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
    }

    public void OnCancel()
        => gameObject.SetActive(false);
}