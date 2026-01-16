using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReplayListReader : MonoBehaviour
{
    [SerializeField] private ReplayButtonCell cellPrefab;
    [SerializeField] private TextMeshProUGUI noReplayText;
    [SerializeField] private ReplayListMessage messageBox;
    [SerializeField] private string deleteConfirmMessage;
    [SerializeField] private string deleteFailedMessage;
    [SerializeField] private float spacing;

    private ReplayButtonCell[] _replayButtonCells;
    
    private void Awake() => GenerateGiboCells().Cancel();

    /// <summary>기보 리스트 UI 새로고침</summary>
    private async Awaitable GenerateGiboCells()
    {
        if (_replayButtonCells != null)
        {
            foreach (ReplayButtonCell cell in _replayButtonCells)
            {
                Destroy(cell.gameObject);
            }
        }
        
        noReplayText.enabled = true;
        noReplayText.text = "기보 리스트 읽는 중...";
        
        List<GiboFileManager.GiboTitleData> giboList = await GiboFileManager.ReadGiboList();
        
        if (giboList.Count == 0)
        {
            noReplayText.text = "저장된 기보가 없습니다.";
            return;
        }
        
        ScrollRect scrollRect = GetComponent<ScrollRect>();
        RectTransform content = scrollRect.content;
        float cellHeight = cellPrefab.GetComponent<RectTransform>().rect.height;
        float unitHeight = cellHeight + spacing;
        
        content.sizeDelta = new Vector2(content.sizeDelta.x,
            cellHeight * giboList.Count + spacing * (giboList.Count - 1));
        
        _replayButtonCells = new ReplayButtonCell[giboList.Count];
        
        for (int i = 0; i < giboList.Count; ++i)
        {
            _replayButtonCells[i] = Instantiate(cellPrefab, content);
            _replayButtonCells[i].SetUIByData(giboList[i], TryDeleteFile);
            _replayButtonCells[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -unitHeight * i);
        }
        
        noReplayText.enabled = false;
    }
    
    private async void TryDeleteFile(string fileName)
    {
        try
        {
            // 메시지 박스에서 취소 응답이 오면 그대로 종료
            if (await messageBox.OpenMessageBox(deleteConfirmMessage) is false)
                return;

            // 파일 삭제에 성공하면 리스트 UI를 새로고침
            if (GiboFileManager.TryDeleteGiboFile(fileName))
            {
                GenerateGiboCells().Cancel();
                return;
            }

            // 파일 삭제에 실패하면 유저의 선택에 따라 리스트 UI 새로고침
            if (await messageBox.OpenMessageBox(deleteFailedMessage))
                GenerateGiboCells().Cancel();
        }
        catch (Exception e)
        {
            Debug.LogError($"파일 삭제 시도 과정에서 예외 발생: {e}");
            GenerateGiboCells().Cancel();
        }
    }
}