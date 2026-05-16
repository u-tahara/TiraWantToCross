using System;
using System.Collections.Generic;
using System.Linq;
using TiraWantToCross.Stage;

namespace TiraWantToCross.GameLogic
{
    public sealed class RiverCrossingGameState
    {
        private readonly Dictionary<string, EntityData> entityMaster;
        private readonly List<RouteData> routes;

        public StageData StageData { get; }
        public Dictionary<string, string> EntityLocations { get; }
        public string BoatLocation { get; private set; }
        public int MoveCount { get; private set; }
        public bool IsFailed { get; private set; }
        public bool IsCleared { get; private set; }

        public RiverCrossingGameState(StageData stageData)
        {
            StageData = stageData ?? throw new ArgumentNullException(nameof(stageData));
            BoatLocation = stageData.boat.startLocation;
            MoveCount = 0;
            IsFailed = false;
            IsCleared = false;

            entityMaster = (stageData.entities ?? Array.Empty<EntityData>()).ToDictionary(x => x.entityId, x => x);
            routes = new List<RouteData>(stageData.routes ?? Array.Empty<RouteData>());

            EntityLocations = new Dictionary<string, string>();
            foreach (var entity in stageData.entities ?? Array.Empty<EntityData>())
            {
                EntityLocations[entity.entityId] = entity.startLocation;
            }

            EvaluateState();
        }

        public MoveResult TryMove(string routeId, IReadOnlyList<string> passengers)
        {
            if (IsFailed || IsCleared)
            {
                return MoveResult.Fail("ゲーム終了後は移動できません。");
            }

            if (string.IsNullOrWhiteSpace(routeId))
            {
                return MoveResult.Fail("routeId が未指定です。");
            }

            var passengerList = passengers?.Distinct().ToList() ?? new List<string>();
            if (passengerList.Count == 0)
            {
                return MoveResult.Fail("最低1キャラクターを乗船させてください。");
            }

            if (passengerList.Count > StageData.boat.capacity)
            {
                return MoveResult.Fail($"最大乗船人数を超えています。capacity={StageData.boat.capacity}, request={passengerList.Count}");
            }

            foreach (var entityId in passengerList)
            {
                if (!entityMaster.ContainsKey(entityId))
                {
                    return MoveResult.Fail($"未知の entityId です: {entityId}");
                }

                if (EntityLocations[entityId] != BoatLocation)
                {
                    return MoveResult.Fail($"{entityId} はボート地点({BoatLocation})にいません。");
                }
            }

            if (IsOperatorRequired() && !passengerList.Any(id => entityMaster[id].canOperateBoat))
            {
                return MoveResult.Fail("このステージでは操船可能キャラクターが必要です。");
            }

            var route = routes.Find(x => x.routeId == routeId);
            if (route == null)
            {
                return MoveResult.Fail($"routeId が存在しません: {routeId}");
            }

            var nextLocation = ResolveDestination(route, BoatLocation);
            if (string.IsNullOrEmpty(nextLocation))
            {
                return MoveResult.Fail($"現在地 {BoatLocation} から route {routeId} は利用できません。");
            }

            foreach (var entityId in passengerList)
            {
                EntityLocations[entityId] = nextLocation;
            }

            BoatLocation = nextLocation;
            MoveCount += 1;
            EvaluateState();

            return MoveResult.Success(nextLocation);
        }

        public bool IsWithinOptimalMoves()
        {
            return MoveCount <= StageData.optimalMoves;
        }

        public bool IsExactlyOptimalMoves()
        {
            return MoveCount == StageData.optimalMoves;
        }

        private void EvaluateState()
        {
            IsFailed = EvaluateFailConditions();
            IsCleared = !IsFailed && EvaluateClearConditions();
        }

        private bool EvaluateClearConditions()
        {
            var clearConditions = StageData.clearConditions ?? Array.Empty<ClearConditionData>();
            if (clearConditions.Length == 0)
            {
                return false;
            }

            foreach (var goal in clearConditions)
            {
                if (goal.conditionType == "all_entities_at_location")
                {
                    var isAllAtTarget = EntityLocations.Values.All(loc => loc == goal.targetLocationId);
                    if (!isAllAtTarget)
                    {
                        return false;
                    }

                    continue;
                }

                return false;
            }

            return true;
        }

        private bool EvaluateFailConditions()
        {
            foreach (var condition in StageData.failConditions ?? Array.Empty<FailConditionData>())
            {
                if (condition.conditionType == "entity_alone_with" && condition.entityIds != null && !string.IsNullOrEmpty(condition.locationId))
                {
                    var targetsAtLocation = EntityLocations.Where(x => x.Value == condition.locationId).Select(x => x.Key).ToHashSet();
                    if (condition.entityIds.All(targetsAtLocation.Contains))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string ResolveDestination(RouteData route, string boatLocation)
        {
            if (route.from == boatLocation)
            {
                return route.to;
            }

            if (route.bidirectional && route.to == boatLocation)
            {
                return route.from;
            }

            return null;
        }

        private bool IsOperatorRequired()
        {
            return (StageData.entities ?? Array.Empty<EntityData>()).Any(entity => entity.canOperateBoat);
        }
    }

    public readonly struct MoveResult
    {
        public bool Succeeded { get; }
        public string Message { get; }
        public string Destination { get; }

        private MoveResult(bool succeeded, string message, string destination)
        {
            Succeeded = succeeded;
            Message = message;
            Destination = destination;
        }

        public static MoveResult Success(string destination)
        {
            return new MoveResult(true, "OK", destination);
        }

        public static MoveResult Fail(string message)
        {
            return new MoveResult(false, message, null);
        }
    }
}
