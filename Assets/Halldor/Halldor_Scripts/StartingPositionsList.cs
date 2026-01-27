using System.Collections.Generic;
using UnityEngine;

public class StartingPositionsList : MonoBehaviour
{
    public List<SpawnCarOnStart> startPositions;

    public void SpawnAllCars()
    {
        foreach (SpawnCarOnStart spawnCarOnStart in startPositions)
        {
            spawnCarOnStart.SpawnCar();
        }
    }
}
