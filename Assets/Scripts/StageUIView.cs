using System;
using System.Collections.Generic;
using System.Linq;
using TiraWantToCross.GameLogic;
using TiraWantToCross.Stage;
using UnityEngine;

namespace TiraWantToCross.Prototype
{
    public sealed class StageUIView
    {
        private Vector2 scrollPosition;

        public StageUIViewActions Refresh(StageUIViewContext context)
        {
            var actions = StageUIViewActions.None;
            const int panelWidth = 600;

            var area = new Rect(16, 16, panelWidth, Screen.height - 32);
            GUILayout.BeginArea(area, GUI.skin.box);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            actions.StageToLoad = DrawStageSelector(context.StageIds, context.ActiveStageId);
            GUILayout.Space(8);

            if (context.GameState == null || context.StageData == null)
            {
                GUILayout.Label("ゲーム状態を作成できていません。");
                GUILayout.Label(context.LastMessage);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                return actions;
            }

            DrawSummary(context);
            GUILayout.Space(8);
            DrawLocations(context, actions);
            GUILayout.Space(8);
            DrawBoatPanel(context, actions);
            GUILayout.Space(8);
            DrawActionButtons(context, actions);
            GUILayout.Space(8);
            DrawResult(context);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
            return actions;
        }

        private string DrawStageSelector(IReadOnlyList<string> stageIds, string activeStageId)
        {
            GUILayout.Label("[Stage Select]");
            GUILayout.BeginHorizontal();
            string stageToLoad = null;
            foreach (var stageId in stageIds)
            {
                var style = new GUIStyle(GUI.skin.button);
                if (stageId == activeStageId)
                {
                    style.normal.textColor = Color.green;
                }

                if (GUILayout.Button(stageId, style, GUILayout.Height(28)))
                {
                    stageToLoad = stageId;
                }
            }

            GUILayout.EndHorizontal();
            return stageToLoad;
        }

        private static void DrawSummary(StageUIViewContext context)
        {
            GUILayout.Label("[State Summary]");
            GUILayout.Label($"stage: {context.StageData.stageId} / title: {context.StageData.title}");
            GUILayout.Label($"moves: {context.GameState.MoveCount} / optimalMoves: {context.StageData.optimalMoves}");
            GUILayout.Label($"cleared: {context.GameState.IsCleared} / failed: {context.GameState.IsFailed} / exactOptimal: {context.GameState.IsExactlyOptimalMoves()}");
        }

        private static void DrawLocations(StageUIViewContext context, StageUIViewActions actions)
        {
            GUILayout.Label("[Locations]");
            foreach (var location in context.StageData.locations ?? Array.Empty<LocationData>())
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"LocationPanel: {location.locationId} ({location.displayName})");
                GUILayout.BeginHorizontal();

                var entitiesAtLocation = (context.StageData.entities ?? Array.Empty<EntityData>())
                    .Where(entity => context.GameState.EntityLocations[entity.entityId] == location.locationId)
                    .OrderBy(entity => entity.entityId)
                    .ToArray();

                if (entitiesAtLocation.Length == 0)
                {
                    GUILayout.Label("(entities none)");
                }

                foreach (var entity in entitiesAtLocation)
                {
                    var isSelected = context.SelectedEntities.Contains(entity.entityId);
                    var displayName = string.IsNullOrWhiteSpace(entity.displayName) ? entity.entityId : entity.displayName;
                    var label = isSelected ? $"[SELECTED] {displayName}" : displayName;
                    var canSelectMore = context.SelectedEntities.Count < context.GameState.BoatCapacity;
                    var shouldDisable = !isSelected && !canSelectMore;
                    GUI.enabled = !shouldDisable;
                    if (GUILayout.Button(label, GUILayout.Width(130), GUILayout.Height(32)))
                    {
                        actions.ToggleSelectEntityId = entity.entityId;
                    }

                    GUI.enabled = true;
                }

