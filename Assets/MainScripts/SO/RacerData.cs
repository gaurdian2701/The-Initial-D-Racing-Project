using UnityEngine;

[CreateAssetMenu(fileName = "RacerData", menuName = "Scriptable Objects/RacerData")]
public class RacerData : ScriptableObject
{
    //id used for comparing objects
    public int racerId;
    public string racerName;
    public Sprite racerSprite;
    public GameObject visualsPrefab;
    public Color racerColor;
    public CarStats racerStats;
}
