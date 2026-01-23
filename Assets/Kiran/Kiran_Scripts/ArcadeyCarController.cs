using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class ArcadeyCarController : CarController
    {
        public bool mshowDebug = false;
        
        [SerializeField] private float mgripDuringLateralMovement = 1.0f;
        [SerializeField] private float mgripDuringSidewaysMovement = 2.0f;
        [SerializeField] private float mrearWheelGripDuringDrift = 1.25f;

        private bool mdriftInitiated = false;
        protected override void Update()
        {
            base.Update();
            UpdateSteeringGrip();

            if (mdriftInitiated)
            {
                UpdateWheelValuesOnDrift();
                MaintainSteerWhileDrifting();
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
            mrearLeftWheel.SetGrip(mrearWheelGripDuringDrift);
            mrearRightWheel.SetGrip(mrearWheelGripDuringDrift);
        }

        private void MaintainSteerWhileDrifting()
        {
            
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
            if (mshowDebug)
            {
                Gizmos.color = Color.maroon;
            }
        }
    }
}

