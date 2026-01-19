using ProceduralTracks;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace Game
{
    [CustomEditor(typeof(ProceduralMesh), true)]
    public class ProceduralMeshEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(20);
            if (GUILayout.Button("Update Mesh"))
            {
                ProceduralMesh pm = target as ProceduralMesh;
                pm.UpdateMesh();
            }
        }

        private void OnSceneGUI()
        {
            #if false
            ProceduralMesh pm = target as ProceduralMesh;
            if(pm.Mesh != null)
            {
                Bounds b = pm.Mesh.bounds;
                Handles.color = Color.blue;
                Handles.DrawWireCube(b.center, b.size);

                // calculate center
                Vector3 vCenter = Vector3.zero;
                for (int i = 0; i < pm.Mesh.vertexCount; ++i)
                {
                    vCenter += pm.Mesh.vertices[i];
                }
                vCenter /= (float)pm.Mesh.vertexCount;

                float fRadius = 0.0f;
                for (int i = 0; i < pm.Mesh.vertexCount; ++i)
                {
                    fRadius = Mathf.Max(fRadius, Vector3.Distance(vCenter, pm.Mesh.vertices[i]));
                }

                // draw out bounding sphere
                Handles.color = new Color(1.0f, 0.0f, 0.0f, 0.4f);
                Handles.SphereHandleCap(0, vCenter, Quaternion.identity, fRadius * 2.0f, EventType.Repaint);
            }
            #endif
        }
    }
}