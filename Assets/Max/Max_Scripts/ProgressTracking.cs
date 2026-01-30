using System;
using System.Collections;
using System.Collections.Generic;
using ProceduralTracks;
using TMPro;
using UnityEngine;

public class ProgressTracking : MonoBehaviour
{
    
    public static ProgressTracking Instance;
    private void Awake()
    {
        Instance = this;
    }

    [SerializeField] private int numberOfLaps;
    [SerializeField] Leaderboard leaderboard;
    [SerializeField] WinRace winRace;

    [SerializeField] private TMP_Text lapPopupText;
    [SerializeField] private Animator lapPopupAnimator;
    
    private Tracks tracks;
    
    [HideInInspector]
    public List<RaceProgress>  racersProgress = new List<RaceProgress>();
    
    private List<Checkpoint> checkpoints = new List<Checkpoint>();

    [SerializeField] private AudioClip lapSound;
    [SerializeField] private AudioClip winSound;

    private bool raceOver = false;
    
    void Start()
    {
        StartCoroutine(DelayedStart());
    }
    
    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1.0f);
        Init();
    }
    
    void Init()
    {
        tracks = FindFirstObjectByType<Tracks>();

        int i = 0;
        foreach (var checkpoint in tracks.m_lRacingCheckPoints)
        {
            Checkpoint newCheckpoint = checkpoint.AddComponent<Checkpoint>();
            newCheckpoint.tracking = this;
            newCheckpoint.requiredCheckpoints = i;
            checkpoints.Add(newCheckpoint);
            
            i++;
        }

        checkpoints[^1].isFinishLine = true;
        
        
        
    }


    public void AddProgress(RacerData  toRacer)
    {
        foreach (var racer in racersProgress)
        {
            if (racer.racer.racerId == toRacer.racerId)
            {
                racer.checkpointsCompleted++;
                UpdateLeaderboard();
            }
        }
        
        
    }

    public int CheckProgress(RacerData ofRacer)
    {
        foreach (var racer in racersProgress)
        {
            if (ofRacer.racerId == racer.racer.racerId) return racer.checkpointsCompleted;
        }
        
        Debug.LogError("RacerData not found while checking progress");
        return 404;
    }

    public void AddLap(RacerData toRacer)
    {
        foreach (var racer in racersProgress)
        {
            if (racer.racer.racerId == toRacer.racerId)
            {
                racer.lapsCompleted++;
                
                if (racer.lapsCompleted >= numberOfLaps)
                {
                    EndRace(racer.racer);
                }
                else if (racer.isPlayer)
                {
                    lapPopupText.text = racer.lapsCompleted + "/" + numberOfLaps + " Laps";
                    lapPopupAnimator.SetTrigger("ShowLap");
                    if (SFXManager.Instance!=null)SFXManager.Instance.PlaySFXClip(lapSound,1f);
                }

                racer.checkpointsCompleted = 0;
                
                UnbanIdAtEveryCheckpoint(racer.racer.racerId);
                UpdateLeaderboard();
            }
        }
    }

    private void EndRace(RacerData winner)
    {
        if (raceOver) return;
        raceOver = true;
        Debug.Log("Race ended, winner is: " + winner.racerName);
        if (SFXManager.Instance!=null)SFXManager.Instance.PlaySFXClip(winSound,1f);
        if (SFXManager.Instance!=null)SFXManager.Instance.PlaySFXClip(lapSound,1f);
        winRace.Win(winner);
    }

    private void UnbanIdAtEveryCheckpoint(int idToUnban)
    {
        foreach (var checkpoint in checkpoints)
        {
            checkpoint.UnbanID(idToUnban);
        }
    }

    public void UpdateLeaderboard()
    {
        foreach (var racer in racersProgress)
        {
            racer.pointScore = racer.checkpointsCompleted + racer.lapsCompleted * 100;
        }

        RaceProgress first = racersProgress[0];
        RaceProgress second = racersProgress[1];
        RaceProgress third = racersProgress[2];

        foreach (var racer in racersProgress)
        {
            if (racer.pointScore > first.pointScore)
            {
                first =  racer;                
            }
        }

        foreach (var racer in racersProgress)
        {
            if (racer.racer.racerId == first.racer.racerId) racer.pointScore = -100;
            
            if (racer.pointScore > second.pointScore)
            {
                second =  racer;                
            }
        }
        
        foreach (var racer in racersProgress)
        {
            if (racer.racer.racerId == first.racer.racerId || racer.racer.racerId == second.racer.racerId) racer.pointScore = -100;
            
            if (racer.pointScore > third.pointScore)
            {
                third =  racer;                
            }
        }
        
        leaderboard.UpdateStandings(first.racer, second.racer, third.racer);
        
    }
}




public class RaceProgress
{
    public RacerData racer;
    public int checkpointsCompleted;
    public int lapsCompleted;
    public int pointScore;
    public bool isPlayer;
}