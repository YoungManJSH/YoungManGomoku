using System;
using TMPro;
using UnityEngine;

public class MessageBoxManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;

    public event Action OnOpened;
    public event Action TurnBackToGame;

    private Action _requestedAction;
    private bool _isGameEnded;

    private void Awake()
    {
        _isGameEnded = false;
        EventManager.Instance.OnGameEnd += OnGameEnd;
    }

    private void Start() => gameObject.SetActive(false);
    private void OnEnable() => OnOpened!.Invoke();

    private void OnDisable()
    {
        if (_isGameEnded is false) TurnBackToGame!.Invoke();
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

    private void OnGameEnd()
    {
        _isGameEnded = true;
        gameObject.SetActive(false);
    }
}