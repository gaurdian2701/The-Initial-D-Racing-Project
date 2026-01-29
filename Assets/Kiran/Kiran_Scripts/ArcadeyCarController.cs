using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class ArcadeyCarController : CarController
    {
        [Header("Movement Values")]
        [SerializeField] private float mgripDuringLateralMovement = 1.0f;
        [SerializeField] private float mgripDuringSidewaysMovement = 2.0f;

        [Header("Drifting Values")] 
        [SerializeField] private float mfrontWheelGripDuringDrift = 3.5f;
        [SerializeField] private float mrearWheelGripDuringDrift = 1.25f;
        [SerializeField] private float mfrontWheelAdditionalVelocityDuringDrift = 5.0f;
        [SerializeField][Range(0.0f, 0.2f)] private float mslideVelocityDampingConstant = 0.15f;
        [SerializeField] [Range(0.0f, 30.0f)] private float mdampingLimit = 5.0f;
        
        public delegate void OnDrift(bool isDrifting);
        public event OnDrift onDrift;
        
        public bool mdriftInitiated = false;
        
        private float currentSlidingVelocity = 0.0f;
        private float mpreviousFrameDriftVelocity = 0.0f;

        
        protected override void Update()
        {
            base.Update();
            UpdateSteeringGrip();

            if (mdriftInitiated)
            {
                UpdateWheelValuesOnDrift();
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            
            if (mdriftInitiated)
            {
                DampenSpinWhileDrifting();
                AddFrontalWheelVelocity();
            }
        }

        private void UpdateSteeringGrip()
        {
            if (mcarRigidBody.linearVelocity.magnitude > 0.5f)
            {
                if (msteerInput > 0.01f || msteerInput < -0.01f)
                {
                    mfrontLeftWheel.SetGrip(mgripDuringSidewaysMovement);
                    mfrontRightWheel.SetGrip(mgripDuringSidewaysMovement);
                    mrearLeftWheel.SetGrip(mgripDuringSidewaysMovement);
                    mrearRightWheel.SetGrip(mgripDuringSidewaysMovement);
                }
                else
                {
                    mfrontLeftWheel.SetGrip(mgripDuringLateralMovement);
                    mfrontRightWheel.SetGrip(mgripDuringLateralMovement);
                    mrearLeftWheel.SetGrip(mgripDuringLateralMovement);
                    mrearRightWheel.SetGrip(mgripDuringLateralMovement);
                }
            }
        }

        private void UpdateWheelValuesOnDrift()
        {
            mfrontLeftWheel.SetGrip(mfrontWheelGripDuringDrift);
            mfrontRightWheel.SetGrip(mfrontWheelGripDuringDrift);
            
            mrearLeftWheel.SetGrip(mrearWheelGripDuringDrift);
            mrearRightWheel.SetGrip(mrearWheelGripDuringDrift);
        }
        
        private void DampenSpinWhileDrifting()
        {
            DampenSpinOnWheel(ref mrearLeftWheel);
            DampenSpinOnWheel(ref mrearRightWheel);
        }

        private void AddFrontalWheelVelocity()
        {
            mcarRigidBody.AddForceAtPosition(mfrontLeftWheel.GetTransform().forward * mfrontWheelAdditionalVelocityDuringDrift,
                mfrontLeftWheel.GetTransform().position);
            mcarRigidBody.AddForceAtPosition(mfrontRightWheel.GetTransform().forward * mfrontWheelAdditionalVelocityDuringDrift,
                mfrontRightWheel.GetTransform().position);
        }

        private void DampenSpinOnWheel(ref IWheel someWheel)
        {
            currentSlidingVelocity = Vector3.Dot(mcarRigidBody.GetPointVelocity(someWheel.GetTransform().position),
                someWheel.GetTransform().right);
            
            float velocityChange = Mathf.Abs(currentSlidingVelocity);
            float restorationForce = mslideVelocityDampingConstant * velocityChange;
            restorationForce = Mathf.Clamp(restorationForce, -mdampingLimit, mdampingLimit);

            Vector3 forceToBeApplied = -Mathf.Sign(currentSlidingVelocity) * restorationForce * mcarRigidBody.transform.right;
            mcarRigidBody.AddForceAtPosition(forceToBeApplied, someWheel.GetTransform().position);
        }

        public void ReceiveDriftInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                mdriftInitiated = true;
                onDrift?.Invoke(mdriftInitiated);
            }

            if (context.canceled)
            {
                mdriftInitiated = false;
                onDrift?.Invoke(mdriftInitiated);
            }
        }

        void OnDrawGizmos()
        {
            
        }
    }
}

