using System.Collections.Generic;
using System.Linq;
using System.Text;
using TiraWantToCross.GameLogic;
using TiraWantToCross.Stage;
using UnityEngine;

namespace TiraWantToCross.DebugTools
{
    public class StageDataDebugLogger : MonoBehaviour
    {
        private void Start()
        {
            var targets = new[] { "stage_001", "stage_002", "stage_003" };
            foreach (var stageId in targets)
            {
                RunDebugScenario(stageId);
            }
        }

        private static void RunDebugScenario(string stageId)
        {
            var stageData = StageDataLoader.LoadByStageId(stageId);
            if (stageData == null)
            {
                Debug.LogWarning($"[StageDebug] ステージ読み込み失敗: {stageId}");
                return;
            }

            var state = new RiverCrossingGameState(stageData);
            Debug.Log($"[StageDebug] ===== {stageData.stageId} ({stageData.title}) =====");
            Debug.Log(BuildStateLog("初期状態", state));

            foreach (var move in BuildScenarioMoves(stageId))
            {
                var result = state.TryMove(move.routeId, move.passengerIds);
                Debug.Log($"[StageDebug] Move route={move.routeId}, passengers=[{string.Join(",", move.passengerIds)}], result={result.Succeeded}, message={result.Message}");
                Debug.Log(BuildStateLog("移動後状態", state));
            }
        }

        private static string BuildStateLog(string header, RiverCrossingGameState state)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[StageDebug] --- {header} ---");
            sb.AppendLine($"boat: {state.BoatLocation}");
            foreach (var entity in state.EntityLocations.OrderBy(x => x.Key))
            {
                sb.AppendLine($"entity {entity.Key}: {entity.Value}");
            }

            sb.AppendLine($"moves: {state.MoveCount}");
            sb.AppendLine($"cleared: {state.IsCleared}");
            sb.AppendLine($"failed: {state.IsFailed}");
            sb.AppendLine($"withinOptimal: {state.IsWithinOptimalMoves()}");
            sb.AppendLine($"exactOptimal: {state.IsExactlyOptimalMoves()}");
            return sb.ToString();
        }

        private static IReadOnlyList<DebugMoveCommand> BuildScenarioMoves(string stageId)
        {
            if (stageId == "stage_001")
            {
                return new[]
                {
                    new DebugMoveCommand("lr", "tira_1", "tira_2")
                };
            }

            if (stageId == "stage_002")
            {
                return new[]
                {
                    new DebugMoveCommand("lr", "tira_1", "tira_2"),
                    new DebugMoveCommand("lr", "tira_1"),
                    new DebugMoveCommand("lr", "tira_1", "tira_3")
                };
            }

            return new[]
            {
                new DebugMoveCommand("lr", "tira_2"),
                new DebugMoveCommand("lr", "tira_leader", "tira_2"),
                new DebugMoveCommand("lr", "tira_leader"),
                new DebugMoveCommand("lr", "tira_leader", "tira_3")
            };
        }

        private readonly struct DebugMoveCommand
        {
            public string routeId { get; }
            public IReadOnlyList<string> passengerIds { get; }

            public DebugMoveCommand(string routeId, params string[] passengerIds)
            {
                this.routeId = routeId;
                this.passengerIds = passengerIds;
            }
        }
    }
}
