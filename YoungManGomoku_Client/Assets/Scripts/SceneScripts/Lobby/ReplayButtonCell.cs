using System;
using System.Globalization;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReplayButtonCell : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dateTime;
    [SerializeField] private TextMeshProUGUI players;
    [SerializeField] private TextMeshProUGUI result;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Image outline;
    [SerializeField, Tooltip("결과가 승리일 때의 글자색")]
    private Color winTextColor;
    [SerializeField, Tooltip("결과가 패배일 때의 글자색")]
    private Color loseTextColor;
    [SerializeField, Tooltip("결과가 무승부일 때의 글자색")]
    private Color drawTextColor;
    
    private TweenerCore<Color, Color, ColorOptions> _fadeTween;
    private string _fileName;

    private void Awake()
    {
        outline.enabled = false;
        GetComponent<Button>().onClick.AddListener(OpenFile);
        deleteButton.onClick.AddListener(OutlineOn);
    }
    
    private void OnDestroy() => _fadeTween?.Kill();

    public void SetUIByData(in GiboFileManager.GiboTitleData data, Action<string, Action> deleteFunc)
    {
        _fileName = data.FileName;
        
        dateTime.text = data.DateTime.ToString("yy-MM-dd\ntt hh:mm", CultureInfo.InvariantCulture);
        players.text = $"{data.BlackPlayer}\n{data.WhitePlayer}";
        
        string[] resultSegments = data.Result.Split(' ');
        
        if (resultSegments.Length == 1)
        {
            result.text = $"{data.LastTurn}수\n{resultSegments[0]}";
            result.color = drawTextColor;
            return;
        }
        
        if (resultSegments[1] == "접속끊김승")
            resultSegments[1] = "끊김승";
        else if (resultSegments[1] == "접속끊김패")
            resultSegments[1] = "끊김패";
        
        result.text = $"{resultSegments[0]} {data.LastTurn}수\n{resultSegments[1]}";
        
        char winLose = result.text[^1];
        if (winLose == '승') result.color = winTextColor;
        else if (winLose == '패') result.color = loseTextColor;
        else result.color = drawTextColor;

        deleteButton.onClick.AddListener(() => deleteFunc(_fileName, OutlineOff));
    }

    private void OpenFile()
    {
        GiboFileManager.GiboFileName = _fileName;
        SceneLoadManager.LoadScene(SceneLoadManager.SceneType.GiboPlayScene).Cancel();
    }

    private void OutlineOn()
    {
        outline.enabled = true;
        _fadeTween = outline.DOFade(endValue: 0.4f, duration: 0.5f).
            SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    private void OutlineOff()
    {
        _fadeTween.Rewind();
        _fadeTween.Kill();
        outline.enabled = false;
    }
}
