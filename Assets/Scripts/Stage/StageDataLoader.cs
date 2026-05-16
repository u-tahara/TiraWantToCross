using System;
using System.Collections.Generic;
using UnityEngine;

namespace TiraWantToCross.Stage
{
    public static class StageDataLoader
    {
        private const string StageResourcePath = "Data/Stages";

        public static IReadOnlyList<StageData> LoadAllStages()
        {
            var stageList = new List<StageData>();
            var stageAssets = Resources.LoadAll<TextAsset>(StageResourcePath);

            if (stageAssets == null || stageAssets.Length == 0)
            {
                Debug.LogWarning($"[StageDataLoader] ステージデータが存在しません: Resources/{StageResourcePath}");
                return stageList;
            }

            Array.Sort(stageAssets, (left, right) => string.CompareOrdinal(left.name, right.name));

            foreach (var stageAsset in stageAssets)
            {
                var stageData = LoadFromTextAsset(stageAsset);
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

            var stageAsset = Resources.Load<TextAsset>($"{StageResourcePath}/{stageId}");
            if (stageAsset == null)
            {
                Debug.LogWarning($"[StageDataLoader] ステージファイルが見つかりません: Resources/{StageResourcePath}/{stageId}.json");
                return null;
            }

            return LoadFromTextAsset(stageAsset);
        }

        private static StageData LoadFromTextAsset(TextAsset textAsset)
        {
            try
            {
                var stageData = JsonUtility.FromJson<StageData>(textAsset.text);

                if (stageData == null)
                {
                    Debug.LogWarning($"[StageDataLoader] JSONの読み込みに失敗しました: {textAsset.name}");
                    return null;
                }

                return stageData;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StageDataLoader] JSON読み込み中に例外が発生しました: {textAsset.name}\n{ex.Message}");
                return null;
            }
        }
    }
}
