using System;
using System.Collections.Generic;
using UnityEngine;

//This script is responsible for bringing data from racer select into the main game scene
public class RacerDataHolder : MonoBehaviour
{
    public static RacerDataHolder Instance;
    
    //Data carried over from racer select into game scene
    [HideInInspector] public RacerData  selectedRacer;
    [HideInInspector] public List<RacerData> availableRacers = new List<RacerData>();
    
    
    //make sure there is only one Eligible Racer data holder in the scene and dont destroy on load
    private void Awake()
    {
        RacerDataHolder[] dataHolders = FindObjectsByType<RacerDataHolder>(FindObjectsSortMode.None);

        if (dataHolders.Length > 1)
        {
            
            Destroy(gameObject);
        }
        
        Instance = this;
        
        DontDestroyOnLoad(gameObject);
    }


    public void OnNewRacerSelected(RacerData racer, RacerList allRacers)
    {
        availableRacers.Clear();
        
        selectedRacer = racer;

        foreach (var racerData in allRacers.allRacers)
        {
            if (racerData.racerId != selectedRacer.racerId) availableRacers.Add(racerData);
        }
    }
}
