using Bezier;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Bezier.BezierCurve;

namespace ProceduralTracks
{
    [RequireComponent(typeof(BezierCurve))]
    public class Tracks : ProceduralMesh
    {
        [SerializeField, Range(0.25f, 5.0f)] private float m_fTrackSegmentLength = 4.0f;

        [SerializeField] private Vector3 m_vRoadSize = new Vector3(1.0f, 0.2f, 0.3f);

        [SerializeField] private Vector2 m_vRoadOutlineSize = new Vector2(1.0f, 0.2f);

        [SerializeField] private Vector2 m_vRailingPoleSize = new Vector2(1.0f, 0.2f);
        [SerializeField] private Vector2 m_vRailingBarrierSize = new Vector2(1.0f, 0.2f);
        [SerializeField] private float m_fAngleStep = 5f;

        [SerializeField] public List<GameObject> m_lEdgeBoxColliders = new List<GameObject>();

        [System.Serializable]
        public class Vector3List
        {
            public List<Vector3> points = new List<Vector3>();
        }

        private List<Vector3List> m_railingBarrierPosesList = new List<Vector3List>();


        #region Properties

        public Vector3 SleeperSize => m_vRoadSize;

        #endregion

        protected override Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.hideFlags = HideFlags.DontSave;
            mesh.name = "Tracks";

            List<Vector3> vertices = new List<Vector3>();
            List<int> trackTriangles = new List<int>();
            List<int> outlineTrackTriangles = new List<int>();
            List<int> railingTriangles = new List<int>();
            List<int> railingBarrierTriangles = new List<int>();

            // Generate track!
            DestroyAllEdgeBoxColliders();
            m_railingBarrierPosesList.Clear();
            AddRoadSegment(vertices, trackTriangles);
            GenerateTrackOutline(m_vRoadSize.x, vertices, outlineTrackTriangles);
            GenerateTrackOutline(-m_vRoadSize.x, vertices, outlineTrackTriangles);
            GeneratePolesRailing(m_vRoadSize.x, vertices, railingTriangles);
            GeneratePolesRailing(-m_vRoadSize.x, vertices, railingTriangles);
            GenerateRailingBarrier(m_vRoadSize.x, vertices, railingBarrierTriangles);
            GenerateRailingBarrier(-m_vRoadSize.x, vertices, railingBarrierTriangles);
            GenerateEdgeBoxColliders();

            // assign the mesh data
            mesh.vertices = vertices.ToArray();

            mesh.subMeshCount = 4;
            mesh.SetTriangles(trackTriangles.ToArray(), 0);
            mesh.SetTriangles(outlineTrackTriangles.ToArray(), 1);
            mesh.SetTriangles(railingTriangles.ToArray(), 2);
            mesh.SetTriangles(railingBarrierTriangles.ToArray(), 3);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (GetComponent<MeshCollider>() != null)
            {
                GetComponent<MeshCollider>().sharedMesh = mesh;
            }
            return mesh;
        }

        protected void AddRoadSegment(List<Vector3> vertices, List<int> triangles)
        {
            BezierCurve bc = GetComponent<BezierCurve>();
            int iSegmentCount = Mathf.CeilToInt(bc.TotalDistance / m_fTrackSegmentLength);
            int vertsPerSlice = 8;
            bool canConnectToPrevious = true;

            for (int i = 0; i <= iSegmentCount; ++i)
            {
                float fPrc = i / (float)iSegmentCount;
                float distance = fPrc * bc.TotalDistance;

                Pose pose = bc.GetPose(distance);
                ControlPoint cp = bc.GetControlPointAtDistance(distance);

                Vector3 vRight = pose.right * m_vRoadSize.x;
                Vector3 vUp = pose.up * m_vRoadSize.y;
                Vector3 vForward = pose.forward * m_vRoadSize.z;

                vertices.AddRange(new Vector3[]
                {
                    pose.position + vRight + vUp + vForward,   // 0 top right front
                    pose.position - vRight + vUp + vForward,   // 1 top left front
                    pose.position - vRight - vUp + vForward,   // 2 bottom left front
                    pose.position + vRight - vUp + vForward,   // 3 bottom right front

                    pose.position + vRight + vUp - vForward,   // 4 top right back
                    pose.position - vRight + vUp - vForward,   // 5 top left back
                    pose.position - vRight - vUp - vForward,   // 6 bottom left back
                    pose.position + vRight - vUp - vForward    // 7 bottom right back
                });

                if (cp != null && cp.m_bIsEdge)
                {
                    canConnectToPrevious = false;
                    continue;
                }

                // add triangles
                if (i > 0 && canConnectToPrevious)
                {
                    int baseIndex = i * vertsPerSlice;
                    int prevBase = baseIndex - vertsPerSlice;

                    AddQuad(triangles, prevBase + 0, prevBase + 1, baseIndex + 1, baseIndex + 0); // top
                    AddQuad(triangles, prevBase + 2, prevBase + 3, baseIndex + 3, baseIndex + 2); // bottom
                    //AddQuad(triangles, prevBase + 1, prevBase + 2, baseIndex + 2, baseIndex + 1); // left
                    //AddQuad(triangles, prevBase + 0, prevBase + 3, baseIndex + 3, baseIndex + 0); // right
                }
                canConnectToPrevious = true;
            }
        }

