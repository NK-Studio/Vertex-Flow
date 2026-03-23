using UnityEngine;
using UnityEditor;
using VertexFlow.Core;

namespace VertexFlow.Scene
{
    public class VertexPainterSceneView
    {
        private readonly VertexPainterCore _core;
        private readonly System.Action _onPaintStateChanged;
        private readonly System.Action _onRepaintRequested;

        private bool _hasRecordedUndo;

        public bool IsEnabled { get; set; }
        public bool IsPainting { get; private set; }

        public VertexPainterSceneView(VertexPainterCore core, System.Action onPaintStateChanged,
            System.Action onRepaintRequested)
        {
            _core = core;
            _onPaintStateChanged = onPaintStateChanged;
            _onRepaintRequested = onRepaintRequested;
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            if (!IsEnabled || _core.Mesh == null || _core.VertexColors == null || _core.SelectedObject == null) return;

            Event e = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (e.type == EventType.KeyDown)
            {
                if ((e.control || e.command) && e.keyCode == KeyCode.Z && !e.shift)
                {
                    _core.PerformUndo();
                    e.Use();
                    return;
                }

                if (((e.control || e.command) && e.keyCode == KeyCode.Y) ||
                    ((e.control || e.command) && e.shift && e.keyCode == KeyCode.Z))
                {
                    _core.PerformRedo();
                    e.Use();
                    return;
                }

                if (e.keyCode == KeyCode.LeftBracket)
                {
                    _core.BrushSize = Mathf.Max(0.01f, _core.BrushSize - 0.1f);
                    _onRepaintRequested?.Invoke();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.RightBracket)
                {
                    _core.BrushSize = Mathf.Min(5.0f, _core.BrushSize + 0.1f);
                    _onRepaintRequested?.Invoke();
                    e.Use();
                }
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            bool hasValidHit = _core.RaycastMesh(ray, out Vector3 hitPoint, out Vector3 hitNormal);

            if (hasValidHit)
            {
                Handles.color = new Color(_core.PaintColor.r, _core.PaintColor.g, _core.PaintColor.b, 0.2f);
                Handles.DrawSolidDisc(hitPoint, hitNormal, _core.BrushSize);
                Handles.color = new Color(_core.PaintColor.r, _core.PaintColor.g, _core.PaintColor.b, 0.8f);
                Handles.DrawWireDisc(hitPoint, hitNormal, _core.BrushSize);

                Vector3 right = Vector3.Cross(hitNormal, Vector3.up).normalized;
                if (right.magnitude < 0.1f) right = Vector3.Cross(hitNormal, Vector3.forward).normalized;
                Vector3 forward = Vector3.Cross(hitNormal, right).normalized;

                Handles.color = Color.white;
                Handles.DrawLine(hitPoint + right * _core.BrushSize * 0.1f, hitPoint - right * _core.BrushSize * 0.1f);
                Handles.DrawLine(hitPoint + forward * _core.BrushSize * 0.1f,
                    hitPoint - forward * _core.BrushSize * 0.1f);
            }

            switch (e.GetTypeForControl(controlID))
            {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(controlID);
                    break;
                case EventType.MouseDown:
                    if (e.button == 0 && hasValidHit)
                    {
                        SetPaintingState(true);
                        if (!_hasRecordedUndo)
                        {
                            _core.SaveUndoState();
                            _hasRecordedUndo = true;
                        }

                        _core.PaintAtPosition(hitPoint);
                        e.Use();
                    }

                    break;
                case EventType.MouseDrag:
                    if (e.button == 0 && IsPainting && hasValidHit)
                    {
                        _core.PaintAtPosition(hitPoint);
                        e.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (e.button == 0)
                    {
                        if (IsPainting && _core.Mesh != null)
                        {
                            EditorUtility.SetDirty(_core.Mesh);
                            EditorUtility.SetDirty(_core.MeshFilter);
                        }

                        SetPaintingState(false);
                        _hasRecordedUndo = false;
                        e.Use();
                    }

                    break;
                case EventType.MouseMove:
                    sceneView.Repaint();
                    break;
            }

            if (hasValidHit || IsPainting) HandleUtility.Repaint();
        }

        private void SetPaintingState(bool state)
        {
            if (IsPainting != state)
            {
                IsPainting = state;
                _onPaintStateChanged?.Invoke();
            }
        }
    }
}