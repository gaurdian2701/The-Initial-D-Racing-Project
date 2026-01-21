using System;
using Unity.VisualScripting;
using UnityEngine;

public interface IFollowTarget
{
    public float GetVelocity();
}

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private GameObject mfollowTarget;
    [SerializeField] private AnimationCurve mfovCurve;
    [SerializeField] [Range(60.0f, 120.0f)] private float mmaxFOV = 60.0f;
    [SerializeField] [Range(0.0f, 50.0f)] private float mvelocityThresholdForFOVChange = 20.0f;
    
    private Camera mmainCamera;
    private IFollowTarget mfollowTargetInterface;
    
    public float zOffset = 4.56f;
    public float yOffset = 1.88f;
    public float mfollowResponsiveness = 12.0f;
    public float mlookAtResponsiveness = 5.0f;
    [Range(0.0f, 1.0f)] public float mFOVChangeSensitivity = 0.5f;
    [Range(1.0f, 1.5f)] public float mFOVScaling = 1.0f;

    private float mdefaultFOV = 60.0f;
    
    private void Start()
    {
        mmainCamera = GetComponent<Camera>();
        mfollowTargetInterface =  mfollowTarget.GetComponent<IFollowTarget>();
        mdefaultFOV = mmainCamera.fieldOfView;

        if (mfollowTargetInterface == null)
        {
            Debug.LogError("Follow target must implement IFollowTarget interface!");
        }
    }

    protected void LateUpdate()
    {
        UpdateCameraTransform();
        UpdateFOV();
    }

    private void UpdateCameraTransform()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, mfollowTarget.transform.rotation, mlookAtResponsiveness * Time.deltaTime);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0.0f);
        
        //Update follow position for camera
        Vector3 followPosition = mfollowTarget.transform.position - mfollowTarget.transform.forward * zOffset;
        followPosition.y += yOffset;
        transform.position = Vector3.Lerp(transform.position, followPosition, mfollowResponsiveness * Time.deltaTime);
        
        Physics.SyncTransforms();
    }

    private void UpdateFOV()
    {
        float targetVelocity =  mfollowTargetInterface.GetVelocity();
        float evaluatedFOV = mdefaultFOV;
        
        //If the target velocity is beyond a certain threshold, start changing the FOV
        if (targetVelocity > mvelocityThresholdForFOVChange)
        {
            evaluatedFOV = mfovCurve.Evaluate(1 - 1 / targetVelocity) * mdefaultFOV * mFOVScaling;
            evaluatedFOV = Mathf.Clamp(evaluatedFOV, mdefaultFOV, mmaxFOV);
        }
        
        mmainCamera.fieldOfView = Mathf.Lerp(mmainCamera.fieldOfView, evaluatedFOV, mFOVChangeSensitivity * Time.deltaTime);
        
        Debug.Log($"TARGET VELOCITY: {targetVelocity}");
        Debug.Log($"CALCULATED FOV: {evaluatedFOV}");
        Debug.Log("VALUE ON CURVE: " + (1 - 1 / targetVelocity));
        Debug.Log("EVALUATED CURVE VALUE: " + mfovCurve.Evaluate(1 - 1 / targetVelocity));
    }
}
