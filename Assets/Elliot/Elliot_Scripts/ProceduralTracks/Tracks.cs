using Bezier;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;

namespace ProceduralTracks
{
    [RequireComponent(typeof(BezierCurve))]
    public class Tracks : ProceduralMesh
    {
        [SerializeField, Range(0.25f, 5.0f)] private float m_fTrackSegmentLength = 4.0f;

        [SerializeField] private Vector3 m_vRoadSize = new Vector3(1.0f, 0.2f, 0.3f);

        [SerializeField] private Vector3 m_vRoadOutlineSize = new Vector3(1.0f, 0.2f, 0.3f);


        #region Properties

        public Vector3 SleeperSize => m_vRoadSize;

        #endregion

        protected override Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.hideFlags = HideFlags.DontSave;
            mesh.name = "Tracks";

            List<Vector3> vertices = new List<Vector3>();
            List<int> sleeperTriangles = new List<int>();
            List<int> trackTriangles = new List<int>();

            // Generate track!
            AddRoadSegment(vertices, sleeperTriangles);
            GenerateTrackOutline(m_vRoadSize.x, vertices, trackTriangles);
            GenerateTrackOutline(-m_vRoadSize.x, vertices, trackTriangles);

            // assign the mesh data
            mesh.vertices = vertices.ToArray();

            mesh.subMeshCount = 2;
            mesh.SetTriangles(sleeperTriangles.ToArray(), 0);
            mesh.SetTriangles(trackTriangles.ToArray(), 1);

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

            for (int i = 0; i <= iSegmentCount; ++i)
            {
                float fPrc = i / (float)iSegmentCount;
                Pose pose = bc.GetPose(fPrc * bc.TotalDistance);

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


                // add triangles
                if (i > 0)
                {
                    int baseIndex = i * vertsPerSlice;
                    int prevBase = baseIndex - vertsPerSlice;

                    AddQuad(triangles, prevBase + 0, prevBase + 1, baseIndex + 1, baseIndex + 0); // top
                    AddQuad(triangles, prevBase + 2, prevBase + 3, baseIndex + 3, baseIndex + 2); // bottom
                    //AddQuad(triangles, prevBase + 1, prevBase + 2, baseIndex + 2, baseIndex + 1); // left
                    //AddQuad(triangles, prevBase + 0, prevBase + 3, baseIndex + 3, baseIndex + 0); // right
                }
            }
        }

        protected void GenerateTrackOutline(float roadOutlineOffset, List<Vector3> vertices, List<int> triangles)
        {
            BezierCurve bc = GetComponent<BezierCurve>();

            int iStart = vertices.Count;
            int iSegmentCount = Mathf.CeilToInt(bc.TotalDistance / m_fTrackSegmentLength);

            for (int i = 0; i <= iSegmentCount; ++i)
            {
                float fPrc = i / (float)iSegmentCount;
                Pose pose = bc.GetPose(fPrc * bc.TotalDistance);

                Vector3 vRight = pose.right * m_vRoadOutlineSize.x;
                Vector3 vUp = pose.up * m_vRoadOutlineSize.y;
                Vector3 vOffset = roadOutlineOffset * pose.right;


                vertices.AddRange(new Vector3[]
                {
                    pose.position + vOffset - vRight,           
                    pose.position + vOffset - vRight * 0.75f + vUp,   
                    pose.position + vOffset + vRight * 0.75f + vUp,   
                    pose.position + vOffset + vRight,
                });

                // add triangles
                if (i < iSegmentCount)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        int iCurr = iStart + i * 4 + j;

                        triangles.AddRange(new int[]
                        {
                            iCurr + 0, iCurr + 4, iCurr + 1,
                            iCurr + 1, iCurr + 4, iCurr + 5
                        });
                    }
                }
            }
        }

        void AddQuad(List<int> tris, int a, int b, int c, int d)
        {
            tris.Add(a);
            tris.Add(b);
            tris.Add(c);

            tris.Add(a);
            tris.Add(c);
            tris.Add(d);
        }
    }
}
