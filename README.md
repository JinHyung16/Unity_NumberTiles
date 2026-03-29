# NumberTiles

> Unity 기반 모바일 숫자 퍼즐 게임 — 접근성은 높고, 판단은 신중하게

<br>

## 프로젝트 개요

사과 게임이 유행하던 시기, **"왜 이 게임이 재밌는가?"** 라는 코어 재미 분석을 출발점으로 삼았습니다.
분석 중 발견한 **Number Tiles** 장르에서 동일한 재미 구조를 확인했고, 이를 직접 구현하며 게임의 재미 설계를 검증한 프로젝트입니다.

<br>

## 코어 재미 분석

| 요소 | 설명 |
|------|------|
| **낮은 진입 장벽** | 같은 숫자 또는 합이 10이 되는 두 숫자를 연결하는 단순한 규칙 |
| **전략적 판단** | 스테이지별 고유한 맵 형태(ActiveRanges)에 따라 어떤 라인을 먼저 지울지 신중하게 선택해야 함 |
| **두뇌 활성화 경험** | 제한된 힌트 없이 스스로 경우의 수를 계산하는 과정이 "똑똑해지는 느낌"을 제공하는 기능성 게임적 요소 |
| **승부욕 자극** | 스테이지가 올라갈수록 맵 복잡도가 증가하며 클리어 시 성취감과 다음 스테이지에 대한 도전 욕구 유발 |

<br>

## 게임 규칙

- **매칭 조건**: 두 숫자가 같거나, 합이 10
- **이동 방향**: 가로 / 세로 / 대각선 / 행 끝→다음 행 시작(flat row-major)
- **경로 조건**: 두 타일 사이 활성 타일이 없어야 연결 가능
- **클리어 조건**: 보드에 등장했던 숫자 1~9가 모두 제거됨
- **실패 조건**: 줄 추가 아이템이 없고, 매칭 가능한 쌍이 남아있지 않음

<br>

## 기술 스택

- **Unity** (Mobile / 2D UI)
- **C# / .NET** — Unity C# 코딩 컨벤션 적용
- **DOTween** — UI 애니메이션

<br>

## 아키텍처 및 구현 특징

### 1. Observer 패턴 기반 보드 상태 전파
`ITileObserver` / `TileNotify` 인터페이스로 보드 변경 이벤트를 구독자에게 전달합니다.
`GameManager`가 옵저버로 등록되어 `BoardChanged`, `ItemCountChanged` 알림 수신 시 게임 결과를 판정합니다.

```
TileManager ──(TileNotify)──► ITileObserver (GameManager, TileWindow, ...)
```

### 2. Factory 패턴 기반 아이템 시스템
`IItemFactory` / `ITileItem` 인터페이스로 아이템 종류를 추상화했습니다.
새로운 아이템 추가 시 `TileItemFactory`에 케이스 추가만으로 확장 가능합니다.

```
TileItemFactory.Create(ItemType) → ITileItem → Execute(IFactoryInput) → IFactoryOutput
```

현재 구현 아이템: `AddTiles` (줄 추가), `BreakOneTile` (타일 1개 파괴)

### 3. 매칭 알고리즘 — 4방향 경로 검사
방향별 독립 함수로 구현하여 각 로직의 명확성을 확보했습니다.

| 함수 | 방향 |
|------|------|
| `IsBlockedOnRow` | 가로 |
| `IsBlockedOnCol` | 세로 |
| `IsBlockedOnDiagonal` | 대각선 |
| `IsBlockedOnFlatRowMajor` | 행 끝 → 다음 행 시작 (row-major wrap) |

### 4. 스테이지별 맵 형태 설계 (ScriptableObject)
`StageData` ScriptableObject의 `ActiveRanges`(행별 활성 열 범위)로 각 스테이지의 보드 모양을 정의합니다.
코드 변경 없이 에디터에서 다양한 퍼즐 형태를 디자인할 수 있습니다.

### 5. 진행 상황 자동 저장 / 복원
게임 중 상태를 `GameProgressData`(JSON)로 직렬화하여 로컬에 저장합니다.
앱 재시작 시 가장 최근 플레이 스테이지를 자동으로 복원합니다.

```
GameProgressSaver.Build() → JsonUtility.ToJson() → 로컬 파일
GameProgressSaver.TryLoad() → TileManager.TryApplyProgress() → 보드 복원
```

### 6. Object Pooling (TileUIComponent)
`PoolManager`가 `TileUIComponent` 프리팹을 `Stack<T>` 기반으로 관리합니다.
사전 워밍(Prewarm 100개)으로 게임 진행 중 Instantiate 호출을 최소화했습니다.

### 7. GC 최적화
- 랜덤 스폰 후보 배열(`_digitCandidates`)을 멤버 변수로 사전 할당 — 핫패스 내 `new int[]` 제거
- `List<int>` → `Queue<int>` 교체로 AddTiles 큐의 Dequeue O(n) → O(1) 개선
- 숫자 추적 배열(`_digitSeen`, `_digitCleared`, `_digitCount`)을 `Array.Clear`로 재사용

<br>

## 프로젝트 구조

```
Assets/Scripts/
├── TileManager.cs              # 핵심 게임 로직 (보드, 매칭, 스폰, 승패 판정)
├── GameManager.cs              # 씬 흐름 및 UI 전환 관리
├── StageData.cs                # 스테이지 정의 (ScriptableObject)
├── GameProgressData.cs         # 저장 데이터 구조체
├── GameProgressSaver.cs        # 진행 상황 저장/불러오기
├── GameMetaSaver.cs            # 클리어 이력 및 메타 데이터 저장
├── PoolingManager.cs           # TileUIComponent 오브젝트 풀
├── TileNotify.cs               # Observer 이벤트 정의
├── ItemFactory/
│   ├── ItemFactoryInterfaces.cs
│   ├── TileItemFactory.cs
│   ├── AddTilesItem.cs
│   └── BreakOneTileItem.cs
└── UI/
    ├── TileWindow.cs
    ├── TileUIComponent.cs
    ├── LobbyWindow.cs
    ├── GameResultWindow.cs
    └── ...
```

<br>

## 개발 의도

단순히 게임을 클론한 것이 아닌, **"이 장르가 왜 재밌는가"를 분석하고 직접 구현하며 검증**하는 것이 목적이었습니다.
기획자이자 개발자로서 코어 루프 설계, 아키텍처 구성, 성능 최적화까지 1인으로 진행한 프로젝트입니다.
