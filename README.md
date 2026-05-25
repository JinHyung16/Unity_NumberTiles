# NumberTiles (넘버 팡)

Unity 기반 모바일 숫자 퍼즐 — 1인 기획 / 클라이언트 / 데이터 파이프라인 프로젝트

| 규모 | |
|---|---|
| 스테이지 수 | 300 (전 스테이지 직접 디자인) |
| 라인 종류 (StageLevel) | 41종 (1칸 ~ 9칸) |
| 특별 맵 | 12종 (10스테이지마다 등장) |
| 분기 스테이지 | 6개 (50 / 100 / 150 / 200 / 250 / 300) |
| 스킬 (아이템) | 4종 |

---

## 게임 개요

최근 유행한 '사과 게임' 의 코어 재미를 분석하기 위해 진행한 프로젝트로, 모바일 게임 'Number Tiles' 를 모방하여 코어 재미를 구현했습니다. 분석한 재미를 그대로 두지 않고, 직접 설계한 300 스테이지의 레벨 디자인과 시트 기반 데이터 운영 환경까지 1인으로 완성했습니다.

목표는 단순 클론이 아니라 세 가지를 끝까지 가져가 보는 것이었습니다.

1. 코어 루프 — 매칭/클리어/실패의 재미 분석과 재현
2. 운영 가능한 레벨 디자인 — 300 스테이지의 난이도 곡선과 신선함
3. 데이터 파이프라인 — 코드 빌드 없이 밸런싱이 가능한 워크플로우

---

## 게임 규칙

- 매칭 조건: 두 숫자가 같거나, 합이 10
- 이동 방향: 가로 / 세로 / 대각선 / 행 끝 → 다음 행 시작 (flat row-major)
- 경로 조건: 두 타일 사이 활성 타일이 없어야 연결 가능 (빈 셀은 통과 허용)
- 클리어 조건: 보드에 등장했던 숫자 1~9가 모두 제거됨
- 실패 조건: 줄 추가 아이템이 없고 매칭 가능한 쌍이 남아있지 않음

---

## 분석한 코어 재미

- 같은 숫자 찾기, 합 10 만들기 라는 쉬운 규칙
- 모바일에서는 숫자 2개를 터치하면 되는 조작감 (웹의 드래그 선택과 다른 캐주얼함)
- 맵 모양에 따라 달라지는 새로움
- 어떤 줄을 먼저 정리할지 판단하는 전략적 사고
- 메인 메뉴 없이 바로 플레이로 진입해 진입 장벽을 낮춤

---

## 주요 기능 구현

### Google Sheets 기반 데이터 파이프라인

코드 빌드 없이 시트만 고치면 게임에 반영되는 환경이 결국 이터레이션 속도를 좌우한다고 판단해, 단기 기능 구현보다 이 부분에 시간을 더 투자했습니다.

- Google Cloud Console에서 OAuth2 클라이언트 발급 → Sheets API + Drive API 연동
- 시트 → CSV → JSON 자동 변환, 시트 헤더 기반으로 데이터 컨테이너 클래스와 Enum까지 자동 생성
- Editor 윈도우 (`Tools/Google Sheet Data Loader`) 에서 인증과 동기화를 한 번에 처리
- Refresh token 으로 1회 로그인 후 재인증 불필요

```
Google Spreadsheet ──(OAuth2 / Sheets+Drive API)──► CSV
                                                    │
                                                    ▼
                                       SheetJsonConverter (CSV → JSON)
                                                    │
                                                    ▼
                                Assets/Resources/GoogleSheetData/*.json
                                                    │
                                                    ▼
                              DataManager.GetContainer<T>() (런타임 로드)

                  + DataContainerCodeGenerator (열 정의 기반 컨테이너 클래스 자동 생성)
                  + EnumCodeGenerator (지정 시트 → C# enum 자동 생성)
```

| 시트 | 컬럼 | 역할 |
|------|------|------|
| `Stage` | `id`, `SpawnTileCount`, `StageLevel(intArray)` | 스테이지별 타일 수 + 사용할 라인 패턴 시퀀스 |
| `StageLevel` | `id`, `StartColumn`, `EndColumn` | 한 행의 활성 컬럼 범위 (보드 모양 단위) |

### 300 스테이지 레벨 디자인 — 밸런싱과 맵 구조 설계

