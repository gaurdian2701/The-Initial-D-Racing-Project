using UnityEngine;

public class SpawnCarOnStart : MonoBehaviour
{
    [SerializeField]
    private GameObject _startPosition;
    [HideInInspector]
    public GameObject car;
    public bool shouldSpawnCar = true;

    public void SpawnCar()
    {
        if (shouldSpawnCar)
        {
            Instantiate(car, _startPosition.transform.position, transform.rotation);
        }
    }
}
