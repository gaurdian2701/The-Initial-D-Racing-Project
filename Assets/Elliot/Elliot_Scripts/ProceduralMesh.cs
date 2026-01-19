using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ProceduralTracks
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [ExecuteInEditMode]
    public abstract class ProceduralMesh : MonoBehaviour
    {
        private Mesh m_mesh;

        #region Properties

        public Mesh Mesh => m_mesh;

        #endregion

        protected virtual void Start()
        {
            UpdateMesh();
        }

        protected virtual void OnDestroy()
        {
            Cleanup();
        }

        void Cleanup()
        {
            // cleanup after ourselves
            if (m_mesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_mesh);
                }
                else
                {
                    DestroyImmediate(m_mesh);
                }

                m_mesh = null;
            }
        }

        protected abstract Mesh CreateMesh();

        public virtual void UpdateMesh()
        {
            m_mesh = CreateMesh();
            GetComponent<MeshFilter>().mesh = m_mesh;
        }
    }
}
