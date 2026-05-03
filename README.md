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

## 레벨 디자인

### 설계 철학

| 원칙 | 적용 방식 |
|------|----------|
| **점진적 난이도 + 깜짝 스파이크** | 로그 곡선으로 부드럽게 오르다가 정해진 구간에 갑작스러운 도전을 끼움 |
| **맵 형태로 난이도 조절** | 타일 수만 늘리지 않고, 보드 모양(StageLevel) 자체를 바꿔 난이도와 시각적 변화를 동시에 부여 |
| **호흡 조절 = 신선함의 트리거** | 유저가 동일 패턴에 지루해질 시점을 예측해 정기적으로 새로운 맵 구조를 등장시켜 흥미 유지 |

### 난이도 곡선 (Stage 1~300)

기본 타일 수는 **로그 함수**로 산출하고, 일정 주기마다 보정 계수를 곱해 곡선에 굴곡을 만듭니다.

```
SpawnTileCount(n) = round( 47.74 * ln((n+20)/21) ) + 30   // base 30 → 160
```

| 구간 | 보정 | 효과 |
|------|------|------|
| 베이스 | × 1.00 | 자연스러운 점진 상승 (초반 빠르게 / 후반 천천히) |
| 미니 스파이크 | 매 25스테이지 × **1.10** | 가벼운 도전, 잠깐의 긴장 |
| **보스 스파이크** | 매 50스테이지 × **1.20** | 타일 폭증 + 시각적으로 압도적인 패턴 |
| 휴식 구간 | 보스 직후 × **0.92** | 긴장 해소, 다음 페이즈로 자연스러운 전환 |

### 페이즈 구성

스테이지가 진행되며 사용 가능한 라인 풀(StageLevel id)과 평균 라인 수가 단계적으로 확장됩니다.

| Phase | 스테이지 | 라인 풀 | 평균 라인 수 | 의도 |
|-------|---------|---------|-------------|------|
| 1 | 1~20 | 1~5 (넓은 줄만) | 2~3 | 워밍업, 룰 학습 |
| 2 | 21~50 | 1~10 | 3~4 | 다양화 시작 |
| 3 | 51~100 | 1~21 | 4~6 | 좁은 줄 도입, 전략적 사고 요구 |
| 4 | 101~175 | 1~28 | 5~8 | 깊이 있는 패턴 |
| 5 | 176~250 | 1~36 | 6~9 | 마스터 |
| 6 | 251~300 | 1~41 | 7~11 | 엔드게임, 모든 패턴 활용 |

### 신선함 패턴 (매 10스테이지)

지루함을 끊기 위해 12종 패턴이 사이클로 등장합니다. 같은 베이스 룰이지만 보드 모양이 확연히 다르기에 유저가 새로운 맵을 만난 듯한 인상을 받습니다.

| 사이클 | 패턴 | 형태 |
|--------|------|------|
| 0 | 피라미드 | 좁→넓→좁 (대칭) |
| 1 | 모래시계 | 넓→좁→넓 |
| 2 | 사다리 우향 | 위치 슬라이드 |
| 3 | 사다리 좌향 | 반대 슬라이드 |
| 4 | 벽 | 꽉찬 직사각형 |
| 5 | 양옆 절벽 | 좌끝/우끝 번갈아 |
| 6 | 외딴섬 | 단일 셀 줄 모음 |
| 7 | 돔 | 가운데 좁게 강조 |
| 8 | 체스판 | 넓은 줄 / 좁은 줄 교차 |
| 9 | 계단식 좁아짐 | 단조 감소 |
| 10 | 거울 대칭 | 좌우 대칭 |
| 11 | 방울방울 | 좁/넓 교차 |

### 보스 패턴 (매 50스테이지)

타일 수 +20% 스파이크와 함께 등장하는 시각적으로 압도적인 대형 패턴.

| Stage | 패턴 | 의미 |
|-------|------|------|
| 50 | 거대 피라미드 | 첫 보스, 룰 통달 검증 |
| 100 | 모래시계 | Phase 3 마무리 |
| 150 | 무지개 | 새로운 시각 자극 |
| 200 | 다이아몬드 | 마름모 형태 |
| 250 | 나선 | 회전하는 흐름 |
| 300 | 마스터 | 모든 패턴의 합 (24줄) |

### 데이터 소스

스테이지/맵 정의는 **Google Spreadsheet**에서 관리됩니다.

- `Stage` 시트: `id`, `SpawnTileCount`, `StageLevel(intArray)`
- `StageLevel` 시트: `id`, `StartColumn`, `EndColumn`

`Tools/Google Sheet Data Loader` 에디터 윈도우에서 OAuth2 인증 후 동기화하면 `Assets/Resources/GoogleSheetData/*.json`으로 변환되어 런타임에 로드됩니다. 밸런싱 변경은 **코드 빌드 없이 시트 편집만으로 즉시 반영** 가능합니다.

<br>

## 스킬 (아이템) 목록

각 스테이지 시작 시 모든 아이템을 3개씩 보유합니다.

| 아이템 | 종류 | 설명 |
|--------|------|------|
| **AddTiles** | 즉시 | 보드 빈 셀에 새 숫자 타일을 일괄 스폰 (배치당 20개) |
| **BreakOneTile** | 타겟 1단계 | 무장 후 타일 1개 클릭 → 해당 타일 제거 |
| **LineSwap** | 타겟 2단계 | 무장 후 첫 행 클릭 → 둘째 행 클릭 → 두 행 통째로 교체 (같은 shape의 행끼리만 가능) |
| **DiagonalClear** | 타겟 1단계 | 무장 후 셀 1개 클릭 → 해당 셀이 속한 ↖↘ 대각선 위 모든 타일 일괄 제거 |

**의도**:
- `AddTiles`는 진행 막힘 해소(실패 회피)
- `BreakOneTile`은 한 점 핀포인트 정리
- `LineSwap`은 보드 전체 구도 변경(전략적 자유도)
- `DiagonalClear`는 광역 정리 + 대각선이라는 NumberTiles 매칭 룰의 강조

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

현재 구현 아이템: `AddTiles`, `BreakOneTile`, `LineSwap`, `DiagonalClear` (자세한 동작은 [스킬 목록](#스킬-아이템-목록) 참조)

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
│   ├── BreakOneTileItem.cs
│   ├── LineSwapItem.cs
│   └── DiagonalClearItem.cs
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
