using UnityEngine;

[CreateAssetMenu(fileName = "RacerData", menuName = "Scriptable Objects/RacerData")]
public class RacerData : ScriptableObject
{
    //id used for comparing objects
    public int racerId;
    public string racerName;
    public Sprite racerSprite;
    public Material minimapIconMaterial;
    public GameObject carPrefab;
    public GameObject carAIPrefab;
    public Color racerColor;
    public CarStats racerStats;
}
