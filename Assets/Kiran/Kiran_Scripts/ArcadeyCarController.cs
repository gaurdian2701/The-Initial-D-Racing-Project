using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class ArcadeyCarController : CarController
    {
        [SerializeField] private float mgripDuringLateralMovement = 1.0f;
        [SerializeField] private float mgripDuringSidewaysMovement = 2.0f;
        [SerializeField] private float mrearGripDuringDrifting = 0.8f;

        private bool mdriftButtonPressed = false;
        protected override void Update()
        {
            base.Update();
            UpdateSteeringGrip();
            UpdateWheelValuesOnDrift();
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
            if (mdriftButtonPressed)
            {
                mrearLeftWheel.SetGrip(mrearGripDuringDrifting);
                mrearRightWheel.SetGrip(mrearGripDuringDrifting);
                
            }
        }

        public void ReceiveDriftInput(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                mdriftButtonPressed = true;
            }

            if (context.canceled)
            {
                mdriftButtonPressed = false;
            }
        }
    }
}

