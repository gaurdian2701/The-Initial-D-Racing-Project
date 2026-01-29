using System.Collections;
using System.Collections.Generic;
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
    AudioSource _audioSource;

    public void StartCountDown()
    {
        _audioSource = GetComponent<AudioSource>();
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
                _audioSource.Play();
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
