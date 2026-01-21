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
        
        [SerializeField] private Rigidbody mcarRigidBody;

        [Header("Car Forces Properties")] 
        [SerializeField] private float menginePower = 4.0f;
        [SerializeField][Range(0.1f, 1.5f)] private float mbrakingPower = 1.0f;
        [SerializeField] private float mairDragConstant = 0.003f;

        [Header("Car Steering Properties - Default values are from Ford Mustang 5th gen")] 
        [SerializeField] private float mwheelBaseLength = 2.72f;
        [SerializeField] private float mturnRadius = 11.5f;
        [SerializeField] private float mrearTrackLength = 1.6f;
        
        [Header("Car Wheels")]
        [SerializeField] private GameObject mfrontLeftWheelObject;
        [SerializeField] private GameObject mfrontRightWheelObject;
        [SerializeField] private GameObject mrearLeftWheelObject;
        [SerializeField] private GameObject mrearRightWheelObject;

        private IWheel mfrontLeftWheel;
        private IWheel mfrontRightWheel;
        private IWheel mrearLeftWheel;
        private IWheel mrearRightWheel;
        
        //Use these variables to control the car
        protected float msteerInput = 0.0f; // steer the car X
        protected float mthrottleInput = 0.0f; //move the car forwards or backwards Y
        
        private float mrightWheelSteerAngle = 0.0f;
        private float mleftWheelSteerAngle = 0.0f;

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

        public void ReceiveInput(InputAction.CallbackContext context)
        {
            if (misAIController)
            {
                return;
            }
            
            Vector2 input = context.ReadValue<Vector2>();
            
            if (input.x > 0)
            {
                msteerInput = 1.0f;
            }
            else if (input.x < 0)
            {
                msteerInput = -1.0f;
            }
            else
            {
                msteerInput = 0.0f;
            }

            if (input.y > 0)
            {
                mthrottleInput = 1.0f;
            }
            else if (input.y < 0)
            {
                mthrottleInput = -1.0f;
                
                //Are we moving forward?
                if (Vector3.Dot(mcarRigidBody.linearVelocity, mcarRigidBody.transform.forward) > 0.01f)
                {
                    mthrottleInput *= mbrakingPower;
                }
            }
            else
            {
                mthrottleInput = 0.0f;
            }
        }

        protected void FixedUpdate()
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
            mrearLeftWheel.ApplyThrottleForce(mthrottleInput * menginePower);
            mrearRightWheel.ApplyThrottleForce(mthrottleInput * menginePower);
            mfrontLeftWheel.ApplyThrottleForce(mthrottleInput * menginePower);
            mfrontRightWheel.ApplyThrottleForce(mthrottleInput * menginePower);
        }

        private void ApplyDragForces()
        {
            mdragVector = mairDragConstant * mcarRigidBody.linearVelocity.magnitude * -mcarRigidBody.linearVelocity;
            mcarRigidBody.AddForce(mdragVector);
        }

        protected void Update()
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
            mfrontLeftWheel.GetTransform().localRotation = Quaternion.AngleAxis(mleftWheelSteerAngle, Vector3.up);
            mfrontRightWheel.GetTransform().localRotation = Quaternion.AngleAxis(mrightWheelSteerAngle, Vector3.up);
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
