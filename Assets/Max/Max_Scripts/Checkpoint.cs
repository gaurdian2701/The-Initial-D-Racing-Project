using System;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public ProgressTracking tracking;
    
    public int requiredCheckpoints;

    public List<int> bannedRacerIds = new List<int>();

    public bool isFinishLine;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Car")) return;
        
        
        RacerData enteredRacer = other.gameObject.GetComponentInChildren<RacerIdentityTracker>().racerData;
        
        
        if (IsRacerIDBanned(enteredRacer.racerId)) return;
        
        
        if (tracking.CheckProgress(enteredRacer) >= requiredCheckpoints)
        {
            if (!isFinishLine)
            {
                tracking.AddProgress(enteredRacer);
                bannedRacerIds.Add(enteredRacer.racerId);
            }
            else
            {
                tracking.AddLap(enteredRacer);
            }
        }
    }

    private bool IsRacerIDBanned(int racerId)
    {
        foreach (int id in bannedRacerIds)
        {
            if (racerId == id)
            {
                
                return true;
            } 
                
        }
        
        return false;
    }

    public void UnbanID(int racerId)
    {
        int removeAt = 404;
        int i = 0;
        foreach (var id in bannedRacerIds)
        {
            if (id  == racerId) removeAt = i;
            i++;
        }
        
        if (removeAt != 404) bannedRacerIds.RemoveAt(removeAt);
        
    }
}
