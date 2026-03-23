using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace VertexFlow.Core
{
    public class VertexPainterCore
    {
        public Color PaintColor = Color.red;
        public float BrushSize = 0.5f;
        public float BrushStrength = 1.0f;
        public BrushMode CurrentMode = BrushMode.Paint;

        public GameObject SelectedObject;
        public MeshFilter MeshFilter;
        public Mesh Mesh;
        public Color[] VertexColors;

        private Stack<Color[]> _undoStack = new();
        private Stack<Color[]> _redoStack = new();
        private const int _maxUndoSteps = 50;

        public int GetUndoCount() => Mathf.Max(0, _undoStack.Count - 1);
        public int GetRedoCount() => _redoStack.Count;

        public bool NeedsCloning()
        {
            if (SelectedObject == null)
                return false;

            var filter = SelectedObject.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                return false;

            var paintedComp = SelectedObject.GetComponent<VertexPaintedMesh>();

            // If the component exists and has a painted mesh assigned, we don't need to clone again.
            if (paintedComp != null && paintedComp.PaintedMesh != null)
            {
                return false;
            }
            
            return true;
        }

        public void CloneAndSaveMesh(string savePath)
        {
            if (SelectedObject == null) return;
            var filter = SelectedObject.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;

            Mesh original = filter.sharedMesh;
            Mesh clonedMesh = Object.Instantiate(original);
            clonedMesh.name = original.name + "_Painted";

            Color[] colors = clonedMesh.colors;
            if (colors == null || colors.Length != clonedMesh.vertexCount)
            {
                colors = new Color[clonedMesh.vertexCount];
                for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
                clonedMesh.colors = colors;
            }

            AssetDatabase.CreateAsset(clonedMesh, savePath);
            AssetDatabase.SaveAssets();

            var paintedComp = SelectedObject.GetComponent<VertexPaintedMesh>();
            if (paintedComp == null)
            {
                paintedComp = SelectedObject.AddComponent<VertexPaintedMesh>();
            }

            if (paintedComp.OriginalMesh == null)
            {
                paintedComp.OriginalMesh = original;
            }

            paintedComp.PaintedMesh = clonedMesh;

            filter.sharedMesh = clonedMesh;

            OnSelectionChanged();
        }

        public void OnSelectionChanged()
        {
            SelectedObject = Selection.activeGameObject;
            if (SelectedObject != null)
            {
                MeshFilter = SelectedObject.GetComponent<MeshFilter>();
                if (MeshFilter != null && MeshFilter.sharedMesh != null)
                {
                    if (!NeedsCloning())
                    {
                        var paintedComp = SelectedObject.GetComponent<VertexPaintedMesh>();
                        if (paintedComp != null && paintedComp.PaintedMesh != null && MeshFilter.sharedMesh != paintedComp.PaintedMesh)
                        {
                            // Ensure the MeshFilter is actually using the PaintedMesh if it has one
                            MeshFilter.sharedMesh = paintedComp.PaintedMesh;
                        }

                        Mesh = MeshFilter.sharedMesh;
                        VertexColors = Mesh.colors;
                        if (VertexColors == null || VertexColors.Length != Mesh.vertexCount)
                        {
                            VertexColors = new Color[Mesh.vertexCount];
                            for (int i = 0; i < VertexColors.Length; i++) VertexColors[i] = Color.white;
                            Mesh.colors = VertexColors;
                        }

                        SaveUndoState();
                    }
                    else
                    {
                        ClearMesh();
                    }
                }
                else
                {
                    ClearMesh();
                }
            }
            else
            {
                ClearMesh();
            }
        }

        private void ClearMesh()
        {
            Mesh = null;
            VertexColors = null;
            _undoStack.Clear();
            _redoStack.Clear();
        }

        public void SaveUndoState()
        {
            if (VertexColors == null || VertexColors.Length == 0) return;
            Color[] snapshot = new Color[VertexColors.Length];
            System.Array.Copy(VertexColors, snapshot, VertexColors.Length);
            _undoStack.Push(snapshot);

            if (_undoStack.Count > _maxUndoSteps)
            {
                var tempList = new List<Color[]>(_undoStack);
                tempList.RemoveAt(tempList.Count - 1);
                _undoStack = new Stack<Color[]>(tempList);
            }

            _redoStack.Clear();
        }

        public void SaveUndoStateWithoutClearingRedo()
        {
            if (VertexColors == null || VertexColors.Length == 0) return;
            Color[] snapshot = new Color[VertexColors.Length];
            System.Array.Copy(VertexColors, snapshot, VertexColors.Length);
            _undoStack.Push(snapshot);
            if (_undoStack.Count > _maxUndoSteps)
            {
                var tempList = new List<Color[]>(_undoStack);
                tempList.RemoveAt(tempList.Count - 1);
                _undoStack = new Stack<Color[]>(tempList);
            }
        }

        public void PerformUndo()
        {
            if (_undoStack.Count <= 1 || Mesh == null) return;
            Color[] currentState = new Color[VertexColors.Length];
            System.Array.Copy(VertexColors, currentState, VertexColors.Length);
            _redoStack.Push(currentState);

            Color[] previousState = _undoStack.Pop();
            System.Array.Copy(previousState, VertexColors, VertexColors.Length);
            Mesh.colors = VertexColors;

            EditorUtility.SetDirty(Mesh);
            if (MeshFilter != null) EditorUtility.SetDirty(MeshFilter);
            SceneView.RepaintAll();
        }

        public void PerformRedo()
        {
            if (_redoStack.Count == 0 || Mesh == null) return;
            Color[] nextState = _redoStack.Pop();
            SaveUndoStateWithoutClearingRedo();

            System.Array.Copy(nextState, VertexColors, VertexColors.Length);
            Mesh.colors = VertexColors;

            EditorUtility.SetDirty(Mesh);
            if (MeshFilter != null) EditorUtility.SetDirty(MeshFilter);
            SceneView.RepaintAll();
        }

        public bool RaycastMesh(Ray ray, out Vector3 hitPoint, out Vector3 hitNormal)
        {
            hitPoint = Vector3.zero;
            hitNormal = Vector3.up;
            if (Mesh == null || SelectedObject == null) return false;

            Transform transform = SelectedObject.transform;
            Vector3[] vertices = Mesh.vertices;
            int[] triangles = Mesh.triangles;

            float closestDistance = float.MaxValue;
            bool foundHit = false;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = transform.TransformPoint(vertices[triangles[i]]);
                Vector3 v1 = transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 v2 = transform.TransformPoint(vertices[triangles[i + 2]]);

                if (RayIntersectsTriangle(ray, v0, v1, v2, out Vector3 intersection))
                {
                    float distance = Vector3.Distance(ray.origin, intersection);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        hitPoint = intersection;
                        hitNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                        foundHit = true;
                    }
                }
            }

            return foundHit;
        }

        private bool RayIntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 intersection)
        {
            intersection = Vector3.zero;
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 h = Vector3.Cross(ray.direction, edge2);
            float a = Vector3.Dot(edge1, h);

            if (a > -0.00001f && a < 0.00001f) return false;

            float f = 1.0f / a;
            Vector3 s = ray.origin - v0;
            float u = f * Vector3.Dot(s, h);
            if (u < 0.0f || u > 1.0f) return false;

            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(ray.direction, q);
            if (v < 0.0f || u + v > 1.0f) return false;

            float t = f * Vector3.Dot(edge2, q);
            if (t > 0.00001f)
            {
                intersection = ray.origin + ray.direction * t;
                return true;
            }

            return false;
        }

        public void PaintAtPosition(Vector3 worldHitPoint)
        {
            if (Mesh == null || VertexColors == null || SelectedObject == null) return;

            Transform transform = SelectedObject.transform;
            Vector3 localHitPoint = transform.InverseTransformPoint(worldHitPoint);
            Vector3[] vertices = Mesh.vertices;
            bool modified = false;

            for (int i = 0; i < vertices.Length; i++)
            {
                float distance = Vector3.Distance(vertices[i], localHitPoint);
                if (distance < BrushSize)
                {
                    float falloff = 1.0f - (distance / BrushSize);
                    falloff = Mathf.Pow(falloff, 2);
                    float strength = falloff * BrushStrength;

                    switch (CurrentMode)
                    {
                        case BrushMode.Paint:
                            VertexColors[i] = Color.Lerp(VertexColors[i], PaintColor, strength);
                            break;
                        case BrushMode.Erase:
                            VertexColors[i] = Color.Lerp(VertexColors[i], Color.white, strength);
                            break;
                        case BrushMode.Smooth:
                            Color avgColor = GetAverageColorAround(i, vertices);
                            VertexColors[i] = Color.Lerp(VertexColors[i], avgColor, strength * 0.5f);
                            break;
                    }

                    modified = true;
                }
            }

            if (modified) Mesh.colors = VertexColors;
        }

        private Color GetAverageColorAround(int vertexIndex, Vector3[] vertices)
        {
            Color avgColor = VertexColors[vertexIndex];
            int count = 1;
            float radius = BrushSize * 0.3f;

            for (int i = 0; i < vertices.Length; i++)
            {
                if (i != vertexIndex)
                {
                    float distance = Vector3.Distance(vertices[vertexIndex], vertices[i]);
                    if (distance < radius)
                    {
                        avgColor += VertexColors[i];
                        count++;
                    }
                }
            }

            return avgColor / count;
        }

        public void FillAllVertices(Color color)
        {
            if (Mesh == null || VertexColors == null) return;
            SaveUndoState();
            for (int i = 0; i < VertexColors.Length; i++) VertexColors[i] = color;
            Mesh.colors = VertexColors;
            EditorUtility.SetDirty(Mesh);
            if (MeshFilter != null) EditorUtility.SetDirty(MeshFilter);
            SceneView.RepaintAll();
        }
    }
}