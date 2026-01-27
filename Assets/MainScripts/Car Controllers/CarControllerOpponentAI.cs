using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Car
{
    public class CarControllerOpponentAI : ArcadeyCarController
    {
        //Variables --> Exposed
        [Header("AI")] 
        [SerializeField]
        private GameObject targetPoint;
        
        [SerializeField] [Range(0, 1)]
        private float turnBreakBuffer = 0.0f;

        [SerializeField]
        private float turnBreakVelocityLimit = 0.0f;

        //Variables --> Not Exposed
        private Vector3 _dirForward;
        private Vector3 _dirToTarget;
        
        
        //Methods
        protected override void Update()
        {
            base.Update();
            
            UpdateDirection();
            
            float cross = (_dirToTarget.x * _dirForward.z) - (_dirToTarget.z * _dirForward.x);
            UpdateSteering(cross);
            
            float dot = Vector3.Dot(_dirForward, _dirToTarget);
            UpdateThrottle(dot, cross);
        }

        private void UpdateDirection()
        {
            _dirForward = transform.forward;
            //Debug.DrawLine(transform.position, transform.position + _dirForward*1000.0f, Color.red);
            
            _dirToTarget = targetPoint.transform.position - transform.position;
            _dirToTarget.y = 0.0f;
            _dirToTarget.Normalize();
            //Debug.DrawLine(transform.position, transform.position + _dirToTarget*1000.0f, Color.blue);
        }
        
        private void UpdateSteering(float crossProduct)
        {
            float gain = 10000f;
            float deadzone = 0.02f;

            float steer = Mathf.Clamp(crossProduct * gain, -1f, 1f);
            if (Mathf.Abs(crossProduct) < deadzone) steer = 0f;

            msteerInput = steer;
        }

        private void UpdateThrottle(float dotProduct, float crossProduct)
        {
            if (Mathf.Abs(crossProduct) > turnBreakBuffer
                && GetVelocity() > turnBreakVelocityLimit)
            {
                mthrottleInput = -1f;
            }
            else
            {
                mthrottleInput = 1f;
            }
        }
    }
}