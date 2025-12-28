using UnityEngine;
using UnityEngine.UI;

public class SoundSetting : MonoBehaviour
{
    [SerializeField] private Image masterVolumeImage;
    [SerializeField] private Image BGMVolumeImage;
    [SerializeField] private Image SFXVolumeImage;
    
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider BGMVolumeSlider;
    [SerializeField] private Slider SFXVolumeSlider;
    
    private void Start()
    {
        SoundManager.Instance.InitializeSoundManager(masterVolumeImage, BGMVolumeImage, SFXVolumeImage,
            masterVolumeSlider, BGMVolumeSlider, SFXVolumeSlider);
        
        masterVolumeImage.GetComponent<Button>().onClick.AddListener(()=>SoundManager.Instance.ChangeVolumeState(VolumeType.Master));
        BGMVolumeImage.GetComponent<Button>().onClick.AddListener(()=>SoundManager.Instance.ChangeVolumeState(VolumeType.BGM));
        SFXVolumeImage.GetComponent<Button>().onClick.AddListener(()=>SoundManager.Instance.ChangeVolumeState(VolumeType.SFX));
        
        masterVolumeSlider.onValueChanged.AddListener(SoundManager.Instance.SetMasterVolume);
        BGMVolumeSlider.onValueChanged.AddListener(SoundManager.Instance.SetBGMVolume);
        SFXVolumeSlider.onValueChanged.AddListener(SoundManager.Instance.SetSFXVolume);
    }
}
