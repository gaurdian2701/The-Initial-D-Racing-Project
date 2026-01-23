using Car;
using UnityEngine;
/// <summary>
/// ////////////////////// THIS SCRIPT IS OBSOLETE AND SHOULD NOT BE USED
/// </summary>


//script is very barebones for now due to some unceirtanties in implementation and stuff
public class CarCharacterInitializer : MonoBehaviour
{
    private CarController _carController;
    private RacerDataHolder _racerDataHolder;
    [SerializeField] private MeshRenderer carRenderer;
    
    void Start()
    {
        if (RacerDataHolder.Instance == null)
        {
            Debug.LogWarning("No racer data found, Character initialization will not work.");
            return;
        } 
        
        _racerDataHolder = RacerDataHolder.Instance;
        _carController = GetComponent<CarController>();
        
        //TODO apply stats to car controller once values are made public
        CarStats carStats = _racerDataHolder.selectedRacer.racerStats;
        GameObject carVisuals = Instantiate(_racerDataHolder.selectedRacer.carPrefab, transform);
        carRenderer.material.color = _racerDataHolder.selectedRacer.racerColor;

    }

    
}
