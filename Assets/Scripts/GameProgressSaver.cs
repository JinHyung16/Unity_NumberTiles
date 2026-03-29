using System;
using System.IO;
using UnityEngine;

namespace NTGame
{
    public static class GameProgressSaver
    {
        static readonly string StageFilePrefix = "Stage_";
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
                CellCount = tileManager.CellCount,
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
            Helper.TryDeleteFromJsonData(fileName, "GameProgressSaver");
        }

        public static void DeleteAll()
        {
            try
            {
                string folder = Helper.GetJsonDataPath();
                if (Directory.Exists(folder) == false)
                    return;

                var files = Directory.GetFiles(folder, $"{StageFilePrefix}*.json");
                if (files == null || files.Length <= 0)
                    return;

                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = Path.GetFileName(files[i]);
                    Helper.TryDeleteFromJsonData(fileName, "GameProgressSaver");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameProgressSaver] DeleteAll 실패 ({e.GetType().Name}) {e.Message}");
            }
        }

        public static bool TryFindMostRecentStageKey(out int stageKey)
        {
            stageKey = 0;

            long bestSavedAt = -1;
            int bestStageKey = 0;

            ScanFolderForStageProgress(Helper.GetJsonDataPath(), ref bestSavedAt, ref bestStageKey);

            if (bestStageKey <= 0)
                return false;

            stageKey = bestStageKey;
            return true;
        }

        static string GetFileName(int stageKey)
        {
            return $"Stage_{stageKey}.json";
        }

        static void ScanFolderForStageProgress(string folderPath, ref long bestSavedAt, ref int bestStageKey)
        {
            try
            {
                if (Directory.Exists(folderPath) == false)
                    return;

                var files = Directory.GetFiles(folderPath, $"{StageFilePrefix}*.json");
                if (files == null || files.Length <= 0)
                    return;

                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                    if (TryParseStageKeyFromFileName(file, out int stageKey) == false)
                        continue;

                    long savedAt = -1;
                    if (TryReadSavedAtUnixMs(file, out long readSavedAt))
                        savedAt = readSavedAt;

                    if (savedAt < 0)
                    {
                        if (bestSavedAt < 0 && stageKey > bestStageKey)
                            bestStageKey = stageKey;
                        continue;
                    }

                    if (savedAt > bestSavedAt)
                    {
                        bestSavedAt = savedAt;
                        bestStageKey = stageKey;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameProgressSaver] 폴더 스캔 실패: {folderPath} ({e.GetType().Name}) {e.Message}");
            }
        }

        static bool TryParseStageKeyFromFileName(string fullPath, out int stageKey)
        {
            stageKey = 0;
            if (string.IsNullOrEmpty(fullPath))
                return false;

            string fileName = Path.GetFileNameWithoutExtension(fullPath);
            if (fileName == null)
                return false;

            if (fileName.StartsWith(StageFilePrefix) == false)
                return false;

            string numStr = fileName.Substring(StageFilePrefix.Length);
            if (int.TryParse(numStr, out int parsed) == false)
                return false;

            if (parsed <= 0)
                return false;

            stageKey = parsed;
            return true;
        }

        static bool TryReadSavedAtUnixMs(string fullPath, out long savedAtUnixMs)
        {
            savedAtUnixMs = 0;
            try
            {
                if (File.Exists(fullPath) == false)
                    return false;

                string json = File.ReadAllText(fullPath);
                if (string.IsNullOrEmpty(json))
                    return false;

                var data = JsonUtility.FromJson<GameProgressData>(json);
                if (data == null)
                    return false;

                savedAtUnixMs = data.SavedAtUnixMs;
                return savedAtUnixMs > 0;
            }
            catch
            {
                savedAtUnixMs = 0;
                return false;
            }
        }

        static bool SaveToJsonDataFolder(GameProgressData data, string fileName)
        {
            string json = JsonUtility.ToJson(data, true);
            return Helper.TryWriteTextToJsonData(fileName, json, "GameProgressSaver");
        }

        static bool TryReadFromJsonDataFolder(string fileName, out string json)
        {
            return Helper.TryReadTextFromJsonData(fileName, out json);
        }
    }
}

