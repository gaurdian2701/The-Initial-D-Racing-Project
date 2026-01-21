using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class CarControllerOpponentAI : CarController
    {
        private void Start()
        {
            Debug.Log("CarControllerOpponentAI started");
        }

        protected override void Update()
        {
            base.Update();

            msteerInput = 1;
            mthrottleInput = 0.5f;
        }
    }
}