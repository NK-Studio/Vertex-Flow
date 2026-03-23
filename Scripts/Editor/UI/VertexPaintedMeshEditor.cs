using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VertexFlow.Core;

namespace VertexFlow.UI
{
    [CustomEditor(typeof(VertexPaintedMesh))]
    public class VertexPaintedMeshEditor : Editor
    {
        private void OnEnable()
        {
            Localization.VertexPainterLocalization.LoadCSV();
            ApplyComponentIcon();
        }

        private void ApplyComponentIcon()
        {
            string iconGuid = "4c00c0fdc2d644d22a2b0afdc1ea768b";
            string iconPath = AssetDatabase.GUIDToAssetPath(iconGuid);
            
            if (string.IsNullOrEmpty(iconPath) || !System.IO.File.Exists(iconPath))
            {
                iconPath = "Packages/com.nkstudio.vertexflow/Scripts/Editor/Asset/Icon/icon.png";
            }

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);

            if (icon != null)
            {
                EditorGUIUtility.SetIconForObject(target, icon);
            }
            else
            {
                EditorGUIUtility.SetIconForObject(target, null);
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // 배경 및 마진 설정 (약간의 꾸밈 요소)
            var container = new VisualElement
            {
                style =
                {
                    marginTop = 10,
                    marginBottom = 10,
                    paddingTop = 15,
                    paddingBottom = 15,
                    paddingLeft = 10,
                    paddingRight = 10,
                    backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.5f),
                    borderTopLeftRadius = 5,
                    borderTopRightRadius = 5,
                    borderBottomLeftRadius = 5,
                    borderBottomRightRadius = 5,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = new Color(0.3f, 0.3f, 0.3f),
                    borderBottomColor = new Color(0.3f, 0.3f, 0.3f),
                    borderLeftColor = new Color(0.3f, 0.3f, 0.3f),
                    borderRightColor = new Color(0.3f, 0.3f, 0.3f)
                }
            };

            // 타이틀 헤더 컨테이너 (아이콘과 텍스트를 나란히 배치)
            var headerContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 10
                }
            };

            // 인스펙터 내 UI 아이콘 적용
            string iconGuid = "4c00c0fdc2d644d22a2b0afdc1ea768b";
            string iconPath = AssetDatabase.GUIDToAssetPath(iconGuid);
            
            if (string.IsNullOrEmpty(iconPath) || !System.IO.File.Exists(iconPath))
            {
                iconPath = "Assets/Plugins/VertexFlow/Scripts/Editor/Asset/Icon/icon.png";
            }
            
            Texture2D iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            
            if (iconTexture != null)
            {
                var iconElement = new VisualElement
                {
                    style =
                    {
                        width = 20,
                        height = 20,
                        marginRight = 8,
                        backgroundImage = iconTexture
                    }
                };
                headerContainer.Add(iconElement);
            }

            var header = new Label("Vertex Painted Mesh")
            {
                style =
                {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.6f, 0.8f, 1f)
                }
            };
            headerContainer.Add(header);
            container.Add(headerContainer);

            // 설명 텍스트
            string localizedDesc = Localization.VertexPainterLocalization.GetText("paintedMeshDescription")
                .Replace("\\n", "\n");
            var description = new Label(localizedDesc)
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 15,
                    color = new Color(0.8f, 0.8f, 0.8f)
                }
            };
            container.Add(description);

            // 경고 박스 (동적으로 보이거나 숨겨짐)
            var warningBox = new HelpBox("메시 참조가 유실되었습니다!\n에셋이 삭제되었거나 경로가 변경되었습니다. 올바른 메시를 직접 다시 할당해주세요.", HelpBoxMessageType.Error)
            {
                style = { marginBottom = 10, display = DisplayStyle.None }
            };
            container.Add(warningBox);

            var propOriginalMesh = serializedObject.FindProperty("OriginalMesh");
            var propPaintedMesh = serializedObject.FindProperty("PaintedMesh");

            var originalMeshField = new PropertyField(propOriginalMesh) { style = { display = DisplayStyle.None } };
            container.Add(originalMeshField);

            var paintedMeshField = new PropertyField(propPaintedMesh);
            container.Add(paintedMeshField);

            // 상태 업데이트 함수
            void UpdateReferenceState()
            {
                bool isMissing = propOriginalMesh.objectReferenceValue == null || propPaintedMesh.objectReferenceValue == null;
                
                warningBox.style.display = isMissing ? DisplayStyle.Flex : DisplayStyle.None;
                originalMeshField.style.display = isMissing ? DisplayStyle.Flex : DisplayStyle.None;
                
                paintedMeshField.SetEnabled(isMissing);
            }

            // 초기 상태 세팅
            UpdateReferenceState();

            // 프로퍼티 값 변경 추적
            root.TrackPropertyValue(propOriginalMesh, _ => UpdateReferenceState());
            root.TrackPropertyValue(propPaintedMesh, _ => UpdateReferenceState());

            root.Add(container);

            return root;
        }
    }
}