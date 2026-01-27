using System;
using UnityEngine;

public class RacerIdentityTracker : MonoBehaviour
{
    private MeshRenderer _iconMeshRenderer;
    
    [HideInInspector]
    public RacerData racerData;

    private void Awake()
    {
        _iconMeshRenderer = GetComponent<MeshRenderer>();
    }

    public void ChangeIconMaterial(Material material)
    {
        _iconMeshRenderer.material = material;
    }
}
