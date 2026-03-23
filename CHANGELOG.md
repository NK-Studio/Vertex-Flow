# 변경 기록 (CHANGELOG)

## 1.0.0 - 2026-03-23 (YYYY-MM-DD)

### 추가됨 (Added)
- 버텍스 페인팅 기능을 위한 Unity 에디터 확장 도구 초기 릴리즈.
- UI Toolkit 기반의 커스텀 에디터 창 구현.
- 메시 에셋 복제 및 저장을 통한 퍼포먼스 최적화 및 원본 메시 보호 기능.
- 복제된 메시를 추적하기 위한 `VertexPaintedMesh` 컴포넌트 추가.
- 다국어(한국어, 영어, 일본어) 지원을 위한 CSV 기반 로컬라이제이션 시스템 구현.

### 변경됨 (Changed)
- `VertexPainter.cs` 단일 파일을 역할별(`Core`, `UI`, `Scene`, `Localization`, `Enums`)로 분리하고, `Assets/Plugins/VertexFlow/Scripts/Editor/` 경로로 재구성.
- `VertexPainterWindow`의 "페인팅 활성화" 토글 강조 디자인 개선.
- Undo/Redo 단축키 표기를 OS(`Ctrl`/`Cmd`)에 따라 동적으로 변경하도록 업데이트.
- `VertexPaintedMesh` 컴포넌트의 인스펙터 커스텀 GUI 구현.

### 수정됨 (Fixed)
- CSV 로딩 시 `Replace("", "")`로 인한 `ArgumentException` 발생 문제 해결.
- `VertexPaintedMesh` 컴포넌트가 존재함에도 "메시 복제" 버튼이 활성화되던 문제 해결.
- `VertexPaintedMesh` 컴포넌트의 `PaintedMesh` 또는 `OriginalMesh` 참조가 유실되었을 때 경고 메시지 출력 및 사용자 수정을 위한 필드 활성화.
- 에디터 창 크기 조절 시 UI 요소가 깨지는 문제 해결을 위해 `ScrollView` 적용.
- 매쉬 필터가 없는 오브젝트 선택 시 관련 없는 모든 페인팅 툴 숨김 처리.
- 메뉴 아이템 경로 `Tools/Vertex Painter`를 `Tools/Vertex Flow`로 변경.
- 모든 스크립트 변수명 네이밍 컨벤션 통일 (private 필드는 `_` + camelCase, public 필드는 PascalCase).
- `VertexPaintedMeshEditor`에 아이콘 적용.

### 제거됨 (Removed)
- `VertexPainter.cs`의 기존 코드 (리팩토링으로 인한 파일 분리)
