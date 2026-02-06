using System;
using TMPro;
using UnityEngine;
using YoungManGomoku_Protocol.TypeEnum.InGame;

[CreateAssetMenu(fileName = "GameResultTexts", menuName = "Scriptable Objects/GameResultTexts")]
public class GameResultTexts : ScriptableObject
{
    [Serializable]
    private struct GameResultText
    {
        public string mainText;
        public string detailText;

        public void ApplyText(TextMeshProUGUI main, TextMeshProUGUI detail)
        {
            main.text = mainText;
            detail.text = detailText;
        }
    }

    #region 인스펙터 입력부
    [SerializeField] private GameResultText gomokuWin;
    [SerializeField] private GameResultText gomokuLose;
    [SerializeField] private GameResultText unmovableWin;
    [SerializeField] private GameResultText unmovableLose;
    [SerializeField] private GameResultText surrenderWin;
    [SerializeField] private GameResultText surrenderLose;
    [SerializeField] private GameResultText timeoutWin;
    [SerializeField] private GameResultText timeoutLose;
    [SerializeField] private GameResultText disconnectedWin;
    [SerializeField] private GameResultText disconnectedLose;
    [SerializeField] private GameResultText draw;
    #endregion

    /// <summary>게임 결과 종류에 따라 결과 텍스트 UI 적용</summary>
    /// <param name="main">게임 결과</param>
    /// <param name="detail">결과 상세 설명</param>
    /// <param name="endCode">게임 결과 종류</param>
    public void ApplyText(TextMeshProUGUI main, TextMeshProUGUI detail, GameEndCode endCode)
    {
        switch (endCode)
        {
            case GameEndCode.GomokuWin:
                gomokuWin.ApplyText(main, detail);
                break;
            case GameEndCode.GomokuLose:
                gomokuLose.ApplyText(main, detail);
                break;
            case GameEndCode.BlackUnmovable:
                (GameManager.Instance.IsPlayerBlack ? unmovableLose : unmovableWin).ApplyText(main, detail);
                break;
            case GameEndCode.SurrenderWin:
                surrenderWin.ApplyText(main, detail);
                break;
            case GameEndCode.SurrenderLose:
                surrenderLose.ApplyText(main, detail);
                break;
            case GameEndCode.TimeOutWin:
                timeoutWin.ApplyText(main, detail);
                break;
            case GameEndCode.TimeOutLose:
                timeoutLose.ApplyText(main, detail);
                break;
            case GameEndCode.DisconnectedWin:
                disconnectedWin.ApplyText(main, detail);
                break;
            case GameEndCode.DisconnectedLose:
                disconnectedLose.ApplyText(main, detail);
                break;
            case GameEndCode.Draw:
                draw.ApplyText(main, detail);
                break;
            default:
                Debug.LogError("잘못된 endCode로 결과창 텍스트 변경 시도!!");
                break;
        }
    }
}
