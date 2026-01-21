using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Car
{
    public interface IWheel
    {
        public void ApplyThrottleForce(float someThrottleForce);
        public Transform GetTransform();
        public bool IsGrounded();
    }
    
    public class CarWheel : MonoBehaviour, IWheel
    {
        [SerializeField] private Rigidbody mparentRigidbody;
        [SerializeField] private GameObject mspring;
        [SerializeField] private GameObject mwheelMesh;

        public bool mshowSpringDebug = false;
        public bool mshowWheelDebug = false;
        public bool misGrounded = false;

        #region Suspension properties

        [Header("Spring Properties")] [SerializeField]
        private float mspringConstant = 1.0f;

        [SerializeField] private float mspringRestLength = 1.0f;
        [SerializeField] private float mspringTravelLength = 0.5f;
        [SerializeField] private float mspringDampingConstant = 1.0f;
        [SerializeField] private float mwheelRadius = 1.0f;

        private Vector3 mfinalWheelRestingPosition = Vector3.zero;
        private RaycastHit mspringCastHitInfo;

        #endregion

        #region Wheel properties

        [Header("Wheel Properties")] public bool misLeftWheel = false;
        [SerializeField] [Range(0.0f, 10.0f)] private float mgrip = 1.0f;
        [SerializeField] [Range(0.0f, 0.1f)] private float mrollingFrictionConstant = 0.02f;

        #endregion

        #region Physics and Forces variables

        private Vector3 mspringForce = Vector3.zero; //This is also the N value in kinetic friction F = mu * N
        private Vector3 mslidingFrictionForce = Vector3.zero;
        private Vector3 mrollingFrictionForce = Vector3.zero;
        private Vector3 mwheelVelocity = Vector3.zero;

        private float mspringLengthCurrentFrame = 0.0f;
        private float mspringLengthPreviousFrame = 0.0f;
        private float mspringVelocity = 0.0f;
        private float mminspringLength = 0.0f;
        private float mmaxspringLength = 0.0f;
        private float mparentMass = 0.0f;

        #endregion

        #region Debug

        private Vector3 mdebugWheelProbePoint = Vector3.zero;
        private Vector3 mdebugCounterSlideForce = Vector3.zero;
            
                

        #endregion

        void Start()
        {
            mspringLengthCurrentFrame = mspringRestLength;
            mspringLengthPreviousFrame = mspringLengthCurrentFrame;
            mminspringLength = mspringRestLength - mspringTravelLength;
            mmaxspringLength = mspringRestLength + mspringTravelLength;
            mparentMass = mparentRigidbody.mass;
        }

        void FixedUpdate()
        {
            mwheelVelocity = mparentRigidbody.GetPointVelocity(transform.position);
            
            CalculateWheelRestingPosition();
            RotateWheels();
            CalculateRestorationForce();
            CalculateRollingFriction();
            CalculateSlidingFriction();
            ApplyWheelForces();
        }

        public Transform GetTransform()
        {
            return transform;
        }

        public bool IsGrounded()
        {
            return misGrounded;
        }

        private void CalculateWheelRestingPosition()
        {
            //Using raycasts to determine where to place the wheel
            if (Physics.Raycast(mspring.transform.position, -mparentRigidbody.transform.up, out mspringCastHitInfo,
                    mspringRestLength + mwheelRadius))
            {
                //If raycast hits the ground, place the wheel on the ground and apply lifting/spring forces
                //to the car since the spring "compresses"
                mdebugWheelProbePoint = mspringCastHitInfo.point;
                mfinalWheelRestingPosition = mspringCastHitInfo.point + mwheelRadius * mparentRigidbody.transform.up;
                mspringLengthCurrentFrame = mspringCastHitInfo.distance;
                misGrounded = true;
            }
            else
            {
                //Else, simply hang the spring in the air at rest length
                mdebugWheelProbePoint = mspring.transform.position -
                                        (mspringRestLength + mwheelRadius)
                                        * mparentRigidbody.transform.up;
                mfinalWheelRestingPosition = mwheelMesh.transform.position;
                mspringLengthCurrentFrame = mspringRestLength;
                misGrounded = false;
            }

            mspringLengthCurrentFrame = Mathf.Clamp(mspringLengthCurrentFrame, mminspringLength, mmaxspringLength);
            mwheelMesh.transform.position = mfinalWheelRestingPosition;
        }

        private void RotateWheels()
        {
            float rollingDirection = Mathf.Sign(Vector3.Dot(mwheelVelocity, mparentRigidbody.transform.forward)); //are we moving forwards or backwards?
            float angularVelocity = rollingDirection * mwheelVelocity.magnitude / mwheelRadius; //w = v/r radians per second
            float wheelRollingRotationStep = mwheelMesh.transform.localEulerAngles.x; 
            wheelRollingRotationStep += angularVelocity;
            mwheelMesh.transform.localEulerAngles = new Vector3(
                wheelRollingRotationStep, mwheelMesh.transform.localEulerAngles.y, mwheelMesh.transform.localEulerAngles.z);
        }

        //NOTE: ISOLATE SPRING LOGIC - IT DOES NOT CARE ABOUT WHEEL POSITIONS AND OUTSIDE FORCES. ONLY IT'S OWN LENGTH
        private void CalculateRestorationForce()
        {
            mspringVelocity = (mspringLengthCurrentFrame - mspringLengthPreviousFrame) / Time.fixedDeltaTime;
            mspringLengthPreviousFrame = mspringLengthCurrentFrame;

            //Calculate CHANGE in spring's length over time and take that as velocity to multiply with the damping constant
            float displacement = mspringRestLength - mspringLengthCurrentFrame;
            float restorationForce = mspringConstant * displacement;
            float dampingForce = mspringVelocity * mspringDampingConstant;

            mspringForce = (restorationForce - dampingForce)
                           * mparentRigidbody.transform.up;
        }
        
        private void CalculateSlidingFriction()
        {
            //Problem: -m*v works but when we get to high speeds, even when we don't need sideways friction, we'll
            //still have sideways forces getting applied even when we don't have to since the velocity magnitude will be high.
            
            float slideVelocity = Vector3.Dot(mwheelVelocity, transform.right);
            float maxFriction = mgrip * mspringForce.magnitude;
            
            //F = m * a
            float desiredSidewaysFriction = -mparentMass * slideVelocity / Time.fixedDeltaTime;

            desiredSidewaysFriction = Mathf.Clamp(desiredSidewaysFriction, -maxFriction, maxFriction);
            mslidingFrictionForce = desiredSidewaysFriction * transform.right;

            //Debug Code
            mdebugCounterSlideForce = desiredSidewaysFriction * transform.right;
            if (!misGrounded)
            {
                mdebugCounterSlideForce = Vector3.zero;
            }
        }

        private void CalculateRollingFriction()
        {
            //Calculate rolling friction which is basically a linear function of velocity with a constant
            float rollingVelocity = Vector3.Dot(mwheelVelocity, transform.forward);
            float rollingFriction = mrollingFrictionConstant * mspringForce.magnitude;

            if (rollingVelocity < 0.1f && rollingVelocity > -0.1f)
            {
                mrollingFrictionForce = Vector3.zero;
                return;
            }
            
            mrollingFrictionForce = -Mathf.Sign(rollingVelocity) * rollingFriction * transform.forward;
        }

        private void ApplyWheelForces()
        {
            if (!misGrounded)
            {
                mslidingFrictionForce = Vector3.zero;
                mrollingFrictionForce = Vector3.zero;
            }
            mparentRigidbody.AddForceAtPosition(mspringForce + mslidingFrictionForce + mrollingFrictionForce, mspring.transform.position);
        }

        public void ApplyThrottleForce(float someThrottleForce)
        {
            if (misGrounded)
            {
                mparentRigidbody.AddForceAtPosition(someThrottleForce * transform.forward, transform.position);
            }
        }

        void OnDrawGizmos()
        {
            if (mshowWheelDebug)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(mfinalWheelRestingPosition, mwheelRadius);

                Gizmos.color = Color.magenta;
                Gizmos.DrawCube(mdebugWheelProbePoint, new Vector3(0.1f, 0.1f, 0.1f));

                Gizmos.color = Color.orange;
                Gizmos.DrawLine(transform.position,
                    transform.position + mparentRigidbody.GetPointVelocity(transform.position) * 3.0f);

                Gizmos.color = Color.darkGreen;
                Gizmos.DrawLine(transform.position, transform.position + mdebugCounterSlideForce);
            }

            if (mshowSpringDebug)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(mspring.transform.position, 0.1f);

                Gizmos.color = Color.green;
                Gizmos.DrawLine(mspring.transform.position, mdebugWheelProbePoint);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(mspring.transform.position, transform.position + mspringForce);
            }
        }
    }
}