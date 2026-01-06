using System;
using UnityEngine;

[Serializable]
public class SoundData
{
    public bool isMuteMasterVolume; 
    public bool isMuteBGMVolume;
    public bool isMuteSFXVolume;

    public float MasterVolume;
    public float BGMVolume;
    public float SFXVolume;

    public SoundData()
    {
        isMuteMasterVolume = false;
        isMuteBGMVolume = false;
        isMuteSFXVolume = false;
        
        MasterVolume = 0.0f;
        BGMVolume = 0.0f;
        SFXVolume = 0.0f;
    }
}