        protected void GenerateTrackOutline(float roadOutlineOffset, List<Vector3> vertices, List<int> triangles)
        {
            BezierCurve bc = GetComponent<BezierCurve>();

            int iStart = vertices.Count;
            int iSegmentCount = Mathf.CeilToInt(bc.TotalDistance / m_fTrackSegmentLength);
            bool canConnectToPrevious = true;

            for (int i = 0; i <= iSegmentCount; ++i)
            {
                float fPrc = i / (float)iSegmentCount;
                float distance = fPrc * bc.TotalDistance;

                Pose pose = bc.GetPose(distance);
                ControlPoint cp = bc.GetControlPointAtDistance(distance);

                Vector3 vRight = pose.right * m_vRoadOutlineSize.x;
                Vector3 vUp = pose.up * m_vRoadOutlineSize.y;
                Vector3 vOffset = roadOutlineOffset * pose.right;

                vertices.AddRange(new Vector3[]
                {
                    pose.position + vOffset - vRight,           
                    pose.position + vOffset - vRight * 0.75f + vUp,   
                    pose.position + vOffset + vRight * 0.75f + vUp,   
                    pose.position + vOffset + vRight,

                    pose.position + vOffset + vRight * 0.75f - vUp,
                    pose.position + vOffset - vRight * 0.75f - vUp,
                });

                if (cp != null && cp.m_bIsEdge)
                {
                    canConnectToPrevious = false;
                    continue;
                }

                // add triangles
                if (i < iSegmentCount && canConnectToPrevious)
                {
                    int curr = iStart + i * 6;
                    int next = curr + 6;

                    for (int j = 0; j < 6; j++)
                    {
                        int jNext = (j + 1) % 6;

                        triangles.Add(curr + j);
                        triangles.Add(next + j);
                        triangles.Add(curr + jNext);

                        triangles.Add(curr + jNext);
                        triangles.Add(next + j);
                        triangles.Add(next + jNext);
                    }
                }
                canConnectToPrevious = true;
            }
        }

        protected void GeneratePolesRailing(float roadOutlineOffset, List<Vector3> vertices, List<int> triangles)
        {
            BezierCurve bc = GetComponent<BezierCurve>();
            int iSegmentCount = Mathf.CeilToInt(bc.TotalDistance / m_fTrackSegmentLength);
            bool canConnectToPrevious = true;

            Vector3 prevTangent = Vector3.zero;
            float accumulatedAngle = 0f;
            const float tangentThreshold = 5f;
            Vector3List currentGroup = null;
            bool inGroup = false;


            for (int i = 0; i < iSegmentCount; i++)
            {
                float prc = i / (float)iSegmentCount;
                float distance = prc * bc.TotalDistance;

                Pose pose = bc.GetPose(distance);
                ControlPoint cp = bc.GetControlPointAtDistance(distance);
                Vector3 tangent = pose.forward;

                bool validSection = Mathf.Abs(tangent.x) <= tangentThreshold && Mathf.Abs(tangent.z) <= tangentThreshold && Mathf.Abs(tangent.y) <= 0.001f;
                if (!validSection)
                {
                    // close group if we were in one
                    if (inGroup && currentGroup.points.Count > 1)
                    {
                        m_railingBarrierPosesList.Add(currentGroup);
                    }

                    currentGroup = null;
                    inGroup = false;
                    accumulatedAngle = 0f;
                    prevTangent = tangent;
                    continue;
                }

                if (i > 0)
                {
                    float angle = Vector3.Angle(prevTangent, tangent);
                    accumulatedAngle += angle;

                    if (cp != null && cp.m_bIsEdge && inGroup)
                    {
                        if (currentGroup.points.Count > 1)  m_railingBarrierPosesList.Add(currentGroup);

                        currentGroup = null;
                        inGroup = false;
                        accumulatedAngle = 0f;
                        prevTangent = tangent;
                        continue;
                    }

                    if (accumulatedAngle >= m_fAngleStep)
                    {
                        Vector3 polePosition = pose.position + pose.right * roadOutlineOffset;

                        if (!inGroup)
                        {
                            currentGroup = new Vector3List();
                            inGroup = true;
                        }

                        AddCylinderPole(vertices, triangles, polePosition, inGroup);
                        currentGroup.points.Add(polePosition);
                        accumulatedAngle = 0f;
                    }
                }
                prevTangent = tangent;
            }
            if (inGroup && currentGroup != null && currentGroup.points.Count > 1)
            {
                m_railingBarrierPosesList.Add(currentGroup);
            }
        }

