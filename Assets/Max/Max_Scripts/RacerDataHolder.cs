using System;
using System.Collections.Generic;
using UnityEngine;

//This script is responsible for bringing data from racer select into the main game scene
public class RacerDataHolder : MonoBehaviour
{
    public static RacerDataHolder instance;
    
    //Data carried over from racer select into game scene
    [HideInInspector] public RacerData  selectedRacers;
    [HideInInspector] public List<RacerData> racerDataList = new List<RacerData>();
    
    
    //make sure there is only one Eligible Racer data holder in the scene and dont destroy on load
    private void Awake()
    {
        RacerDataHolder[] dataHolders = FindObjectsByType<RacerDataHolder>(FindObjectsSortMode.None);

        if (dataHolders.Length > 1)
        {
            
            Destroy(gameObject);
        }
        
        instance = this;
        
        DontDestroyOnLoad(gameObject);
    }
}