처음에는 "난이도를 어떻게 올릴까" 와 "어떻게 안 질리게 할까" 를 따로 풀어야 한다고 봤지만, 둘 다 결국 맵 모양에서 나오는 효과라는 점을 발견했습니다. 좁은 줄/단일 셀이 섞일수록 어려워지고, 모양이 매판 다르면 그 자체로 새로움이 됩니다. 그래서 시트의 `StageLevel` 컬럼 하나만 다듬어도 난이도와 신선함이 같이 움직이는 구조로 정리했습니다.

- 타일 수는 로그 함수로 부드럽게 오르고, 25/50 스테이지마다 보정 계수를 곱해 작은 긴장과 큰 분기를 부여
- 맵 모양 자체가 난이도가 되도록 설계 — 9칸 줄(쉬움) ~ 1칸 단일 셀(어려움)까지 진행에 따라 점진 개방
- 10 스테이지마다 미리 정해둔 모양의 특별 맵 12종을 사이클로 배치해 시각적 환기 부여

#### 타일 수 곡선

```
SpawnTileCount(n) = round( 47.74 * ln((n+20)/21) ) + 30   // 30 → 160
```

| 구간 | 보정 | 의도 |
|------|------|------|
| 평상시 | × 1.00 | 부드럽게 오름 |
| 25스테이지마다 | × 1.10 | 살짝 어려운 판 |
| 50스테이지마다 | × 1.20 | 확 어려워지는 판 — 특별 맵과 같이 등장 |
| 그 다음 판 | × 0.92 | 한숨 돌리는 휴식 |

#### 라인 개방 곡선

스테이지가 올라갈수록 사용하는 라인 종류가 넓은 줄에서 좁은 줄까지 점점 늘어납니다. 타일 수가 같아도 좁은 줄이 섞이면 매칭이 까다로워져 자연스럽게 어려워집니다.

| 구간 | 스테이지 | 사용하는 라인 | 라인 성격 | 한 판당 라인 수 |
|------|---------|--------------|----------|----------------|
| 1 | 1~20 | id 1~5 | 9칸 넓은 줄만 | 2~3 |
| 2 | 21~50 | id 1~10 | 6~8칸 줄 추가 | 3~4 |
| 3 | 51~100 | id 1~21 | 4~5칸 좁은 줄 등장 | 4~6 |
| 4 | 101~175 | id 1~28 | 3칸 줄까지 | 5~8 |
| 5 | 176~250 | id 1~36 | 2칸 줄까지 | 6~9 |
| 6 | 251~300 | id 1~41 | 1칸 단일 셀까지 | 7~11 |

#### 10스테이지마다 특별 맵 12종

평소 스테이지는 그 구간의 라인을 조합해 만들지만, 10스테이지마다는 모양이 정해져 있는 맵 12종을 돌아가며 배치합니다. 룰은 동일하지만 보드 모양이 달라져 패턴 피로를 끊는 역할을 합니다.

| # | 이름 | 모양 |
|---|------|------|
| 0 | 피라미드 | 좁→넓→좁 (대칭) |
| 1 | 모래시계 | 넓→좁→넓 |
| 2 | 사다리 우향 | 위치 슬라이드 |
| 3 | 사다리 좌향 | 반대 슬라이드 |
| 4 | 벽 | 꽉찬 직사각형 |
| 5 | 양옆 절벽 | 좌끝/우끝 번갈아 |
| 6 | 외딴섬 | 한 칸짜리 줄 모음 |
| 7 | 돔 | 가운데가 좁아짐 |
| 8 | 체스판 | 넓은 줄/좁은 줄 교차 |
| 9 | 계단식 좁아짐 | 단조 감소 |
| 10 | 거울 대칭 | 좌우 대칭 |
| 11 | 방울방울 | 좁/넓 교차 |

같은 모양이라도 진행에 따라 줄 수가 점점 늘어납니다. 예를 들어 "벽" 은 초반엔 6~7줄, 후반엔 14줄로 등장해 같은 모티브를 후반까지 재활용하면서 부담은 점점 키웁니다.

#### 분기 스테이지

별도 보스 시스템을 두지 않고, 50스테이지마다 타일 수 +20% + 풀사이즈로 등장하는 특별 맵을 배치해 구간이 바뀐다는 감각을 부여했습니다.

| Stage | 들어간 맵 | 의도 |
|-------|----------|------|
| 50 | 풀사이즈 피라미드 | 첫 큰 분기, 룰 점검 |
| 100 | 풀사이즈 모래시계 | 두 번째 분기 |
| 150 | 무지개 (한 칸 줄까지 동원) | 새로운 시각 자극 |
| 200 | 다이아몬드 | 마름모 모양 |
| 250 | 나선 | 회전하는 흐름 |
| 300 | 마스터 (24줄, 전 패턴 조합) | 엔딩 |

