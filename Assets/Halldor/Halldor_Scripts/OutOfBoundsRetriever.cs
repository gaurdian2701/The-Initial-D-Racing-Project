using System;
using Car;
using UnityEngine;
using Beziers;

public class OutOfBoundsRetriever : MonoBehaviour
{
    [SerializeField]
    private Beziers.BezierCurve _tracedPath;
    [SerializeField]
    private GameObject _car;
    [SerializeField]
    private int _amountOfCheckpoints = 1;
    [SerializeField]
    private float _checkpointInterval = 1;
    [SerializeField]
    private float _heightOffset = 8;
    [SerializeField] 
    private float _speed = 1.0f;
    [SerializeField] 
    private float _BezierTangent = 0.25f;
    private float _timer;
    private bool _rewind = false;

    private bool _areVectorsClose(Vector3 a, Vector3 b, float tolerance = 1f)
    {
        return Vector3.Distance(a, b) <= tolerance;
    }

    private float _lerpTimer;
    private float _distance;
    private float shitasstimer;
    private bool shitassbool = true;
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
        Rigidbody rb = _car.GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        
        _rewind = false;
    }

    private void ResetRotation()
    {
        //Cant seem to find anything that works
    }

    private void Start()
    {
        _carController = _car.GetComponent<CarController>();
    }

    void Update()
    {
        shitasstimer += Time.deltaTime;
        if (shitasstimer >= 4)
        {
            if (shitassbool)
            {
                shitassbool = false;
                OutOfBounds();
            }
        }
        
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
                
                BezierCurve.ControlPoint NewPosition = new BezierCurve.ControlPoint();
                NewPosition.m_vPosition = new Vector3(_car.transform.position.x, _car.transform.position.y + _heightOffset, _car.transform.position.z);
                
                _tracedPath.m_points.Add(NewPosition);
            }
        }
        
        else
        {
            _lerpTimer += Time.deltaTime * _speed;
            float newDistance = Mathf.Lerp(_distance, 0, _lerpTimer);
            _car.transform.position = _tracedPath.GetPose(newDistance).position;
            if (_areVectorsClose(_car.transform.position, _tracedPath.m_points[0].m_vPosition))
            {
                Debug.Log("InBounds");
                InBounds();
            }
        }
    }
}
