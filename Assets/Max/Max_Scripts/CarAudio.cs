using System;
using UnityEngine;

public class CarAudio : MonoBehaviour
{
    [SerializeField] private AudioSource engine;
    [SerializeField] private AudioSource drift;

    public float engineAudioSpeedMax;
    public float engineAudioSpeedMin;
    private float engineSpeed;
    
    
    
    private Rigidbody rb;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        engineSpeed = rb.linearVelocity.magnitude;
        
        engine.volume =  (1 / engineAudioSpeedMax) * Mathf.Clamp(engineSpeed, engineAudioSpeedMin, engineAudioSpeedMax);
    }

    public void EnableDriftAudio()
    {
        drift.Play();
    }

    public void DisableDriftAudio()
    {
        drift.Stop();
    }
}
