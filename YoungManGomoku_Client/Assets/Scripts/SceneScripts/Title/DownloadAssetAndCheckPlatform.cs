using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CheckData : MonoBehaviour
{
    /// <summary>
    /// 씬에서 참조해 지정할 변수들
    /// UI로 정보 전달이 목적인 변수들이다.
    /// </summary>
    [Header("UI")] 
    [SerializeField] private GameObject waitMessage;
    [SerializeField] private GameObject downMessage;
    [SerializeField] private GameObject downloadErrorMessage;
    [SerializeField] private Slider downSlider;
    [SerializeField] private TextMeshProUGUI sizeInfoText;
    [SerializeField] private TextMeshProUGUI downValueText;

    /// <summary>
    /// 다운 받을 파일 그룹
    /// </summary>
    [Header("Label")] 
    [SerializeField] private AssetLabelReference defaultLabel;

    /// <summary>
    /// 다운 받을 용량의 총 사이즈 및 패치 받을 파일 관리할 딕셔너리
    /// </summary>
    private long patchSize;

    private Dictionary<string, long> patchMap;

    private void Awake()
    {
        patchMap = new Dictionary<string, long>();
    }

    private void Start()
    {
        waitMessage.SetActive(true);
        downMessage.SetActive(false);

        StartCoroutine(InitAddressable());
        StartCoroutine(CheckUpdateFiles());
    }

    private IEnumerator InitAddressable()
    {
        var init = Addressables.InitializeAsync();
        yield return init;
    }

    /// <summary>
    /// 패치할 파일 있는지 판단하는 함수
    ///
    /// size로 해당 여부를 판단하며,
    /// 패치가 필요하면 다운 받을 용량을 UI로 표시하고
    /// 패치가 필요하지 않다면 1초 후 다음 씬을 로드함
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckUpdateFiles()
    {
        var labels = new List<string>() { defaultLabel.labelString };

        patchSize = default;

        foreach (var label in labels)
        {
            var handle = Addressables.GetDownloadSizeAsync(label);

            yield return handle;

            if (HasDownloadError(handle))
            {
                yield break;
            }

            patchSize = handle.Result;
        }

        if (patchSize > decimal.Zero)
        {
            waitMessage.SetActive(false);
            downMessage.SetActive(true);

            sizeInfoText.text = GetFileSize(patchSize);
        }
        else
        {
            downValueText.text = " 100% ";
            downSlider.value = 1f;
            yield return new WaitForSeconds(1f);
            CacheData();
        }
    }

    /// <summary>
    /// 다운받을 용량을 단위별로 표기하는 함수
    /// </summary>
    /// <param name="byteCnt"></param>
    /// <returns></returns>
    private string GetFileSize(long byteCnt)
    {
        string patchSize = "0 Bytes";

        if (byteCnt >= 1073741824.0)
        {
            patchSize = string.Format("{0:##.##}", byteCnt / 1073741824.0) + " GB";
        }
        else if (byteCnt >= 1048576.0)
        {
            patchSize = string.Format("{0:##.##}", byteCnt / 1048576.0) + " MB";
        }
        else if (byteCnt >= 1024.0)
        {
            patchSize = string.Format("{0:##.##}", byteCnt / 1024.0) + " KB";
        }
        else if (0 < byteCnt && byteCnt <= 1024.0)
        {
            patchSize = $"{byteCnt} Bytes";
        }

        return patchSize;
    }

    /// <summary>
    /// 버튼에 바인드 되어, 버튼 입력으로 다운로드를 시작한다.
    /// </summary>
    public void StartDownload()
    {
        StartCoroutine(PatchFiles());
    }

    /// <summary>
    /// 모든 패치 파일을 다운받는 함수
    /// </summary>
    /// <returns></returns>
    IEnumerator PatchFiles()
    {
        var labels = new List<string>() { defaultLabel.labelString };

        foreach (var label in labels)
        {
            var handle = Addressables.GetDownloadSizeAsync(label);

            yield return handle;
            
            if (HasDownloadError(handle))
            {
                yield break;
            }

            if (handle.Result != decimal.Zero)
            {
                StartCoroutine(DownLoadLabel(label));
            }
        }

        yield return CheckDownLoad();
    }

    /// <summary>
    /// 라벨에 속한 에셋을 다운받으며, 진행 상황을 패치맵에 기록하는 함수
    /// </summary>
    /// <param name="label"></param>
    /// <returns></returns>
    IEnumerator DownLoadLabel(string label)
    {
        patchMap.Add(label, 0);

        var handle = Addressables.DownloadDependenciesAsync(label, false);

        while (!handle.IsDone)
        {
            patchMap[label] = handle.GetDownloadStatus().DownloadedBytes;
            
            if (HasDownloadError(handle))
            {
                yield break;
            }
            
            yield return new WaitForEndOfFrame();
        }

        patchMap[label] = handle.GetDownloadStatus().TotalBytes;
        Addressables.Release(handle);
    }

    IEnumerator CheckDownLoad()
    {
        var totalDownloadedSize = 0f;
        downValueText.text = "0 %";

        while (true)
        {
            totalDownloadedSize += patchMap.Sum(tmp => tmp.Value);

            downSlider.value = totalDownloadedSize / patchSize;
            downValueText.text = (int)(downSlider.value * 100) + " %";

            if (Mathf.Approximately(totalDownloadedSize, patchSize))
            {
                yield return new WaitForSeconds(1f);
                CacheData();
            }

            totalDownloadedSize = 0f;
            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary>
    /// 이후 씬에서 사용할 수 있게 데이터를 저장해 놓는다.
    /// </summary>
    private async void CacheData()
    {
        await AssetLoadManager.Instance.LoadAssetsAsync();
        LoadSceneViaPlatform();
    }
    
    /// <summary>
    /// 다음 씬으로 전환하는 함수
    /// 
    /// 어드레서블 에셋을 모두 다음받으면 실행되고,
    /// 현재 플랫폼에 맞춰 다른 씬을 로드한다.
    /// </summary>
    private void LoadSceneViaPlatform()
        => SceneLoadManager.LoadScene(SceneLoadManager.SceneType.TitleScene);

    /// <summary>
    /// 다운로드 실패시, 리턴값으로 알려주며, 안내창을 띄운다.
    /// </summary>
    /// <param name="handle"></param>
    /// <returns></returns>
    private bool HasDownloadError(AsyncOperationHandle handle)
    {
        if (handle.Status == AsyncOperationStatus.Failed)
        {
            downloadErrorMessage.SetActive(true);

            return true;
        }

        return false;
    }

    /// <summary>
    /// 다운로드 에셋 실패시 나오는 버튼에 할당되어, 씬을 다시 시작하도록 한다.
    /// </summary>
    public void ReloadNowScene()
        => SceneLoadManager.LoadScene(SceneLoadManager.SceneType.LoadingScene);
}