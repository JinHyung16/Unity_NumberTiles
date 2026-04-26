using System;
using System.Collections.Generic;
using Jinhyeong_GameData;
using UnityEngine;

namespace NTGame
{
    public class TileManager : SceneSingleton<TileManager>
    {
        public const int BoardCols = 9;

        const int AddTilesSpawnPerBatch = 20;
        const int MaxDigit = 9;
        const int DigitArraySize = MaxDigit + 1;
        const int GuardLoopMax = 100000;
        const int InitialItemCount = 3;

        List<ITileObserver> _observerList = new List<ITileObserver>(8);

        bool[] _digitSeen = new bool[DigitArraySize];
        bool[] _digitCleared = new bool[DigitArraySize];
        int[] _digitCount = new int[DigitArraySize];
        int[] _digitCandidates = new int[MaxDigit];

        Dictionary<ItemType, int> _itemCountDict = new Dictionary<ItemType, int>(8);
        Queue<int> _addTilesQueue = new Queue<int>(AddTilesSpawnPerBatch);
        TileItemFactory _itemFactory = new TileItemFactory();

        public int Cols => BoardCols;
        public int Rows => _rows;
        public int CellCount => _cellCount;

        Stage _stage;
        List<StageLevel> _stageLevels = new List<StageLevel>(8);

        int[,] _values;
        bool[,] _active;
        int _rows;
        int _cellCount;
        int[] _rowActiveTileCount;
        int _nextEmptyScanIndex;
        int _spawnCursorIndex;
        ItemType _pendingTargetItemType = ItemType.None;

        public int CurrentStageKey => _stage != null ? _stage.Id : 0;

        public void FillProgressExtra(GameProgressData data)
        {
            data.SpawnCursorIndex = _spawnCursorIndex;
            data.NextEmptyScanIndex = _nextEmptyScanIndex;
            data.PendingTargetItemType = _pendingTargetItemType;

            if (_stage != null)
            {
                data.StageInitialSpawnCount = _stage.SpawnTileCount;
                data.StageShapeHash = CalcStageShapeHash();
            }
            else
            {
                data.StageInitialSpawnCount = 0;
                data.StageShapeHash = 0;
            }

            data.DigitSeen = (bool[])_digitSeen.Clone();
            data.DigitCleared = (bool[])_digitCleared.Clone();
            data.DigitCount = (int[])_digitCount.Clone();

            data.ItemCounts.Clear();
            foreach (var kv in _itemCountDict)
            {
                data.ItemCounts.Add(new ItemCountData
                {
                    ItemType = kv.Key,
                    Count = kv.Value
                });
            }
        }

        public bool TryApplyProgress(int stageKey, GameProgressData data)
        {
            if (data == null)
            {
                return false;
            }

            if (data.Cols != BoardCols)
            {
                return false;
            }

            ResolveStageByKey(stageKey);

            if (_stage != null)
            {
                if (data.StageInitialSpawnCount <= 0)
                {
                    return false;
                }

                if (data.StageInitialSpawnCount != _stage.SpawnTileCount)
                {
                    return false;
                }

                int shapeHash = CalcStageShapeHash();
                if (data.StageShapeHash != shapeHash)
                {
                    return false;
                }
            }

            _rows = Mathf.Max(2, data.Rows);
            _cellCount = data.CellCount > 0 ? data.CellCount : (_rows * Cols);
            _cellCount = Mathf.Clamp(_cellCount, 0, _rows * Cols);
            _values = new int[Rows, Cols];
            _active = new bool[Rows, Cols];
            _rowActiveTileCount = new int[Rows];

            ClearDigitArrays();

            int total = Rows * Cols;
            if (data.Values == null || data.Active == null)
            {
                return false;
            }

            if (data.Values.Length < total || data.Active.Length < total)
            {
                return false;
            }

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    int idx = r * Cols + c;
                    bool isActive = data.Active[idx];
                    int value = data.Values[idx];

                    _active[r, c] = isActive;
                    _values[r, c] = isActive ? value : 0;

                    if (isActive && value > 0)
                    {
                        AddDigitCount(r, value);
                    }
                }
            }

