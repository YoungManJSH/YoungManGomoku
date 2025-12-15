using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using TMPro;

public struct ReplayData
{
    public readonly string nickname;
    public readonly bool isWin;
    public readonly string date;

    public ReplayData(string nickname, bool isWin, string date)
    {
        this.nickname = nickname;
        this.isWin = isWin;
        this.date = date;
    }
}

public struct CellData
{
    public readonly TextMeshProUGUI resultText;
    public readonly TextMeshProUGUI playerNameText;
    public readonly TextMeshProUGUI dateText;
    public readonly RectTransform rectTransform;

    public CellData(TextMeshProUGUI resultText, TextMeshProUGUI playerNameText, TextMeshProUGUI dateText,
        RectTransform rectTransform)
    {
        this.resultText = resultText;
        this.playerNameText = playerNameText;
        this.dateText = dateText;
        this.rectTransform = rectTransform;
    }
}

public class ReplayInfiniteScroll : MonoBehaviour
{
    // 인스펙터창에서 설정해줘야할 부분
    [Header("Settings")] [SerializeField] private GameObject cellPrefab;

    // 파일 입출력으로 값 조정
    [SerializeField] private int dataCount;
    [SerializeField] private float cellHeight;
    [SerializeField] private float spacing;
    [SerializeField] private int poolSize;

    // 스크롤뷰에 해당 스크립트 부착시키면, Awake 단계에서 찾아서 값이 정해짐
    private ScrollRect scroll;
    private RectTransform content;
    private RectTransform viewport;

    // 각 셀에 표시할 데이터들
    private List<ReplayData> data = new List<ReplayData>();

    // 생성된 셀들을 관리하는 리스트 데이터
    private List<CellData> pool = new List<CellData>();

    // 부착된 오브젝트를 기준으로 변수를 초기화 한 후, 셀 생성 및 data와 pool 변수를 채워 넣는다.
    private void Awake()
    {
        scroll = GetComponent<ScrollRect>();
        content = scroll.content;
        viewport = scroll.viewport;

        // 데이터 넣기 (테스트용) 파일 입출력으로 변경 예정
        for (int i = 0; i < dataCount; i++)
        {
            string userName;
            bool isWin;

            if (i % 2 == 0)
            {
                userName = "김재환";
                isWin = true;
            }
            else
            {
                userName = "강찬구";
                isWin = false;
            }

            var replayData = new ReplayData(userName, isWin, $"2025-12-{22 - i}");

            data.Add(replayData);
        }

        // totalHeight 값으로 스크롤바의 크기가 정해진다.
        float totalHeight = (cellHeight * dataCount) + (spacing * (dataCount - 1));
        content.sizeDelta = new Vector2(content.sizeDelta.x, totalHeight);

        for (int i = 0; i < poolSize; i++)
        {
            var go = CreateCell();
            var cellData = new CellData(go.Find("GameResultText").GetComponent<TextMeshProUGUI>(),
                go.Find("RivalPlayer/PlayerName").GetComponent<TextMeshProUGUI>(),
                go.Find("Date").GetComponent<TextMeshProUGUI>(),
                go);
            pool.Add(cellData);
        }

        scroll.onValueChanged.AddListener(_ => UpdateCells());
        UpdateCells();
    }

    private void OnEnable()
    {
        scroll.normalizedPosition = new Vector2(0, 1f);
    }

    RectTransform CreateCell()
    {
        GameObject go = Instantiate(cellPrefab, content);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, cellHeight);

        return go.GetComponent<RectTransform>();
    }


    void UpdateCells()
    {
        float top = content.anchoredPosition.y;
        float bottom = top + viewport.rect.height;
        for (int i = 0; i < pool.Count; i++)
        {
            int dataIndex = i + Mathf.FloorToInt(top / (cellHeight + spacing));
            if (dataIndex < 0 || dataIndex >= data.Count)
            {
                pool[i].rectTransform.gameObject.SetActive(false);
                continue;
            }

            pool[i].rectTransform.gameObject.SetActive(true);
            float y = dataIndex * (cellHeight + spacing);
            pool[i].rectTransform.anchoredPosition = new Vector2(0, -y);

            if (data[dataIndex].isWin)
            {
                pool[i].resultText.text = "Win";
                pool[i].resultText.color = new Color(1f, 242f / 255f, 0f);
                ;
            }
            else
            {
                pool[i].resultText.text = "Lose";
                pool[i].resultText.color =
                    new Color(156f / 255f, 156f / 255f, 156f / 255f);
            }

            pool[i].playerNameText.text =
                data[dataIndex].nickname;
            pool[i].dateText.text = data[dataIndex].date;
        }
    }
}