using UnityEngine;

[CreateAssetMenu(fileName = "SoundData", menuName = "Scriptable Objects/SoundData")]
public class SoundData : ScriptableObject
{
    public bool isMuteMasterVolume = false; 
    public bool isMuteBGMVolume = false;
    public bool isMuteSFXVolume = false;

    public float MasterVolume;
    public float BGMVolume;
    public float SFXVolume;
}