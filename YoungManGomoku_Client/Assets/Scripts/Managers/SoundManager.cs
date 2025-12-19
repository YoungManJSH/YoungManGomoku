using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public enum VolumeType
    {
        Master,
        BGM,
        SFX
    }

    public static SoundManager Instance;

    [SerializeField] private AudioMixer audioMixer;

    [SerializeField] private Sprite soundIcon;
    [SerializeField] private Sprite muteIcon;

    [SerializeField] private SoundData soundData;

    private Image masterVolumeImage;
    private Image BGMVolumeImage;
    private Image SFXVolumeImage;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 저장된 데이터를 기반으로 환경설정의 세팅을 UI에 반영함
    public void InitializeSoundManager(
        Image _masterVolumeImage, Image _BGMVolumeImage, Image _SFXVolumeImage,
        Slider _masterVolumeSlider, Slider _BGMVolumeSlider, Slider _SFXVolumeSlider)
    {
        masterVolumeImage = _masterVolumeImage;
        BGMVolumeImage = _BGMVolumeImage;
        SFXVolumeImage = _SFXVolumeImage;

        masterVolumeImage.sprite = soundData.isMuteMasterVolume ? muteIcon : soundIcon;
        BGMVolumeImage.sprite = soundData.isMuteBGMVolume ? muteIcon : soundIcon;
        SFXVolumeImage.sprite = soundData.isMuteSFXVolume ? muteIcon : soundIcon;

        _masterVolumeSlider.value = soundData.MasterVolume;
        _BGMVolumeSlider.value = soundData.BGMVolume;
        _SFXVolumeSlider.value = soundData.SFXVolume;
    }

    // c#에서는 bool값이 값형이라서, 함수에서 값을 변경하면 원본에 반영이 안된다.
    // 그래서 ref로 인자를 넘겨줘야 함
    public void ChangeVolumeState(VolumeType type)
    {
        switch (type)
        {
            case VolumeType.Master: ChangeVolumeState(ref soundData.isMuteMasterVolume, masterVolumeImage); break;
            case VolumeType.BGM: ChangeVolumeState(ref soundData.isMuteBGMVolume, BGMVolumeImage); break;
            case VolumeType.SFX: ChangeVolumeState(ref soundData.isMuteSFXVolume, SFXVolumeImage); break;
        }
    }
    
    private void ChangeVolumeState(ref bool muteFlag, Image targetImage)
    {
        muteFlag = !muteFlag;
        targetImage.sprite = muteFlag ? muteIcon : soundIcon;
    }

    // 오디오 믹서의 값은 -80~0
    // 슬라이더의 value를 기준으로 보정한다.
    public void SetMasterVolume(float value)
    {
        audioMixer.SetFloat("MasterVolume", value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f);

        soundData.MasterVolume = value;
    }

    public void SetBGMVolume(float value)
    {
        audioMixer.SetFloat("BGMVolume", value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f);

        soundData.BGMVolume = value;
    }

    public void SetSFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f);

        soundData.SFXVolume = value;
    }
}