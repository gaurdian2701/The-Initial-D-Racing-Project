using Bezier;
using ExternalForInspector;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Bezier.BezierCurve;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

namespace ProceduralTracks
{
    [System.Serializable]
    public class Vector3List
    {
        public List<Vector3> points = new List<Vector3>();
    }

    [RequireComponent(typeof(BezierCurve))]
    public class Tracks : ProceduralMesh
    {
        #region Properties

        [Header("Track Settings")]
        [SerializeField, Range(0.1f, 25.0f)] private float m_fTrackSegmentLength = 4.0f;
        [SerializeField] private Vector2 m_vRoadOutlineSize = new Vector2(1.0f, 0.2f);
        [SerializeField] private Vector3 m_vFinishLineSize = new Vector3(1.0f, 0.2f, 0.3f);

        [Header("Railing Settings")]
        [SerializeField] private bool m_bEnableRailing = true;
        [SerializeField, ShowIf("m_bEnableRailing")] private Vector2 m_vRailingPoleSize = new Vector2(0.05f, 1.0f);
        [SerializeField, ShowIf("m_bEnableRailing")] private Vector2 m_vRailingBarrierSize = new Vector2(0.1f, 0.15f);
        [SerializeField, ShowIf("m_bEnableRailing")] private float m_fRailingAngleStep = 5f;
        [SerializeField, ShowIf("m_bEnableRailing")] private Vector3 tangentThreshold = new Vector3(5.0f, 0.01f, 5.0f);
        private List<Vector3List> m_railingBarrierPosesList = new List<Vector3List>();

        [Header("Gameplay Objects")]
        [SerializeField] public List<GameObject> m_lEdgeBoxColliders = new List<GameObject>();
        [SerializeField] public GameObject m_gFinishLinePrefab;
        [SerializeField] public GameObject m_gFinishLineGameObject;

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
            List<int> FinishLineTriangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            // Generate track!
            DestroyAllEdgeBoxColliders();
            m_railingBarrierPosesList.Clear();
            AddRoadSegment(vertices, uvs, trackTriangles);
            GenerateTrackOutline(true, vertices, uvs, outlineTrackTriangles);
            GenerateTrackOutline(false, vertices, uvs, outlineTrackTriangles);
            if(m_bEnableRailing)
            {
                GeneratePolesRailing(true, vertices, uvs, railingTriangles);
                GeneratePolesRailing(false, vertices, uvs, railingTriangles);
                GenerateRailingBarrier(vertices, uvs, railingBarrierTriangles);
                GenerateRailingBarrier(vertices, uvs, railingBarrierTriangles);
            }
            GenerateFinishLine(vertices, uvs, FinishLineTriangles);
            GenerateEdgeBoxColliders();

            // assign the mesh data
            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs.ToArray();

            mesh.subMeshCount = 5;
            mesh.SetTriangles(trackTriangles.ToArray(), 0);
            mesh.SetTriangles(outlineTrackTriangles.ToArray(), 1);
            mesh.SetTriangles(railingTriangles.ToArray(), 2);
            //mesh.SetTriangles(railingBarrierTriangles.ToArray(), 3);
            mesh.SetTriangles(FinishLineTriangles.ToArray(), 4);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (GetComponent<MeshCollider>() != null)
            {
                GetComponent<MeshCollider>().sharedMesh = mesh;
            }
            return mesh;
        }

