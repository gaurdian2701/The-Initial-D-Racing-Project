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
        [SerializeField] [Range(0, 1)]
        private float turnBreakBuffer = 0.0f;

        [SerializeField]
        private float turnBreakVelocityLimit = 0.0f;
        
        [SerializeField]
        LayerMask obstacleLayerMask;
        
        [SerializeField]
        private float originOffsetForBreaking = 5.0f;
        
        [SerializeField]
        private Vector3 halfExtentsForBreaking;
        
        [SerializeField]
        private float obstacleHitRange = 2.0f;

        //Variables --> Not Exposed
        private GameObject targetPoint;
        
        private Vector3 _dirForward;
        private Vector3 _dirToTarget;
        private Vector3 originForReversing;
        private Vector3 originForBreaking;
        private float obstacleRangeForBreaking = 20;
        private float _sharpTurnBreakVelocityLimit = 0.0f;
        private float _sharpTurnDetectionRange = 50.0f;

        private float speed;
        
        bool didHitRailing = false;

        protected override void Update()
        {
            base.Update();
         
            //print(GetVelocity());
            
            UpdateDirection();
            
            float cross = (_dirToTarget.x * _dirForward.z) - (_dirToTarget.z * _dirForward.x);
            UpdateSteering(cross);
            
            float dot = Vector3.Dot(_dirForward, _dirToTarget);
            UpdateThrottle(dot, cross);
        }

        private void UpdateDirection()
        {
            _dirForward = transform.forward;

            if (!targetPoint) return;
            
            _dirToTarget = targetPoint.transform.position - transform.position;
            _dirToTarget.y = 0.0f;
            _dirToTarget.Normalize();
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
            RaycastHit hitInfo;
            _dirForward.Normalize();

            bool withinReversingRange = false;
            originForReversing = transform.position;
            originForReversing.y += originOffsetForBreaking;
            withinReversingRange = Physics.BoxCast(originForReversing, halfExtentsForBreaking,_dirForward, 
                out hitInfo, Quaternion.identity, obstacleRangeForBreaking, obstacleLayerMask);

            bool hitRoad = true;
            for (int i = 1; i < 8; i++)
            {
                originForBreaking = transform.position + (_dirForward*_sharpTurnDetectionRange)/i;
                originForBreaking.y += 50;
                hitRoad = Physics.Raycast(originForBreaking, Vector3.down, out hitInfo, Mathf.Infinity, obstacleLayerMask);
                if (!hitRoad) break;
            }
            
            if (didHitRailing)
            {
                mthrottleInput = -0.25f;
                msteerInput *= -1;
            }
            else
            {
                didHitRailing = Physics.BoxCast(originForReversing, halfExtentsForBreaking,_dirForward, 
                    out hitInfo, Quaternion.identity, obstacleHitRange, obstacleLayerMask);
                
                if ((Mathf.Abs(crossProduct) > turnBreakBuffer
                    && GetVelocity() > turnBreakVelocityLimit)
                    || 
                    (!hitRoad 
                     && GetVelocity() > _sharpTurnBreakVelocityLimit)
                    )
                {
                    mthrottleInput = -1f;
                }
                else
                {
                    mthrottleInput = 1f;
                }
            }

            if (!withinReversingRange) didHitRailing = false;
        }

        void OnDrawGizmos()
        {
            Vector3 start;
            Vector3 end;
            
            for (int i = 1; i < 8; i++)
            {
                start = transform.position + _dirForward * _sharpTurnDetectionRange/i;
                start.y += 10;
                end = start + Vector3.down * 100;

                Gizmos.color = Color.yellow;

                // start & end boxes
                Gizmos.DrawWireCube(start, halfExtentsForBreaking * 2f);
                Gizmos.DrawWireCube(end, halfExtentsForBreaking * 2f);

                // connect corners
                DrawBoxConnections(start, end, halfExtentsForBreaking);
            }
            
            
            start = originForReversing;
            end = start + _dirForward * _sharpTurnDetectionRange;

            Gizmos.color = Color.blue;

            // start & end boxes
            Gizmos.DrawWireCube(start, halfExtentsForBreaking * 2f);
            Gizmos.DrawWireCube(end, halfExtentsForBreaking * 2f);

            // connect corners
            DrawBoxConnections(start, end, halfExtentsForBreaking);
        }

        void DrawBoxConnections(Vector3 start, Vector3 end, Vector3 half)
        {
            Vector3[] offsets =
            {
                new Vector3( half.x,  half.y,  half.z),
                new Vector3(-half.x,  half.y,  half.z),
                new Vector3( half.x, -half.y,  half.z),
                new Vector3(-half.x, -half.y,  half.z),
                new Vector3( half.x,  half.y, -half.z),
                new Vector3(-half.x,  half.y, -half.z),
                new Vector3( half.x, -half.y, -half.z),
                new Vector3(-half.x, -half.y, -half.z),
            };

            foreach (var o in offsets)
                Gizmos.DrawLine(start + o, end + o);
        }


        public void SetTargetPoint(GameObject target)
        {
            targetPoint = target;
        }

        public void SetObstacleRangeForBreaking(float value)
        {
            obstacleRangeForBreaking = value;
        }

        public void SetSharpTurnBreakVelocityLimit(float value)
        {
            _sharpTurnBreakVelocityLimit = value;
        }

        public void SetSharpTurnDetectionRange(float value)
        {
            _sharpTurnDetectionRange = value;
        }
    }
}