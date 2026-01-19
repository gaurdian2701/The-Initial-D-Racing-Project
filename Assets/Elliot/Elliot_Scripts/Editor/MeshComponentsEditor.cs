using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Game
{
    [CustomEditor(typeof(MeshComponents))]
    public class MeshComponentsEditor : Editor
    {
        public enum Visualiation
        {
            None,
            VertexPositions,
            Normals,
        }

        private Vector3[] m_positions;
        private Vector3[] m_normals;
        private int[] m_triangles;

        private static Visualiation sm_visualization;

        private void OnEnable()
        {
            MeshComponents mcs = target as MeshComponents;
            MeshFilter mf = mcs.GetComponent<MeshFilter>();
            m_positions = mf.sharedMesh.vertices;
            m_normals = mf.sharedMesh.normals;
            m_triangles = mf.sharedMesh.triangles;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(10);
            sm_visualization = (Visualiation)EditorGUILayout.EnumPopup("Visualization", sm_visualization);
        }

        private void OnSceneGUI()
        {
            const float VERTEX_DISTANCE = 15.0f;

            // get camera position etc
            Vector3 vCameraPosition = SceneView.currentDrawingSceneView.camera.transform.position;
            Vector3 vCameraForward = SceneView.currentDrawingSceneView.camera.transform.forward;
            MeshComponents mcs = target as MeshComponents;

            // find visible vertices
            HashSet<int> visibleVertices = new HashSet<int>();
            if (sm_visualization != Visualiation.None)
            {
                for (int i = 0; i < m_triangles.Length; i += 3)
                {
                    Plane p = new Plane(m_positions[m_triangles[i + 0]],
                                        m_positions[m_triangles[i + 1]],
                                        m_positions[m_triangles[i + 2]]);
                    if (Vector3.Dot(vCameraForward, p.normal) < 0.0f)
                    {
                        visibleVertices.Add(m_triangles[i + 0]);
                        visibleVertices.Add(m_triangles[i + 1]);
                        visibleVertices.Add(m_triangles[i + 2]);
                    }
                }
            }

            switch (sm_visualization)
            {
                case Visualiation.None:
                    break;

                case Visualiation.VertexPositions:

                    // draw visible vertices
                    Handles.color = Color.black;
                    foreach (int i in visibleVertices)
                    {
                        Vector3 v = mcs.transform.TransformPoint(m_positions[i]);
                        float fDistance = Vector3.Distance(vCameraPosition, v);
                        float fAmount = Mathf.InverseLerp(VERTEX_DISTANCE, 0.0f, fDistance);
                        if (fAmount > 0.001f)
                        {
                            Handles.DotHandleCap(0, v, Quaternion.identity, 0.04f * fAmount, EventType.Repaint);
                        }
                    }
                    break;

                case Visualiation.Normals:

                    // draw normals
                    Handles.color = Color.black;
                    foreach (int i in visibleVertices)
                    {
                        Vector3 v = mcs.transform.TransformPoint(m_positions[i]);
                        Vector3 n = mcs.transform.TransformDirection(m_normals[i]);
                        float fDistance = Vector3.Distance(vCameraPosition, v);
                        float fAmount = Mathf.InverseLerp(VERTEX_DISTANCE, 0.0f, fDistance);
                        if (fAmount > 0.001f)
                        {
                            Handles.DrawLine(v, v + n * 0.3f, 1.5f);
                        }
                    }
                    break;
            }
        }
    }
}
