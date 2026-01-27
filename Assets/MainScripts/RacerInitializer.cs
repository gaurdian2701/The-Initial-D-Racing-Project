using Car;
using UnityEngine;


public class RacerInitializer : MonoBehaviour
{
    private RacerDataHolder _racerDataHolder;

    [SerializeField] private Transform playerCarSpawnPoint;
    
    [SerializeField] private StartingPositionsList startingPositionsList;
    [SerializeField] private SpeedDisplay speedDisplay; 
    [SerializeField] private RaceCountDown raceCountDown;
    
    
    [HideInInspector]public GameObject playerCar;
    
    
    void Start()
    {
        if (RacerDataHolder.Instance == null)
        {
            Debug.LogWarning("No racer data found, Character initialization will not work.");
            return;
        } 
        
        _racerDataHolder = RacerDataHolder.Instance;
        
        playerCar = Instantiate(_racerDataHolder.selectedRacer.carPrefab, playerCarSpawnPoint.position, playerCarSpawnPoint.rotation);
        
        CarStats carStats = _racerDataHolder.selectedRacer.racerStats;
        CarController  carController = playerCar.GetComponent<CarController>();
        
        carController.menginePower = carStats.enginePower;
        carController.mbrakingPower =  carStats.brakingPower;
        carController.mturnRadius  = carStats.turnRadius;

        playerCar.GetComponentInChildren<RacerMinimapIcon>().ChangeIconMaterial(_racerDataHolder.selectedRacer.minimapIconMaterial);
        
        if (Camera.main != null)
        {
            CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
            cameraFollow.mfollowTarget = playerCar;
            cameraFollow.Startup();
            Camera.main.GetComponent<DynamicSpeedLines>()._carRB = playerCar.GetComponent<Rigidbody>();   
        }
        else Debug.LogError("No Camera is tagged as the main camera.");
        
        speedDisplay._carRB = playerCar.GetComponent<Rigidbody>();

        int i = 0;
        foreach (var carSpawn in startingPositionsList.startPositions)
        {
            carSpawn.car = _racerDataHolder.availableRacers[i].carAIPrefab;

            carSpawn.SpawnCar(_racerDataHolder.availableRacers[i]);
            i++;
        }
        
        
        raceCountDown.StartCountDown();
    }
}
