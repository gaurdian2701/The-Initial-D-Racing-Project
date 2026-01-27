using Car;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject _startPosition;
    [HideInInspector]
    public GameObject car;
    public bool shouldSpawnCar = true;

    public void SpawnCar(RacerData racerData)
    {
        if (shouldSpawnCar)
        {
           GameObject newCar = Instantiate(car, _startPosition.transform.position, transform.rotation);

           RacerIdentityTracker newRacerIdentity = newCar.GetComponentInChildren<RacerIdentityTracker>();
           newRacerIdentity.ChangeIconMaterial(racerData.minimapIconMaterial);
           newRacerIdentity.racerData = racerData;
           
           
           CarController carController = newCar.GetComponent<CarController>();
           CarStats stats = racerData.racerStats;
           
           carController.menginePower = stats.enginePower;
           carController.mbrakingPower =  stats.brakingPower;
           carController.mturnRadius  = stats.turnRadius;

           RaceProgress raceProgress = new RaceProgress();

           raceProgress.racer = racerData;
           ProgressTracking.Instance.racersProgress.Add(raceProgress);
        }
        
    }
}
