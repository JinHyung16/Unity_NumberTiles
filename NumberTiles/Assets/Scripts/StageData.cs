using System;
using System.Collections.Generic;
using UnityEngine;

namespace NTGame
{
    [CreateAssetMenu(menuName = "NumberTiles/Stage Data", fileName = "StageData")]
    public class StageData : ScriptableObject
    {
        // 가로는 무조건 9
        public const int BoardCols = 9;

        [Serializable]
        public struct RowRange : IEquatable<RowRange>
        {
            [Tooltip("활성 구간 시작, -1이면 전부 비활성.")]
            public int StartCol;

            [Tooltip("활성 구간 끝 Col, -1이면 이 행은 전부 비활성.")]
            public int EndCol;

            public RowRange(int startCol, int endCol)
            {
                StartCol = startCol;
                EndCol = endCol;
            }

            public bool Equals(RowRange other)
            {
                return other.StartCol == StartCol && other.EndCol == EndCol;
            }

            public override bool Equals(object obj)
            {
                return obj is RowRange other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Helper.CombineHashCode(StartCol, EndCol);
            }
        }

        [Serializable]
        public class StageInfo
        {
            [Min(1)]
            public int StageKey = 1;

            [Header("Board (Initial Spawn)")]
            [Tooltip("초기 보드에 채울 '타일(숫자)' 개수.\n- ActiveRanges를 따라 row-major로 채웁니다.\n- Rows는 이 값에 맞춰 자동 계산됩니다.")]
            [Min(14)]
            public int InitialSpawnTileCount = 9;

            [Header("Shape Ranges")]
            [SerializeField]
            private List<RowRange> _activeRanges = new List<RowRange>();

            public List<RowRange> ActiveRanges => _activeRanges;
        }

        [SerializeField] 
        private List<StageInfo> _stageList = new List<StageInfo>();

        public bool TryGetStage(int stageKey, out StageInfo stage)
        {
            stage = null;
            if (_stageList == null)
                return false;

            for (int i = 0; i < _stageList.Count; i++)
            {
                var stg = _stageList[i];
                if (stg == null) 
                    continue;

                if (stg.StageKey != stageKey) 
                    continue;

                stage = stg;
                return true;
            }

            return false;
        }

        public static bool IsCellActive(StageInfo stage, int row, int col)
        {
            if (stage == null)
                return false;

            if (row < 0)
                return false;

            if (col < 0 || col >= BoardCols)
                return false;

            var activeRanges = stage.ActiveRanges;
            if (activeRanges != null && activeRanges.Count > 0)
            {
                var rowRange = activeRanges[row % activeRanges.Count];
                if (rowRange.StartCol < 0 || rowRange.EndCol < 0)
                    return false;

                return col >= rowRange.StartCol && col <= rowRange.EndCol;
            }

            // ActiveRanges가 없으면 기본은 전체 비활성
            return false;
        }

        void OnValidate()
        {
            if (_stageList == null) 
                return;

            foreach (var stage in _stageList)
            {
                if (stage == null) 
                    continue;

                stage.InitialSpawnTileCount = Mathf.Clamp(stage.InitialSpawnTileCount, 0, 9999);
            }
        }
    }
}
