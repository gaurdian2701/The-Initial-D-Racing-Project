using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bezier;
using Car;
using ProceduralTracks;
using UnityEngine;
using UnityEngine.Serialization;

public class TrackAiManager : MonoBehaviour
{
    //Variables --> Exposed
    [SerializeField] 
    private GameObject aiTargetPrefab;
    
    [SerializeField]
    private TrackAiData aiTrackData;
    
    [SerializeField]
    private float targetMoveSpeed = 500.0f;
    
    
    //Variables --> Not Exposed
    private BezierCurve _trackBezierCurve;
    
    private TrackAiDataEntry _currentTrackAiData;

    private List<CarControllerOpponentAI> _aiCars = new List<CarControllerOpponentAI>();
     
    private List<GameObject> aiTargets = new List<GameObject>();
    
    private List<float> _distancesAlongCurve = new List<float>();

    private List<float> _targetThresholds = new List<float>();

    
    
    //Methods
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
        //find track
        Tracks track = FindAnyObjectByType<Tracks>();
        _trackBezierCurve = track.gameObject.GetComponent<BezierCurve>();
        if(!_trackBezierCurve) Debug.LogError("TrackAiManager couldnt find a track");

        //find current track data
        foreach (TrackAiDataEntry trackAiDataEntry in aiTrackData.trackAiData)
        {
            if (_trackBezierCurve == trackAiDataEntry._track.gameObject.GetComponent<BezierCurve>())
            {
                _currentTrackAiData = trackAiDataEntry;
                print("FOUND TRACK AI DATA WOHOOOOO");
                break;
            }
        }
        
        //find cars
        _aiCars = FindObjectsByType<CarControllerOpponentAI>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
        
        for (int i = 0; i < _aiCars.Count; i++)
        {
            _targetThresholds.Add(30);
            _distancesAlongCurve.Add(0);
            
            if(!aiTargetPrefab) continue;
            
            GameObject aiTarget = Instantiate(aiTargetPrefab);
            aiTargets.Add(aiTarget);
            
            _aiCars[i].SetTargetPoint(aiTargets[i]);
        }
    }
    
    void Update()
    {
        for (int i = 0; i < _aiCars.Count; i++)
        {
            UpdateAiTarget(i);
        }
    }

    void UpdateAiTarget(int aiNumber)
    {
        //reset distance along curve if needed
        _trackBezierCurve.UpdateDistances();
        float bezierCurveTotalDistance = _trackBezierCurve.TotalDistance;
        if (_distancesAlongCurve[aiNumber] > bezierCurveTotalDistance) _distancesAlongCurve[aiNumber] = 0;

        
        //find immediate angle
        float distanceA = _distancesAlongCurve[aiNumber] - _currentTrackAiData.MinDirectionBuffer;
        float distanceB = _distancesAlongCurve[aiNumber] + _currentTrackAiData.MaxDirectionBuffer;
        if(distanceA > bezierCurveTotalDistance) distanceA = 0;
        if(distanceB > bezierCurveTotalDistance) distanceB = 0;
        Vector3 dirA = _trackBezierCurve.GetPose(_distancesAlongCurve[aiNumber]).forward;
        Vector3 dirB = _trackBezierCurve.GetPose(_distancesAlongCurve[aiNumber]).forward;
        float angle = Vector3.Angle(dirA, dirB);
        
        
        //set target threshold based on angle
        if(angle < 30) _targetThresholds[aiNumber] = 50;
        else if(30 <= angle && angle < 60) _targetThresholds[aiNumber] = 30;
        else if(60 <= angle && angle < 90) _targetThresholds[aiNumber] = 20;
        else _targetThresholds[aiNumber] = 15;
            
        
        //move ai target
        GameObject aiTarget = aiTargets[aiNumber];
        GameObject car = _aiCars[aiNumber].gameObject;
        if(!aiTarget || !car) return;
        
        int numberOfCars = _aiCars.Count;
        
        Vector3 worldPos = (_trackBezierCurve.GetPose(_distancesAlongCurve[aiNumber]).position + _trackBezierCurve.gameObject.transform.position) * _trackBezierCurve.gameObject.transform.localScale.x;
        Vector3 rightOfWorldPos = (_trackBezierCurve.GetPose(_distancesAlongCurve[aiNumber]).right);
        Vector3 finalWorldPos;
        
        if (aiNumber < (int)(numberOfCars / 2)) finalWorldPos = worldPos + (rightOfWorldPos * aiNumber * 1);
        else finalWorldPos = worldPos - (rightOfWorldPos * aiNumber * 1);
        
        aiTarget.transform.localPosition = finalWorldPos;
        
        
        //add to distance along curve if target is within range
        float distFromTarget = (worldPos - car.transform.position).magnitude;
        
        if(distFromTarget < _targetThresholds[0]) _distancesAlongCurve[aiNumber] += targetMoveSpeed * Time.deltaTime;
    }
}
