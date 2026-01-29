using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceCountDown : MonoBehaviour
{
    [SerializeField] private float waitTime = 3f;
    [SerializeField] private float musicWaitTime = 1f;
    [SerializeField] private AudioClip countDownClip;
    private float _timeLeft;
    private float _timeLeftMusic;
    bool _counting = false;
    bool _countingMusic = false;
    List<AudioSource> _audioSources;

    public void StartCountDown()
    {
        _audioSources = GetComponents<AudioSource>().ToList();
        _timeLeft  = waitTime;
        _timeLeftMusic = musicWaitTime;
        _counting = true;
        _countingMusic = true;
        ChangePauseStateOnCars(true);
        if (SFXManager.Instance!=null)SFXManager.Instance.PlaySFXClip(countDownClip,1f);
    }

    // Update is called once per frame
    void Update()
    {
        if (_counting)
        {
            _timeLeft -= Time.deltaTime;

            if (_timeLeft <= 0)
            {
                _counting = false;
                ChangePauseStateOnCars(false);
            }
        }

        if (_countingMusic)
        {
            _timeLeftMusic  -= Time.deltaTime;
            
            if (_timeLeftMusic <= 0)
            {
                _countingMusic = false;
                
                int random = Random.Range(0, _audioSources.Count);
                print("RANDOM:" + random);
                _audioSources[random].Play();
            }
        }
    }
    
    
    public void ChangePauseStateOnCars(bool pause)
    {
        Rigidbody[] bodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);

        foreach (Rigidbody rb in bodies)
        {
            rb.isKinematic = pause;
        }
        
    }

   
    
}
