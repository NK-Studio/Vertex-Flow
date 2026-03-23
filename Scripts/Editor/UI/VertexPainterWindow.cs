using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using VertexFlow.Core;
using VertexFlow.Localization;
using VertexFlow.Scene;

namespace VertexFlow.UI
{
    public class VertexPainterWindow : EditorWindow
    {
        private VertexPainterCore _core;
        private VertexPainterSceneView _sceneViewHandler;

        // UI Elements
        private Label _titleLabel, _selectedObjectLabel, _vertexCountLabel;
        private HelpBox _cloneHelpBox;
        private Button _cloneMeshBtn;
        private VisualElement _paintingToolsContainer;

        private Toggle _enablePaintingToggle;
        private HelpBox _statusHelpBox, _paintingHelpBox, _instructionsHelpBox;
        private ColorField _colorField;
        private Slider _brushSizeSlider, _brushStrengthSlider, _redSlider, _greenSlider, _blueSlider;
        private Label _brushSizeLabel, _brushStrengthLabel, _redLabel, _greenLabel, _blueLabel;

        private Label _objectInfoLabel,
            _brushModeLabel,
            _brushSettingsLabel,
            _colorPaletteLabel,
            _rgbSlidersLabel,
            _undoRedoLabel,
            _utilitiesLabel;

        private Button _paintModeBtn,
            _eraseModeBtn,
            _smoothModeBtn,
            _undoBtn,
            _redoBtn,
            _fillWhiteBtn,
            _fillBlackBtn,
            _fillCurrentBtn,
            _resetBtn;

        // Containers for enabling/disabling
        private VisualElement _brushModeBox, _brushSettingsBox, _undoRedoBox, _utilityBox;
        private VisualElement _colorSettingsContainer;

        [MenuItem("Tools/Vertex Flow")]
        public static void ShowWindow()
        {
            GetWindow<VertexPainterWindow>("Vertex Flow");
        }

        private void OnEnable()
        {
            _core = new VertexPainterCore();
            _sceneViewHandler = new VertexPainterSceneView(_core, UpdateUI, OnRepaintRequested);

            VertexPainterLocalization.LoadCSV();
            SceneView.duringSceneGui += _sceneViewHandler.OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;

            _core.OnSelectionChanged();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= _sceneViewHandler.OnSceneGUI;
            Selection.selectionChanged -= OnSelectionChanged;
            _sceneViewHandler.IsEnabled = false;
        }

        private void OnSelectionChanged()
        {
            _core.OnSelectionChanged();
            UpdateUI();
            Repaint();
        }

        private void OnRepaintRequested()
        {
            if (_brushSizeSlider != null)
            {
                _brushSizeSlider.value = _core.BrushSize;
                _brushSizeLabel.text = $"{GetText("brushSize")}: {_core.BrushSize:F2}";
            }

            Repaint();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;

            // Create a ScrollView to hold all content and prevent UI breakage on small screens
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.style.paddingTop = 10;
            scrollView.style.paddingBottom = 10;
            scrollView.style.paddingLeft = 10;
            scrollView.style.paddingRight = 10;
            root.Add(scrollView);

            var headerRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center,
                    marginBottom = 10
                }
            };

            _titleLabel = new Label(GetText("title"))
                { style = { fontSize = 18, unityFontStyleAndWeight = FontStyle.Bold } };
            headerRow.Add(_titleLabel);

            var languageDropdown = new DropdownField();
            languageDropdown.choices = new List<string> { "English", "한국어", "日本語" };

            switch (VertexPainterLocalization.CurrentLanguage)
            {
                case Language.English: languageDropdown.value = "English"; break;
                case Language.Korean: languageDropdown.value = "한국어"; break;
                case Language.Japanese: languageDropdown.value = "日本語"; break;
            }

            languageDropdown.style.width = 100;
            languageDropdown.RegisterValueChangedCallback(evt =>
            {
                switch (evt.newValue)
                {
                    case "English": VertexPainterLocalization.CurrentLanguage = Language.English; break;
                    case "한국어": VertexPainterLocalization.CurrentLanguage = Language.Korean; break;
                    case "日本語": VertexPainterLocalization.CurrentLanguage = Language.Japanese; break;
                }

                UpdateLanguage();
            });
            headerRow.Add(languageDropdown);
            scrollView.Add(headerRow);

