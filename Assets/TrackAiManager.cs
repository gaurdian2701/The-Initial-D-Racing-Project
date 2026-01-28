using System.Collections.Generic;
using System.Linq;
using Bezier;
using Car;
using UnityEngine;
using UnityEngine.Serialization;

public class TrackAiManager : MonoBehaviour
{
    //Variables --> Exposed
    [SerializeField]
    private GameObject trackPrefab;

    [SerializeField] 
    private GameObject aiTargetPrefab;
    
    [SerializeField]
    private float max = 30.0f;
    
    [SerializeField]
    private float min = 30.0f;
    
    [SerializeField]
    private float targetMoveSpeed = 500.0f;
    
    
    //Variables --> Not Exposed
    private BezierCurve _trackBezierCurve;

    private List<CarControllerOpponentAI> _aiCars = new List<CarControllerOpponentAI>();
     
    private List<GameObject> aiTargets = new List<GameObject>();
    
    private List<float> _distancesAlongCurve = new List<float>();

    private List<float> _targetThresholds = new List<float>();

    
    
    //Methods
    void Start()
    {
        if(trackPrefab) _trackBezierCurve = trackPrefab.GetComponent<BezierCurve>();
        else Debug.LogError("TrackAiManagers needs to have a track prefab");

        _aiCars = FindObjectsByType<CarControllerOpponentAI>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
        
        for (int i = 0; i < _aiCars.Count; i++)
        {
            _targetThresholds.Add(max);
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
        Vector3 dirA = _trackBezierCurve.GetPose(_distancesAlongCurve[aiNumber] - 100).forward;
        Vector3 dirB = _trackBezierCurve.GetPose(_distancesAlongCurve[aiNumber] + 50).forward;
        float angle = Vector3.Angle(dirA, dirB);
        print("Angle within 40 units: " + angle);

        //_aiCars[aiNumber].UpdateAngle(angle);
        
        if(angle < 30) _targetThresholds[aiNumber] = 70;
        else if(30 <= angle && angle < 60) _targetThresholds[aiNumber] = 40;
        else if(60 <= angle && angle < 90) _targetThresholds[aiNumber] = 25;
        else _targetThresholds[aiNumber] = 20;
        
        
        int numberOfCars = _aiCars.Count;
        
        GameObject aiTarget = aiTargets[aiNumber];
        GameObject car = _aiCars[aiNumber].gameObject;
        
        if(!aiTarget || !car) return;
        
        //reset distance along curve if needed
        _trackBezierCurve.UpdateDistances();
        float bezierCurveTotalDistance = _trackBezierCurve.TotalDistance;
        if (_distancesAlongCurve[aiNumber] > bezierCurveTotalDistance) _distancesAlongCurve[aiNumber] = 0;
            
        //move ai target
        Vector3 worldPos = (_trackBezierCurve.GetPose(_distancesAlongCurve[aiNumber]).position + trackPrefab.transform.position) * trackPrefab.transform.localScale.x;
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
