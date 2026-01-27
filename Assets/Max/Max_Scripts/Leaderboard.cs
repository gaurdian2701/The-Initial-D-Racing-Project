using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Leaderboard : MonoBehaviour
{
    [SerializeField] private TMP_Text firstName;
    [SerializeField] private TMP_Text secondName;
    [SerializeField] private TMP_Text thirdName;
    
    [SerializeField] private Image firstImage;
    [SerializeField] private Image secondImage;
    [SerializeField] private Image thirdImage;
    
    
    public void UpdateStandings(RacerData first, RacerData second, RacerData third)
    {
        Debug.Log("UpdateStandings");
        firstName.text = first.racerName;
        secondName.text = second.racerName;
        thirdName.text = third.racerName;

        firstImage.sprite = first.racerSprite;
        secondImage.sprite = second.racerSprite;
        thirdImage.sprite = third.racerSprite;
    }
}