### 숫자 매칭 로직 + Grid 기반 타일 데이터 구조

- 두 타일의 매칭(같은 숫자 또는 합 10)과 경로 차단 여부를 4방향으로 분리해 판별
- 빈 셀 경유는 허용하되 활성 타일은 차단하는 규칙을 방향마다 명시적으로 정의
- 좌표 기반 2D 배열로 보드 상태 관리, 빈 줄 정리와 스폰 인덱스도 좌표 단위로 단순화

| 함수 | 방향 |
|------|------|
| `IsBlockedOnRow` | 가로 |
| `IsBlockedOnCol` | 세로 |
| `IsBlockedOnDiagonal` | 대각선 |
| `IsBlockedOnFlatRowMajor` | 행 끝 → 다음 행 시작 (row-major wrap) |

### 아이템 시스템 — 팩토리 패턴

`IItemFactory` / `ITileItem` 인터페이스로 아이템을 추상화. `TileItemFactory.Create(ItemType)` 가 캐싱된 인스턴스를 반환합니다. 새 아이템 추가 시 `ItemType` enum 값 하나, 클래스 하나, 팩토리 케이스 한 줄만 추가하면 확장됩니다. 즉시 발동형(AddTiles)과 토글로 무장 후 클릭하는 타겟형(BreakOneTile, LineSwap, DiagonalClear) 모두 같은 인터페이스로 처리합니다.

| 아이템 | 사용 방식 | 설명 | 의도 |
|--------|----------|------|------|
| AddTiles | 즉시 | 보드 빈 칸에 새 숫자 타일을 한 번에 스폰 (20개) | 막혔을 때 다시 풀리게 — 실패 회피 |
| BreakOneTile | 토글 → 타일 클릭 | 클릭한 타일 1개 제거 | 한 점만 콕 집어 정리 |
| LineSwap | 토글 → 행 두 번 클릭 | 두 행을 통째로 바꿔치기 | 보드 구도 자체를 바꾸는 전략 카드 |
| DiagonalClear | 토글 → 셀 클릭 | 셀이 속한 ↖↘ 대각선 위 타일 전부 제거 | 광역 정리 + 대각선 매칭 강조 |

토글을 켜면 무장, 끄면 취소됩니다. 다른 토글을 켜면 이전 토글은 자동으로 꺼져 한 번에 하나만 무장됩니다. UI는 매니저를 직접 호출하지 않고 `IListener` 를 통해서만 무장/취소를 요청하며, `_syncing` 가드로 동기화 콜백이 무한 루프를 만들지 않도록 처리했습니다.

### UI 구조 — 옵저버 패턴으로 커플링 감소

- `ITileObserver` / `TileNotify` 로 보드 변경 이벤트를 모든 구독 UI에 전파. `GameManager` 는 `BoardChanged` / `ItemCountChanged` 로 결과 판정, `TileWindow` 는 셀 단위 알림(`CellValueChanged` / `CellOpenChanged` / `CellSelectedChanged`) 으로 부분 갱신만 수행
- UI 컴포넌트는 `TileManager` 를 직접 호출하지 않고 `IListener` 인터페이스로 액션을 요청 (Component → Window → GameManager → TileManager 위임 구조)
- 팩토리 패턴 + 풀링으로 UI 프리팹을 미리 로드해 런타임 `Instantiate` 호출 최소화
- 결과적으로 새 UI 나 아이템이 추가되어도 코어 매니저 코드를 직접 건드릴 필요가 없는 구조

```
TileManager ──(TileNotify)──► ITileObserver (GameManager, TileWindow, StageItemGroup)
```

### 진행 상황 자동 저장/복원

`GameProgressData` (JSON) 에 보드 상태와 함께 **shape hash** 를 저장합니다. 앱 재시작 시 시트 변경으로 스테이지 형태가 달라졌다면 복원을 거부하고 새로 시작 — 데이터 변경에 따른 충돌을 방지합니다.

```
GameProgressSaver.Build()    → JsonUtility.ToJson() → 로컬 파일
GameProgressSaver.TryLoad()  → TileManager.TryApplyProgress() → shape hash 검증 → 복원
```

### GC / 풀링 최적화

