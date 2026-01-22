using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RacerList", menuName = "Scriptable Objects/RacerList")]
public class RacerList : ScriptableObject
{
    public List<RacerData> allRacers = new List<RacerData>();
}