            var objectInfoBox = CreateSection("Object Info", out _objectInfoLabel);
            _selectedObjectLabel = new Label($"{GetText("selected")}: {GetText("none")}");
            objectInfoBox.Add(_selectedObjectLabel);
            _vertexCountLabel = new Label($"{GetText("vertices")}: 0") { style = { marginTop = 5 } };
            objectInfoBox.Add(_vertexCountLabel);
            scrollView.Add(objectInfoBox);

            // Cloning Message & Button
            _cloneHelpBox = new HelpBox(GetText("requiresClone"), HelpBoxMessageType.Warning)
                { style = { marginTop = 10, display = DisplayStyle.None } };
            scrollView.Add(_cloneHelpBox);

            _cloneMeshBtn = new Button(OnCloneMeshClicked)
            {
                text = GetText("cloneMeshBtn"),
                style =
                {
                    marginTop = 5, height = 30, backgroundColor = new Color(0.15f, 0.45f, 0.75f),
                    display = DisplayStyle.None
                }
            };
            scrollView.Add(_cloneMeshBtn);

            _statusHelpBox = new HelpBox(GetText("selectMeshFilter"), HelpBoxMessageType.Warning)
                { style = { marginBottom = 10 } };
            scrollView.Add(_statusHelpBox); // Add to scrollView directly so it can remain visible when tools are hidden

            // Container for all painting tools
            _paintingToolsContainer = new VisualElement();
            scrollView.Add(_paintingToolsContainer);