        protected void AddRoadSegment(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
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
                ControlPoint ControlPointA, ControlPointB;

                // determine segment CPs
                int cpIndex = bc.GetControlPointIndexAtDistance(distance);
                ControlPointA = bc.m_points[cpIndex];
                ControlPointB = (cpIndex + 1 < bc.m_points.Count) ? bc.m_points[cpIndex + 1] : ControlPointA;

                Vector3 roadSize = GetRoadSizeBetween(ControlPointA, ControlPointB, distance);

                Vector3 vRight = pose.right * roadSize.x;
                Vector3 vUp = pose.up * roadSize.y;
                Vector3 vForward = pose.forward * roadSize.z;

                Vector3[] slices = new Vector3[]
                {
                    pose.position + vRight + vUp + vForward,   // 0 top right front
                    pose.position - vRight + vUp + vForward,   // 1 top left front
                    pose.position - vRight - vUp + vForward,   // 2 bottom left front
                    pose.position + vRight - vUp + vForward,   // 3 bottom right front

                    pose.position + vRight + vUp - vForward,   // 4 top right back
                    pose.position - vRight + vUp - vForward,   // 5 top left back
                    pose.position - vRight - vUp - vForward,   // 6 bottom left back
                    pose.position + vRight - vUp - vForward    // 7 bottom right back
                };

                vertices.AddRange(slices);
                for(int j = 0; j < slices.Length; j++)
                {
                    uvs.Add(Vector2.zero);
                }

                if (ControlPointA != null && ControlPointA.m_bIsEdge)
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

        protected void GenerateTrackOutline(bool isRightHandSide, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
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

                Vector3 vRight = pose.right * m_vRoadOutlineSize.x;
                Vector3 vUp = pose.up * m_vRoadOutlineSize.y;

                ControlPoint ControlPointA, ControlPointB;

                // determine segment CPs
                int cpIndex = bc.GetControlPointIndexAtDistance(distance);
                ControlPointA = bc.m_points[cpIndex];
                ControlPointB = (cpIndex + 1 < bc.m_points.Count) ? bc.m_points[cpIndex + 1] : ControlPointA;

                Vector3 roadSize = GetRoadSizeBetween(ControlPointA, ControlPointB, distance);

                float roadOutlineOffset = isRightHandSide ? roadSize.x : -roadSize.x;
                Vector3 vOffset = roadOutlineOffset * pose.right;

                Vector3[] slices = new Vector3[]
                {
                    pose.position + vOffset - vRight,
                    pose.position + vOffset - vRight * 0.75f + vUp,
                    pose.position + vOffset + vRight * 0.75f + vUp,
                    pose.position + vOffset + vRight,

                    pose.position + vOffset + vRight * 0.75f - vUp,
                    pose.position + vOffset - vRight * 0.75f - vUp,
                };

                vertices.AddRange(slices);

                for (int j = 0; j < slices.Length; j++)
                {
                    uvs.Add(Vector2.zero);
                }

                if (ControlPointA != null && ControlPointA.m_bIsEdge)
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

        protected void GeneratePolesRailing(bool isRightHandSide, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            BezierCurve bc = GetComponent<BezierCurve>();
            int iSegmentCount = Mathf.CeilToInt(bc.TotalDistance / m_fTrackSegmentLength);

            Vector3 prevTangent = Vector3.zero;
            float accumulatedAngle = 0f;
            Vector3List currentGroup = null;
            bool inGroup = false;
            bool justClosedGroup = false;

            for (int i = 0; i < iSegmentCount; i++)
            {
                float prc = i / (float)iSegmentCount;
                float distance = prc * bc.TotalDistance;

                Pose pose = bc.GetPose(distance);
                ControlPoint ControlPointA, ControlPointB;

                // determine segment CPs
                int cpIndex = bc.GetControlPointIndexAtDistance(distance);
                ControlPointA = bc.m_points[cpIndex];
                ControlPointB = (cpIndex + 1 < bc.m_points.Count) ? bc.m_points[cpIndex + 1] : ControlPointA;

                Vector3 tangent = pose.forward;

                bool validSection = (Mathf.Abs(tangent.x) <= tangentThreshold.x && Mathf.Abs(tangent.y) <= tangentThreshold.y && Mathf.Abs(tangent.z) <= tangentThreshold.z);
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
                    justClosedGroup = true;
                    prevTangent = tangent;
                    continue;
                }

                if (i > 0)
                {
                    float angle = Vector3.Angle(prevTangent, tangent);
                    accumulatedAngle += angle;

                    if (ControlPointA != null && ControlPointA.m_bIsEdge && inGroup)
                    {
                        if (currentGroup.points.Count > 1)  m_railingBarrierPosesList.Add(currentGroup);

                        currentGroup = null;
                        inGroup = false;
                        accumulatedAngle = 0f;
                        justClosedGroup = true;
                        prevTangent = tangent;
                        continue;
                    }

                    if (accumulatedAngle >= m_fRailingAngleStep)
                    {
                        if (justClosedGroup)
                        {
                            accumulatedAngle = 0f;
                            justClosedGroup = false;
                            prevTangent = tangent;
                            continue; // skip first pole to break continuity
                        }

                        Vector3 roadSize = GetRoadSizeBetween(ControlPointA, ControlPointB, distance);
                        float roadOutlineOffset = isRightHandSide ? roadSize.x : -roadSize.x;
                        Vector3 polePosition = pose.position + pose.right * roadOutlineOffset;

                        if (!inGroup)
                        {
                            currentGroup = new Vector3List();
                            inGroup = true;
                        }

                        AddCylinderPole(vertices, uvs, triangles, polePosition, inGroup);
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

        protected void AddCylinderPole(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, Vector3 position, bool shouldEnableTriangles)
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
                uvs.Add(Vector2.zero);
                uvs.Add(Vector2.zero);
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

        protected void GenerateRailingBarrier(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            foreach (Vector3List poleGroup in m_railingBarrierPosesList)
            {
                if (poleGroup.points.Count < 2) continue;

                int groupStartVertex = vertices.Count;
                int groupSectionCount = 0;
                int prevSectionStart = -1;

                float startExtension = Vector3.Distance(poleGroup.points[0], poleGroup.points[1]) * 0.1f;

                int lastIndex = poleGroup.points.Count - 1;
                float endExtension = Vector3.Distance(poleGroup.points[lastIndex], poleGroup.points[lastIndex - 1]) * 0.1f;

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

                    if (i == 0) basePos -= forward * startExtension; 
                    else if (i == lastIndex) basePos += forward * endExtension;

                    Vector3 vRight = right * m_vRailingBarrierSize.x;
                    Vector3 vUp = up * m_vRailingBarrierSize.y;

                    Vector3[] slices = new Vector3[]
                    {
                        basePos - vRight,
                        basePos - vRight * 0.75f + vUp,
                        basePos + vRight * 0.75f + vUp,
                        basePos + vRight,

                        basePos + vRight * 0.75f - vUp,
                        basePos - vRight * 0.75f - vUp,
                    };

                    int currSectionStart = vertices.Count;
                    vertices.AddRange(slices);

                    for (int j = 0; j < slices.Length; j++)
                    {
                        uvs.Add(Vector2.zero);
                    }

                    groupSectionCount++;

                    if (prevSectionStart != -1)
                    {
                        for (int j = 0; j < 6; j++)
                        {
                            int jNext = (j + 1) % 6;

                            triangles.Add(prevSectionStart + j);
                            triangles.Add(currSectionStart + j);
                            triangles.Add(prevSectionStart + jNext);

                            triangles.Add(prevSectionStart + jNext);
                            triangles.Add(currSectionStart + j);
                            triangles.Add(currSectionStart + jNext);
                        }
                    }

                    prevSectionStart = currSectionStart;
                }
                AddCap(triangles, groupStartVertex, flip: false);
                int endBase = groupStartVertex + (groupSectionCount - 1) * 6;
                AddCap(triangles, endBase, flip: true);
            }
        }

        protected void AddCap(List<int> triangles, int baseIndex, bool flip)
        {
            int[] order = flip
                ? new[] { 0, 5, 4, 3, 2, 1 }
                : new[] { 0, 1, 2, 3, 4, 5 };

            for (int i = 1; i < 5; i++)
            {
                triangles.Add(baseIndex + order[0]);
                triangles.Add(baseIndex + order[i]);
                triangles.Add(baseIndex + order[i + 1]);
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
                newBoxCollider.size = new Vector3( cp.m_vRoadSize.z * multiplyXValue, cp.m_vRoadSize.y * multiplyYValue, cp.m_vRoadSize.x * 3.0f);
                newBoxCollider.isTrigger = true;
                m_lEdgeBoxColliders.Add(colliderGO);
            }
        }

        private void GenerateFinishLine(List<Vector3> vertices, List<Vector2> uvs, List<int> finishLineTriangles)
        {
            BezierCurve bc = GetComponent<BezierCurve>();

            float finishDistance = m_vFinishLineSize.z; // 0–1
            Pose pose = bc.GetPose(0.0f);
            ControlPoint cp = bc.GetControlPointAtDistance(0.0f);

            float lineDepth = 1.2f;
            float heightOffset = cp.m_vRoadSize.y + 0.001f;
            int segments = 4;
            float segmentWidth = cp.m_vRoadSize.x * 2 / segments;

            Vector3 forward = pose.forward * (lineDepth * 0.5f);
            Vector3 up = Vector3.up * heightOffset; // world up
            float halfRoadWidth = cp.m_vRoadSize.x;

            for (int i = 0; i < segments; i++)
            {
                float xStart = -halfRoadWidth + segmentWidth * i;
                float xEnd = xStart + segmentWidth;

                Vector3 rightStart = pose.right * xStart;
                Vector3 rightEnd = pose.right * xEnd;

                int baseIndex = vertices.Count;

                // Quad vertices
                vertices.Add(pose.position + up - rightStart - forward); // 0
                vertices.Add(pose.position + up - rightEnd - forward);   // 1
                vertices.Add(pose.position + up - rightEnd + forward);   // 2
                vertices.Add(pose.position + up - rightStart + forward); // 3

                // UVs
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(0, 1));

                // Triangles (winding order flipped to point up)
                finishLineTriangles.Add(baseIndex + 0);
                finishLineTriangles.Add(baseIndex + 1);
                finishLineTriangles.Add(baseIndex + 2);

                finishLineTriangles.Add(baseIndex + 0);
                finishLineTriangles.Add(baseIndex + 2);
                finishLineTriangles.Add(baseIndex + 3);
            }
            ScaledFinishLinePrefab();
        }

        private void ScaledFinishLinePrefab()
        {
            BezierCurve bc = GetComponent<BezierCurve>();
            if (m_gFinishLineGameObject)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_gFinishLineGameObject);
                }
                else
                {
                    DestroyImmediate(m_gFinishLineGameObject);
                }
            }
            const float multiplyXValue = 20.0f;
            const float multiplyYValue = 80.0f;
            BezierCurve.ControlPoint cp = bc.m_points[0];

            GameObject finishLinePrefab = Instantiate(m_gFinishLinePrefab);
            finishLinePrefab.name = "FinishLine";

            finishLinePrefab.transform.SetParent(transform, false);

            finishLinePrefab.transform.localPosition = cp.m_vPosition;
            Quaternion rotation = Quaternion.LookRotation(cp.m_vTangent.normalized) * Quaternion.Euler(0f, 90f, 0f);
            finishLinePrefab.transform.localRotation = rotation;

            GameObject pole_L = finishLinePrefab.transform.Find("Pole_L").gameObject;
            Vector3 poleLPos_L = pole_L.transform.localPosition;
            poleLPos_L.z = cp.m_vRoadSize.x;
            pole_L.transform.localPosition = poleLPos_L;
            GameObject pole_R = finishLinePrefab.transform.Find("Pole_R").gameObject;
            Vector3 poleLPos_R = pole_R.transform.localPosition;
            poleLPos_R.z = -cp.m_vRoadSize.x;
            pole_R.transform.localPosition = poleLPos_R;

            GameObject banner = finishLinePrefab.transform.Find("Banner").gameObject;
            Vector3 bannerScale = banner.transform.localScale;
            bannerScale.z = cp.m_vRoadSize.x * 0.25f;
            banner.transform.localScale = bannerScale;

            BoxCollider newBoxCollider = finishLinePrefab.AddComponent<BoxCollider>();
            newBoxCollider.center = new Vector3(0.0f, pole_L.transform.localPosition.y, 0.0f);
            newBoxCollider.size = new Vector3(cp.m_vRoadSize.z * multiplyXValue, cp.m_vRoadSize.y * multiplyYValue, cp.m_vRoadSize.x * 2.0f);
            newBoxCollider.isTrigger = true;
            m_gFinishLineGameObject = finishLinePrefab;
        }

        Vector3 GetRoadSizeBetween(ControlPoint a,ControlPoint b, float distance)
        {
            if (a == null || b == null) return a != null ? a.m_vRoadSize : Vector3.zero;

            if (a.m_bIsEdge || b.m_bIsEdge) return a.m_vRoadSize;

            float t = Mathf.InverseLerp(a.m_fDistance, b.m_fDistance, distance);

            return Vector3.Lerp(a.m_vRoadSize, b.m_vRoadSize, t);
        }
    }
}
