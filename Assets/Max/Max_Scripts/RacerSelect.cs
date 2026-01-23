using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RacerSelect : MonoBehaviour
{
    [SerializeField] private GameObject racerGrid;
    [SerializeField] private GameObject racerBoxPrefab;
    [SerializeField] private GameObject startButton;
    
    //probs want to swap this to scene build index later maybe
    [SerializeField] private string gameSceneName;
    
    public RacerList allRacers;
    
    [HideInInspector] public List<RacerBox> spawnedRacerBoxes = new List<RacerBox>();
    private RacerDataHolder _racerDataHolder;

    private CarPreview _preview;

    private void Start()
    {
        _racerDataHolder = RacerDataHolder.Instance;
        
        startButton.SetActive(false);

        _preview = FindAnyObjectByType<CarPreview>();
        //spawn the actual ui elements for the racers
        foreach (var racerData in allRacers.allRacers)
        {
            RacerBox newRacerBox = Instantiate(racerBoxPrefab, racerGrid.transform).GetComponent<RacerBox>();
            newRacerBox.Initialize(racerData, this);
            spawnedRacerBoxes.Add(newRacerBox);
        }
    }
    
    public void NewRacerSelected(RacerData selectedRacer)
    {
        if (!startButton.activeInHierarchy)
        {
            startButton.SetActive(true);
        }
        
        DeselectAllRacers();
        
        _preview.NewCarPreview(selectedRacer);
        
        _racerDataHolder.OnNewRacerSelected(selectedRacer, allRacers);
    }

    private void DeselectAllRacers()
    {
        foreach (var racerBox in spawnedRacerBoxes)
        {
          racerBox.DeselectRacer();  
        }
    }
    
    public void StartGame()
    {
        //small safety check just in case
        if (_racerDataHolder.selectedRacer != null)
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }
    
}
