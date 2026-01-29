using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class ArcadeyCarController : CarController
    {
        [Header("Movement Values")] [SerializeField]
        private float mgripDuringLateralMovement = 1.0f;

        [SerializeField] private float mgripDuringSidewaysMovement = 2.0f;

        [Header("Drifting Values")] [SerializeField]
        private float mfrontWheelGripDuringDrift = 3.5f;

        [SerializeField] private float mrearWheelGripDuringDrift = 1.25f;
        [SerializeField] private float mfrontWheelAdditionalVelocityDuringDrift = 5.0f;
        [SerializeField] [Range(0.0f, 0.2f)] private float mslideVelocityDampingConstant = 0.15f;
        [SerializeField] [Range(0.0f, 30.0f)] private float mdampingLimit = 5.0f;
        [SerializeField] [Range(1.0f, 20.0f)] private float mforceForStuckRecovery = 10.0f;

        public delegate void OnDrift(bool isDrifting);

        public event OnDrift onDrift;

        public bool mdriftInitiated = false;

        private float currentSlidingVelocity = 0.0f;
        private float mpreviousFrameDriftVelocity = 0.0f;

        private bool misStuck = false;

        protected override void Update()
        {
            base.Update();
            UpdateSteeringGrip();

            if (mdriftInitiated)
            {
                UpdateGripValuesOnDrift();
            }

            CheckIfStuck();
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

        public void ReceiveActivateUnstuckInput(InputAction.CallbackContext context)
        {
            float input = context.ReadValue<float>();

            if (misStuck)
            {
                if (input > 0.0f)
                {
                    ApplyUpwardsForceToFrontWheels();
                }
                else if (input < 0.0f)
                {
                    ApplyUpwardsForceToRearWheels();
                }
            }
        }

        protected void ApplyUpwardsForceToFrontWheels()
        {
            mcarRigidBody.AddForceAtPosition(mforceForStuckRecovery * Vector3.up,
                mfrontLeftWheel.GetTransform().position);
            mcarRigidBody.AddForceAtPosition(mforceForStuckRecovery * Vector3.up,
                mfrontRightWheel.GetTransform().position);
        }

        protected void ApplyUpwardsForceToRearWheels()
        {
            mcarRigidBody.AddForceAtPosition(mforceForStuckRecovery * Vector3.up,
                mrearLeftWheel.GetTransform().position);
            mcarRigidBody.AddForceAtPosition(mforceForStuckRecovery * Vector3.up,
                mrearRightWheel.GetTransform().position);
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

        protected void CheckIfStuck()
        {
            misStuck = IsGrounded() && (IsToppled() || IsFlippedOnSide());
        }

        protected bool IsToppled()
        {
            return Vector3.Dot(mcarRigidBody.transform.up, Vector3.up) < 0;
        }

        protected bool IsFlippedOnSide()
        {
            return !(mfrontLeftWheel.IsGrounded() && mfrontRightWheel.IsGrounded() && mrearLeftWheel.IsGrounded() &&
                     mrearRightWheel.IsGrounded()) &&
                   Physics.Raycast(mcarRigidBody.transform.position, Vector3.down, 3.0f, LayerMask.GetMask("Track"));
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

        private void UpdateGripValuesOnDrift()
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
            mcarRigidBody.AddForceAtPosition(
                mfrontLeftWheel.GetTransform().forward * mfrontWheelAdditionalVelocityDuringDrift,
                mfrontLeftWheel.GetTransform().position);
            mcarRigidBody.AddForceAtPosition(
                mfrontRightWheel.GetTransform().forward * mfrontWheelAdditionalVelocityDuringDrift,
                mfrontRightWheel.GetTransform().position);
        }

        private void DampenSpinOnWheel(ref IWheel someWheel)
        {
            currentSlidingVelocity = Vector3.Dot(mcarRigidBody.GetPointVelocity(someWheel.GetTransform().position),
                someWheel.GetTransform().right);

            float velocityChange = Mathf.Abs(currentSlidingVelocity);
            float restorationForce = mslideVelocityDampingConstant * velocityChange;
            restorationForce = Mathf.Clamp(restorationForce, -mdampingLimit, mdampingLimit);

            Vector3 forceToBeApplied =
                -Mathf.Sign(currentSlidingVelocity) * restorationForce * mcarRigidBody.transform.right;
            mcarRigidBody.AddForceAtPosition(forceToBeApplied, someWheel.GetTransform().position);
        }


        void OnDrawGizmos()
        {
        }
    }
}