                if (context.SelectedEntities.Count >= context.GameState.BoatCapacity)
                {
                    GUILayout.Label("これ以上乗せられません。");
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        private static void DrawBoatPanel(StageUIViewContext context, StageUIViewActions actions)
        {
            GUILayout.Label("[Boat]");
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"boat location: {context.GameState.BoatLocation}");
            GUILayout.Label($"capacity: {context.GameState.BoatCapacity}");
            GUILayout.Label($"selected entities: {(context.SelectedEntities.Count == 0 ? "(none)" : string.Join(", ", context.SelectedEntities))}");
            GUILayout.EndVertical();

            GUILayout.Label("[Boat Routes]");
            if (context.AvailableRoutes.Count == 0)
            {
                GUILayout.Label("現在地から利用可能な route がありません。");
            }

            foreach (var route in context.AvailableRoutes)
            {
                var destination = ResolveDestination(route, context.GameState.BoatLocation);
                if (GUILayout.Button($"Move: {context.GameState.BoatLocation} -> {destination} ({route.routeId})", GUILayout.Height(36)))
                {
                    actions.MoveRouteId = route.routeId;
                }
            }
        }

        private static void DrawActionButtons(StageUIViewContext context, StageUIViewActions actions)
        {
            GUILayout.Label("[Actions]");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("選択解除", GUILayout.Height(32)))
            {
                actions.ClearSelection = true;
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Restart", GUILayout.Height(32)))
            {
                actions.RestartStage = true;
            }

            var canGoToNextStage = CanGoToNextStage(context.GameState);
            GUI.enabled = canGoToNextStage;
            if (GUILayout.Button("NextStage", GUILayout.Height(32)))
            {
                actions.NextStage = true;
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }

        private static void DrawResult(StageUIViewContext context)
        {
            GUILayout.Label("[Result]");
            GUILayout.Label(BuildGameResultSummary(context));
            GUILayout.TextArea(context.LastMessage, GUILayout.MinHeight(60));
        }

        private static string BuildGameResultSummary(StageUIViewContext context)
        {
            if (context.GameState.IsFailed)
            {
                return "FAILED: 失敗しました。Restartしてください。";
            }

            if (CanGoToNextStage(context.GameState))
            {
                return "CLEAR: 最短手数でクリアしました。NextStageに進めます。";
            }

            if (context.GameState.IsCleared && !context.GameState.IsExactlyOptimalMoves())
            {
                return "NOT OPTIMAL: 最短手数ではありません。Restartしてください。";
            }

            return "PLAYING: プレイ中はNextStageに進めません。";
        }

        private static bool CanGoToNextStage(RiverCrossingGameState gameState)
        {
            return gameState != null
                && gameState.IsCleared
                && !gameState.IsFailed
                && gameState.IsExactlyOptimalMoves();
        }

        private static string ResolveDestination(RouteData route, string currentBoatLocation)
        {
            if (route.from == currentBoatLocation)
            {
                return route.to;
            }

            if (route.bidirectional && route.to == currentBoatLocation)
            {
                return route.from;
            }

            return null;
        }
    }

    public sealed class StageUIViewContext
    {
        public IReadOnlyList<string> StageIds { get; }
        public string ActiveStageId { get; }
        public StageData StageData { get; }
        public IReadOnlyDictionary<string, StageData> StageDataById { get; }
        public RiverCrossingGameState GameState { get; }
        public IReadOnlyCollection<string> SelectedEntities { get; }
        public string LastMessage { get; }
        public IReadOnlyList<RouteData> AvailableRoutes { get; }
        public bool IsStageSelectMode { get; }
        public int HighestUnlockedStageIndex { get; }
        public string SelectedStageIdInSelect { get; }

        public StageUIViewContext(
            IReadOnlyList<string> stageIds,
            string activeStageId,
            StageData stageData,
            IReadOnlyDictionary<string, StageData> stageDataById,
            RiverCrossingGameState gameState,
            IReadOnlyCollection<string> selectedEntities,
            string lastMessage,
            IReadOnlyList<RouteData> availableRoutes,
            bool isStageSelectMode,
            int highestUnlockedStageIndex,
            string selectedStageIdInSelect)
        {
            StageIds = stageIds;
            ActiveStageId = activeStageId;
            StageData = stageData;
            StageDataById = stageDataById;
            GameState = gameState;
            SelectedEntities = selectedEntities;
            LastMessage = lastMessage;
            AvailableRoutes = availableRoutes;
            IsStageSelectMode = isStageSelectMode;
            HighestUnlockedStageIndex = highestUnlockedStageIndex;
            SelectedStageIdInSelect = selectedStageIdInSelect;
        }
    }

    public sealed class StageUIViewActions
    {
        public static StageUIViewActions None => new StageUIViewActions();

        public string StageToLoad { get; set; }
        public string ToggleSelectEntityId { get; set; }
        public string MoveRouteId { get; set; }
        public bool ClearSelection { get; set; }
        public bool RestartStage { get; set; }
        public bool NextStage { get; set; }
    }
}
