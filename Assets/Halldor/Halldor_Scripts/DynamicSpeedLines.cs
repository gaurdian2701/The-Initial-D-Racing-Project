using System;
using UnityEngine;

public class DynamicSpeedLines : MonoBehaviour
{
    [SerializeField]
    private CameraFollow _cameraFollow;
    [SerializeField]
    private AnimationCurve _speedCurve;
    [SerializeField]
    private ParticleSystem _speedLines;
    private Rigidbody _carRB;
    private float _maxSpeed = 66;
    private float _minSpeedforParticles = 30;
    private int _maxParticles = 100;
    private int _minParticleSpeed = 30;
    private int _maxParticleSpeed = 70;
    private float _speedPoint;
    
    private void Start()
    {
        _carRB = _cameraFollow.mfollowTarget.GetComponent<Rigidbody>();
    }

    private void Update()
    {
        _speedPoint = _carRB.linearVelocity.magnitude/_maxSpeed;
        _speedCurve.Evaluate(_speedPoint);
        
        if (_speedCurve.Evaluate(_speedPoint) * _maxParticleSpeed < _minParticleSpeed)
            _speedLines.startSpeed = _minParticleSpeed;
        else 
            _speedLines.startSpeed = _speedCurve.Evaluate(_speedPoint) * _maxParticleSpeed;

        if (_carRB.linearVelocity.magnitude >= _minSpeedforParticles)
            _speedLines.emissionRate = _speedCurve.Evaluate(_speedPoint) * _maxParticles;
        else
            _speedLines.emissionRate = 0;
    }
}
