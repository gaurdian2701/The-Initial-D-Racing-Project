using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RacerBox : MonoBehaviour
{
    [SerializeField] private Image racerPortrait;
    [SerializeField] private TMP_Text racerName;
    public GameObject selectedBorder;
    public GameObject nameBg;
    
    [HideInInspector] public RacerData myRacer;
    private RacerSelect _racerSelector;
    
    //acquire and set relevant values when spawned in RacerSelect
    public void Initialize(RacerData racerData, RacerSelect racerSelect)
    {
        myRacer = racerData;
        racerPortrait.sprite = racerData.racerSprite;
        racerName.text = racerData.racerName;
        _racerSelector = racerSelect;
    }

    public void SelectRacer()
    {
        //this will call deselect racer so its important it happens first here
        _racerSelector.NewRacerSelected(myRacer);
        
        selectedBorder.SetActive(true);
        nameBg.SetActive(true);
    }

    //purely Visual
    public void DeselectRacer()
    {
        selectedBorder.SetActive(false);
        nameBg.SetActive(false);
    }
}
