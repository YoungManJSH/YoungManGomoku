using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReplayListReader : MonoBehaviour
{
    [Header("Settings"), SerializeField]
    private ReplayButtonCell cellPrefab;
    [SerializeField] private TextMeshProUGUI noReplayText;
    [SerializeField] private float spacing;
    
    private ScrollRect _scrollRect;
    private RectTransform _content;
    private float _cellHeight;
    private float _unitHeight;

    private void Awake()
    {
        noReplayText.text = "기보 리스트 읽는 중...";
        GiboFileManager.ReadGiboList(out var giboList);

        if (giboList.Count == 0)
        {
            noReplayText.text = "저장된 기보가 없습니다.";
            return;
        }
        
        _scrollRect = GetComponent<ScrollRect>();
        _content = _scrollRect.content;
        _cellHeight = cellPrefab.GetComponent<RectTransform>().rect.height;
        _unitHeight = _cellHeight + spacing;
        
        _content.sizeDelta = new Vector2(_content.sizeDelta.x,
            _cellHeight * giboList.Count + spacing * (giboList.Count - 1));
        
        for (int i = 0; i < giboList.Count; ++i)
        {
            ReplayButtonCell nowCell = Instantiate(cellPrefab, _content);
            nowCell.SetUIByData(giboList[i]);
            nowCell.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -_unitHeight * i);
        }
        
        noReplayText.enabled = false;
    }
}