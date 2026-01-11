using System.Globalization;
using TMPro;
using UnityEngine;

public class ReplayButtonCell : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dateTime;
    [SerializeField] private TextMeshProUGUI players;
    [SerializeField] private TextMeshProUGUI result;
    [SerializeField, Tooltip("결과가 승리일 때의 글자색")]
    private Color winTextColor;
    [SerializeField, Tooltip("결과가 패배일 때의 글자색")]
    private Color loseTextColor;
    [SerializeField, Tooltip("결과가 무승부일 때의 글자색")]
    private Color drawTextColor;
    
    private string _fileName;
    
    public void SetUIByData(in GiboFileManager.GiboTitleData data)
    {
        _fileName = data.FileName;
        
        dateTime.text = data.DateTime.ToString("yy-MM-dd\ntt hh:mm", CultureInfo.InvariantCulture);
        players.text = $"{data.BlackPlayer}\n{data.WhitePlayer}";
        
        string[] resultSegments = data.Result.Split(' ');
        
        if (resultSegments.Length == 1)
        {
            result.text = resultSegments[0];
            result.color = drawTextColor;
            return;
        }
        
        if (resultSegments[1] == "접속끊김승")
            resultSegments[1] = "끊김승";
        else if (resultSegments[1] == "접속끊김패")
            resultSegments[1] = "끊김패";
        
        result.text = $"{resultSegments[0]}\n{resultSegments[1]}";
        
        char winLose = result.text[^1];
        if (winLose == '승') result.color = winTextColor;
        else if (winLose == '패') result.color = loseTextColor;
        else result.color = drawTextColor;
    }

    public void LoadThisFile()
    {
        GiboFileManager.GiboFileName = _fileName;
        SceneLoadManager.LoadScene(SceneLoadManager.SceneType.GiboPlayScene).Cancel();
    }
}
