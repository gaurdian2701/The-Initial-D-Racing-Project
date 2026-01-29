using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelBox : MonoBehaviour
{
    [SerializeField] private Image levelPortrait;
    [SerializeField] private TMP_Text levelName;
    private string levelSceneName;
    public GameObject selectedBorder;
    public GameObject nameBg;
    
    [HideInInspector] public LevelData myLevel;
    private RacerSelect _racerSelector;

    [SerializeField] private AudioClip onClickSound;
    
    //acquire and set relevant values when spawned in RacerSelect
    public void Initialize(LevelData leveldata, RacerSelect racerSelect)
    {
        myLevel = leveldata;
        levelName.text = leveldata.levelName;
        levelSceneName = leveldata.levelSceneName;
        levelPortrait.sprite = leveldata.levelSprite;
        _racerSelector = racerSelect;
    }

    public void SelectLevel()
    {
        if (SFXManager.Instance!=null) SFXManager.Instance.PlaySFXClip(onClickSound,1f);
        //this will call deselect racer so its important it happens first here
        _racerSelector.NewLevelSelected(myLevel);
        
        selectedBorder.SetActive(true);
        nameBg.SetActive(true);
    }
    
    //purely Visual
    public void DeselectLevel()
    {
        selectedBorder.SetActive(false);
        nameBg.SetActive(false);
    }
}