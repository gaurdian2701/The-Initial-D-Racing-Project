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
        [SerializeField] private float mmaxSidewaysForceDuringDrift = 10.0f;
        [SerializeField][Range(0.0f, 1.0f)] private float mslideVelocityDampingConstant = 0.5f;
        
        private bool mdriftInitiated = false;
        
        private float mcurrentFrameDriftVelocity = 0.0f;
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

        private void DampenSpinOnWheel(ref IWheel someWheel)
        {
            mcurrentFrameDriftVelocity = Vector3.Dot(mcarRigidBody.GetPointVelocity(someWheel.GetTransform().position),
                someWheel.GetTransform().right);
            
            Debug.Log($"CURRENT FRAME VELOCITY: {mcurrentFrameDriftVelocity}");
            Debug.Log($"MAX VELOCITY: {mmaxSidewaysForceDuringDrift}");

            if (Mathf.Abs(mcurrentFrameDriftVelocity) > mmaxSidewaysForceDuringDrift)
            {
                float velocityChange = Mathf.Abs(mcurrentFrameDriftVelocity) - mmaxSidewaysForceDuringDrift;
                float restorationForce = mslideVelocityDampingConstant * velocityChange;

                Vector3 forceToBeApplied = -Mathf.Sign(mcurrentFrameDriftVelocity) * restorationForce * mcarRigidBody.transform.right;
                mcarRigidBody.AddForceAtPosition(forceToBeApplied, someWheel.GetTransform().position);
            }
        }

        public void ReceiveDriftInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                mdriftInitiated = true;
            }

            if (context.canceled)
            {
                mdriftInitiated = false;
            }
        }

        void OnDrawGizmos()
        {

        }
    }
}