- `PoolManager` 가 `TileUIComponent` 를 `Stack<T>` 기반으로 관리. 사전 워밍(Prewarm 100개)으로 런타임 `Instantiate` 호출 최소화
- 랜덤 스폰 후보 배열(`_digitCandidates`)을 멤버 변수로 사전 할당 → 핫패스의 `new int[]` 제거
- `List<int>` → `Queue<int>` 로 AddTiles 큐의 Dequeue O(n) → O(1) 개선
- 숫자 추적 배열(`_digitSeen` / `_digitCleared` / `_digitCount`)을 `Array.Clear` 로 재사용

---

## 기술 스택

- Unity (Mobile / 2D UI)
- C# / .NET — Unity C# 코딩 컨벤션 적용
- DOTween — UI 애니메이션
- Google Sheets API + OAuth2 — 데이터 운영 파이프라인 (Editor 도구)
- Node.js (npm scripts) — Sheets MCP 서버 / 스테이지 데이터 자동 생성기

---

## 프로젝트 구조

```
Assets/Scripts/
├── TileManager.cs                  # 핵심 게임 로직 (보드, 매칭, 스폰, 승패 판정, 아이템 효과)
├── GameManager.cs                  # 씬 흐름 / 윈도우 전환 / 매니저 위임
├── EnumTypes.cs                    # ItemType, GameResultType, LineSwapHighlightType
├── GameProgressData.cs             # 저장 데이터 구조체 (shape hash 포함)
├── GameProgressSaver.cs            # 진행 상황 저장 / 불러오기
├── GameMetaSaver.cs                # 클리어 이력 / 메타 데이터
├── PoolingManager.cs               # TileUIComponent 오브젝트 풀
├── TileNotify.cs                   # Observer 이벤트 정의
├── ItemFactory/
│   ├── ItemFactoryInterfaces.cs
│   ├── TileItemFactory.cs
│   ├── AddTilesItem.cs
│   ├── BreakOneTileItem.cs
│   ├── LineSwapItem.cs
│   └── DiagonalClearItem.cs
├── GoogleSheetDataLoader/
│   ├── Editor/                     # OAuth2, Sheets API, JSON 컨버터, 코드 생성기
│   └── Runtime/
└── UI/
    ├── TileWindow.cs
    ├── TileUIComponent.cs
    ├── StageItemGroupComponent.cs
    ├── LobbyWindow.cs
    ├── GameResultWindow.cs
    └── ...
```

---

## 설계 철학 & 기술 성장

### 변수 하나로 두 역할

처음에는 난이도와 신선함을 각각의 변수로 풀어야 한다고 생각했지만, 결국 둘 다 맵 모양에서 나오는 효과였습니다. 시트의 `StageLevel` 컬럼 하나만 다듬어도 난이도와 신선함이 같이 움직이는 구조로 정리했고, 변수가 적을수록 1인이 끝까지 가져가기 쉬운 형태가 된다고 판단했습니다.

### 운영까지 고려한 데이터 환경

단순 데이터 임포트가 아니라 OAuth2 인증 → API 통신 → JSON 변환 → 컨테이너 코드 자동 생성까지 하나의 흐름으로 묶었습니다. 코드 빌드 없이 밸런싱이 가능한 환경이 이터레이션 속도를 좌우한다고 봤기 때문에, 기능 구현보다 이 인프라 쪽에 시간을 더 썼습니다.

### UI / 로직 / 데이터 분리 경험

- 옵저버 패턴으로 보드 상태 변경을 UI 가 알아서 따라오게 분리
- 팩토리 패턴으로 아이템과 UI 프리팹을 같은 추상화로 처리
- Manager 와 Window 사이는 `IListener` 위임으로 직접 묶이지 않게 처리
- 결과적으로 새 아이템 / UI / 스테이지 추가가 코어 로직 변경 없이 가능

---

## 담당 영역 (1인 프로젝트)

| 영역 | 내용 |
|------|------|
| 기획 | 코어 루프 분석, 300 스테이지 레벨 디자인, 난이도 곡선과 신선함 설계, 스킬 4종 의도 |
| 클라이언트 | Unity C#, Observer/Factory 패턴, 4방향 매칭, GC 최적화, Object Pooling, 토글/무장 동기화 |
| 데이터 연동 | OAuth2 로 Google Sheets / Drive API 연동, CSV → JSON 변환, 컨테이너 / enum 코드 자동 생성 |
| 에디터 도구 | `Tools/Google Sheet Data Loader` 윈도우 — 시트만 고치면 게임에 바로 반영 |
