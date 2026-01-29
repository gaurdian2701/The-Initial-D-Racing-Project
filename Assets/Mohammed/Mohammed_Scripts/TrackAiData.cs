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

    [SerializeField] 
    public float angle1Max;
    [SerializeField] 
    public float angle1AiTargetRadius;
    
    [SerializeField] 
    public float angle2Max;
    [SerializeField] 
    public float angle2AiTargetRadius;
    
    [SerializeField] 
    public float angle3Max;
    [SerializeField] 
    public float angle3AiTargetRadius;
    
    [SerializeField] 
    public float angle4AiTargetRadius;

    [SerializeField] 
    public float obstacleRangeForBreaking;
    
    [SerializeField] 
    public float sharpTurnBreakVelocityLimit;
    
    [SerializeField] 
    public float sharpTurnDetectionRange;

    public TrackAiDataEntry(Tracks track, float minDirectionBuffer)
    {
        _track = null;
        MinDirectionBuffer = 100;
        MaxDirectionBuffer = 50;
        angle1Max = 0;
        angle1AiTargetRadius = 0;
        angle2Max = 0;
        angle2AiTargetRadius = 0;
        angle3Max = 0;
        angle3AiTargetRadius = 0;
        angle4AiTargetRadius = 0;
        obstacleRangeForBreaking = 20;
        sharpTurnBreakVelocityLimit = 15;
        sharpTurnDetectionRange = 100;
    }
}

[CreateAssetMenu(fileName = "Data", menuName = "Scriptable Objects/TrackAiData", order = 1)]
public class TrackAiData : ScriptableObject
{
    [SerializeField]
    public List<TrackAiDataEntry> trackAiData = new List<TrackAiDataEntry>();
}
