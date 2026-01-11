using System;
using System.IO;
using UnityEngine;

namespace NTGame
{
    [Serializable]
    public class GameMetaData
    {
        public int Version = 1;
        public long SavedAtUnixMs;
        public int ClearedStageKey;
        public int LastStageKey;
    }
    

    public static class GameMetaSaver
    {
        static readonly string MetaFileName = "GameMeta.json";

        public static bool TryLoad(out GameMetaData data)
        {
            data = null;

            if (Helper.TryReadTextFromJsonData(MetaFileName, out string json) == false)
                return false;

            try
            {
                data = JsonUtility.FromJson<GameMetaData>(json);
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameMetaSaver] 로드 실패: {MetaFileName} ({e.GetType().Name}) {e.Message}");
                data = null;
                return false;
            }
        }

        public static bool Save(GameMetaData data)
        {
            if (data == null)
                return false;

            data.SavedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = JsonUtility.ToJson(data, true);
            return Helper.TryWriteTextToJsonData(MetaFileName, json, "GameMetaSaver");
        }

        public static void EnsureCreated()
        {
            if (TryLoad(out var _))
                return;

            Save(new GameMetaData
            {
                ClearedStageKey = 0,
                LastStageKey = 0
            });
        }

        public static void Reset()
        {
            Save(new GameMetaData
            {
                ClearedStageKey = 0,
                LastStageKey = 0
            });
        }

        public static void UpdateLastStage(int stageKey)
        {
            if (stageKey <= 0)
                return;

            if (TryLoad(out var data) == false)
            {
                data = new GameMetaData();
            }

            data.LastStageKey = stageKey;
            Save(data);
        }

        public static void UpdateClearedStage(int stageKey)
        {
            if (stageKey <= 0)
                return;

            if (TryLoad(out var data) == false)
            {
                data = new GameMetaData();
            }

            if (stageKey > data.ClearedStageKey)
                data.ClearedStageKey = stageKey;

            Save(data);
        }

        public static int GetNextStageAfterClearOrDefault(int fallbackStageKey)
        {
            int fallback = Mathf.Max(1, fallbackStageKey);

            if (TryLoad(out var data) == false)
                return fallback;

            if (data.ClearedStageKey <= 0)
                return fallback;

            return Mathf.Max(1, data.ClearedStageKey + 1);
        }

    }
}

