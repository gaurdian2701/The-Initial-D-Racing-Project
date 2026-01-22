using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class CarControllerOpponentAI : CarController
    {
        //Variables
        [Header("Misc")] 
        [SerializeField]
        private GameObject TargetPoint;
        
        
        //Methods
        private void Start()
        {
            Debug.Log("CarControllerOpponentAI started");
        }

        protected override void Update()
        {
            base.Update();
            
            mthrottleInput = 1f;
            
            Vector3 dirForward = transform.forward;
            Debug.DrawLine(transform.position, transform.position + dirForward*1000.0f, Color.red);
            
            //find out if point is on left or right side of me
            Vector3 dirToTarget = TargetPoint.transform.position - transform.position;
            Debug.DrawLine(transform.position, transform.position + dirToTarget*1000.0f, Color.yellow);
            
            float crossProduct = (dirForward.x * dirToTarget.z) - (dirForward.z * dirToTarget.x);
            if (crossProduct > 0) //dirForward on the right of dirToTarget
            {
                msteerInput = -1.0f;
            }
            else if (crossProduct < 0) //dirForward on the left of dirToTarget
            {
                msteerInput = 1.0f;
            }
            else
            {
                msteerInput = 0.0f;
            }
        }
    }
}