using System;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [SerializeField] private AudioSource soundFXObject;
    

    private void Awake()
    {
        SFXManager[] SFXManagers = FindObjectsByType<SFXManager>(FindObjectsSortMode.None);

        if (SFXManagers.Length > 1)
        {
            
            Destroy(gameObject);
        }
        
        Instance = this;
        
        DontDestroyOnLoad(gameObject);
    }

    public void PlaySFXClip(AudioClip clip, float volume)
    {
        AudioSource audioSource = Instantiate(soundFXObject, Camera.main.transform.position,Quaternion.identity,transform);
        
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
        
        float clipLength = audioSource.clip.length;
        
        Destroy(audioSource.gameObject, clipLength);
    }
}
