using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour, IFollowTarget
    {
        public bool mshowDebug = false;
        public bool misAIController = false;
        
        [SerializeField] protected Rigidbody mcarRigidBody;

        [Header("Car Forces Properties")] 
        public float menginePower = 4.0f;
        [Range(0.1f, 3.0f)] public float mbrakingPower = 1.0f;
        public float mairDragConstant = 0.003f;

        [Header("Car Steering Properties - Default values are from Ford Mustang 5th gen")] 
        public float mwheelBaseLength = 2.72f;
        public float mturnRadius = 11.5f;
        public float mrearTrackLength = 1.6f;
        
        [Header("Car Wheels")]
        [SerializeField] protected GameObject mfrontLeftWheelObject;
        [SerializeField] protected GameObject mfrontRightWheelObject;
        [SerializeField] protected GameObject mrearLeftWheelObject;
        [SerializeField] protected GameObject mrearRightWheelObject;

        protected IWheel mfrontLeftWheel;
        protected IWheel mfrontRightWheel;
        protected IWheel mrearLeftWheel;
        protected IWheel mrearRightWheel;
        
        //Use these variables to control the car
        protected float msteerInput = 0.0f; // steer the car X
        protected float mthrottleInput = 0.0f; //move the car forwards or backwards Y
        
        protected float mrightWheelSteerAngle = 0.0f;
        protected float mleftWheelSteerAngle = 0.0f;

        private Vector3 mdragVector = Vector3.zero;
        
        void Awake()
        {
            mfrontLeftWheel = mfrontLeftWheelObject.GetComponent<IWheel>();
            mfrontRightWheel = mfrontRightWheelObject.GetComponent<IWheel>();
            mrearLeftWheel = mrearLeftWheelObject.GetComponent<IWheel>();
            mrearRightWheel = mrearRightWheelObject.GetComponent<IWheel>();

            if (mfrontLeftWheel == null || mfrontRightWheel == null || mrearLeftWheel == null ||
                mrearRightWheel == null)
            {
                Debug.LogError("Wheels must implement IWheel interface!");
            }
        }

        public float GetVelocity()
        {
            return mcarRigidBody.linearVelocity.magnitude;
        }

        public void ReceiveThrottleInput(InputAction.CallbackContext context)
        {
            if (misAIController)
            {
                return;
            }
            
            float input = context.ReadValue<float>();

            if (input > -0.01f && input < 0.01f)
            {
                mthrottleInput = 0.0f;
            }
            else
            {
                mthrottleInput = input;
                
                if (input < -0.01f)
                {
                    mthrottleInput = Mathf.Clamp(input, -0.5f, -0.1f);
                    
                    //Are we moving forward? If yes, then add extra power to brake the car
                    if (Vector3.Dot(mcarRigidBody.linearVelocity, mcarRigidBody.transform.forward) > 0.01f)
                    {
                        mthrottleInput *= mbrakingPower;
                    }
                }
            }
        }

        public void ReceiveSteerInput(InputAction.CallbackContext context)
        {
            if (misAIController)
            {
                return;
            }
            
            float input = context.ReadValue<float>();
            
            if (input > 0.01f || input < -0.01f)
            {
                msteerInput = input;
            }
            else
            {
                msteerInput = 0.0f;
            }
        }

        protected virtual void FixedUpdate()
        {
            ThrottleCar();
            ApplyDragForces();
        }

        public bool IsGrounded()
        {
            return mfrontLeftWheel.IsGrounded() ||
                   mfrontRightWheel.IsGrounded() ||
                   mrearLeftWheel.IsGrounded() ||
                   mrearRightWheel.IsGrounded();
        }
        
        //Move car forwards or backwards
        protected void ThrottleCar()
        {
            mfrontLeftWheel.ApplyThrottleForce(mthrottleInput * menginePower);
            mfrontRightWheel.ApplyThrottleForce(mthrottleInput * menginePower);
            mrearLeftWheel.ApplyThrottleForce(mthrottleInput * menginePower);
            mrearRightWheel.ApplyThrottleForce(mthrottleInput * menginePower);
        }

        private void ApplyDragForces()
        {
            mdragVector = mairDragConstant * mcarRigidBody.linearVelocity.magnitude * -mcarRigidBody.linearVelocity;
            mcarRigidBody.AddForce(mdragVector);
        }

        protected virtual void Update()
        {
            SteerCar();
        }

        //Steer the car left or right
        private void SteerCar()
        {
            if (msteerInput > 0.0f) //If we are steering right
            {
                mrightWheelSteerAngle = Mathf.Rad2Deg * Mathf.Atan2(mwheelBaseLength, mturnRadius - mrearTrackLength / 2) * msteerInput;
                mleftWheelSteerAngle = Mathf.Rad2Deg * Mathf.Atan2(mwheelBaseLength, mturnRadius + mrearTrackLength / 2) * msteerInput;
            }
            else if(msteerInput < 0.0f) //If we are steering left
            {
                mrightWheelSteerAngle = Mathf.Rad2Deg * Mathf.Atan2(mwheelBaseLength, mturnRadius + mrearTrackLength / 2) * msteerInput;
                mleftWheelSteerAngle = Mathf.Rad2Deg * Mathf.Atan2(mwheelBaseLength, mturnRadius - mrearTrackLength / 2) * msteerInput;
            }
            else //Don't steer at all
            {
                mrightWheelSteerAngle = 0.0f;
                mleftWheelSteerAngle = 0.0f;
            }
            mfrontLeftWheel.SteerWheel(mleftWheelSteerAngle);
            mfrontRightWheel.SteerWheel(mrightWheelSteerAngle);
        }
        
        void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            if (mfrontLeftWheel != null)
            {
                Handles.Label(mfrontLeftWheel.GetTransform().position, "Steer angle: " + mleftWheelSteerAngle);
            }

            if (mfrontRightWheel != null)
            {
                Handles.Label(mfrontRightWheel.GetTransform().position, "Steer angle: " + mrightWheelSteerAngle);
            }
        }
    }
}
