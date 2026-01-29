using UnityEngine;
using UnityEngine.Audio;

public class VolumeControll : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;


    public void SetMasterVolume(float level)
    {
        mixer.SetFloat("MasterVolume", Mathf.Log10(level) * 20f);
    }
}
