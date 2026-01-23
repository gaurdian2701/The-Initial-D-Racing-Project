using Car;
using UnityEngine;
using UnityEngine.Serialization;

public class RacerInitializer : MonoBehaviour
{
    private RacerDataHolder _racerDataHolder;

    [SerializeField] private Transform playerCarSpawnPoint;
    
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

        if (Camera.main != null)
        {
            CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
            cameraFollow.mfollowTarget = playerCar;
        }
        else Debug.LogError("No Camera is tagged as the main camera.");

    }
}
