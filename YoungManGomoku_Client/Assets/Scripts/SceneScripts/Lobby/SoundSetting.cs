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
        SoundManager.instance.InitializeSoundManager(masterVolumeImage, BGMVolumeImage, SFXVolumeImage,
            masterVolumeSlider, BGMVolumeSlider, SFXVolumeSlider);
        
        masterVolumeImage.GetComponent<Button>().onClick.AddListener(()=>SoundManager.instance.ChangeVolumeState(VolumeType.Master));
        BGMVolumeImage.GetComponent<Button>().onClick.AddListener(()=>SoundManager.instance.ChangeVolumeState(VolumeType.BGM));
        SFXVolumeImage.GetComponent<Button>().onClick.AddListener(()=>SoundManager.instance.ChangeVolumeState(VolumeType.SFX));
        
        masterVolumeSlider.onValueChanged.AddListener(SoundManager.instance.SetMasterVolume);
        BGMVolumeSlider.onValueChanged.AddListener(SoundManager.instance.SetBGMVolume);
        SFXVolumeSlider.onValueChanged.AddListener(SoundManager.instance.SetSFXVolume);
    }
}
