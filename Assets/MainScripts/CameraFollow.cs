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
    [SerializeField] [Range(60.0f, 120.0f)]
    private float mmaxFOV = 60.0f;
    
    private Camera mmainCamera;
    private IFollowTarget mfollowTargetInterface;
    
    public float zOffset = 4.56f;
    public float yOffset = 1.88f;
    public float mfollowResponsiveness = 5.0f;
    public float mlookAtResponsiveness = 5.0f;

    private float mdefaultFOV = 0.0f;
    
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
        float currentFOV = mfovCurve.Evaluate(targetVelocity);
        currentFOV = Mathf.Clamp(currentFOV, mdefaultFOV, mmaxFOV);
        Debug.Log(targetVelocity);
        mmainCamera.fieldOfView = currentFOV;
    }
}
