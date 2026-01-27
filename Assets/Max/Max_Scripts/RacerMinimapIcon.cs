using System;
using UnityEngine;

public class RacerMinimapIcon : MonoBehaviour
{
    private MeshRenderer _iconMeshRenderer;

    private void Awake()
    {
        _iconMeshRenderer = GetComponent<MeshRenderer>();
    }

    public void ChangeIconMaterial(Material material)
    {
        _iconMeshRenderer.material = material;
    }
}
