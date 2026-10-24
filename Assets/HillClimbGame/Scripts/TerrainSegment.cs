using UnityEngine;

namespace HillClimbGame
{
    /// <summary>
    /// Sits on each pre-created terrain segment GameObject.
    /// Holds MeshFilter and PolygonCollider2D references.
    /// TerrainController calls SetMesh/SetCollider to update geometry without creating new GameObjects.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(PolygonCollider2D))]
    public class TerrainSegment : MonoBehaviour
    {
        private MeshFilter      _filter;
        private PolygonCollider2D _collider;
        private Mesh            _mesh;

        void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_filter == null) _filter = GetComponent<MeshFilter>();
            if (_collider == null) _collider = GetComponent<PolygonCollider2D>();
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "TerrainSegmentMesh" };
                if (_filter != null) _filter.sharedMesh = _mesh;
            }
        }

        public void SetMesh(Vector3[] vertices, int[] triangles, Vector2[] uvs)
        {
            EnsureInitialized();
            _mesh.Clear();
            _mesh.vertices  = vertices;
            _mesh.triangles = triangles;
            _mesh.uv        = uvs;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            if (_filter != null) _filter.sharedMesh = _mesh;
        }

        public void SetCollider(Vector2[] path)
        {
            EnsureInitialized();
            if (_collider == null) return;
            _collider.SetPath(0, path);
        }
    }
}
