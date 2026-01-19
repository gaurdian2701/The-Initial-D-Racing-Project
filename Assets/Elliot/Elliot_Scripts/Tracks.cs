using Bezier;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;

namespace ProceduralTracks
{
    [RequireComponent(typeof(BezierCurve))]
    public class Tracks : ProceduralMesh
    {
        [SerializeField, Range(0.1f, 3.0f)]
        private float m_fSleeperSpacing = 0.5f;

        [SerializeField, Range(0.25f, 5.0f)]
        private float m_fTrackSegmentLength = 4.0f;

        [SerializeField, Range(0.05f, 1.0f)]
        private float m_fTrackSize = 0.4f;

        [SerializeField, Range(0.05f, 1.0f)]
        private float m_fTrackOffset = 0.4f;

        [SerializeField]
        private Vector3 m_vSleeperSize = new Vector3(1.0f, 0.2f, 0.3f);

        #region Properties

        public Vector3 SleeperSize => m_vSleeperSize;

        #endregion

        protected override Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.hideFlags = HideFlags.DontSave;
            mesh.name = "Tracks";

            List<Vector3> vertices = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> sleeperTriangles = new List<int>();
            List<int> trackTriangles = new List<int>();

            // Generate track!
            GenerateSleepers(vertices, colors, sleeperTriangles);
            GenerateTrack(m_fTrackOffset, vertices, colors, trackTriangles);
            GenerateTrack(-m_fTrackOffset, vertices, colors, trackTriangles);

            // assign the mesh data
            mesh.vertices = vertices.ToArray();
            mesh.colors = colors.ToArray();

            mesh.subMeshCount = 2;
            mesh.SetTriangles(sleeperTriangles.ToArray(), 0);
            mesh.SetTriangles(trackTriangles.ToArray(), 1);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        protected void GenerateSleepers(List<Vector3> vertices, List<Color> colors, List<int> triangles)
        {
            // grab the bezier curve
            BezierCurve bc = GetComponent<BezierCurve>();
            for (float f = 0.0f; f < bc.TotalDistance; f += m_fSleeperSpacing)
            {
                Pose pose = bc.GetPose(f);
                AddSleeper(pose, vertices, colors, triangles);
            }
        }

        protected void AddSleeper(Pose pose, List<Vector3> vertices, List<Color> colors, List<int> triangles)
        {
            int iStart = vertices.Count;

            // random rotation?
            Quaternion qRandom = Quaternion.Euler(0.0f, Random.Range(-10.0f, 10.0f), 0.0f);
            pose.rotation *= qRandom;

            Vector3 vRight = pose.right * m_vSleeperSize.x * 0.5f * Random.Range(0.8f, 1.2f);
            Vector3 vUp = pose.up * m_vSleeperSize.y;
            Vector3 vForward = pose.forward * m_vSleeperSize.z * 0.5f * Random.Range(0.8f, 1.2f);

            // calculate sleeper vertices
            vertices.AddRange(new Vector3[]
            {
                pose.position + vRight - vForward - vUp,
                pose.position + -vRight - vForward - vUp,
                pose.position + -vRight + vForward - vUp,
                pose.position + vRight + vForward - vUp,
                pose.position + vRight - vForward,
                pose.position + -vRight - vForward,
                pose.position + -vRight + vForward,
                pose.position + vRight + vForward,
            });

            // calculate a random brown color
            Color brown = new Color(Random.Range(0.3f, 0.6f),
                                    Random.Range(0.1f, 0.3f),
                                    0.0f);

            // add colors
            colors.AddRange(new Color[]
            {
                brown * 0.7f, brown * 0.7f, brown * 0.7f, brown * 0.7f,
                brown, brown, brown, brown
            });

            // add triangles
            triangles.AddRange(new int[]
            {
                iStart + 0, iStart + 4, iStart + 3, iStart + 3, iStart + 4, iStart + 7,
                iStart + 1, iStart + 2, iStart + 5, iStart + 2, iStart + 6, iStart + 5,
                iStart + 4, iStart + 5, iStart + 6, iStart + 6, iStart + 7, iStart + 4,
                iStart + 1, iStart + 4, iStart + 0, iStart + 1, iStart + 5, iStart + 4,
                iStart + 3, iStart + 7, iStart + 6, iStart + 3, iStart + 6, iStart + 2,
            });
        }

        protected void GenerateTrack(float fOffset, List<Vector3> vertices, List<Color> colors, List<int> triangles)
        {
            BezierCurve bc = GetComponent<BezierCurve>();

            int iStart = vertices.Count;
            int iSegmentCount = Mathf.CeilToInt(bc.TotalDistance / m_fTrackSegmentLength);

            for (int i = 0; i <= iSegmentCount; ++i)
            {
                float fPrc = i / (float)iSegmentCount;
                Pose pose = bc.GetPose(fPrc * bc.TotalDistance);

                Vector3 vOffset = fOffset * pose.right;

                vertices.AddRange(new Vector3[]
                {
                    pose.position + vOffset - pose.right * m_fTrackSize,
                    pose.position + vOffset - pose.right * 0.75f * m_fTrackSize + pose.up * m_fTrackSize,
                    pose.position + vOffset + pose.right * 0.75f * m_fTrackSize + pose.up * m_fTrackSize,
                    pose.position + vOffset + pose.right * m_fTrackSize,
                });

                float fGray = Random.Range(0.4f, 0.6f);
                Color gray = new Color(fGray, fGray, fGray);
                colors.AddRange(new Color[] { gray, gray, gray, gray });

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
    }
}
