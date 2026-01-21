using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class ArcadeyCarController : CarController
    {
        [SerializeField] private float mgripDuringLateralMovement = 1.0f;
        [SerializeField] private float mgripDuringSidewaysMovement = 4.0f;
        protected override void Update()
        {
            base.Update();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }
    }
}