            var enablePaintingContainer = new VisualElement
            {
                style =
                {
                    marginTop = 10,
                    marginBottom = 5,
                    paddingTop = 6,
                    paddingBottom = 6,
                    paddingLeft = 8,
                    paddingRight = 8,
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.4f),
                    borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                    borderTopColor = new Color(0.1f, 0.1f, 0.1f), borderBottomColor = new Color(0.1f, 0.1f, 0.1f),
                    borderLeftColor = new Color(0.1f, 0.1f, 0.1f), borderRightColor = new Color(0.1f, 0.1f, 0.1f)
                }
            };

            _enablePaintingToggle = new Toggle(GetText("enablePainting"));
            _enablePaintingToggle.value = _sceneViewHandler.IsEnabled;

            // Set initial style based on current state
            if (_sceneViewHandler.IsEnabled)
            {
                enablePaintingContainer.style.backgroundColor = new Color(0.18f, 0.38f, 0.58f, 0.5f);
                enablePaintingContainer.style.borderTopColor = new Color(0.25f, 0.5f, 0.8f);
                enablePaintingContainer.style.borderBottomColor = new Color(0.25f, 0.5f, 0.8f);
                enablePaintingContainer.style.borderLeftColor = new Color(0.25f, 0.5f, 0.8f);
                enablePaintingContainer.style.borderRightColor = new Color(0.25f, 0.5f, 0.8f);
            }

            _enablePaintingToggle.RegisterValueChangedCallback(evt =>
            {
                _sceneViewHandler.IsEnabled = evt.newValue;

                // Update styling based on toggle state
                if (evt.newValue)
                {
                    enablePaintingContainer.style.backgroundColor =
                        new Color(0.18f, 0.38f, 0.58f, 0.5f); // Subtle blue when active
                    enablePaintingContainer.style.borderTopColor = new Color(0.25f, 0.5f, 0.8f);
                    enablePaintingContainer.style.borderBottomColor = new Color(0.25f, 0.5f, 0.8f);
                    enablePaintingContainer.style.borderLeftColor = new Color(0.25f, 0.5f, 0.8f);
                    enablePaintingContainer.style.borderRightColor = new Color(0.25f, 0.5f, 0.8f);
                }
                else
                {
                    enablePaintingContainer.style.backgroundColor =
                        new Color(0.2f, 0.2f, 0.2f, 0.4f); // Default dark when inactive
                    enablePaintingContainer.style.borderTopColor = new Color(0.1f, 0.1f, 0.1f);
                    enablePaintingContainer.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);
                    enablePaintingContainer.style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f);
                    enablePaintingContainer.style.borderRightColor = new Color(0.1f, 0.1f, 0.1f);
                }

                UpdateUI();
                SceneView.RepaintAll();
            });

            var toggleLabel = _enablePaintingToggle.Q<Label>();
            if (toggleLabel != null) toggleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            enablePaintingContainer.Add(_enablePaintingToggle);
            _paintingToolsContainer.Add(enablePaintingContainer);

            _paintingHelpBox = new HelpBox(GetText("paintingNow"), HelpBoxMessageType.Warning)
                { style = { display = DisplayStyle.None } };
            _paintingToolsContainer.Add(_paintingHelpBox);

            _brushModeBox = CreateSection("Brush Mode", out _brushModeLabel);
            var modeButtons = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };

            _paintModeBtn = CreateModeButton("Paint", BrushMode.Paint);
            _eraseModeBtn = CreateModeButton("Erase", BrushMode.Erase);
            _smoothModeBtn = CreateModeButton("Smooth", BrushMode.Smooth);

            modeButtons.Add(_paintModeBtn);
            modeButtons.Add(_eraseModeBtn);
            modeButtons.Add(_smoothModeBtn);
            _brushModeBox.Add(modeButtons);
            _paintingToolsContainer.Add(_brushModeBox);

            UpdateModeButtons();

            _brushSettingsBox = CreateSection("Brush Settings", out _brushSettingsLabel);

            // --- Color Settings Group ---
            _colorSettingsContainer = new VisualElement();
            _brushSettingsBox.Add(_colorSettingsContainer);

            _colorField = new ColorField(GetText("paintColor")) { value = _core.PaintColor };
            _colorField.RegisterValueChangedCallback(evt =>
            {
                _core.PaintColor = evt.newValue;
                UpdateRGBSliders();
            });
            _colorSettingsContainer.Add(_colorField);

            _colorPaletteLabel = new Label(GetText("colorPalette"))
                { style = { marginTop = 10, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } };
            _colorSettingsContainer.Add(_colorPaletteLabel);

            var paletteContainer = new VisualElement { style = { marginTop = 5 } };
            paletteContainer.Add(CreateColorPaletteRow(new[]
            {
                Color.red, new(1f, 0.5f, 0f), Color.yellow, Color.green, Color.cyan, Color.blue,
                new(0.5f, 0f, 1f), Color.magenta
            }));
            paletteContainer.Add(CreateColorPaletteRow(new Color[]
            {
                new(0.5f, 0f, 0f), new(0.5f, 0.25f, 0f), new(0.5f, 0.5f, 0f), new(0f, 0.5f, 0f),
                new(0f, 0.5f, 0.5f), new(0f, 0f, 0.5f), new(0.25f, 0f, 0.5f),
                new(0.5f, 0f, 0.5f)
            }));
            paletteContainer.Add(CreateColorPaletteRow(new Color[]
            {
                Color.white, new(0.75f, 0.75f, 0.75f), new(0.5f, 0.5f, 0.5f),
                new(0.25f, 0.25f, 0.25f), Color.black, new(1f, 0.8f, 0.6f), new(0.6f, 0.4f, 0.2f),
                new(1f, 0.75f, 0.8f)
            }));
            _colorSettingsContainer.Add(paletteContainer);

            _rgbSlidersLabel = new Label(GetText("rgbSliders"))
                { style = { marginTop = 15, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } };
            _colorSettingsContainer.Add(_rgbSlidersLabel);

            var redContainer = new VisualElement { style = { marginTop = 5 } };
            _redLabel = new Label($"R: {(int)(_core.PaintColor.r * 255)}")
                { style = { color = new Color(1f, 0.5f, 0.5f) } };
            redContainer.Add(_redLabel);
            _redSlider = new Slider(0f, 1f) { value = _core.PaintColor.r, style = { marginTop = 2 } };
            _redSlider.RegisterValueChangedCallback(evt =>
            {
                _core.PaintColor.r = evt.newValue;
                UpdateColorFromRGB();
            });
            redContainer.Add(_redSlider);
            _colorSettingsContainer.Add(redContainer);

            var greenContainer = new VisualElement { style = { marginTop = 8 } };
            _greenLabel = new Label($"G: {(int)(_core.PaintColor.g * 255)}")
                { style = { color = new Color(0.5f, 1f, 0.5f) } };
            greenContainer.Add(_greenLabel);
            _greenSlider = new Slider(0f, 1f) { value = _core.PaintColor.g, style = { marginTop = 2 } };
            _greenSlider.RegisterValueChangedCallback(evt =>
            {
                _core.PaintColor.g = evt.newValue;
                UpdateColorFromRGB();
            });
            greenContainer.Add(_greenSlider);
            _colorSettingsContainer.Add(greenContainer);

            var blueContainer = new VisualElement { style = { marginTop = 8 } };
            _blueLabel = new Label($"B: {(int)(_core.PaintColor.b * 255)}")
                { style = { color = new Color(0.5f, 0.5f, 1f) } };
            blueContainer.Add(_blueLabel);
            _blueSlider = new Slider(0f, 1f) { value = _core.PaintColor.b, style = { marginTop = 2 } };
            _blueSlider.RegisterValueChangedCallback(evt =>
            {
                _core.PaintColor.b = evt.newValue;
                UpdateColorFromRGB();
            });
            blueContainer.Add(_blueSlider);
            _colorSettingsContainer.Add(blueContainer);
            // --- End of Color Settings Group ---

            var sizeContainer = new VisualElement { style = { marginTop = 10 } };
            _brushSizeLabel = new Label($"{GetText("brushSize")}: {_core.BrushSize:F2}");
            sizeContainer.Add(_brushSizeLabel);
            _brushSizeSlider = new Slider(0.01f, 5.0f) { value = _core.BrushSize, style = { marginTop = 2 } };
            _brushSizeSlider.RegisterValueChangedCallback(evt =>
            {
                _core.BrushSize = evt.newValue;
                _brushSizeLabel.text = $"{GetText("brushSize")}: {_core.BrushSize:F2}";
            });
            sizeContainer.Add(_brushSizeSlider);
            _brushSettingsBox.Add(sizeContainer);

            var strengthContainer = new VisualElement { style = { marginTop = 10 } };
            _brushStrengthLabel = new Label($"{GetText("brushStrength")}: {_core.BrushStrength:F2}");
            strengthContainer.Add(_brushStrengthLabel);
            _brushStrengthSlider = new Slider(0.01f, 1.0f) { value = _core.BrushStrength, style = { marginTop = 2 } };
            _brushStrengthSlider.RegisterValueChangedCallback(evt =>
            {
                _core.BrushStrength = evt.newValue;
                _brushStrengthLabel.text = $"{GetText("brushStrength")}: {_core.BrushStrength:F2}";
            });
            strengthContainer.Add(_brushStrengthSlider);
            _brushSettingsBox.Add(strengthContainer);
            _paintingToolsContainer.Add(_brushSettingsBox);

            _undoRedoBox = CreateSection("Undo/Redo", out _undoRedoLabel);
            var undoRedoRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };
            _undoBtn = new Button(() => _core.PerformUndo())
                { text = GetText("undo"), style = { flexGrow = 1, marginRight = 5 } };
            undoRedoRow.Add(_undoBtn);
            _redoBtn = new Button(() => _core.PerformRedo()) { text = GetText("redo"), style = { flexGrow = 1 } };
            undoRedoRow.Add(_redoBtn);
            _undoRedoBox.Add(undoRedoRow);

            var undoStatusLabel = new Label("Undo steps available: 0") { style = { marginTop = 5, fontSize = 10 } };
            _undoRedoBox.Add(undoStatusLabel);
            _paintingToolsContainer.Add(_undoRedoBox);

            root.schedule.Execute(() =>
            {
                if (undoStatusLabel != null)
                    undoStatusLabel.text = $"Undo: {_core.GetUndoCount()} | Redo: {_core.GetRedoCount()}";
            }).Every(100);

            _utilityBox = CreateSection("Utilities", out _utilitiesLabel);
            var topRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };
            _fillWhiteBtn = new Button(() => _core.FillAllVertices(Color.white))
                { text = GetText("fillWhite"), style = { flexGrow = 1, marginRight = 5 } };
            topRow.Add(_fillWhiteBtn);
            _fillBlackBtn = new Button(() => _core.FillAllVertices(Color.black))
                { text = GetText("fillBlack"), style = { flexGrow = 1 } };
            topRow.Add(_fillBlackBtn);
            _utilityBox.Add(topRow);

            _fillCurrentBtn = new Button(() => _core.FillAllVertices(_core.PaintColor))
                { text = GetText("fillCurrentColor"), style = { marginTop = 5 } };
            _utilityBox.Add(_fillCurrentBtn);
            _resetBtn = new Button(() => _core.FillAllVertices(Color.white))
                { text = GetText("resetToOriginal"), style = { marginTop = 5 } };
            _utilityBox.Add(_resetBtn);
            _paintingToolsContainer.Add(_utilityBox);

            _instructionsHelpBox = new HelpBox(GetText("howToUse"), HelpBoxMessageType.Info)
                { style = { marginTop = 15 } };
            _paintingToolsContainer.Add(_instructionsHelpBox);

            UpdateUI();
            UpdateLanguage();
        }

        private void OnCloneMeshClicked()
        {
            if (_core.SelectedObject == null) return;
            var filter = _core.SelectedObject.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return;

            string defaultName = filter.sharedMesh.name + "_Painted.asset";
            string path =
                EditorUtility.SaveFilePanelInProject(GetText("saveMeshTitle"), defaultName, "asset",
                    "Save cloned mesh");

            if (!string.IsNullOrEmpty(path))
            {
                _core.CloneAndSaveMesh(path);
                UpdateUI();
            }
        }

        private string GetText(string key)
        {
            string text = VertexPainterLocalization.GetText(key);
            string ctrlKey = Application.platform == RuntimePlatform.OSXEditor ? "Cmd" : "Ctrl";
            return text.Replace("{CTRL}", ctrlKey);
        }

        private VisualElement CreateSection(string titleKey, out Label titleLabel)
        {
            var container = new VisualElement
            {
                style =
                {
                    marginTop = 10, paddingTop = 10, paddingBottom = 10, paddingLeft = 10, paddingRight = 10,
                    backgroundColor = new Color(0, 0, 0, 0.1f), borderTopLeftRadius = 5, borderTopRightRadius = 5,
                    borderBottomLeftRadius = 5, borderBottomRightRadius = 5
                }
            };
            titleLabel = new Label(GetText(titleKey))
                { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } };
            container.Add(titleLabel);
            return container;
        }

        private Button CreateModeButton(string text, BrushMode mode)
        {
            var btn = new Button(() =>
            {
                _core.CurrentMode = mode;
                UpdateModeButtons();
                UpdateUI();
            }) { text = text, style = { flexGrow = 1, marginRight = 2, marginLeft = 2 } };
            return btn;
        }

        private void UpdateModeButtons()
        {
            if (_paintModeBtn != null)
                _paintModeBtn.style.backgroundColor = _core.CurrentMode == BrushMode.Paint
                    ? new Color(0.3f, 0.5f, 0.8f)
                    : new Color(0.2f, 0.2f, 0.2f, 0.5f);
            if (_eraseModeBtn != null)
                _eraseModeBtn.style.backgroundColor = _core.CurrentMode == BrushMode.Erase
                    ? new Color(0.3f, 0.5f, 0.8f)
                    : new Color(0.2f, 0.2f, 0.2f, 0.5f);
            if (_smoothModeBtn != null)
                _smoothModeBtn.style.backgroundColor = _core.CurrentMode == BrushMode.Smooth
                    ? new Color(0.3f, 0.5f, 0.8f)
                    : new Color(0.2f, 0.2f, 0.2f, 0.5f);
        }

        private void UpdateUI()
        {
            if (_selectedObjectLabel != null)
                _selectedObjectLabel.text =
                    $"{GetText("selected")}: {(_core.SelectedObject != null ? _core.SelectedObject.name : GetText("none"))}";

            bool hasMeshFilter = _core.SelectedObject != null && _core.SelectedObject.GetComponent<MeshFilter>() != null;
            bool needsClone = _core.NeedsCloning();

            if (_cloneHelpBox != null) _cloneHelpBox.style.display = needsClone && hasMeshFilter ? DisplayStyle.Flex : DisplayStyle.None;
            if (_cloneMeshBtn != null) _cloneMeshBtn.style.display = needsClone && hasMeshFilter ? DisplayStyle.Flex : DisplayStyle.None;
            
            // Show painting tools only if we have a mesh filter and we don't need cloning
            if (_paintingToolsContainer != null)
                _paintingToolsContainer.style.display = (!needsClone && hasMeshFilter) ? DisplayStyle.Flex : DisplayStyle.None;

            if (_vertexCountLabel != null && _core.Mesh != null && !needsClone)
            {
                _vertexCountLabel.text = $"{GetText("vertices")}: {_core.Mesh.vertexCount}";
                _vertexCountLabel.style.display = DisplayStyle.Flex;
            }
            else if (_vertexCountLabel != null) _vertexCountLabel.style.display = DisplayStyle.None;

            if (_statusHelpBox != null)
            {
                // We show status help box when there's NO mesh filter, or when there IS a mesh filter and NO cloning needed
                // Wait, the prompt says: "매쉬 필터가 없는 오브젝트를 셀렉팅하면 ... 현재 존재하는 helpbox만 보여지도록 하자"
                // This means statusHelpBox should be outside _paintingToolsContainer, or we adjust visibility.
                
                if (!hasMeshFilter)
                {
                    _statusHelpBox.style.display = DisplayStyle.Flex;
                    _statusHelpBox.text = GetText("selectMeshFilter");
                    _statusHelpBox.messageType = HelpBoxMessageType.Warning;
                }
                else if (!needsClone)
                {
                    _statusHelpBox.style.display = DisplayStyle.Flex;
                    if (_sceneViewHandler.IsEnabled)
                    {
                        _statusHelpBox.text = GetText("paintingActive");
                        _statusHelpBox.messageType = HelpBoxMessageType.Warning;
                    }
                    else
                    {
                        _statusHelpBox.text = GetText("readyToPaint");
                        _statusHelpBox.messageType = HelpBoxMessageType.Info;
                    }
                }
                else
                {
                    // If it needs clone, we hide the status help box because the clone help box is showing.
                    _statusHelpBox.style.display = DisplayStyle.None;
                }
            }

            if (_paintingHelpBox != null)
            {
                _paintingHelpBox.text = GetText("paintingNow");
                _paintingHelpBox.style.display = _sceneViewHandler.IsPainting ? DisplayStyle.Flex : DisplayStyle.None;
            }

            bool hasValidMesh = _core.Mesh != null && !needsClone;
            bool isPaintingEnabled = _sceneViewHandler.IsEnabled && hasValidMesh;

            // Toggle interactability of sections based on IsEnabled
            if (_brushModeBox != null) _brushModeBox.SetEnabled(isPaintingEnabled);
            if (_brushSettingsBox != null) _brushSettingsBox.SetEnabled(isPaintingEnabled);
            if (_undoRedoBox != null) _undoRedoBox.SetEnabled(isPaintingEnabled);
            if (_utilityBox != null) _utilityBox.SetEnabled(isPaintingEnabled);

            // Color Settings Container is only active when Mode is Paint
            if (_colorSettingsContainer != null)
                _colorSettingsContainer.SetEnabled(_core.CurrentMode == BrushMode.Paint);

            if (_brushSizeSlider != null) _brushSizeSlider.SetEnabled(hasValidMesh);
            if (_brushStrengthSlider != null) _brushStrengthSlider.SetEnabled(hasValidMesh);
        }

        private VisualElement CreateColorPaletteRow(Color[] colors)
        {
            var row = new VisualElement
                { style = { flexDirection = FlexDirection.Row, marginTop = 3, marginBottom = 3 } };
            foreach (var color in colors)
            {
                var colorBtn = new Button(() => SelectPaletteColor(color))
                {
                    tooltip = $"RGB({(int)(color.r * 255)}, {(int)(color.g * 255)}, {(int)(color.b * 255)})",
                    style =
                    {
                        width = 30, height = 30, marginLeft = 2, marginRight = 2, backgroundColor = color,
                        borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4,
                        borderBottomRightRadius = 4, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1,
                        borderRightWidth = 1, borderTopColor = new Color(0.3f, 0.3f, 0.3f),
                        borderBottomColor = new Color(0.3f, 0.3f, 0.3f), borderLeftColor = new Color(0.3f, 0.3f, 0.3f),
                        borderRightColor = new Color(0.3f, 0.3f, 0.3f)
                    }
                };
                row.Add(colorBtn);
            }

            return row;
        }

        private void SelectPaletteColor(Color color)
        {
            _core.PaintColor = color;
            if (_colorField != null) _colorField.value = color;
            UpdateRGBSliders();
        }

        private void UpdateRGBSliders()
        {
            if (_redSlider != null)
            {
                _redSlider.value = _core.PaintColor.r;
                _redLabel.text = $"R: {(int)(_core.PaintColor.r * 255)}";
            }

            if (_greenSlider != null)
            {
                _greenSlider.value = _core.PaintColor.g;
                _greenLabel.text = $"G: {(int)(_core.PaintColor.g * 255)}";
            }

            if (_blueSlider != null)
            {
                _blueSlider.value = _core.PaintColor.b;
                _blueLabel.text = $"B: {(int)(_core.PaintColor.b * 255)}";
            }
        }

        private void UpdateColorFromRGB()
        {
            if (_colorField != null) _colorField.value = _core.PaintColor;
            if (_redLabel != null) _redLabel.text = $"R: {(int)(_core.PaintColor.r * 255)}";
            if (_greenLabel != null) _greenLabel.text = $"G: {(int)(_core.PaintColor.g * 255)}";
            if (_blueLabel != null) _blueLabel.text = $"B: {(int)(_core.PaintColor.b * 255)}";
        }

        private void UpdateLanguage()
        {
            if (_titleLabel != null) _titleLabel.text = GetText("title");
            if (_objectInfoLabel != null) _objectInfoLabel.text = GetText("objectInfo");
            if (_selectedObjectLabel != null)
                _selectedObjectLabel.text =
                    $"{GetText("selected")}: {(_core.SelectedObject != null ? _core.SelectedObject.name : GetText("none"))}";
            if (_vertexCountLabel != null && _core.Mesh != null)
                _vertexCountLabel.text = $"{GetText("vertices")}: {_core.Mesh.vertexCount}";

            if (_cloneHelpBox != null) _cloneHelpBox.text = GetText("requiresClone");
            if (_cloneMeshBtn != null) _cloneMeshBtn.text = GetText("cloneMeshBtn");

            if (_enablePaintingToggle != null) _enablePaintingToggle.label = GetText("enablePainting");
            UpdateUI();
            if (_brushModeLabel != null) _brushModeLabel.text = GetText("brushMode");
            if (_paintModeBtn != null) _paintModeBtn.text = GetText("paint");
            if (_eraseModeBtn != null) _eraseModeBtn.text = GetText("erase");
            if (_smoothModeBtn != null) _smoothModeBtn.text = GetText("smooth");
            if (_brushSettingsLabel != null) _brushSettingsLabel.text = GetText("brushSettings");
            if (_colorField != null) _colorField.label = GetText("paintColor");
            if (_colorPaletteLabel != null) _colorPaletteLabel.text = GetText("colorPalette");
            if (_rgbSlidersLabel != null) _rgbSlidersLabel.text = GetText("rgbSliders");
            if (_brushSizeLabel != null) _brushSizeLabel.text = $"{GetText("brushSize")}: {_core.BrushSize:F2}";
            if (_brushStrengthLabel != null)
                _brushStrengthLabel.text = $"{GetText("brushStrength")}: {_core.BrushStrength:F2}";
            if (_undoRedoLabel != null) _undoRedoLabel.text = GetText("undoRedo");
            if (_undoBtn != null) _undoBtn.text = GetText("undo");
            if (_redoBtn != null) _redoBtn.text = GetText("redo");
            if (_utilitiesLabel != null) _utilitiesLabel.text = GetText("utilities");
            if (_fillWhiteBtn != null) _fillWhiteBtn.text = GetText("fillWhite");
            if (_fillBlackBtn != null) _fillBlackBtn.text = GetText("fillBlack");
            if (_fillCurrentBtn != null) _fillCurrentBtn.text = GetText("fillCurrentColor");
            if (_resetBtn != null) _resetBtn.text = GetText("resetToOriginal");
            if (_instructionsHelpBox != null) _instructionsHelpBox.text = GetText("howToUse");
            Repaint();
        }
    }
}