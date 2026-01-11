using System;
using System.IO;
using UnityEngine;

namespace NTGame
{
    public static class GameProgressSaver
    {
        readonly static string JsonFileName = "Progress.json";
        public static bool SaveCurrent()
        {
            var data = Build();
            return SaveToJsonDataFolder(data, GetFileName(data.StageKey));
        }

        public static GameProgressData Build()
        {
            var tileManager = TileManager.Instance;
            var data = new GameProgressData
            {
                SavedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                StageKey = tileManager.CurrentStageKey,
                Rows = tileManager.Rows,
                Cols = tileManager.Cols,
            };

            int total = data.Rows * data.Cols;
            data.Values = new int[total];
            data.Active = new bool[total];

            for (int r = 0; r < data.Rows; r++)
            {
                for (int c = 0; c < data.Cols; c++)
                {
                    int idx = r * data.Cols + c;
                    data.Active[idx] = tileManager.IsActiveCell(r, c);
                    data.Values[idx] = tileManager.GetValue(r, c);
                }
            }

            tileManager.FillProgressExtra(data);
            return data;
        }

        public static bool TryLoad(int stageKey, out GameProgressData data)
        {
            data = null;
            if (stageKey <= 0)
                return false;

            string fileName = GetFileName(stageKey);
            if (TryReadFromJsonDataFolder(fileName, out string json) == false)
                return false;

            try
            {
                data = JsonUtility.FromJson<GameProgressData>(json);
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameProgressSaver] 로드 실패: {fileName} ({e.GetType().Name}) {e.Message}");
                data = null;
                return false;
            }
        }

        public static void Delete(int stageKey)
        {
            if (stageKey <= 0)
                return;

            string fileName = GetFileName(stageKey);
            TryDelete(GetResourcesJsonDataPath(), fileName);
            TryDelete(GetPersistentJsonDataPath(), fileName);

            TryDelete(GetResourcesJsonDataPath(), JsonFileName);
            TryDelete(GetPersistentJsonDataPath(), JsonFileName);
        }

        static string GetFileName(int stageKey)
        {
            return $"Stage_{stageKey}.json";
        }

        static bool SaveToJsonDataFolder(GameProgressData data, string fileName)
        {
            string json = JsonUtility.ToJson(data, true);

            // 요청: Assets/Resources/JsonData
            // 주의: 빌드 런타임에선 쓰기 불가할 수 있어 persistentDataPath로 폴백
            string resourcesPath = GetResourcesJsonDataPath();
            if (TryWrite(resourcesPath, fileName, json))
                return true;

            string fallbackPath = GetPersistentJsonDataPath();
            return TryWrite(fallbackPath, fileName, json);
        }

        static bool TryReadFromJsonDataFolder(string fileName, out string json)
        {
            json = null;

            string resourcesPath = Path.Combine(GetResourcesJsonDataPath(), fileName);
            if (File.Exists(resourcesPath))
            {
                json = File.ReadAllText(resourcesPath);
                return true;
            }

            string persistentPath = Path.Combine(GetPersistentJsonDataPath(), fileName);
            if (File.Exists(persistentPath))
            {
                json = File.ReadAllText(persistentPath);
                return true;
            }

            return false;
        }

        static string GetResourcesJsonDataPath()
        {
            return Path.Combine(Application.dataPath, "Resources", "JsonData");
        }

        static string GetPersistentJsonDataPath()
        {
            return Path.Combine(Application.persistentDataPath, "JsonData");
        }

        static bool TryWrite(string folderPath, string fileName, string content)
        {
            try
            {
                Directory.CreateDirectory(folderPath);
                File.WriteAllText(Path.Combine(folderPath, fileName), content);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameProgressSaver] 저장 실패: {folderPath} ({e.GetType().Name}) {e.Message}");
                return false;
            }
        }

        static void TryDelete(string folderPath, string fileName)
        {
            try
            {
                var fullPath = Path.Combine(folderPath, fileName);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameProgressSaver] 삭제 실패: {folderPath}/{fileName} ({e.GetType().Name}) {e.Message}");
            }
        }
    }
}

