using Bezier;
using ExternalForInspector;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Bezier.BezierCurve;
using static UnityEditor.FilePathAttribute;
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
        [SerializeField] private bool m_bEnableRotationToRoad = false;

        [Header("Railing Settings")]
        [SerializeField] private bool m_bEnableRailing = true;
        [SerializeField, ShowIf("m_bEnableRailing")] private Vector2 m_vRailingPoleSize = new Vector2(0.05f, 1.0f);
        [SerializeField, ShowIf("m_bEnableRailing")] private Vector2 m_vRailingBarrierSize = new Vector2(0.1f, 0.15f);
        [SerializeField, ShowIf("m_bEnableRailing")] private float m_fRailingAngleStep = 5f;
        [SerializeField, ShowIf("m_bEnableRailing")] private Vector3 tangentThreshold = new Vector3(5.0f, 0.01f, 5.0f);
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


        #endregion

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
            DestroyAllEdgeBoxColliders();
            DestroyRaceCheckPoints();
            ClearRailingObjects();
            m_railingBarrierPosesList.Clear();
            AddRoadSegment(vertices, uvs, trackTriangles);
            GenerateTrackOutline(true, vertices, uvs, outlineTrackTriangles);
            GenerateTrackOutline(false, vertices, uvs, outlineTrackTriangles);
            if(m_bEnableRailing) CreateRailingMesh();
            GenerateFinishLine(vertices, uvs, FinishLineTriangles);
            GenerateEdgeBoxColliders();
            GenerateRaceCheckPoints();

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
            return mesh;
        }

        void ClearRailingObjects()
        {
            Transform RailingTransform_R = transform.Find("Railing_R");
            if (RailingTransform_R != null)
            {
                GameObject railing = RailingTransform_R.gameObject;
                if (railing != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(railing);
                    }
                    else
                    {
                        DestroyImmediate(railing);
                    }
                }
            }

            Transform RailingTransform_L = transform.Find("Railing_L");
            if (RailingTransform_L != null)
            {
                GameObject railing = RailingTransform_L.gameObject;
                if (railing != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(railing);
                    }
                    else
                    {
                        DestroyImmediate(railing);
                    }
                }
            }
        }

        protected void CreateRailingMesh()
        {
            #region RightRailing
            GameObject railing_R = new GameObject("Railing_R");
            railing_R.transform.SetParent(transform, false);
            railing_R.AddComponent<MeshFilter>();
            railing_R.AddComponent<MeshRenderer>();
            railing_R.AddComponent<MeshCollider>();

            List<Vector3> verticesRailing_R = new List<Vector3>();
            List<int> railingTriangles_R = new List<int>();
            List<int> railingBarrierTriangles_R = new List<int>();

            Mesh meshRailing_R = new Mesh();
            meshRailing_R.hideFlags = HideFlags.DontSave;
            meshRailing_R.name = "Railing_R";

            GeneratePolesRailing(true, verticesRailing_R, railingTriangles_R);
            GenerateRailingBarrier(verticesRailing_R, railingBarrierTriangles_R);
            meshRailing_R.vertices = verticesRailing_R.ToArray();

            meshRailing_R.subMeshCount = 2;
            meshRailing_R.SetTriangles(railingTriangles_R.ToArray(), 0);
            meshRailing_R.SetTriangles(railingBarrierTriangles_R.ToArray(), 1);
            meshRailing_R.RecalculateNormals();
            meshRailing_R.RecalculateBounds();

            railing_R.GetComponent<MeshFilter>().mesh = meshRailing_R;
            if (railing_R.GetComponent<MeshCollider>() != null)
            {
                railing_R.GetComponent<MeshCollider>().sharedMesh = meshRailing_R;
            }

            // Use sharedMaterials to avoid material instantiation in edit mode.
            var mrR = railing_R.GetComponent<MeshRenderer>();
            mrR.sharedMaterials = new Material[] { m_mRailingPolesMAT, m_mRailingBarrierMAT };

            m_gRailing_R = railing_R;

            #endregion

            m_railingBarrierPosesList.Clear();

            #region LeftRailing
            GameObject railing_L = new GameObject("Railing_L");
            railing_L.transform.SetParent(transform, false);
            railing_L.AddComponent<MeshFilter>();
            railing_L.AddComponent<MeshRenderer>();
            railing_L.AddComponent<MeshCollider>();

            List<Vector3> verticesRailing_L = new List<Vector3>();
            List<int> railingTriangles_L = new List<int>();
            List<int> railingBarrierTriangles_L = new List<int>();

            Mesh meshRailing_L = new Mesh();
            meshRailing_L.hideFlags = HideFlags.DontSave;
            meshRailing_L.name = "Railing_L";

            GeneratePolesRailing(false, verticesRailing_L, railingTriangles_L);
            GenerateRailingBarrier(verticesRailing_L, railingBarrierTriangles_L);
            meshRailing_L.vertices = verticesRailing_L.ToArray();

            meshRailing_L.subMeshCount = 2;
            meshRailing_L.SetTriangles(railingTriangles_L.ToArray(), 0);
            meshRailing_L.SetTriangles(railingBarrierTriangles_L.ToArray(), 1);
            meshRailing_L.RecalculateNormals();
            meshRailing_L.RecalculateBounds();

            railing_L.GetComponent<MeshFilter>().mesh = meshRailing_L;
            if (railing_L.GetComponent<MeshCollider>() != null)
            {
                railing_L.GetComponent<MeshCollider>().sharedMesh = meshRailing_L;
            }

            // Use sharedMaterials to avoid material instantiation in edit mode.
            var mrL = railing_L.GetComponent<MeshRenderer>();
            mrL.sharedMaterials = new Material[] { m_mRailingPolesMAT, m_mRailingBarrierMAT };

            m_gRailing_L = railing_L;
            #endregion
        }

        protected void AddRoadSegment(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
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

                if(m_bEnableRotationToRoad) // derive world-space axes from final rotation and scale by road sizes
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

        protected void GeneratePolesRailing(bool isRightHandSide, List<Vector3> vertices, List<int> triangles)
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

                            // Use finalRotation to derive the frame so outline follows CP rotations
                            rightDir = (finalRotation * Vector3.right).normalized;
                            upDir = (finalRotation * Vector3.up).normalized;
                        }
                        else
                        {
                            rightDir = pose.right;
                            upDir = pose.up;
                        }

                        Vector3 roadSize = GetRoadSizeBetween(ControlPointA, ControlPointB, distance);
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

        protected void GenerateRailingBarrier(List<Vector3> vertices, List<int> triangles)
        {
            const float EPS = 1e-6f;
            const float MAX_CONNECT_DISTANCE = 50.0f; // threshold to avoid creating giant triangles

            foreach (Vector3List poleGroup in m_railingBarrierPosesList)
            {
                if (poleGroup.points.Count < 2) continue;

                int groupStartVertex = vertices.Count;
                int groupSectionCount = 0;
                int prevSectionStart = -1;

                float startExtension = 0f;
                if (poleGroup.points.Count >= 2)
                    startExtension = Vector3.Distance(poleGroup.points[0], poleGroup.points[1]) * 0.1f;

                int lastIndex = poleGroup.points.Count - 1;
                float endExtension = 0f;
                if (poleGroup.points.Count >= 2)
                    endExtension = Vector3.Distance(poleGroup.points[lastIndex], poleGroup.points[lastIndex - 1]) * 0.1f;

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
                            // Skip connecting these sections (break continuity)
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

        protected void AddCap(List<int> triangles, int baseIndex, bool flip, int vertexCount)
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

            Transform parentTransform = transform.Find("FinishLine");
            if (parentTransform != null)
            {
                GameObject finishLine = parentTransform.gameObject;
                if (finishLine != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(finishLine);
                    }
                    else
                    {
                        DestroyImmediate(finishLine);
                    }
                }
            }

            GameObject finishLinePrefab = Instantiate(m_gFinishLinePrefab);
            finishLinePrefab.name = "FinishLine";

            finishLinePrefab.transform.SetParent(transform, false);

            finishLinePrefab.transform.localPosition = cp.m_vPosition;
            Quaternion rotation = Quaternion.LookRotation(cp.m_vTangent.normalized) * Quaternion.Euler(0f, 90f, 0f);
            if (m_bEnableRotationToRoad)
            {
                Quaternion newConstructedQuat = new Quaternion(-cp.m_qRotation.z, cp.m_qRotation.y, cp.m_qRotation.x, cp.m_qRotation.w);
                finishLinePrefab.transform.localRotation = rotation * newConstructedQuat;
            }
            else finishLinePrefab.transform.localRotation = rotation;

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
            newBoxCollider.size = new Vector3(cp.m_vRoadSize.z * multiplyXValue, cp.m_vRoadSize.y * multiplyYValue, cp.m_vRoadSize.x * 2.0f + m_vRoadOutlineSize.x * 2.0f);
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

        private void DestroyRaceCheckPoints()
        {
            // clean up colliders
            foreach (GameObject boxCollider in m_lRacingCheckPoints)
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
            m_lRacingCheckPoints.Clear();

            Transform parentTransform = transform.Find("CheckPoints");
            if (parentTransform == null) return;

            GameObject parent = parentTransform.gameObject;
            if (parent == null) return;
            if (Application.isPlaying)
            {
                Destroy(parent);
            }
            else
            {
                DestroyImmediate(parent);
            }
        }

        protected void GenerateRaceCheckPoints()
        {
            const float multiplyXValue = 20.0f;
            const float multiplyYValue = 100.0f;  
            GameObject CheckPointsParent = new GameObject("CheckPoints");
            CheckPointsParent.transform.SetParent(transform, false);

            BezierCurve bc = GetComponent<BezierCurve>();
            for (int i = 2; i <= bc.m_points.Count; ++i)
            {
                ControlPoint checkPointCP = bc.m_points[i - 1];
                GameObject colliderGO = new GameObject("CheckPoint_ref_" + i);
                colliderGO.transform.SetParent(CheckPointsParent.transform, false);

                colliderGO.transform.localPosition = checkPointCP.m_vPosition + Vector3.up * (multiplyYValue / 12.0f);
                Quaternion rotation = Quaternion.LookRotation(checkPointCP.m_vTangent.normalized) * Quaternion.Euler(0f, 90f, 0f);
                if (m_bEnableRotationToRoad)
                {
                    Quaternion newConstructedQuat = new Quaternion(-checkPointCP.m_qRotation.z, checkPointCP.m_qRotation.y, checkPointCP.m_qRotation.x, checkPointCP.m_qRotation.w);
                    colliderGO.transform.localRotation = rotation * newConstructedQuat;
                }
                else colliderGO.transform.localRotation = rotation;


                BoxCollider newBoxCollider = colliderGO.AddComponent<BoxCollider>();
                newBoxCollider.size = new Vector3(checkPointCP.m_vRoadSize.z * multiplyXValue, checkPointCP.m_vRoadSize.y * multiplyYValue, checkPointCP.m_vRoadSize.x * 2.0f + m_vRoadOutlineSize.x * 2.0f);
                newBoxCollider.isTrigger = true;
                m_lRacingCheckPoints.Add(colliderGO);
            }
            if(m_gFinishLineGameObject != null) m_lRacingCheckPoints.Add(m_gFinishLineGameObject);
        }
    }
}
