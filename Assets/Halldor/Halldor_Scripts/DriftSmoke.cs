using System;
using Car;
using UnityEngine;

public class DriftSmoke : MonoBehaviour
{
    [SerializeField] private ArcadeyCarController _carController;
    [SerializeField] private ParticleSystem _driftSmokeA;
    [SerializeField] private ParticleSystem _driftSmokeB;
    [SerializeField] private Rigidbody _carRigidBody;

    private CarAudio _carAudio;
    
    private void Start()
    {
        _carAudio = GetComponent<CarAudio>();
        _carController.onDrift += EnableDriftSmoke;
        _driftSmokeA.Play();
        _driftSmokeB.Play();
        DisableDriftSmoke();
    }

    private void EnableDriftSmoke(bool isdrifting)
    {
        if (isdrifting && _carRigidBody.linearVelocity.magnitude > 30f)
        {
            _driftSmokeA.enableEmission = true;
            _driftSmokeB.enableEmission = true;
            _carAudio.EnableDriftAudio();
        }
        else
        {
            DisableDriftSmoke();
        }
    }

    private void DisableDriftSmoke()
    {
        _driftSmokeA.enableEmission = false;
        _driftSmokeB.enableEmission = false;
        _carAudio.DisableDriftAudio();
    }
}
