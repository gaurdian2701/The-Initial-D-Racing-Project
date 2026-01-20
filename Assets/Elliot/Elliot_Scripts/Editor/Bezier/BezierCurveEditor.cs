using UnityEngine;
using UnityEditor;

namespace Bezier
{
    [CustomEditor(typeof(BezierCurve), true)]
    public class BezierCurveEditor : Editor
    {
        private Tool m_oldTool;
        private float m_fBlend;
        private float m_fDistanceAlongCurve = 0.0f;

        private void OnEnable()
        {
            m_oldTool = Tools.current;
        }

        private void OnDisable()
        {
            Tools.current = m_oldTool;
        }

        public override void OnInspectorGUI()
        {
            BezierCurve bc = target as BezierCurve;

            EditorGUI.BeginChangeCheck();

            base.OnInspectorGUI();

            GUILayout.Space(10);
            m_fBlend = EditorGUILayout.Slider("Blend", m_fBlend, 0.0f, 1.0f);
            m_fDistanceAlongCurve = EditorGUILayout.Slider("Point On Curve", m_fDistanceAlongCurve, -1.0f, bc.LastPoint.m_fDistance + 1.0f);

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
                bc.UpdateDistances();
            }

            GUILayout.Space(10);
            DebugWeights();
        }

        private void OnSceneGUI()
        {
            //Debug.Log("Scene GUI");
            BezierCurve bc = target as BezierCurve;
            Tools.current = Tool.None;

            //DrawCurve_LerpLerpLerp();
            DrawCurve_Bezier();

            // draw control points
            foreach (BezierCurve.ControlPoint cp in bc.m_points)
            {
                // select point?
                Handles.color = new Color(1.0f, 0.3f, 0.3f);
                Handles.SphereHandleCap(0, cp.m_vPosition, Quaternion.identity, 0.5f, EventType.Repaint);

                // draw tangent line
                Handles.color = new Color(0.3f, 1.0f, 0.3f);
                Handles.DrawLine(cp.m_vPosition, cp.m_vPosition + cp.m_vTangent, 3.0f);
                Handles.DrawDottedLine(cp.m_vPosition, cp.m_vPosition - cp.m_vTangent, 5.0f);

                // draw point distance
                Handles.Label(cp.m_vPosition + Vector3.up * 0.5f, cp.m_fDistance.ToString("0.00"));
            }

            // point on curve!
            Pose vPoseOnCurve = bc.GetPose(m_fDistanceAlongCurve);
            Handles.color = Color.yellow;
            Handles.SphereHandleCap(0, vPoseOnCurve.position, Quaternion.identity, 0.5f, EventType.Repaint);
            Handles.DrawLine(vPoseOnCurve.position, vPoseOnCurve.position + vPoseOnCurve.forward * 3.0f, 5.0f);
        }

        protected void DebugWeights()
        {
            float fOneMinusT = 1.0f - m_fBlend;

            float w1 = fOneMinusT * fOneMinusT * fOneMinusT;
            float w2 = 3 * fOneMinusT * fOneMinusT * m_fBlend;
            float w3 = 3 * fOneMinusT * m_fBlend * m_fBlend;
            float w4 = m_fBlend * m_fBlend * m_fBlend;

            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Weights", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("w1", w1.ToString("0.00"));
            EditorGUILayout.LabelField("w2", w2.ToString("0.00"));
            EditorGUILayout.LabelField("w3", w3.ToString("0.00"));
            EditorGUILayout.LabelField("w4", w4.ToString("0.00"));
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Sum", (w1 + w2 + w3 + w4).ToString("0.00"));
            GUILayout.EndVertical();
        }

        protected void DrawCurve_LerpLerpLerp()
        {
            BezierCurve bc = target as BezierCurve;
            Handles.matrix = Matrix4x4.Translate(Vector3.up * 0.1f);

            for (int i = 1; i < bc.m_points.Count; ++i)
            {
                BezierCurve.ControlPoint A = bc.m_points[i - 1];
                BezierCurve.ControlPoint B = bc.m_points[i];

                Vector3 p0 = A.m_vPosition;                 // <-- Start at A
                Vector3 p1 = A.m_vPosition + A.m_vTangent;
                Vector3 p2 = B.m_vPosition - B.m_vTangent;
                Vector3 p3 = B.m_vPosition;                 // <-- End at B

                // Lerp #1
                Vector3 p01 = Vector3.Lerp(p0, p1, m_fBlend);
                Vector3 p12 = Vector3.Lerp(p1, p2, m_fBlend);
                Vector3 p23 = Vector3.Lerp(p2, p3, m_fBlend);
                Handles.color = Color.yellow;
                Handles.DrawLine(p0, p1, 2.0f);
                Handles.DrawLine(p1, p2, 2.0f);
                Handles.DrawLine(p2, p3, 2.0f);
                Handles.color = new Color(1.0f, 0.5f, 0.0f);
                Handles.CubeHandleCap(0, p01, Quaternion.identity, 0.2f, EventType.Repaint);
                Handles.CubeHandleCap(0, p12, Quaternion.identity, 0.2f, EventType.Repaint);
                Handles.CubeHandleCap(0, p23, Quaternion.identity, 0.2f, EventType.Repaint);

                // Lerp #2
                Vector3 p012 = Vector3.Lerp(p01, p12, m_fBlend);
                Vector3 p123 = Vector3.Lerp(p12, p23, m_fBlend);
                Handles.color = Color.cyan;
                Handles.DrawLine(p01, p12, 2.0f);
                Handles.DrawLine(p12, p23, 2.0f);
                Handles.color = Color.blue;
                Handles.CubeHandleCap(0, p012, Quaternion.identity, 0.2f, EventType.Repaint);
                Handles.CubeHandleCap(0, p123, Quaternion.identity, 0.2f, EventType.Repaint);

                // Lerp #3
                Vector3 p0123 = Vector3.Lerp(p012, p123, m_fBlend);
                Handles.color = Color.magenta;
                Handles.DrawLine(p012, p123, 2.0f);
                Handles.color = Color.white;
                Handles.SphereHandleCap(0, p0123, Quaternion.identity, 0.4f, EventType.Repaint);
                Debug.DrawLine(p0123 - Vector3.up * 0.2f, p0123 + Vector3.up * 0.2f, Color.white, 5.0f);
            }

            Handles.matrix = Matrix4x4.identity;
        }

        protected void DrawCurve_Bezier()
        {
            BezierCurve bc = target as BezierCurve;
            Handles.color = Color.white;

            for (int i = 1; i < bc.m_points.Count; ++i)
            {
                BezierCurve.ControlPoint A = bc.m_points[i - 1];
                BezierCurve.ControlPoint B = bc.m_points[i];

                Vector3 vLast = A.m_vPosition;
                for (float f = 0.0f; f <= 1.0f; f += 0.025f)
                {
                    Vector3 vCurr = BezierCurve.GetPosition(A, B, f);
                    Handles.DrawLine(vLast, vCurr, 4.0f);
                    vLast = vCurr;
                }
            }
        }
    }
}
