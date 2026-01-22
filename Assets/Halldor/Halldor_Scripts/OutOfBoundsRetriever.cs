using System;
using System.Linq;
using Car;
using Bezier;
using UnityEngine;

public class OutOfBoundsRetriever : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Bezier.BezierCurve _tracedPath;
    [SerializeField]
    private GameObject _car;
    
    [Header("Traced Path Settings")]
    [SerializeField]
    private int _amountOfCheckpoints = 3;
    [SerializeField]
    private float _checkpointInterval = 0.5f;
    [SerializeField]
    private float _heightOffset = 8;
    [SerializeField] 
    private float _speed = 0.3f;
    [SerializeField] 
    private float _BezierTangent = 5f;
    private float _timer;
    [Header("Do Not Touch")]
    public bool _rewind = false;

    private bool _areVectorsClose(Vector3 a, Vector3 b, float tolerance = 1f)
    {
        return Vector3.Distance(a, b) <= tolerance;
    }

    private float _lerpTimer;
    private float _distance;
    private CarController _carController;

    public void OutOfBounds()
    {
        _rewind = true;
        Debug.Log("OutOfBounds");

        Rigidbody rb = _car.GetComponent<Rigidbody>();
        
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        ResetRotation();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        BezierCurve.ControlPoint NewPosition = new BezierCurve.ControlPoint();
        NewPosition.m_vPosition = _car.transform.position;
        _tracedPath.m_points.Add(NewPosition);
        
        _tracedPath.UpdateDistances();
        _tracedPath.CalculateSmoothTangents(_BezierTangent);
        _distance = _tracedPath.TotalDistance;
        _lerpTimer = 0;
    }

    private void InBounds()
    {
        Debug.Log("InBounds");
        Rigidbody rb = _car.GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        _tracedPath.m_points.Clear();
        
        _rewind = false;
    }

    private void ResetRotation()
    {
        //Cant seem to find anything that works
        _car.transform.rotation = Quaternion.identity;
    }

    private void AddCheckpoint()
    {
        BezierCurve.ControlPoint NewPosition = new BezierCurve.ControlPoint();
        NewPosition.m_vPosition = new Vector3(_car.transform.position.x, _car.transform.position.y + _heightOffset, _car.transform.position.z);
        _tracedPath.m_points.Add(NewPosition);
    }

    private void Start()
    {
        _carController = _car.GetComponent<CarController>();
    }

    void Update()
    {
        if (!_rewind)
        {
            _timer += Time.deltaTime;
            if (_timer >= _checkpointInterval && _carController.IsGrounded())
            {
                _timer = 0;
                if (_tracedPath.m_points.Count > _amountOfCheckpoints)
                {
                    _tracedPath.m_points.RemoveAt(0);
                }

                if (_tracedPath.m_points.Count > 0)
                {
                    Vector3 LastPoint = new Vector3(_tracedPath.LastPoint.m_vPosition.x, _tracedPath.LastPoint.m_vPosition.y - _heightOffset, _tracedPath.LastPoint.m_vPosition.z);
                    if (!_areVectorsClose(LastPoint, _car.transform.position, 5))
                    {
                        AddCheckpoint();
                    }
                }
                
                else
                {
                    AddCheckpoint();
                }
            }
        }
        
        else
        {
            _lerpTimer += Time.deltaTime * _speed;
            float newDistance = Mathf.Lerp(_distance, 0, _lerpTimer);
            _car.transform.position = _tracedPath.GetPose(newDistance).position;
            if (_areVectorsClose(_car.transform.position, _tracedPath.m_points[0].m_vPosition))
            {
                InBounds();
            }
        }
    }
}
