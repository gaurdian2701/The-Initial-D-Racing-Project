using System;
using System.Collections.Generic;
using ProceduralTracks;
using UnityEngine;

[Serializable]
public struct TrackAiDataEntry
{
    [SerializeField]
    public Tracks _track;

    [SerializeField] 
    public float MinDirectionBuffer;
    
    [SerializeField] 
    public float MaxDirectionBuffer;

    public TrackAiDataEntry(Tracks track, float minDirectionBuffer)
    {
        _track = null;
        MinDirectionBuffer = 100;
        MaxDirectionBuffer = 50;
    }
}

[CreateAssetMenu(fileName = "Data", menuName = "Scriptable Objects/TrackAiData", order = 1)]
public class TrackAiData : ScriptableObject
{
    [SerializeField]
    public List<TrackAiDataEntry> trackAiData = new List<TrackAiDataEntry>();
}
