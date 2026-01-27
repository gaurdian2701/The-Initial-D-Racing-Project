using Car;
using UnityEngine;


public class RacerInitializer : MonoBehaviour
{
    private RacerDataHolder _racerDataHolder;

    [SerializeField] private Transform playerCarSpawnPoint;
    
    [SerializeField] private StartingPositionsList startingPositionsList;
    [SerializeField] private SpeedDisplay speedDisplay; 
    
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
            Camera.main.GetComponent<DynamicSpeedLines>()._carRB = playerCar.GetComponent<Rigidbody>();   
        }
        else Debug.LogError("No Camera is tagged as the main camera.");
        
        speedDisplay._carRB = playerCar.GetComponent<Rigidbody>();
        
        
        foreach (var carSpawn in startingPositionsList.startPositions)
        {
            int randomCharacter = Random.Range(0, _racerDataHolder.availableRacers.Count);
            
            carSpawn.car = _racerDataHolder.availableRacers[randomCharacter].carAIPrefab;

            carSpawn.SpawnCar(_racerDataHolder.availableRacers[randomCharacter]);
        }
    }
}
