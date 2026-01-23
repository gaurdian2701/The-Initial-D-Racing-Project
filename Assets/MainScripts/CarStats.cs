using UnityEngine;

[System.Serializable]
public class CarStats
{
    public float enginePower = 4.0f;
    [Range(0.1f, 1.5f)] public float brakingPower = 1.0f;
    public float turnRadius = 11.5f;
}