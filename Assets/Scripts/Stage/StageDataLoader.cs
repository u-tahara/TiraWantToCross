using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TiraWantToCross.Stage
{
    public static class StageDataLoader
    {
        private const string StageDirectoryRelativePath = "Data/Stages";

        public static IReadOnlyList<StageData> LoadAllStages()
        {
            var stageList = new List<StageData>();
            var stageDirectoryPath = Path.Combine(Application.dataPath, StageDirectoryRelativePath);

            if (!Directory.Exists(stageDirectoryPath))
            {
                Debug.LogWarning($"[StageDataLoader] ステージディレクトリが存在しません: {stageDirectoryPath}");
                return stageList;
            }

            var jsonFiles = Directory.GetFiles(stageDirectoryPath, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(jsonFiles, StringComparer.Ordinal);

            foreach (var jsonFilePath in jsonFiles)
            {
                var stageData = LoadFromFile(jsonFilePath);
                if (stageData != null)
                {
                    stageList.Add(stageData);
                }
            }

            return stageList;
        }

        public static StageData LoadByStageId(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId))
            {
                Debug.LogWarning("[StageDataLoader] stageId が未指定です。");
                return null;
            }

            var filePath = Path.Combine(Application.dataPath, StageDirectoryRelativePath, $"{stageId}.json");
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[StageDataLoader] ステージファイルが見つかりません: {filePath}");
                return null;
            }

            return LoadFromFile(filePath);
        }

        private static StageData LoadFromFile(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var stageData = JsonUtility.FromJson<StageData>(json);

                if (stageData == null)
                {
                    Debug.LogWarning($"[StageDataLoader] JSONの読み込みに失敗しました: {filePath}");
                    return null;
                }

                return stageData;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StageDataLoader] ファイル読み込み中に例外が発生しました: {filePath}\n{ex.Message}");
                return null;
            }
        }
    }
}
