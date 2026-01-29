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

    private float trackScale;

    
    
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
        
        //scale
        trackScale = _trackBezierCurve.gameObject.transform.localScale.x;

        //find current track data
        foreach (TrackAiDataEntry trackAiDataEntry in aiTrackData.trackAiData)
        {
            IEnumerable<BezierCurve.ControlPoint> pointsA = _trackBezierCurve.Points;
            IEnumerable<BezierCurve.ControlPoint> pointsB = trackAiDataEntry._track.gameObject.GetComponent<BezierCurve>().Points;
            if (SameControlPointsByPos(pointsA, pointsB))
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
            _aiCars[i].SetObstacleRangeForBreaking(_currentTrackAiData.obstacleRangeForBreaking * trackScale);
            _aiCars[i].SetSharpTurnDetectionRange(_currentTrackAiData.sharpTurnDetectionRange * trackScale);
            _aiCars[i].SetSharpTurnBreakVelocityLimit(_currentTrackAiData.sharpTurnBreakVelocityLimit * trackScale);
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
        float distanceA = _distancesAlongCurve[aiNumber] - _currentTrackAiData.MinDirectionBuffer * trackScale;
        float distanceB = _distancesAlongCurve[aiNumber] + _currentTrackAiData.MaxDirectionBuffer * trackScale;
        if(distanceA > bezierCurveTotalDistance) distanceA = 0;
        if(distanceB > bezierCurveTotalDistance) distanceB = 0;
        Vector3 dirA = _trackBezierCurve.GetPose(distanceA).forward;
        Vector3 dirB = _trackBezierCurve.GetPose(distanceB).forward;
        float angle = Vector3.Angle(dirA, dirB);
        
        print("ANGLE: " + angle);
        
        
        //set target threshold based on angle
        if(angle < _currentTrackAiData.angle1Max) _targetThresholds[aiNumber] = _currentTrackAiData.angle1AiTargetRadius * trackScale;
        else if(_currentTrackAiData.angle1Max <= angle && angle < _currentTrackAiData.angle2Max) _targetThresholds[aiNumber] = _currentTrackAiData.angle2AiTargetRadius * trackScale;
        else if(_currentTrackAiData.angle2Max <= angle && angle < _currentTrackAiData.angle3Max) _targetThresholds[aiNumber] = _currentTrackAiData.angle3AiTargetRadius * trackScale;
        else _targetThresholds[aiNumber] = _currentTrackAiData.angle4AiTargetRadius * trackScale;
            
        
        //move ai target
        GameObject aiTarget = aiTargets[aiNumber];
        GameObject car = _aiCars[aiNumber].gameObject;
        if(!aiTarget || !car) return;
        
        int numberOfCars = _aiCars.Count;
        
        Vector3 worldPos = (_trackBezierCurve.GetPose(_distancesAlongCurve[aiNumber]).position + _trackBezierCurve.gameObject.transform.position) * trackScale;
        Vector3 rightOfWorldPos = (_trackBezierCurve.GetPose(_distancesAlongCurve[aiNumber]).right);
        Vector3 finalWorldPos;
        
        if (aiNumber < (int)(numberOfCars / 2)) finalWorldPos = worldPos + (rightOfWorldPos * aiNumber * 1);
        else finalWorldPos = worldPos - (rightOfWorldPos * aiNumber * 1);
        
        aiTarget.transform.localPosition = finalWorldPos;
        
        
        //add to distance along curve if target is within range
        float distFromTarget = (worldPos - car.transform.position).magnitude;
        
        if(distFromTarget < _targetThresholds[0]) _distancesAlongCurve[aiNumber] += targetMoveSpeed * Time.deltaTime;
    }
    
    static bool SameControlPointsByPos(IEnumerable<BezierCurve.ControlPoint> inPointsA, IEnumerable<BezierCurve.ControlPoint> inPointsB, float eps = 0.0001f)
    {
        var pointsA = inPointsA.ToArray();
        var pointsB = inPointsB.ToArray();
        if (pointsA.Length != pointsB.Length) return false;

        float epsSqr = eps * eps;

        for (int i = 0; i < pointsA.Length; i++)
        {
            if ((pointsA[i].m_vPosition - pointsB[i].m_vPosition).sqrMagnitude > epsSqr)
            {
                return false;
            }
        }

        return true;
    }
}
