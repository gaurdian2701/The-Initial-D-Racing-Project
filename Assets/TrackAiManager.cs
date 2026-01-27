using Bezier;
using UnityEngine;
using UnityEngine.Serialization;

public class TrackAiManager : MonoBehaviour
{
    //Variables --> Exposed
    [SerializeField]
    private GameObject trackPrefab;
    
    [SerializeField]
    private GameObject ai;
    
    [SerializeField]
    private GameObject aiTarget;
    
    [SerializeField]
    private float targetMoveDistanceThreshold = 10.0f;
    
    [SerializeField]
    private float targetMoveSpeed = 10.0f;
    
    //Variables --> Not Exposed
    private BezierCurve _trackBezierCurve;

    private float _distanceAlongCurve = 0;
    
    void Start()
    {
        if(trackPrefab) _trackBezierCurve = trackPrefab.GetComponent<BezierCurve>();
        else Debug.LogError("TrackAiManagers needs to have a track prefab");
    }
    
    void Update()
    {
        //reset distance along curve if needed
        _trackBezierCurve.UpdateDistances();
        float bezierCurveTotalDistance = _trackBezierCurve.TotalDistance;
        
        if (_distanceAlongCurve > bezierCurveTotalDistance) _distanceAlongCurve = 0;
        
        
        //move ai target based on distance along curve
        Vector3 worldPos = (_trackBezierCurve.GetPose(_distanceAlongCurve).position + trackPrefab.transform.position) * trackPrefab.transform.localScale.x;
        aiTarget.transform.localPosition = worldPos; //+ left/right based on ai number
        
        
        //add to distance along curve if target is within range
        float distFromTarget = (worldPos - ai.transform.position).magnitude;
        print(distFromTarget);
        if(distFromTarget < targetMoveDistanceThreshold) _distanceAlongCurve += targetMoveSpeed * Time.deltaTime;
    }
}
