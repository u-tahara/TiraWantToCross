using System.Text;
using TiraWantToCross.Stage;
using UnityEngine;

namespace TiraWantToCross.DebugTools
{
    public class StageDataDebugLogger : MonoBehaviour
    {
        [SerializeField] private bool loadAllStages = true;
        [SerializeField] private string stageId = "stage_001";

        private void Start()
        {
            if (loadAllStages)
            {
                var stageList = StageDataLoader.LoadAllStages();
                Debug.Log($"[StageDataDebugLogger] 読み込みステージ数: {stageList.Count}");

                foreach (var stageData in stageList)
                {
                    Debug.Log(BuildStageLog(stageData));
                }

                return;
            }

            var singleStage = StageDataLoader.LoadByStageId(stageId);
            if (singleStage == null)
            {
                Debug.LogWarning($"[StageDataDebugLogger] ステージ読み込み失敗: {stageId}");
                return;
            }

            Debug.Log(BuildStageLog(singleStage));
        }

        private static string BuildStageLog(StageData stageData)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Stage Data ===");
            sb.AppendLine($"stageId: {stageData.stageId}");
            sb.AppendLine($"title: {stageData.title}");
            sb.AppendLine($"optimalMoves: {stageData.optimalMoves}");

            if (stageData.boat != null)
            {
                sb.AppendLine($"boat.capacity: {stageData.boat.capacity}");
                sb.AppendLine($"boat.startLocation: {stageData.boat.startLocation}");
            }

            sb.AppendLine("locations:");
            if (stageData.locations != null)
            {
                foreach (var location in stageData.locations)
                {
                    sb.AppendLine($"- {location.locationId} ({location.displayName})");
                }
            }

            sb.AppendLine("routes:");
            if (stageData.routes != null)
            {
                foreach (var route in stageData.routes)
                {
                    sb.AppendLine($"- {route.routeId}: {route.from} -> {route.to} (bidirectional: {route.bidirectional})");
                }
            }

            sb.AppendLine("entities:");
            if (stageData.entities != null)
            {
                foreach (var entity in stageData.entities)
                {
                    sb.AppendLine($"- {entity.entityId} ({entity.displayName}), start: {entity.startLocation}, canOperateBoat: {entity.canOperateBoat}");
                }
            }

            sb.AppendLine($"failConditions count: {stageData.failConditions?.Length ?? 0}");
            return sb.ToString();
        }
    }
}
