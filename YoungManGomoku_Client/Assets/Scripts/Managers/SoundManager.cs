using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public enum VolumeType
{
    Master,
    BGM,
    SFX
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioMixer audioMixer;

    [SerializeField] private Sprite soundIcon;
    [SerializeField] private Sprite muteIcon;

    [SerializeField] private SoundData soundData;

    private Image _masterVolumeImage;
    private Image _bgmVolumeImage;
    private Image _sfxVolumeImage;

    private void Awake()
    {
        if (Instance != null) Destroy(Instance.gameObject);
           
        Instance = this;
    }
    
    private void OnDestroy() => Instance = null;

    private void Start()
    {
        if (soundData.MasterVolume == 0)
        {
            soundData.MasterVolume = 0.5f;
            soundData.BGMVolume = 0.5f;
            soundData.SFXVolume = 0.5f;
        }
        
        audioMixer.SetFloat("MasterVolume", soundData.isMuteMasterVolume ? -80f : Mathf.Log10(soundData.MasterVolume) * 20f);
        audioMixer.SetFloat("BGMVolume", soundData.isMuteBGMVolume ? -80f : Mathf.Log10(soundData.BGMVolume) * 20f);
        audioMixer.SetFloat("SFXVolume", soundData.isMuteSFXVolume ? -80f : Mathf.Log10(soundData.SFXVolume) * 20f);
    }

    // 저장된 데이터를 기반으로 환경설정의 세팅을 UI에 반영함
    // 외부에서 데이터 주입 필요 (외부 스크립트의 Start에서 적용하기)
    public void InitializeSoundManager(
        Image masterVolumeImage, Image bgmVolumeImage, Image sfxVolumeImage,
        Slider masterVolumeSlider, Slider bgmVolumeSlider, Slider sfxVolumeSlider)
    {
        // bool 값에 따른 이미지 적용
        _masterVolumeImage = masterVolumeImage;
        _bgmVolumeImage = bgmVolumeImage;
        _sfxVolumeImage = sfxVolumeImage;

        _masterVolumeImage.sprite = soundData.isMuteMasterVolume ? muteIcon : soundIcon;
        _bgmVolumeImage.sprite = soundData.isMuteBGMVolume ? muteIcon : soundIcon;
        _sfxVolumeImage.sprite = soundData.isMuteSFXVolume ? muteIcon : soundIcon;

        // 슬라이더 값 적용
        masterVolumeSlider.value = soundData.MasterVolume;
        bgmVolumeSlider.value = soundData.BGMVolume;
        sfxVolumeSlider.value = soundData.SFXVolume;
    }

    // c#에서는 bool값이 값형이라서, 함수에서 값을 변경하면 원본에 반영이 안된다.
    // 그래서 ref로 인자를 넘겨줘야 함
    // 로그 내부에 0이 들어갈 수 없음. 유니티에서 해당 값에 0을 넣으면, 디폴트 값으로 변환하는 것을 확인함
    // 슬라이더의 최소 값을 0이 아닌 0.0001로 바꿔서 해당 문제를 해결.
    public void ChangeVolumeState(VolumeType type)
    {
        switch (type)
        {
            case VolumeType.Master: 
                ChangeVolumeState(ref soundData.isMuteMasterVolume, _masterVolumeImage); 
                audioMixer.SetFloat("MasterVolume", soundData.isMuteMasterVolume ? -80f : Mathf.Log10(soundData.MasterVolume) * 20f);
                break;
            case VolumeType.BGM: 
                ChangeVolumeState(ref soundData.isMuteBGMVolume, _bgmVolumeImage); 
                audioMixer.SetFloat("BGMVolume", soundData.isMuteBGMVolume ? -80f : Mathf.Log10(soundData.BGMVolume) * 20f);
                break;
            case VolumeType.SFX: 
                ChangeVolumeState(ref soundData.isMuteSFXVolume, _sfxVolumeImage); 
                audioMixer.SetFloat("SFXVolume", soundData.isMuteSFXVolume ? -80f : Mathf.Log10(soundData.SFXVolume) * 20f);
                break;
        }
    }

    // UI 이미지 적용
    private void ChangeVolumeState(ref bool muteFlag, Image targetImage)
    {
        muteFlag = !muteFlag;
        targetImage.sprite = muteFlag ? muteIcon : soundIcon;
    }

    // 오디오 믹서의 값은 -80~0
    // 슬라이더의 value를 기준으로 보정한다.
    public void SetMasterVolume(float value)
    {
        soundData.MasterVolume = value;

        if (soundData.isMuteMasterVolume == false)
        {
            audioMixer.SetFloat("MasterVolume", value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f);
        }
    }

    public void SetBGMVolume(float value)
    {
        soundData.BGMVolume = value;
        
        if (soundData.isMuteBGMVolume == false)
        {
            audioMixer.SetFloat("BGMVolume", value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f);
        }
    }

    public void SetSFXVolume(float value)
    {
        soundData.SFXVolume = value;

        if (soundData.isMuteSFXVolume == false)
        {
            audioMixer.SetFloat("SFXVolume", value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f);
        }
    }
}