        protected void AddCylinderPole(List<Vector3> vertices, List<int> triangles, Vector3 position, bool shouldEnableTriangles)
        {
            int startIndex = vertices.Count;
            const int poleSides = 8;

            // bottom & top rings
            for (int i = 0; i <= poleSides; i++)
            {
                float angle = i * Mathf.PI * 2f / poleSides;
                float x = Mathf.Cos(angle) * m_vRailingPoleSize.x;
                float z = Mathf.Sin(angle) * m_vRailingPoleSize.x;

                vertices.Add(position + new Vector3(x, 0f, z));
                vertices.Add(position + new Vector3(x, m_vRailingPoleSize.y, z));
            }

            if (!shouldEnableTriangles) return;

            // side faces
            for (int i = 0; i < poleSides; i++)
            {
                int i0 = startIndex + i * 2;
                int i1 = i0 + 1;
                int i2 = i0 + 2;
                int i3 = i0 + 3;

                triangles.Add(i0);
                triangles.Add(i1);
                triangles.Add(i2);

                triangles.Add(i2);
                triangles.Add(i1);
                triangles.Add(i3);
            }
        }

        protected void GenerateRailingBarrier(float roadOutlineOffset, List<Vector3> vertices, List<int> triangles)
        {
            foreach (Vector3List poleGroup in m_railingBarrierPosesList)
            {
                if (poleGroup.points.Count < 2) continue;

                bool canConnectToPrevious = false;

                for (int i = 0; i < poleGroup.points.Count; i++)
                {
                    Vector3 pos = poleGroup.points[i];

                    // direction along the pole chain
                    Vector3 forward = Vector3.zero;
                    if (i < poleGroup.points.Count - 1) forward = (poleGroup.points[i + 1] - pos).normalized;
                    else forward = (pos - poleGroup.points[i - 1]).normalized;

                    // build a stable frame
                    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                    Vector3 up = Vector3.up;

                    // rail sits on top of poles
                    Vector3 basePos = pos + up * m_vRailingPoleSize.y;

                    Vector3 vRight = right * m_vRailingBarrierSize.x;
                    Vector3 vUp = up * m_vRailingBarrierSize.y;

                    vertices.AddRange(new Vector3[]
                    {
                        basePos - vRight,
                        basePos - vRight * 0.75f + vUp,
                        basePos + vRight * 0.75f + vUp,
                        basePos + vRight,

                        basePos + vRight * 0.75f - vUp,
                        basePos - vRight * 0.75f - vUp,
                    });

                    // connect to previous segment in this group only
                    if (canConnectToPrevious)
                    {
                        int curr = vertices.Count - 6;
                        int prev = curr - 6;

                        for (int j = 0; j < 6; j++)
                        {
                            int jNext = (j + 1) % 6;

                            triangles.Add(prev + j);
                            triangles.Add(curr + j);
                            triangles.Add(prev + jNext);

                            triangles.Add(prev + jNext);
                            triangles.Add(curr + j);
                            triangles.Add(curr + jNext);
                        }
                    }

                    canConnectToPrevious = true;
                }
            }
        }

        protected void AddQuad(List<int> tris, int a, int b, int c, int d)
        {
            tris.Add(a);
            tris.Add(b);
            tris.Add(c);

            tris.Add(a);
            tris.Add(c);
            tris.Add(d);
        }

        private void DestroyAllEdgeBoxColliders()
        {
            // clean up colliders
            foreach (GameObject boxCollider in m_lEdgeBoxColliders)
            {
                if (Application.isPlaying)
                {
                    Destroy(boxCollider);
                }
                else
                {
                    DestroyImmediate(boxCollider);
                }
            }
            m_lEdgeBoxColliders.Clear();
        }

        private void GenerateEdgeBoxColliders()
        {
            const float multiplyXValue = 20.0f;
            const float multiplyYValue = 100.0f;
            BezierCurve bc = GetComponent<BezierCurve>();
            for(int i = 0; i < bc.m_points.Count; ++i)
            {
                BezierCurve.ControlPoint cp = bc.m_points[i];
                if (!cp.m_bIsEdge) continue;

                ControlPoint edgedCP = bc.m_points[i - 1];
                GameObject colliderGO = new GameObject("EdgeCollider Point ref " + i);
                colliderGO.transform.SetParent(transform, false);

                colliderGO.transform.localPosition = edgedCP.m_vPosition + Vector3.up * (multiplyYValue / 20.0f);
                Quaternion rotation = Quaternion.LookRotation(edgedCP.m_vTangent.normalized) * Quaternion.Euler(0f, 90f, 0f);
                colliderGO.transform.localRotation = rotation;

                BoxCollider newBoxCollider = colliderGO.AddComponent<BoxCollider>();
                newBoxCollider.size = new Vector3( m_vRoadSize.z * multiplyXValue, m_vRoadSize.y * multiplyYValue,  m_vRoadSize.x * 3.0f);
                m_lEdgeBoxColliders.Add(colliderGO);
            }
        }
    }
}
