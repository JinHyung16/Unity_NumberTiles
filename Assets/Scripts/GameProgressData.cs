using System;
using System.Collections.Generic;

namespace NTGame
{
    [Serializable]
    public class GameProgressData
    {
        public int Version = 1;
        public long SavedAtUnixMs;

        public int StageKey;
        public int StageInitialSpawnCount;
        public int StageShapeHash;
        public int Rows;
        public int Cols;
        public int CellCount;

        public int[] Values;
        public bool[] Active;

        public int SpawnCursorIndex;
        public int NextEmptyScanIndex;
        public ItemType PendingTargetItemType;

        public bool[] DigitSeen;
        public bool[] DigitCleared;
        public int[] DigitCount;

        public List<ItemCountData> ItemCounts = new List<ItemCountData>(8);
    }

    [Serializable]
    public struct ItemCountData
    {
        public ItemType ItemType;
        public int Count;
    }
}

