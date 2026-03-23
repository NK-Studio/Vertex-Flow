using UnityEngine;

namespace VertexFlow.Core
{
    [RequireComponent(typeof(MeshFilter))]
    public class VertexPaintedMesh : MonoBehaviour
    {
        [Tooltip("원본 메시 (참고용)")] public Mesh OriginalMesh;

        [Tooltip("페인팅을 위해 복제된 에셋 메시")] public Mesh PaintedMesh;
    }
}