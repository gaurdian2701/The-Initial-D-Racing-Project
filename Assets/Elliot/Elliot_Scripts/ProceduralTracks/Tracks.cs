using Bezier;
using ExternalForInspector;
using System.Collections.Generic;
using UnityEngine;
using static Bezier.BezierCurve;

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
        [SerializeField] private bool m_bEnableRotationToRoad = false;

        [Header("Railing Settings")]
        [SerializeField] private bool m_bEnableRailing = true;
        [SerializeField, ShowIf("m_bEnableRailing")] private Vector2 m_vRailingPoleSize = new Vector2(0.05f, 1.0f);
        [SerializeField, ShowIf("m_bEnableRailing")] private Vector2 m_vRailingBarrierSize = new Vector2(0.1f, 0.15f);
        [SerializeField, ShowIf("m_bEnableRailing")] private float m_fRailingAngleStep = 5f;
        [SerializeField, ShowIf("m_bEnableRailing")] private Vector3 m_vTangentThreshold = new Vector3(5.0f, 0.01f, 5.0f);
        [SerializeField, ShowIf("m_bEnableRailing")] private Material m_mRailingPolesMAT;
        [SerializeField, ShowIf("m_bEnableRailing")] private Material m_mRailingBarrierMAT;

        private List<Vector3List> m_railingBarrierPosesList = new List<Vector3List>();

        [Header("Gameplay Objects")]
        [SerializeField] public List<GameObject> m_lEdgeBoxColliders = new List<GameObject>();
        [SerializeField] public List<GameObject> m_lRacingCheckPoints = new List<GameObject>();
        [SerializeField] public GameObject m_gFinishLinePrefab;
        [SerializeField] public GameObject m_gFinishLineGameObject;
        [SerializeField] public GameObject m_gRailing_R;
        [SerializeField] public GameObject m_gRailing_L;

        public float TrackSegmentLength => m_fTrackSegmentLength;
        #endregion

        #region MeshCreation
        protected override Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.hideFlags = HideFlags.DontSave;
            mesh.name = "Tracks";

            List<Vector3> vertices = new List<Vector3>();

            List<int> trackTriangles = new List<int>();
            List<int> outlineTrackTriangles = new List<int>();
            List<int> FinishLineTriangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            // Generate track!
            AddRoadSegment(vertices, uvs, trackTriangles);
            GenerateTrackOutline(true, vertices, uvs, outlineTrackTriangles);
            GenerateTrackOutline(false, vertices, uvs, outlineTrackTriangles);

            //Generate Railing!
            if (m_bEnableRailing)
            {
                m_gRailing_R = new RailingGameObject(transform, "Railing_R").m_gGameObject;
                m_gRailing_L = new RailingGameObject(transform, "Railing_L").m_gGameObject;
            }
            else m_railingBarrierPosesList.Clear();

            //Generate Road GameObjects!
            new FinishLineGameObject(transform, "FinishLine", vertices, uvs, FinishLineTriangles);
            new CheckPointGameObjectList(transform, "CheckPoints");
            new EdgeGameObjectList(transform, "EdgeColliders");

            // assign the mesh data
            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs.ToArray();

            mesh.subMeshCount = 3;
            mesh.SetTriangles(trackTriangles.ToArray(), 0);
            mesh.SetTriangles(outlineTrackTriangles.ToArray(), 1);
            mesh.SetTriangles(FinishLineTriangles.ToArray(), 2);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (GetComponent<MeshCollider>() != null)
            {
                GetComponent<MeshCollider>().sharedMesh = mesh;
            }
            this.gameObject.layer = LayerMask.NameToLayer("Track");
            return mesh;
        }
        #endregion

        #region Track Generation

        private void AddRoadSegment(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            BezierCurve bc = GetComponent<BezierCurve>();
            if (bc == null) return;

            int iSegmentCount = Mathf.CeilToInt(bc.TotalDistance / m_fTrackSegmentLength);
            int vertsPerSlice = 8;
            bool canConnectToPrevious = true;

            if (iSegmentCount <= 0) iSegmentCount = 1;

            for (int i = 0; i <= iSegmentCount; ++i)
            {
                float fPrc = i / (float)iSegmentCount;
                float distance = fPrc * bc.TotalDistance;

                Pose pose = bc.GetPose(distance);

                int cpIndex = Mathf.Clamp(bc.GetControlPointIndexAtDistance(distance), 0, Mathf.Max(0, bc.m_points.Count - 1));
                BezierCurve.ControlPoint ControlPointA = bc.m_points[cpIndex];
                BezierCurve.ControlPoint ControlPointB = (cpIndex + 1 < bc.m_points.Count) ? bc.m_points[cpIndex + 1] : ControlPointA;

                Vector3 roadSize = GetRoadSizeBetween(ControlPointA, ControlPointB, distance);

                Vector3 vRight;
                Vector3 vUp;
                Vector3 vForward;

                if (m_bEnableRotationToRoad) // derive world-space axes from final rotation and scale by road sizes
                {
                    Quaternion cpRotA = (ControlPointA != null) ? ControlPointA.m_qRotation : Quaternion.identity;
                    Quaternion cpRotB = (ControlPointB != null) ? ControlPointB.m_qRotation : Quaternion.identity;
                    float rotT = 0f;
                    if (ControlPointA != null && ControlPointB != null && !Mathf.Approximately(ControlPointA.m_fDistance, ControlPointB.m_fDistance))
                    {
                        rotT = Mathf.InverseLerp(ControlPointA.m_fDistance, ControlPointB.m_fDistance, distance);
                    }
                    else if (ControlPointA != null)
                    {
                        rotT = 0f;
                        cpRotB = cpRotA;
                    }

                    Quaternion interpolatedCpRot = Quaternion.Slerp(cpRotA, cpRotB, rotT);
                    Quaternion finalRotation = pose.rotation * interpolatedCpRot;

                    vRight = finalRotation * Vector3.right * roadSize.x;
                    vUp = finalRotation * Vector3.up * roadSize.y;
                    vForward = finalRotation * Vector3.forward * roadSize.z;
                }
                else
                {
                    vRight = pose.right * roadSize.x;
                    vUp = pose.up * roadSize.y;
                    vForward = pose.forward * roadSize.z;
                }

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

                // Append vertices and placeholder UVs
                int sliceStartIndex = vertices.Count;
                vertices.AddRange(slices);
                for (int j = 0; j < slices.Length; j++)
                {
                    uvs.Add(Vector2.zero);
                }

                // If this control point marks an edge, break continuity and skip connecting triangles
                if (ControlPointA != null && ControlPointA.m_bIsEdge)
                {
                    canConnectToPrevious = false;
                    continue;
                }

                // add triangles connecting to previous slice when allowed
                if (i > 0 && canConnectToPrevious)
                {
                    int currBase = sliceStartIndex;
                    int prevBase = currBase - vertsPerSlice;

                    if (prevBase >= 0 && currBase + vertsPerSlice - 1 < vertices.Count)
                    {
                        // top quad
                        AddQuad(triangles, prevBase + 0, prevBase + 1, currBase + 1, currBase + 0);
                        // bottom quad
                        AddQuad(triangles, prevBase + 2, prevBase + 3, currBase + 3, currBase + 2);
                    }
                }

                canConnectToPrevious = true;
            }
        }

        private void GenerateTrackOutline(bool isRightHandSide, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
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

                ControlPoint ControlPointA, ControlPointB;

                int cpIndex = bc.GetControlPointIndexAtDistance(distance);
                ControlPointA = bc.m_points[cpIndex];
                ControlPointB = (cpIndex + 1 < bc.m_points.Count) ? bc.m_points[cpIndex + 1] : ControlPointA;

                Vector3 roadSize = GetRoadSizeBetween(ControlPointA, ControlPointB, distance);

                Vector3 rightDir;
                Vector3 upDir;
                if (m_bEnableRotationToRoad)
                {
                    Quaternion cpRotA = (ControlPointA != null) ? ControlPointA.m_qRotation : Quaternion.identity;
                    Quaternion cpRotB = (ControlPointB != null) ? ControlPointB.m_qRotation : Quaternion.identity;
                    float rotT = 0f;
                    if (ControlPointA != null && ControlPointB != null && !Mathf.Approximately(ControlPointA.m_fDistance, ControlPointB.m_fDistance))
                    {
                        rotT = Mathf.InverseLerp(ControlPointA.m_fDistance, ControlPointB.m_fDistance, distance);
                    }
                    else if (ControlPointA != null)
                    {
                        rotT = 0f;
                        cpRotB = cpRotA;
                    }
                    Quaternion interpolatedCpRot = Quaternion.Slerp(cpRotA, cpRotB, rotT);
                    Quaternion finalRotation = pose.rotation * interpolatedCpRot;

                    rightDir = (finalRotation * Vector3.right).normalized;
                    upDir = (finalRotation * Vector3.up).normalized;
                }
                else
                {
                    rightDir = pose.right;
                    upDir = pose.up;
                }

                Vector3 vRight = rightDir * m_vRoadOutlineSize.x;
                Vector3 vUp = upDir * m_vRoadOutlineSize.y;

                float roadOutlineOffset = isRightHandSide ? roadSize.x : -roadSize.x;
                Vector3 vOffset = roadOutlineOffset * rightDir;

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

        private void AddQuad(List<int> tris, int a, int b, int c, int d)
        {
            tris.Add(a);
            tris.Add(b);
            tris.Add(c);

            tris.Add(a);
            tris.Add(c);
            tris.Add(d);
        }

        Vector3 GetRoadSizeBetween(ControlPoint a, ControlPoint b, float distance)
        {
            if (a == null || b == null) return a != null ? a.m_vRoadSize : Vector3.zero;

            if (a.m_bIsEdge || b.m_bIsEdge) return a.m_vRoadSize;

            float t = Mathf.InverseLerp(a.m_fDistance, b.m_fDistance, distance);

            return Vector3.Lerp(a.m_vRoadSize, b.m_vRoadSize, t);
        }
        #endregion

        #region GameObject_Generation

        private class RailingGameObject // The phone is ringing!
        {
            public GameObject m_gGameObject { get; }
            private MeshFilter m_mfMeshFilter => m_gGameObject.AddComponent<MeshFilter>();
            private MeshRenderer m_mrMeshRenderer => m_gGameObject.AddComponent<MeshRenderer>();
            private MeshCollider m_mcMeshCollider => m_gGameObject.AddComponent<MeshCollider>();
            private BezierCurve m_bcBezierCurve => m_gGameObject.GetComponentInParent<BezierCurve>();
            private Tracks m_tTracks => m_gGameObject.GetComponentInParent<Tracks>();
            public RailingGameObject(Transform parent, string name)
            {
                DestroyGameObject(parent, name);
                m_gGameObject = new GameObject(name);
                m_gGameObject.transform.SetParent(parent, false);
                m_tTracks.m_railingBarrierPosesList.Clear();

                if (m_tTracks == null) { Debug.LogError($"RailingGameObject: Could not find Tracks component on parent '{parent.name}'."); return; }
                if (m_bcBezierCurve == null) { Debug.LogError("RailingGameObject: m_bcBezierCurve is null."); return; }

                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                List<int> railingBarrierTriangles = new List<int>();

                Mesh meshRailing = new Mesh();
                meshRailing.hideFlags = HideFlags.DontSave;
                meshRailing.name = name;

                bool bIsRighthandSide = name.EndsWith("_R");
                GeneratePolesRailing(bIsRighthandSide, vertices, triangles);
                GenerateRailingBarrier(vertices, railingBarrierTriangles);
                meshRailing.vertices = vertices.ToArray();

                meshRailing.subMeshCount = 2;
                meshRailing.SetTriangles(triangles.ToArray(), 0);
                meshRailing.SetTriangles(railingBarrierTriangles.ToArray(), 1);
                meshRailing.RecalculateNormals();
                meshRailing.RecalculateBounds();

                m_mcMeshCollider.sharedMesh = meshRailing;
                m_mfMeshFilter.mesh = meshRailing;

                m_mrMeshRenderer.sharedMaterials = new Material[] { m_tTracks.m_mRailingPolesMAT, m_tTracks.m_mRailingBarrierMAT };
                m_gGameObject.layer = LayerMask.NameToLayer("Track");
            }
            private static void DestroyGameObject(Transform parent, string name)
            {
                Transform railingsTransform = parent.transform.Find(name);
                if (railingsTransform != null)
                {
                    GameObject railingsGameObject = railingsTransform.gameObject;
                    if (railingsGameObject != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(railingsGameObject);
                        }
                        else
                        {
                            DestroyImmediate(railingsGameObject);
                        }
                    }
                }
            }
            private void GeneratePolesRailing(bool isRightHandSide, List<Vector3> vertices, List<int> triangles)
            {
                int iSegmentCount = Mathf.CeilToInt(m_bcBezierCurve.TotalDistance / m_tTracks.m_fTrackSegmentLength);

                Vector3 prevTangent = Vector3.zero;
                float accumulatedAngle = 0f;
                Vector3List currentGroup = null;
                bool inGroup = false;
                bool justClosedGroup = false;

                for (int i = 0; i < iSegmentCount; i++)
                {
                    float prc = i / (float)iSegmentCount;
                    float distance = prc * m_bcBezierCurve.TotalDistance;

                    Pose pose = m_bcBezierCurve.GetPose(distance);
                    ControlPoint ControlPointA, ControlPointB;

                    // determine segment CPs
                    int cpIndex = m_bcBezierCurve.GetControlPointIndexAtDistance(distance);
                    ControlPointA = m_bcBezierCurve.m_points[cpIndex];
                    ControlPointB = (cpIndex + 1 < m_bcBezierCurve.m_points.Count) ? m_bcBezierCurve.m_points[cpIndex + 1] : ControlPointA;

                    Vector3 tangent = pose.forward;

                    bool validSection = (Mathf.Abs(tangent.x) <= m_tTracks.m_vTangentThreshold.x 
                                        && Mathf.Abs(tangent.y) <= m_tTracks.m_vTangentThreshold.y 
                                        && Mathf.Abs(tangent.z) <= m_tTracks.m_vTangentThreshold.z);
                    if (!validSection)
                    {
                        // close group if we were in one
                        if (inGroup && currentGroup.points.Count > 1)
                        {
                            m_tTracks.m_railingBarrierPosesList.Add(currentGroup);
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
                            if (currentGroup.points.Count > 1) m_tTracks.m_railingBarrierPosesList.Add(currentGroup);

                            currentGroup = null;
                            inGroup = false;
                            accumulatedAngle = 0f;
                            justClosedGroup = true;
                            prevTangent = tangent;
                            continue;
                        }

                        if (accumulatedAngle >= m_tTracks.m_fRailingAngleStep)
                        {
                            if (justClosedGroup)
                            {
                                accumulatedAngle = 0f;
                                justClosedGroup = false;
                                prevTangent = tangent;
                                continue; // skip first pole to break continuity
                            }

                            Vector3 rightDir;
                            Vector3 upDir;
                            if (m_tTracks.m_bEnableRotationToRoad)
                            {
                                Quaternion cpRotA = (ControlPointA != null) ? ControlPointA.m_qRotation : Quaternion.identity;
                                Quaternion cpRotB = (ControlPointB != null) ? ControlPointB.m_qRotation : Quaternion.identity;
                                float rotT = 0f;
                                if (ControlPointA != null && ControlPointB != null && !Mathf.Approximately(ControlPointA.m_fDistance, ControlPointB.m_fDistance))
                                {
                                    rotT = Mathf.InverseLerp(ControlPointA.m_fDistance, ControlPointB.m_fDistance, distance);
                                }
                                else if (ControlPointA != null)
                                {
                                    rotT = 0f;
                                    cpRotB = cpRotA;
                                }
                                Quaternion interpolatedCpRot = Quaternion.Slerp(cpRotA, cpRotB, rotT);
                                Quaternion finalRotation = pose.rotation * interpolatedCpRot;

                                // Use finalRotation to derive the frame so outline follows CP rotations
                                rightDir = (finalRotation * Vector3.right).normalized;
                                upDir = (finalRotation * Vector3.up).normalized;
                            }
                            else
                            {
                                rightDir = pose.right;
                                upDir = pose.up;
                            }

                            Vector3 roadSize = m_tTracks.GetRoadSizeBetween(ControlPointA, ControlPointB, distance);
                            float roadOutlineOffset = isRightHandSide ? roadSize.x : -roadSize.x;
                            Vector3 vOffset = roadOutlineOffset * rightDir;

                            Vector3 polePosition = pose.position + vOffset;

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
                    m_tTracks.m_railingBarrierPosesList.Add(currentGroup);
                }
            }
            private void GenerateRailingBarrier(List<Vector3> vertices, List<int> triangles)
            {
                const float EPS = 1e-6f;
                const float MAX_CONNECT_DISTANCE = 50.0f; // threshold to avoid creating giant triangles

                foreach (Vector3List poleGroup in m_tTracks.m_railingBarrierPosesList)
                {
                    if (poleGroup.points.Count < 2) continue;

                    int groupStartVertex = vertices.Count;
                    int groupSectionCount = 0;
                    int prevSectionStart = -1;

                    float startExtension = 0f;
                    if (poleGroup.points.Count >= 2) startExtension = Vector3.Distance(poleGroup.points[0], poleGroup.points[1]) * 0.1f;

                    int lastIndex = poleGroup.points.Count - 1;
                    float endExtension = 0f;
                    if (poleGroup.points.Count >= 2) endExtension = Vector3.Distance(poleGroup.points[lastIndex], poleGroup.points[lastIndex - 1]) * 0.1f;

                    for (int i = 0; i < poleGroup.points.Count; i++)
                    {
                        Vector3 pos = poleGroup.points[i];

                        // direction along the pole chain - try next, fallback to prev, else fallback to forward
                        Vector3 forward = Vector3.zero;
                        if (i < lastIndex) forward = poleGroup.points[i + 1] - pos;
                        else if (i > 0) forward = pos - poleGroup.points[i - 1];

                        if (forward.sqrMagnitude < EPS)
                        {
                            // try alternative deltas
                            if (i > 0 && (pos - poleGroup.points[i - 1]).sqrMagnitude > EPS) forward = pos - poleGroup.points[i - 1];
                            else if (i < lastIndex && (poleGroup.points[i + 1] - pos).sqrMagnitude > EPS) forward = poleGroup.points[i + 1] - pos;
                            else forward = Vector3.forward;
                        }
                        forward = forward.normalized;

                        // stable frame
                        Vector3 right = Vector3.Cross(Vector3.up, forward);
                        if (right.sqrMagnitude < EPS) right = Vector3.right;
                        else right = right.normalized;
                        Vector3 up = Vector3.up;

                        // rail sits on top of poles
                        Vector3 basePos = pos + up * m_tTracks.m_vRailingPoleSize.y;

                        if (i == 0) basePos -= forward * startExtension;
                        else if (i == lastIndex) basePos += forward * endExtension;

                        Vector3 vRight = right * m_tTracks.m_vRailingBarrierSize.x;
                        Vector3 vUp = up * m_tTracks.m_vRailingBarrierSize.y;

                        Vector3[] slices = new Vector3[]
                        {
                            basePos - vRight,
                            basePos - vRight * 0.75f + vUp,
                            basePos + vRight * 0.75f + vUp,
                            basePos + vRight,

                            basePos + vRight * 0.75f - vUp,
                            basePos - vRight * 0.75f - vUp,
                        };

                        // Validate slice vertices for NaN/Infinity
                        bool valid = true;
                        for (int s = 0; s < slices.Length; s++)
                        {
                            Vector3 v = slices[s];
                            if (float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                                float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z))
                            {
                                valid = false;
                                break;
                            }
                        }
                        if (!valid) continue;

                        int currSectionStart = vertices.Count;
                        vertices.AddRange(slices);

                        groupSectionCount++;

                        if (prevSectionStart != -1)
                        {
                            // Compute a representative distance between sections to avoid connecting across large gaps
                            Vector3 prevCenter = Vector3.zero;
                            Vector3 currCenter = Vector3.zero;
                            for (int k = 0; k < 6; k++)
                            {
                                prevCenter += vertices[prevSectionStart + k];
                                currCenter += vertices[currSectionStart + k];
                            }
                            prevCenter /= 6f;
                            currCenter /= 6f;

                            float centerDist = Vector3.Distance(prevCenter, currCenter);
                            if (centerDist > MAX_CONNECT_DISTANCE)
                            {
                                prevSectionStart = currSectionStart;
                                continue;
                            }

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

                    // Add caps only if the computed base indices are still valid
                    AddCap(triangles, groupStartVertex, flip: false, vertices.Count);
                    int endBase = groupStartVertex + (groupSectionCount - 1) * 6;
                    AddCap(triangles, endBase, flip: true, vertices.Count);
                }
            }
            private void AddCylinderPole(List<Vector3> vertices, List<int> triangles, Vector3 position, bool shouldEnableTriangles)
            {
                int startIndex = vertices.Count;
                const int poleSides = 8;

                // bottom & top rings
                for (int i = 0; i <= poleSides; i++)
                {
                    float angle = i * Mathf.PI * 2f / poleSides;
                    float x = Mathf.Cos(angle) * m_tTracks.m_vRailingPoleSize.x;
                    float z = Mathf.Sin(angle) * m_tTracks.m_vRailingPoleSize.x;

                    vertices.Add(position + new Vector3(x, 0f, z));
                    vertices.Add(position + new Vector3(x, m_tTracks.m_vRailingPoleSize.y, z));
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
            private void AddCap(List<int> triangles, int baseIndex, bool flip, int vertexCount)
            {
                // Validate that there are at least 6 vertices from baseIndex
                if (baseIndex < 0 || baseIndex + 5 >= vertexCount) return;

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
        }

        private class CheckPointGameObjectList
        {
            public GameObject m_gGameObject { get; }
            private BezierCurve m_bcBezierCurve => m_gGameObject.GetComponentInParent<BezierCurve>();
            private Tracks m_tTracks => m_gGameObject.GetComponentInParent<Tracks>();
            public CheckPointGameObjectList(Transform parent, string name)
            {
                DestroyGameObject(parent, name);
                m_gGameObject = new GameObject(name);
                m_gGameObject.transform.SetParent(parent, false);

                if (m_tTracks == null) { Debug.LogError($"CheckPointGameObjectList: Could not find Tracks component on parent '{parent.name}'."); return; }
                if (m_bcBezierCurve == null) { Debug.LogError("CheckPointGameObjectList: m_bcBezierCurve is null."); return; }

                m_tTracks.m_lRacingCheckPoints.Clear();
                for (int i = 0; i < m_bcBezierCurve.m_points.Count; ++i)
                {
                    ControlPoint controlPoint = m_bcBezierCurve.m_points[i];
                    m_tTracks.m_lRacingCheckPoints.Add(new CheckPointGameObject(m_gGameObject.transform, "CheckPoint_ref_" + i, controlPoint).m_gGameObject);
                }
                if (m_tTracks.m_gFinishLineGameObject != null) m_tTracks.m_lRacingCheckPoints.Add(m_tTracks.m_gFinishLineGameObject);
            }
            private static void DestroyGameObject(Transform parent, string name)
            {
                Transform checkPointsTransform = parent.Find(name);
                if (checkPointsTransform != null)
                {
                    GameObject checkPointsGameObject = checkPointsTransform.gameObject;
                    if (checkPointsGameObject != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(checkPointsGameObject);
                        }
                        else
                        {
                            DestroyImmediate(checkPointsGameObject);
                        }
                    }
                }
            }
            private class CheckPointGameObject
            {
                public GameObject m_gGameObject { get; }
                private BoxCollider m_bcBoxCollider = null;
                private Tracks m_tTracks => m_gGameObject.transform.parent.GetComponentInParent<Tracks>();

                public CheckPointGameObject(Transform parent, string name, ControlPoint controlPoint)
                {
                    const float multiplyXValue = 20.0f;
                    const float multiplyYValue = 100.0f;

                    m_gGameObject = new GameObject(name);
                    m_gGameObject.transform.SetParent(parent, false);
                    m_bcBoxCollider = m_gGameObject.AddComponent<BoxCollider>();

                    if (m_tTracks == null) { Debug.LogError($"CheckPointGameObject: Could not find Tracks component on parent '{parent.name}'."); return; }

                    m_gGameObject.transform.localPosition = controlPoint.m_vPosition + Vector3.up * (multiplyYValue / 12.0f);
                    Quaternion rotation = Quaternion.LookRotation(controlPoint.m_vTangent.normalized) * Quaternion.Euler(0f, 90f, 0f);
                    if (m_tTracks.m_bEnableRotationToRoad)
                    {
                        Quaternion newConstructedQuat = new Quaternion(-controlPoint.m_qRotation.z, controlPoint.m_qRotation.y, controlPoint.m_qRotation.x, controlPoint.m_qRotation.w);
                        m_gGameObject.transform.localRotation = rotation * newConstructedQuat;
                    }
                    else m_gGameObject.transform.localRotation = rotation;

                    m_bcBoxCollider.size = new Vector3(controlPoint.m_vRoadSize.z * multiplyXValue, controlPoint.m_vRoadSize.y * multiplyYValue, controlPoint.m_vRoadSize.x * 2.0f + m_tTracks.m_vRoadOutlineSize.x * 2.0f);
                    m_bcBoxCollider.isTrigger = true;
                }
            }
        }

        private class EdgeGameObjectList
        {
            public GameObject m_gGameObject { get; }
            private BezierCurve m_bcBezierCurve => m_gGameObject.GetComponentInParent<BezierCurve>();
            private Tracks m_tTracks => m_gGameObject.GetComponentInParent<Tracks>();
            public EdgeGameObjectList(Transform parent, string name)
            {
                DestroyGameObject(parent, name);
                m_gGameObject = new GameObject(name);
                m_gGameObject.transform.SetParent(parent, false);

                if (m_tTracks == null) { Debug.LogError($"EdgeGameObjectList: Could not find Tracks component on parent '{parent.name}'."); return; }
                if (m_bcBezierCurve == null) { Debug.LogError("EdgeGameObjectList: m_bcBezierCurve is null."); return; }

                m_tTracks.m_lEdgeBoxColliders.Clear();
                for (int i = 0; i < m_bcBezierCurve.m_points.Count; ++i)
                {
                    if (!m_bcBezierCurve.m_points[i].m_bIsEdge) continue;
                    ControlPoint controlPointFront = m_bcBezierCurve.m_points[i];
                    ControlPoint controlPointBack = m_bcBezierCurve.m_points[i - 1];
                    m_tTracks.m_lEdgeBoxColliders.Add(new EdgeGameObject(m_gGameObject.transform, "EdgeCollider" + i, controlPointBack, controlPointFront).m_gGameObject);
                }
            }
            private static void DestroyGameObject(Transform parent, string name)
            {
                Transform EdgesTransform = parent.Find(name);
                if (EdgesTransform != null)
                {
                    GameObject EdgesGameObjects = EdgesTransform.gameObject;
                    if (EdgesGameObjects != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(EdgesGameObjects);
                        }
                        else
                        {
                            DestroyImmediate(EdgesGameObjects);
                        }
                    }
                }
            }
            private class EdgeGameObject
            {
                public GameObject m_gGameObject { get; }
                private BoxCollider m_bcBoxCollider = null;
                private Tracks m_tTracks => m_gGameObject.transform.parent.GetComponentInParent<Tracks>();

                public EdgeGameObject(Transform parent, string name, ControlPoint controlPointBack, ControlPoint controlPointFront)
                {
                    const float multiplyXValue = 20.0f;
                    const float multiplyYValue = 100.0f;
                    m_gGameObject = new GameObject(name);
                    m_gGameObject.transform.SetParent(parent, false);
                    m_bcBoxCollider = m_gGameObject.AddComponent<BoxCollider>();

                    if (m_tTracks == null) { Debug.LogError($"EdgeGameObject: Could not find Tracks component on parent '{parent.name}'."); return; }

                    m_gGameObject.transform.localPosition = controlPointFront.m_vPosition + Vector3.up * (multiplyYValue / 20.0f);
                    Quaternion rotation = Quaternion.LookRotation(controlPointBack.m_vTangent.normalized) * Quaternion.Euler(0f, 90f, 0f);
                    if (m_tTracks.m_bEnableRotationToRoad)
                    {
                        Quaternion newConstructedQuat = new Quaternion(-controlPointBack.m_qRotation.z, controlPointBack.m_qRotation.y, controlPointBack.m_qRotation.x, controlPointBack.m_qRotation.w);
                        m_gGameObject.transform.localRotation = rotation * newConstructedQuat;
                    }
                    else m_gGameObject.transform.localRotation = rotation;

                    m_bcBoxCollider.size = new Vector3(controlPointFront.m_vRoadSize.z * multiplyXValue, controlPointFront.m_vRoadSize.y * multiplyYValue, controlPointFront.m_vRoadSize.x * 3.0f);
                    m_bcBoxCollider.isTrigger = true;
                }
            }
        }

        private class FinishLineGameObject
        {
            public GameObject m_gGameObject { get; private set; }
            private BoxCollider m_bcBoxCollider = null;
            private BezierCurve m_bcBezierCurve = null;
            private Tracks m_tTracks = null;

            public FinishLineGameObject(Transform parent, string name, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
            {
                DestroyGameObject(parent, name);
                m_tTracks = parent.GetComponentInParent<Tracks>();
                m_bcBezierCurve = parent.GetComponentInParent<BezierCurve>();

                if (m_tTracks == null) { Debug.LogError($"FinishLineGameObject: Could not find Tracks component on parent '{parent.name}'."); return; }
                if (m_bcBezierCurve == null) { Debug.LogError("FinishLineGameObject: m_bcBezierCurve is null."); return; }
                if (m_tTracks.m_gFinishLinePrefab == null) { Debug.LogError("FinishLineGameObject: Tracks.m_gFinishLinePrefab is null."); return; }

                ControlPoint controlPoint = m_bcBezierCurve.GetControlPointAtDistance(0.0f);
                if (controlPoint == null) { Debug.LogWarning("FinishLineGameObject: Could not obtain control point for finish line (BezierCurve missing or returned null)."); return; }

                m_gGameObject = Instantiate(m_tTracks.m_gFinishLinePrefab);
                m_gGameObject.transform.SetParent(parent, false);
                m_gGameObject.name = name;

                GenerateFinishLine(vertices, uvs, triangles, controlPoint);
                ScaledFinishLinePrefab(controlPoint);
                m_tTracks.m_gFinishLineGameObject = m_gGameObject;
            }
            private static void DestroyGameObject(Transform parent, string name)
            {
                Transform finishLineTransform = parent.Find(name);
                if (finishLineTransform != null)
                {
                    GameObject finishLineGameObject = finishLineTransform.gameObject;
                    if (finishLineGameObject != null)
                    {
                        if (Application.isPlaying)
                        {
                            Object.Destroy(finishLineGameObject);
                        }
                        else
                        {
                            Object.DestroyImmediate(finishLineGameObject);
                        }
                    }
                }
            }
            private void GenerateFinishLine(List<Vector3> vertices, List<Vector2> uvs, List<int> finishLineTriangles, ControlPoint controlPoint)
            {
                float finishDistance = m_tTracks.m_vFinishLineSize.z; // 0–1
                Pose pose = m_bcBezierCurve.GetPose(0.0f);

                float lineDepth = 1.2f;
                float heightOffset = controlPoint.m_vRoadSize.y + 0.001f;
                int segments = 4;
                float segmentWidth = controlPoint.m_vRoadSize.x * 2 / segments;

                Vector3 forward = pose.forward * (lineDepth * 0.5f);
                Vector3 up = Vector3.up * heightOffset; // world up
                float halfRoadWidth = controlPoint.m_vRoadSize.x;

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

                    // Triangles
                    finishLineTriangles.Add(baseIndex + 0);
                    finishLineTriangles.Add(baseIndex + 1);
                    finishLineTriangles.Add(baseIndex + 2);

                    finishLineTriangles.Add(baseIndex + 0);
                    finishLineTriangles.Add(baseIndex + 2);
                    finishLineTriangles.Add(baseIndex + 3);
                }
            }
            private void ScaledFinishLinePrefab(ControlPoint controlPoint)
            {
                const float multiplyXValue = 20.0f;
                const float multiplyYValue = 80.0f;

                m_gGameObject.transform.localPosition = controlPoint.m_vPosition;
                Quaternion rotation = Quaternion.LookRotation(controlPoint.m_vTangent.normalized) * Quaternion.Euler(0f, 90f, 0f);
                if (m_tTracks.m_bEnableRotationToRoad)
                {
                    Quaternion newConstructedQuat = new Quaternion(-controlPoint.m_qRotation.z, controlPoint.m_qRotation.y, controlPoint.m_qRotation.x, controlPoint.m_qRotation.w);
                    m_gGameObject.transform.localRotation = rotation * newConstructedQuat;
                }
                else m_gGameObject.transform.localRotation = rotation;

                GameObject pole_L = m_gGameObject.transform.Find("Pole_L").gameObject;
                Vector3 poleLPos_L = pole_L.transform.localPosition;
                poleLPos_L.z = controlPoint.m_vRoadSize.x;
                pole_L.transform.localPosition = poleLPos_L;

                GameObject pole_R = m_gGameObject.transform.Find("Pole_R").gameObject;
                Vector3 poleLPos_R = pole_R.transform.localPosition;
                poleLPos_R.z = -controlPoint.m_vRoadSize.x;
                pole_R.transform.localPosition = poleLPos_R;

                GameObject banner = m_gGameObject.transform.Find("Banner").gameObject;
                Vector3 bannerScale = banner.transform.localScale;
                bannerScale.z = controlPoint.m_vRoadSize.x * 0.25f;
                banner.transform.localScale = bannerScale;

                m_bcBoxCollider = m_gGameObject.AddComponent<BoxCollider>();
                m_bcBoxCollider.center = new Vector3(0.0f, pole_L.transform.localPosition.y, 0.0f);
                m_bcBoxCollider.size = new Vector3(controlPoint.m_vRoadSize.z * multiplyXValue, controlPoint.m_vRoadSize.y * multiplyYValue, controlPoint.m_vRoadSize.x * 2.0f + m_tTracks.m_vRoadOutlineSize.x * 2.0f);
                m_bcBoxCollider.isTrigger = true;
            }
        }
        #endregion
    }
}