            if (data.DigitSeen != null && data.DigitSeen.Length >= DigitArraySize)
            {
                Array.Copy(data.DigitSeen, _digitSeen, DigitArraySize);
            }

            if (data.DigitCleared != null && data.DigitCleared.Length >= DigitArraySize)
            {
                Array.Copy(data.DigitCleared, _digitCleared, DigitArraySize);
            }

            if (data.DigitCount != null && data.DigitCount.Length >= DigitArraySize)
            {
                Array.Copy(data.DigitCount, _digitCount, DigitArraySize);
            }

            _itemCountDict.Clear();
            if (data.ItemCounts != null)
            {
                for (int i = 0; i < data.ItemCounts.Count; i++)
                {
                    var it = data.ItemCounts[i];
                    _itemCountDict[it.ItemType] = it.Count;
                }
            }

            _spawnCursorIndex = Mathf.Clamp(data.SpawnCursorIndex, 0, _cellCount);
            _nextEmptyScanIndex = Mathf.Max(0, data.NextEmptyScanIndex);
            _pendingTargetItemType = data.PendingTargetItemType;

            _hasFirst = false;
            _first = default;

            NotifyBoardInitAndChanged();
            return true;
        }

        int CalcStageShapeHash()
        {
            int hash = 17;
            hash = (hash * 31) + _stageLevels.Count;
            for (int i = 0; i < _stageLevels.Count; i++)
            {
                StageLevel lv = _stageLevels[i];
                if (lv == null)
                {
                    continue;
                }
                hash = (hash * 31) + lv.StartColumn;
                hash = (hash * 31) + lv.EndColumn;
            }
            return hash;
        }

        void ResolveStageByKey(int stageKey)
        {
            _stage = null;
            _stageLevels.Clear();

            StageContainer stageContainer = DataManager.Instance.GetContainer<StageContainer>();
            if (stageContainer == null)
            {
                return;
            }
            if (stageContainer.TryGet(stageKey, out Stage stage) == false)
            {
                return;
            }
            _stage = stage;

            StageLevelContainer levelContainer = DataManager.Instance.GetContainer<StageLevelContainer>();
            if (levelContainer == null)
            {
                return;
            }
            if (_stage.StageLevel == null)
            {
                return;
            }

            for (int i = 0; i < _stage.StageLevel.Length; i++)
            {
                int levelId = _stage.StageLevel[i];
                if (levelContainer.TryGet(levelId, out StageLevel level) == false)
                {
                    continue;
                }
                _stageLevels.Add(level);
            }
        }

        bool _hasFirst;
        TileCoordStruct _first = default;

        public int GetItemCount(ItemType itemType)
        {
            if (_itemCountDict.TryGetValue(itemType, out var count))
            {
                return count;
            }

            return 0;
        }

        public void ClearObservers()
        {
            _observerList.Clear();
        }

        public void AddObserver(ITileObserver observer)
        {
            if (_observerList.Contains(observer))
            {
                return;
            }

            _observerList.Add(observer);
        }

        public void RemoveObserver(ITileObserver observer)
        {
            _observerList.Remove(observer);
        }

        void Notify(TileNotify notify)
        {
            for (int i = 0; i < _observerList.Count; i++)
            {
                var obs = _observerList[i];
                if (obs == null)
                {
                    continue;
                }

                obs.OnNotify(notify);
            }
        }

        void NotifyBoardInitAndChanged()
        {
            Notify(TileNotify.BoardInit(Rows, Cols));
            Notify(new TileNotify { Type = TileNotifyType.BoardChanged });
        }

        void ClearDigitArrays()
        {
            Array.Clear(_digitSeen, 0, _digitSeen.Length);
            Array.Clear(_digitCleared, 0, _digitCleared.Length);
            Array.Clear(_digitCount, 0, _digitCount.Length);
        }

        public void StartStage(int stageKey)
        {
            ResolveStageByKey(stageKey);

            if (_stage == null)
            {
                Debug.LogError("[TileManager] Stage 데이터가 없습니다.");
                return;
            }

            _cellCount = Mathf.Max(0, _stage.SpawnTileCount);
            _rows = Mathf.Max(2, Mathf.CeilToInt(_cellCount / (float)Cols));
            _values = new int[Rows, Cols];
            _active = new bool[Rows, Cols];
            _rowActiveTileCount = new int[Rows];
            _nextEmptyScanIndex = 0;
            _spawnCursorIndex = _cellCount;
            _pendingTargetItemType = ItemType.None;

            ClearDigitArrays();
            ResetItemState();

            _hasFirst = false;

            ApplyInitialSpawnCount(_cellCount);
            NotifyBoardInitAndChanged();
        }

        void ApplyInitialSpawnCount(int spawnCount)
        {
            int targetCells = Mathf.Max(0, spawnCount);
            if (targetCells <= 0)
                return;

            _cellCount = targetCells;
            _rows = Mathf.Max(2, Mathf.CeilToInt(_cellCount / (float)Cols));
            _values = new int[Rows, Cols];
            _active = new bool[Rows, Cols];
            _rowActiveTileCount = new int[Rows];

            if (_stageLevels.Count <= 0)
            {
                return;
            }

            int cols = Cols;
            for (int idx = 0; idx < _cellCount; idx++)
            {
                int r = idx / cols;
                int c = idx % cols;
                if (r < 0 || r >= Rows)
                {
                    break;
                }

                if (IsInStageShape(r, c) == false)
                {
                    continue;
                }

                int value = UnityEngine.Random.Range(1, DigitArraySize);
                _active[r, c] = true;
                _values[r, c] = value;
                AddDigitCount(r, value);
            }

            _spawnCursorIndex = _cellCount;
        }

        public bool BeginTargetItem(ItemType itemType)
        {
            _pendingTargetItemType = itemType;
            ClearSelection();
            return true;
        }

        public bool UseItem(ItemType itemType)
        {
            if (itemType == ItemType.None)
            {
                return false;
            }

            if (GetItemCount(itemType) <= 0)
            {
                return false;
            }

            var item = _itemFactory.Create(itemType);
            if (item == null)
            {
                return false;
            }

            var output = item.Execute(new TileItemInput { TileManager = this });
            if (output is TileItemOutput outData == false || outData.Success == false)
            {
                return false;
            }

            if (outData.ConsumeOnExecute)
            {
                _itemCountDict[itemType] = Mathf.Max(0, GetItemCount(itemType) - 1);
                Notify(new TileNotify { Type = TileNotifyType.ItemCountChanged, ItemType = itemType, ItemCount = GetItemCount(itemType) });
            }

            return true;
        }

        public int SpawnAddTilesBatch()
        {
            return SpawnAppendAfterLast(AddTilesSpawnPerBatch);
        }

        int SpawnAppendAfterLast(int spawnCount)
        {
            RemoveClearedDigitsFromAddTilesQueue();
            EnsureAddTilesQueueFilled();

            int spawned = 0;
            int toSpawn = Mathf.Max(0, spawnCount);
            for (int i = 0; i < toSpawn; i++)
            {
                if (_addTilesQueue.Count == 0)
                {
                    break;
                }

                while (_addTilesQueue.Count > 0 && IsDigitCleared(_addTilesQueue.Peek()))
                {
                    _addTilesQueue.Dequeue();
                }

                if (_addTilesQueue.Count == 0)
                {
                    break;
                }

                if (TryGetNextAppendCell(out int r, out int c) == false)
                {
                    break;
                }

                int value = _addTilesQueue.Dequeue();
                SpawnValueAt(r, c, value);
                spawned++;
            }

            EnsureAddTilesQueueFilled();
            return spawned;
        }

        bool TryGetNextAppendCell(out int row, out int col)
        {
            int guard = 0;
            while (guard++ < GuardLoopMax)
            {
                EnsureRowsForIndex(_spawnCursorIndex);

                int idx = _spawnCursorIndex;
                int r = idx / Cols;
                int c = idx % Cols;

                _spawnCursorIndex++;

                if (idx >= _cellCount)
                {
                    _cellCount = idx + 1;
                    Notify(TileNotify.CellCountChanged(_cellCount, Rows, Cols));
                }

                if (IsInStageShape(r, c) == false)
                {
                    continue;
                }

                if (_values[r, c] != 0)
                {
                    continue;
                }

                if (_active[r, c] == false)
                {
                    _active[r, c] = true;
                    Notify(new TileNotify
                    {
                        Type = TileNotifyType.CellOpenChanged,
                        Row = r,
                        Col = c,
                        Flag = true
                    });
                }

                row = r;
                col = c;
                return true;
            }

            row = -1;
            col = -1;
            return false;
        }

        void EnsureRowsForIndex(int index)
        {
            int needRows = (index / Cols) + 1;
            if (needRows <= Rows)
            {
                return;
            }

            int oldRows = Rows;
            int newRows = needRows;

            var newValues = new int[newRows, Cols];
            var newActive = new bool[newRows, Cols];

            for (int r = 0; r < oldRows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    newValues[r, c] = _values[r, c];
                    newActive[r, c] = _active[r, c];
                }
            }

            _rows = newRows;
            _values = newValues;
            _active = newActive;

            Array.Resize(ref _rowActiveTileCount, newRows);

            ClearSelection();
            NotifyBoardInitAndChanged();
        }

        bool IsInStageShape(int row, int col)
        {
            if (_stage == null || _stageLevels.Count <= 0)
            {
                return false;
            }

            if (col < 0 || col >= Cols)
            {
                return false;
            }

            if (row < 0)
                return false;

            StageLevel lv = _stageLevels[row % _stageLevels.Count];
            if (lv == null)
            {
                return false;
            }

            int s = lv.StartColumn;
            int e = lv.EndColumn;
            if (s < 0 || e < 0)
            {
                return false;
            }

            return col >= s && col <= e;
        }

        public bool IsActiveCell(int row, int col)
        {
            return InBounds(row, col) && _active[row, col];
        }

        public bool HasTile(int row, int col)
        {
            return InBounds(row, col) && _active[row, col] && _values[row, col] >= 1;
        }

        public int GetValue(int row, int col)
        {
            return InBounds(row, col) ? _values[row, col] : 0;
        }

        public bool IsEmptyActive(int row, int col)
        {
            return InBounds(row, col) && _active[row, col] && _values[row, col] == 0;
        }

        public bool IsDigitCleared(int digit)
        {
            if (digit < 1 || digit > MaxDigit)
            {
                return false;
            }

            return _digitCleared[digit];
        }

        public bool TryGetRandomSpawnDigit(out int digit)
        {
            int count = 0;
            for (int d = 1; d <= MaxDigit; d++)
            {
                if (_digitCleared[d])
                {
                    continue;
                }

                _digitCandidates[count++] = d;
            }

            if (count <= 0)
            {
                digit = 0;
                return false;
            }

            digit = _digitCandidates[UnityEngine.Random.Range(0, count)];
            return true;
        }

        void ResetItemState()
        {
            _itemCountDict.Clear();
            _itemCountDict[ItemType.AddTiles] = InitialItemCount;
            _itemCountDict[ItemType.BreakOneTile] = InitialItemCount;

            _addTilesQueue.Clear();
            EnsureAddTilesQueueFilled();
        }

        void EnsureAddTilesQueueFilled()
        {
            while (_addTilesQueue.Count < AddTilesSpawnPerBatch)
            {
                if (TryGetRandomSpawnDigit(out int digit))
                {
                    _addTilesQueue.Enqueue(digit);
                }
                else
                {
                    _addTilesQueue.Enqueue(1);
                }
            }
        }

        void RemoveClearedDigitsFromAddTilesQueue()
        {
            int count = _addTilesQueue.Count;
            for (int i = 0; i < count; i++)
            {
                int d = _addTilesQueue.Dequeue();
                if (IsDigitCleared(d) == false)
                {
                    _addTilesQueue.Enqueue(d);
                }
            }
        }

        public void ClearSelection()
        {
            if (_hasFirst)
            {
                Notify(new TileNotify
                {
                    Type = TileNotifyType.CellSelectedChanged,
                    Row = _first.Row,
                    Col = _first.Col,
                    Flag = false
                });
            }
            _hasFirst = false;
        }

        public void OnTileClicked(int row, int col)
        {
            if (_pendingTargetItemType == ItemType.BreakOneTile)
            {
                if (HasTile(row, col))
                {
                    BreakTileAt(row, col);
                    _pendingTargetItemType = ItemType.None;
                    return;
                }
                return;
            }

            if (HasTile(row, col) == false)
            {
                return;
            }

            if (_hasFirst == false)
            {
                if (_first.Equals(default) == false)
                {
                    _first = default;
                }

                _first = new TileCoordStruct
                {
                    Row = row,
                    Col = col
                };

                _hasFirst = true;
                Notify(new TileNotify
                {
                    Type = TileNotifyType.CellSelectedChanged,
                    Row = row,
                    Col = col,
                    Flag = true
                });
                return;
            }

            if (_first.Row == row && _first.Col == col)
            {
                ClearSelection();
                return;
            }

            var second = new TileCoordStruct
            {
                Row = row,
                Col = col
            };

            if (CanRemove(_first, second))
            {
                Notify(new TileNotify
                {
                    Type = TileNotifyType.MatchedPair,
                    Row = _first.Row,
                    Col = _first.Col,
                    Row2 = second.Row,
                    Col2 = second.Col
                });

                Notify(new TileNotify
                {
                    Type = TileNotifyType.CellSelectedChanged,
                    Row = _first.Row,
                    Col = _first.Col,
                    Flag = false
                });
                _hasFirst = false;

                RemoveTile(_first.Row, _first.Col);
                RemoveTile(second.Row, second.Col);
                CollapseEmptyRows_RemoveRow();

                Notify(new TileNotify { Type = TileNotifyType.BoardChanged });
                return;
            }

            Notify(new TileNotify
            {
                Type = TileNotifyType.CellSelectedChanged,
                Row = _first.Row,
                Col = _first.Col,
                Flag = false
            });

            Notify(new TileNotify
            {
                Type = TileNotifyType.CellSelectedChanged,
                Row = second.Row,
                Col = second.Col,
                Flag = false
            });

            _first = default;
            _hasFirst = false;
        }

        public bool TryGetNextEmptyActiveCell(out int row, out int col)
        {
            int total = Rows * Cols;
            if (total <= 0)
            {
                row = -1;
                col = -1;
                return false;
            }

            int start = _nextEmptyScanIndex;
            if (start < 0 || start >= total)
            {
                start = 0;
            }

            for (int i = 0; i < total; i++)
            {
                int idx = (start + i) % total;
                int r = idx / Cols;
                int c = idx % Cols;

                if (_active[r, c] == false)
                {
                    continue;
                }

                if (_values[r, c] != 0)
                {
                    continue;
                }

                _nextEmptyScanIndex = (idx + 1) % total;
                row = r;
                col = c;
                return true;
            }

            row = -1;
            col = -1;
            return false;
        }

        public void SpawnValueAt(int row, int col, int value)
        {
            if (IsEmptyActive(row, col) == false)
            {
                return;
            }

            _values[row, col] = value;
            AddDigitCount(row, value);
            Notify(new TileNotify
            {
                Type = TileNotifyType.CellValueChanged,
                Row = row,
                Col = col,
                Value = value
            });
            Notify(new TileNotify { Type = TileNotifyType.BoardChanged });
        }

        public bool CanRemove(TileCoordStruct a, TileCoordStruct b)
        {
            if (HasTile(a.Row, a.Col) == false || HasTile(b.Row, b.Col) == false)
            {
                return false;
            }

            int av = _values[a.Row, a.Col];
            int bv = _values[b.Row, b.Col];

            if (!((av == bv) || (av + bv == 10)))
            {
                return false;
            }

            int dr = b.Row - a.Row;
            int dc = b.Col - a.Col;

            if (dr == 0)
            {
                return IsBlockedOnRow(a, b) == false;
            }

            if (dc == 0)
            {
                return IsBlockedOnCol(a, b) == false;
            }

            // 대각선: |행차| == |열차| (1칸 인접 또는 빈셀 건너뜀)
            if (Math.Abs(dr) == Math.Abs(dc))
            {
                if (Math.Abs(dr) == 1)
                {
                    return true;
                }

                return IsBlockedOnDiagonal(a, b) == false;
            }

            // 줄 연결: 한 줄 끝 → 다음 줄 처음, 빈셀 경유 허용
            return IsBlockedOnFlatRowMajor(a, b) == false;
        }

        bool IsBlockedOnRow(TileCoordStruct a, TileCoordStruct b)
        {
            int row = a.Row;
            int min = Math.Min(a.Col, b.Col);
            int max = Math.Max(a.Col, b.Col);
            for (int c = min + 1; c <= max - 1; c++)
            {
                if (HasTile(row, c))
                {
                    return true;
                }
            }

            return false;
        }

        bool IsBlockedOnCol(TileCoordStruct a, TileCoordStruct b)
        {
            int col = a.Col;
            int min = Math.Min(a.Row, b.Row);
            int max = Math.Max(a.Row, b.Row);
            for (int r = min + 1; r <= max - 1; r++)
            {
                if (HasTile(r, col))
                {
                    return true;
                }
            }

            return false;
        }

        bool IsBlockedOnFlatRowMajor(TileCoordStruct a, TileCoordStruct b)
        {
            int idxA = a.Row * Cols + a.Col;
            int idxB = b.Row * Cols + b.Col;
            int min = Math.Min(idxA, idxB);
            int max = Math.Max(idxA, idxB);

            for (int idx = min + 1; idx <= max - 1; idx++)
            {
                int r = idx / Cols;
                int c = idx % Cols;
                if (HasTile(r, c))
                {
                    return true;
                }
            }

            return false;
        }

        bool IsBlockedOnDiagonal(TileCoordStruct a, TileCoordStruct b)
        {
            int dr = b.Row - a.Row;
            int dc = b.Col - a.Col;
            int steps = Math.Abs(dr);

            int stepR = dr > 0 ? 1 : -1;
            int stepC = dc > 0 ? 1 : -1;

            for (int i = 1; i <= steps - 1; i++)
            {
                int r = a.Row + (stepR * i);
                int c = a.Col + (stepC * i);
                if (HasTile(r, c))
                {
                    return true;
                }
            }

            return false;
        }

        void RemoveTile(int row, int col)
        {
            if (HasTile(row, col) == false)
            {
                return;
            }

            int prev = _values[row, col];
            _values[row, col] = -prev;
            RemoveDigitCount(row, prev);
            Notify(new TileNotify
            {
                Type = TileNotifyType.CellValueChanged,
                Row = row,
                Col = col,
                Value = -prev
            });
        }

        void BreakTileAt(int row, int col)
        {
            ClearSelection();

            RemoveTile(row, col);
            CollapseEmptyRows_RemoveRow();
            Notify(new TileNotify { Type = TileNotifyType.BoardChanged });

            _itemCountDict[ItemType.BreakOneTile] = Mathf.Max(0, GetItemCount(ItemType.BreakOneTile) - 1);

            Notify(new TileNotify
            {
                Type = TileNotifyType.ItemCountChanged,
                ItemType = ItemType.BreakOneTile,
                ItemCount = GetItemCount(ItemType.BreakOneTile)
            });
        }

        void AddDigitCount(int row, int value)
        {
            if (value < 1 || value > MaxDigit)
            {
                return;
            }

            _digitSeen[value] = true;
            _digitCount[value]++;
            _rowActiveTileCount[row]++;
        }

        void RemoveDigitCount(int row, int value)
        {
            if (value < 1 || value > MaxDigit)
            {
                return;
            }

            if (_digitCount[value] > 0)
            {
                _digitCount[value]--;
            }

            _rowActiveTileCount[row] = Mathf.Max(0, _rowActiveTileCount[row] - 1);

            if (_digitCleared[value] == false && _digitSeen[value] && _digitCount[value] == 0)
            {
                _digitCleared[value] = true;
                RemoveClearedDigitsFromAddTilesQueue();
                Notify(new TileNotify
                {
                    Type = TileNotifyType.DigitCleared,
                    Value = value
                });
            }
        }

        void CollapseEmptyRows_RemoveRow()
        {
            bool changed = false;
            int removedRowCount = 0;
            int removedCellCount = 0;
            int curCellCount = _cellCount;
            for (int r = 0; r < Rows; r++)
            {
                if (IsRowEmpty(r) == false)
                {
                    continue;
                }

                if (curCellCount > 0)
                {
                    int lastRowIndex = (curCellCount - 1) / Cols;
                    if (r >= 0 && r <= lastRowIndex)
                    {
                        int removedCells;
                        if (r == lastRowIndex)
                        {
                            removedCells = curCellCount - (lastRowIndex * Cols);
                            removedCells = Mathf.Clamp(removedCells, 0, Cols);
                        }
                        else
                        {
                            removedCells = Cols;
                        }

                        curCellCount = Mathf.Max(0, curCellCount - removedCells);
                        removedCellCount += removedCells;
                    }
                }

                for (int rr = r; rr < Rows - 1; rr++)
                {
                    for (int c = 0; c < Cols; c++)
                    {
                        _active[rr, c] = _active[rr + 1, c];
                        _values[rr, c] = _values[rr + 1, c];
                    }

                    _rowActiveTileCount[rr] = _rowActiveTileCount[rr + 1];
                }

                _rows--;
                changed = true;
                removedRowCount++;

                r--;
            }

            if (changed == false)
            {
                return;
            }

            if (removedRowCount > 0)
            {
                _cellCount = Mathf.Max(0, curCellCount);
                _spawnCursorIndex = Mathf.Min(_spawnCursorIndex, _cellCount);
            }

            if (_rowActiveTileCount.Length != Rows)
            {
                Array.Resize(ref _rowActiveTileCount, Rows);
            }

            int total = Rows * Cols;
            if (total > 0)
            {
                _nextEmptyScanIndex = Mathf.Clamp(_nextEmptyScanIndex, 0, total - 1);
            }
            else
            {
                _nextEmptyScanIndex = 0;
            }

            ClearSelection();
            NotifyBoardInitAndChanged();

            if (removedRowCount > 0)
            {
                Notify(new TileNotify
                {
                    Type = TileNotifyType.LineCleared,
                    Value = removedRowCount
                });
            }
        }

        bool IsRowEmpty(int row)
        {
            if (HasAnyShapeCellInRow(row) == false)
            {
                return false;
            }

            return _rowActiveTileCount[row] <= 0;
        }

        bool HasAnyShapeCellInRow(int row)
        {
            if (row < 0 || row >= Rows)
            {
                return false;
            }

            int baseIdx = row * Cols;
            if (baseIdx >= _cellCount)
            {
                return false;
            }

            int maxCol = Mathf.Min(Cols - 1, (_cellCount - 1) - baseIdx);
            if (maxCol < 0)
            {
                return false;
            }

            for (int c = 0; c <= maxCol; c++)
            {
                if (IsInStageShape(row, c))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsClearStage()
        {
            bool anySeen = false;
            for (int d = 1; d <= MaxDigit; d++)
            {
                if (_digitSeen[d] == false)
                {
                    continue;
                }

                anySeen = true;

                if (_digitCleared[d] == false)
                {
                    return false;
                }
            }

            return anySeen;
        }

        public bool IsFailStage()
        {
            if (GetItemCount(ItemType.AddTiles) > 0)
            {
                return false;
            }

            if (HasAnyTileOnBoard() == false)
            {
                return false;
            }

            if (HasAnyRemovablePair() == true)
            {
                return false;
            }

            return true;
        }

        bool HasAnyTileOnBoard()
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    if (HasTile(r, c))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        bool IsMatchValue(int a, int b)
        {
            return (a == b) || (a + b == 10);
        }

        bool CheckMatchWithCoord(TileCoordStruct a, TileCoordStruct b)
        {
            int av = _values[a.Row, a.Col];
            int bv = _values[b.Row, b.Col];
            if (IsMatchValue(av, bv) == false)
            {
                return false;
            }

            return CanRemove(a, b);
        }

        bool HasAnyRemovablePair()
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    if (HasTile(r, c) == false)
                    {
                        continue;
                    }

                    var a = new TileCoordStruct { Row = r, Col = c };

                    if (TryFindFirstTileOnRow(r, c, -1, out int leftCol))
                    {
                        if (CheckMatchWithCoord(a, new TileCoordStruct { Row = r, Col = leftCol }))
                        {
                            return true;
                        }
                    }

                    if (TryFindFirstTileOnRow(r, c, 1, out int rightCol))
                    {
                        if (CheckMatchWithCoord(a, new TileCoordStruct { Row = r, Col = rightCol }))
                        {
                            return true;
                        }
                    }

                    if (TryFindFirstTileOnCol(r, c, -1, out int upRow))
                    {
                        if (CheckMatchWithCoord(a, new TileCoordStruct { Row = upRow, Col = c }))
                        {
                            return true;
                        }
                    }

                    if (TryFindFirstTileOnCol(r, c, 1, out int downRow))
                    {
                        if (CheckMatchWithCoord(a, new TileCoordStruct { Row = downRow, Col = c }))
                        {
                            return true;
                        }
                    }

                    if (TryFindFirstTileOnDiagonal(r, c, -1, -1, out var ul))
                    {
                        if (CheckMatchWithCoord(a, ul))
                        {
                            return true;
                        }
                    }

                    if (TryFindFirstTileOnDiagonal(r, c, -1, 1, out var ur))
                    {
                        if (CheckMatchWithCoord(a, ur))
                        {
                            return true;
                        }
                    }

                    if (TryFindFirstTileOnDiagonal(r, c, 1, -1, out var dl))
                    {
                        if (CheckMatchWithCoord(a, dl))
                        {
                            return true;
                        }
                    }

                    if (TryFindFirstTileOnDiagonal(r, c, 1, 1, out var diagDr))
                    {
                        if (CheckMatchWithCoord(a, diagDr))
                        {
                            return true;
                        }
                    }

                    if (TryFindFirstTileOnFlatRowMajor(r, c, -1, out var prev))
                    {
                        if (CheckMatchWithCoord(a, prev))
                        {
                            return true;
                        }
                    }

                    if (TryFindFirstTileOnFlatRowMajor(r, c, 1, out var next))
                    {
                        if (CheckMatchWithCoord(a, next))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        bool TryFindFirstTileOnRow(int row, int col, int dir, out int foundCol)
        {
            if (dir == 0)
            {
                foundCol = -1;
                return false;
            }

            int start = col + dir;
            if (start < 0 || start >= Cols)
            {
                foundCol = -1;
                return false;
            }

            for (int c = start; c >= 0 && c < Cols; c += dir)
            {
                if (HasTile(row, c))
                {
                    foundCol = c;
                    return true;
                }
            }

            foundCol = -1;
            return false;
        }

        bool TryFindFirstTileOnCol(int row, int col, int dir, out int foundRow)
        {
            if (dir == 0)
            {
                foundRow = -1;
                return false;
            }

            int start = row + dir;
            if (start < 0 || start >= Rows)
            {
                foundRow = -1;
                return false;
            }

            for (int r = start; r >= 0 && r < Rows; r += dir)
            {
                if (HasTile(r, col))
                {
                    foundRow = r;
                    return true;
                }
            }

            foundRow = -1;
            return false;
        }

        bool TryFindFirstTileOnDiagonal(int row, int col, int stepR, int stepC, out TileCoordStruct found)
        {
            int r = row + stepR;
            int c = col + stepC;
            while (r >= 0 && r < Rows && c >= 0 && c < Cols)
            {
                if (HasTile(r, c))
                {
                    found = new TileCoordStruct { Row = r, Col = c };
                    return true;
                }

                r += stepR;
                c += stepC;
            }

            found = default;
            return false;
        }

        bool TryFindFirstTileOnFlatRowMajor(int row, int col, int dir, out TileCoordStruct found)
        {
            int total = Rows * Cols;
            int idx = (row * Cols) + col;
            int start = idx + dir;
            if (start < 0 || start >= total)
            {
                found = default;
                return false;
            }

            for (int i = start; i >= 0 && i < total; i += dir)
            {
                int r = i / Cols;
                int c = i % Cols;
                if (HasTile(r, c))
                {
                    found = new TileCoordStruct { Row = r, Col = c };
                    return true;
                }
            }

            found = default;
            return false;
        }

        bool InBounds(int row, int col)
        {
            if (row < 0 || row >= Rows || col < 0 || col >= Cols)
            {
                return false;
            }

            int idx = (row * Cols) + col;
            return idx < _cellCount;
        }
    }
}
