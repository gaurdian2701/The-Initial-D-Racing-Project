using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Bezier
{
    [ExecuteInEditMode]
    public class BezierCurve : MonoBehaviour
    {
        [System.Serializable]
        public class ControlPoint
        {
            public Vector3 m_vPosition;
            public Vector3 m_vTangent;
            public Vector3 m_vRoadSize = new Vector3(7.0f, 0.2f, 0.3f);
            public float m_fDistance;
            public bool m_bIsEdge;
        }

        [SerializeField]
        public List<ControlPoint> m_points = new List<ControlPoint>();

        [SerializeField]
        private bool m_bIsClosed = false;

        [SerializeField]
        private bool m_bWantEditorGizmos = false;

        private ControlPoint m_closedPoint = new ControlPoint();
        private float m_fTotalDistance;

        #region Properties

        public bool IsEmpty => m_points.Count == 0;

        public ControlPoint FirstPoint => !IsEmpty ? m_points[0] : null;

        public ControlPoint LastPoint => !IsEmpty ? m_points[m_points.Count - 1] : null;

        public float TotalDistance => m_fTotalDistance;

        public bool IsClosed => m_bIsClosed;

        public bool WantEditorGizmos => m_bWantEditorGizmos;

        public IEnumerable<ControlPoint> Points
        {
            get
            {
                foreach (ControlPoint cp in m_points)
                {
                    yield return cp;
                }
            }
        }

        #endregion

        private void OnEnable()
        {
            UpdateDistances();
        }

        public Pose GetPose(float fDistanceAlongCurve)
        {
            if (IsEmpty)
            {
                throw new System.Exception("Empty BezierCurve");
            }

            // smaller than first point?
            if (fDistanceAlongCurve <= FirstPoint.m_fDistance)
            {
                return new Pose
                {
                    position = FirstPoint.m_vPosition,
                    rotation = Quaternion.LookRotation(FirstPoint.m_vTangent)
                };
            }

            // larger than last point?
            if (!m_bIsClosed && fDistanceAlongCurve >= LastPoint.m_fDistance)
            {
                return new Pose
                {
                    position = LastPoint.m_vPosition,
                    rotation = Quaternion.LookRotation(LastPoint.m_vTangent)
                };
            }

            // find segment
            for (int i = 1; i < m_points.Count; i++)
            {
                ControlPoint A = m_points[i - 1];
                ControlPoint B = m_points[i];

                if (fDistanceAlongCurve <= B.m_fDistance)
                {
                    // blend between A & B
                    //float fBlend = Mathf.InverseLerp(A.m_fDistance, B.m_fDistance, fDistanceAlongCurve);
                    
                    float localDistance = fDistanceAlongCurve - A.m_fDistance;
                    float fBlend = FindFByDistance(A, B, localDistance);
                    return new Pose
                    {
                        position = GetPosition(A, B, fBlend),
                        rotation = Quaternion.LookRotation(GetForward(A, B, fBlend))
                    };
                }
            }

            if (m_bIsClosed && fDistanceAlongCurve <= m_fTotalDistance)
            {
                float fBlend = Mathf.InverseLerp(LastPoint.m_fDistance, m_fTotalDistance, fDistanceAlongCurve);
                return new Pose
                {
                    position = GetPosition(LastPoint, m_closedPoint, fBlend),
                    rotation = Quaternion.LookRotation(GetForward(LastPoint, m_closedPoint, fBlend))
                };
            }

            // should never happen :(
            throw new System.Exception("Should never happen");
        }

        public static Vector3 GetPosition(ControlPoint A, ControlPoint B, float f)
        {
            Vector3 p0 = A.m_vPosition;
            Vector3 p1 = A.m_vPosition + A.m_vTangent;
            Vector3 p2 = B.m_vPosition - B.m_vTangent;
            Vector3 p3 = B.m_vPosition;

            float fOneMinusT = 1.0f - f;
            return p0 * fOneMinusT * fOneMinusT * fOneMinusT +
                   p1 * 3 * fOneMinusT * fOneMinusT * f +
                   p2 * 3 * fOneMinusT * f * f +
                   p3 * f * f * f;
        }

        public static Vector3 GetForward(ControlPoint A, ControlPoint B, float f)
        {
            Vector3 p0 = A.m_vPosition;
            Vector3 p1 = A.m_vPosition + A.m_vTangent;
            Vector3 p2 = B.m_vPosition - B.m_vTangent;
            Vector3 p3 = B.m_vPosition;

            f = Mathf.Clamp01(f);
            float fOneMinusT = 1f - f;
            return 3f * fOneMinusT * fOneMinusT * (p1 - p0) +
                   6f * fOneMinusT * f * (p2 - p1) +
                   3f * f * f * (p3 - p2);
        }

        public void CalculateSmoothTangents(float fAmount = 0.25f)
        {
            for (int i = 0; i < m_points.Count; ++i)
            {
                Vector3 vPrev = m_points[i > 0 ? i - 1 : 0].m_vPosition;
                Vector3 vCurr = m_points[i].m_vPosition;
                Vector3 vNext = m_points[i < m_points.Count - 1 ? i + 1 : i].m_vPosition;

                Vector3 vDir1 = Vector3.Normalize(vCurr - vPrev);
                Vector3 vDir2 = Vector3.Normalize(vNext - vCurr);
                Vector3 vDir = Vector3.Normalize(vDir1 + vDir2);
                
                m_points[i].m_vTangent = vDir * fAmount;
            }
        }

        public void UpdateDistances()
        {
            m_fTotalDistance = 0.0f;

            if (IsEmpty)
            {
                return;
            }

            // start at distance zero!
            m_points[0].m_fDistance = 0.0f;

            // add up bezier curve segment distances
            for (int i = 1; i < m_points.Count; ++i)
            {
                ControlPoint A = m_points[i - 1];
                ControlPoint B = m_points[i];
                B.m_fDistance = A.m_fDistance + CalculateDistance(A, B);
            }

            m_fTotalDistance += LastPoint.m_fDistance;

            if (m_bIsClosed)
            {
                m_closedPoint.m_vPosition = FirstPoint.m_vPosition;
                m_closedPoint.m_vTangent = FirstPoint.m_vTangent;
                m_fTotalDistance += CalculateDistance(LastPoint, m_closedPoint);
                m_closedPoint.m_fDistance = m_fTotalDistance;
                m_closedPoint.m_vRoadSize = LastPoint.m_vRoadSize;
            }
        }

        protected static float CalculateDistance(ControlPoint A, ControlPoint B, int iNumSegments = 20)
        {
            float fDistance = 0.0f;
            Vector3 vLast = A.m_vPosition;
            for(int i=1; i<=iNumSegments; i++) 
            {
                float f = i / (float)iNumSegments;
                Vector3 vCurr = GetPosition(A, B, f);
                fDistance += Vector3.Distance(vLast, vCurr);
                vLast = vCurr;
            }

            return fDistance;
        }

        //Added this to smooth out the traveling along points, it should no longer slow down and stop at each point anymore.
        protected float FindFByDistance(ControlPoint A, ControlPoint B, float targetDist)
        {
            const int samples = 20;
            float accumulated = 0f;
            Vector3 last = GetPosition(A, B, 0f);
            
            for (int i = 1; i <= samples; ++i)
            {
                float f = i / (float)samples;
                Vector3 curr = GetPosition(A, B, f);
                float d = Vector3.Distance(last, curr);

                if (accumulated + d >= targetDist)
                {
                    float t = (targetDist - accumulated) / d;
                    return Mathf.Lerp((i - 1f) / samples, f, t);
                }
                accumulated += d;
                last = curr;
            }

            return 1f;
        }
        internal ControlPoint GetControlPointAtDistance(float fDistanceAlongCurve)
        {
            if (IsEmpty) return null;

            // Before first point
            if (fDistanceAlongCurve <= FirstPoint.m_fDistance) return FirstPoint;

            // After last point (open curve)
            if (!m_bIsClosed && fDistanceAlongCurve >= LastPoint.m_fDistance) return LastPoint;

            // Find segment
            for (int i = 1; i < m_points.Count; i++)
            {
                ControlPoint B = m_points[i];
                if (fDistanceAlongCurve <= B.m_fDistance)
                {
                    return B;
                }
            }

            // Closed curve wrap
            if (m_bIsClosed && fDistanceAlongCurve <= m_fTotalDistance)
            {
                return m_closedPoint;
            }

            throw new System.Exception("Could not find ControlPoint");
        }

        internal int GetControlPointIndex(ControlPoint cp)
        {
            for (int i = 0; i < m_points.Count; i++)
            {
                if (m_points[i] == cp) return i;
            }
            return -1;
        }

        internal int GetControlPointIndexAtDistance(float distance)
        {
            for (int i = 0; i < m_points.Count - 1; i++)
            {
                if (distance >= m_points[i].m_fDistance && distance < m_points[i + 1].m_fDistance)
                {
                    return i;
                }
            }

            return m_points.Count - 1;
        }


        public void GenerateBezierCurveListFile()
        {
            var fileName = "Assets/Editor/Bezier/BezierCurveList.txt";
            if (m_points.Count < 1) return; 
            if (File.Exists(fileName)) { File.Delete(fileName); }

            var sr = File.CreateText(fileName);
            sr.WriteLine("Points In Bezier Curve");
            for (int i = 0; i < m_points.Count; i++)
            {
                ControlPoint TheControlPoint = m_points[i];
                sr.WriteLine("Element {0}", i);
                sr.WriteLine("  V Position : X = {0} , Y = {1} , Z = {2}", TheControlPoint.m_vPosition.x, TheControlPoint.m_vPosition.y, TheControlPoint.m_vPosition.z);
                sr.WriteLine("  V Tangent  : X = {0} , Y = {1} , Z = {2}", TheControlPoint.m_vTangent.x, TheControlPoint.m_vTangent.y, TheControlPoint.m_vTangent.z);
                sr.WriteLine("  F Distance : {0}", TheControlPoint.m_fDistance);
            }

            sr.WriteLine("___________________________");
            for (int i = m_points.Count - 1; i > 0; i--)
            {
                if (m_points[i].m_fDistance - m_points[i - 1].m_fDistance >= 500f)
                {
                    sr.WriteLine("m_fDistance between ControlPoints {0}, and {1} is more than 500.0f in distance", i, i - 1);
                }
            }

            sr.Close();
        }
    }
}