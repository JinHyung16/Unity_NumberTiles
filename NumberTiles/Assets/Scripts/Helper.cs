using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace NTGame
{
    public static class Helper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CombineHashCode(int h1, int h2)
        {
            unchecked
            {
                // (h1 * prime) + h2
                return (h1 * 397) ^ h2;
            }
        }

        public static string GetJsonDataPath()
        {
            return Path.Combine(Application.persistentDataPath, "JsonData");
        }

        public static bool TryReadTextFromJsonData(string fileName, out string content)
        {
            content = null;
            if (string.IsNullOrEmpty(fileName))
                return false;

            try
            {
                string path = Path.Combine(GetJsonDataPath(), fileName);
                if (File.Exists(path) == false)
                    return false;

                content = File.ReadAllText(path);
                return string.IsNullOrEmpty(content) == false;
            }
            catch
            {
                content = null;
                return false;
            }
        }

        public static bool TryWriteTextToJsonData(string fileName, string content, string logTag)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            try
            {
                string folderPath = GetJsonDataPath();
                Directory.CreateDirectory(folderPath);
                File.WriteAllText(Path.Combine(folderPath, fileName), content);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{logTag}] 저장 실패: {GetJsonDataPath()} ({e.GetType().Name}) {e.Message}");
                return false;
            }
        }

        public static void TryDeleteFromJsonData(string fileName, string logTag)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            try
            {
                string fullPath = Path.Combine(GetJsonDataPath(), fileName);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{logTag}] 삭제 실패: {GetJsonDataPath()}/{fileName} ({e.GetType().Name}) {e.Message}");
            }
        }
    }
}

