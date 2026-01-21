using System;
using Unity.VisualScripting;
using UnityEngine;

public class OutOfBoundsBox : MonoBehaviour
{
    //private BoxCollider _boxCollider;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<OutOfBoundsRetriever>()._rewind == false)
        {
            other.GetComponent<OutOfBoundsRetriever>().OutOfBounds();
        }
    }
}
