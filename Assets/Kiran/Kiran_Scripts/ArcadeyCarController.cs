using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class ArcadeyCarController : CarController
    {
        [SerializeField] private float mgripDuringLateralMovement = 1.0f;
        [SerializeField] private float mgripDuringSidewaysMovement = 2.0f;
        [SerializeField] private float mrearWheelGripDuringDrift = 1.25f;
        [SerializeField] private float mmaxSidewaysForceDuringDrift = 16.6f;
        [SerializeField] private float mvelocityThresholdForCounterSpinningForce = 1.0f;
        
        private bool mdriftInitiated = false;
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
            float sidewaysVelocityOnRearRightWheel = Vector3.Dot(
                mcarRigidBody.GetPointVelocity(mrearRightWheel.GetTransform().position),
                mrearRightWheel.GetTransform().right);
            
            float sidewaysVelocityOnRearLeftWheel = Vector3.Dot(
                mcarRigidBody.GetPointVelocity(mrearLeftWheel.GetTransform().position),
                mrearLeftWheel.GetTransform().right);

            if (sidewaysVelocityOnRearRightWheel > mmaxSidewaysForceDuringDrift - mvelocityThresholdForCounterSpinningForce ||
                sidewaysVelocityOnRearRightWheel < -(mmaxSidewaysForceDuringDrift - mvelocityThresholdForCounterSpinningForce))
            {
                mcarRigidBody.AddForceAtPosition(-sidewaysVelocityOnRearRightWheel
                                                 * mrearRightWheel.GetTransform().right, mrearRightWheel.GetTransform().position, ForceMode.Acceleration);
            }
            
            if (sidewaysVelocityOnRearLeftWheel > mmaxSidewaysForceDuringDrift - mvelocityThresholdForCounterSpinningForce ||
                sidewaysVelocityOnRearLeftWheel < -(mmaxSidewaysForceDuringDrift - mvelocityThresholdForCounterSpinningForce))
            {
                mcarRigidBody.AddForceAtPosition(-sidewaysVelocityOnRearLeftWheel
                                                 * mrearLeftWheel.GetTransform().right, mrearLeftWheel.GetTransform().position, ForceMode.Acceleration);
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
            if (mshowDebug)
            {
                Gizmos.color = Color.maroon;
                Vector3 sidewaysForceOnRightWheel = Vector3.Dot(mcarRigidBody.GetPointVelocity(mrearRightWheel.GetTransform().position),
                    mrearRightWheel.GetTransform().right) * mrearRightWheel.GetTransform().right;
                Gizmos.DrawLine(mrearRightWheel.GetTransform().position, mrearRightWheel.GetTransform().position + sidewaysForceOnRightWheel * 2.0f);
                Vector3 sidewaysForceOnLeftWheel = Vector3.Dot(mcarRigidBody.GetPointVelocity(mrearLeftWheel.GetTransform().position),
                    mrearLeftWheel.GetTransform().right) * mrearLeftWheel.GetTransform().right;
                Gizmos.DrawLine(mrearLeftWheel.GetTransform().position, mrearLeftWheel.GetTransform().position + sidewaysForceOnLeftWheel * 2.0f);
            }
        }
    }
}

