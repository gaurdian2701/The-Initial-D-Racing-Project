using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RacerSelect : MonoBehaviour
{
    [SerializeField] private GameObject LevelSelection;
    [SerializeField] private GameObject RacerSelection;
    [SerializeField] private GameObject racerGrid;
    [SerializeField] private GameObject levelGrid;
    [SerializeField] private GameObject racerBoxPrefab;
    [SerializeField] private GameObject levelBoxPrefab;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private GameObject startButton;
    
    private string gameSceneName;
    
    public RacerList allRacers;
    public LevelList allLevels;
    
    [HideInInspector] public List<RacerBox> spawnedRacerBoxes = new List<RacerBox>();
    [HideInInspector] public List<LevelBox> spawnedLevelBoxes = new List<LevelBox>();
    private RacerDataHolder _racerDataHolder;

    private CarPreview _preview;

    [SerializeField] private AudioClip buttonSound;

    private void Start()
    {
        _racerDataHolder = RacerDataHolder.Instance;
        
        startButton.SetActive(false);
        nextButton.SetActive(false);

        RacerSelection.SetActive(true);
        LevelSelection.SetActive(false);
        
        _preview = FindAnyObjectByType<CarPreview>();
        //spawn the actual ui elements for the racers
        foreach (var racerData in allRacers.allRacers)
        {
            RacerBox newRacerBox = Instantiate(racerBoxPrefab, racerGrid.transform).GetComponent<RacerBox>();
            newRacerBox.Initialize(racerData, this);
            spawnedRacerBoxes.Add(newRacerBox);
        }
        foreach (var level in allLevels.allLevels)
        {
            LevelBox newLevelBox = Instantiate(levelBoxPrefab, levelGrid.transform).GetComponent<LevelBox>();
            newLevelBox.Initialize(level, this);
            spawnedLevelBoxes.Add(newLevelBox);
        }
    }
    
    public void NewRacerSelected(RacerData selectedRacer)
    {
        if (!nextButton.activeInHierarchy)
        {
            nextButton.SetActive(true);
        }
        
        DeselectAllRacers();
        
        _preview.NewCarPreview(selectedRacer);
        
        _racerDataHolder.OnNewRacerSelected(selectedRacer, allRacers);
    }
    
    public void NewLevelSelected(LevelData selectedLevel)
    {
        if (!startButton.activeInHierarchy)
        {
            startButton.SetActive(true);
        }
        
        gameSceneName = selectedLevel.levelSceneName;
        
        DeselectAllLevels();
    }

    private void DeselectAllRacers()
    {
        foreach (var racerBox in spawnedRacerBoxes)
        {
          racerBox.DeselectRacer();  
        }
    }
    
    private void DeselectAllLevels()
    {
        foreach (var LevelBox in spawnedLevelBoxes)
        {
            LevelBox.DeselectLevel();
        }
    }

    public void GoToLevelSelection()
    {
        RacerSelection.SetActive(false);
        LevelSelection.SetActive(true);
    }
    
    public void StartGame()
    {
        //small safety check just in case
        if (_racerDataHolder.selectedRacer != null)
        {
            if (SFXManager.Instance!=null)SFXManager.Instance.PlaySFXClip(buttonSound,0.8f);
            SceneManager.LoadScene(gameSceneName);
        }
    }
    
}
