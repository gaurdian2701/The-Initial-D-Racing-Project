using UnityEngine;

public class SimpleRotator : MonoBehaviour
{
    public Vector3 axis = new Vector3(0,1,0);
    public float rotationSpeed = 45;


    // Update is called once per frame
    void Update()
    {
        transform.Rotate(axis.x * rotationSpeed * Time.deltaTime,axis.y * rotationSpeed * Time.deltaTime,axis.z * rotationSpeed * Time.deltaTime);
    }
}
