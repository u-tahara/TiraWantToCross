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
            DrawActionButtons(actions);
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
                    var label = isSelected ? $"[SELECTED] {entity.entityId}" : entity.entityId;
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
            GUILayout.Label($"onboard entities: {(context.OnboardPassengers.Count == 0 ? "(none)" : string.Join(", ", context.OnboardPassengers))}");
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

        private static void DrawActionButtons(StageUIViewActions actions)
        {
            GUILayout.Label("[Actions]");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("乗船", GUILayout.Height(32)))
            {
                actions.BoardSelected = true;
            }

            if (GUILayout.Button("降船", GUILayout.Height(32)))
            {
                actions.UnboardSelected = true;
            }

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

            if (GUILayout.Button("NextStage", GUILayout.Height(32)))
            {
                actions.NextStage = true;
            }

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
                return "FAILED";
            }

            if (context.GameState.IsCleared && context.GameState.IsExactlyOptimalMoves())
            {
                return "CLEAR";
            }

            if (context.GameState.IsCleared && !context.GameState.IsExactlyOptimalMoves())
            {
                return "NOT OPTIMAL";
            }

            return "PLAYING";
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
        public RiverCrossingGameState GameState { get; }
        public IReadOnlyCollection<string> SelectedEntities { get; }
        public IReadOnlyCollection<string> OnboardPassengers { get; }
        public string LastMessage { get; }
        public IReadOnlyList<RouteData> AvailableRoutes { get; }

        public StageUIViewContext(
            IReadOnlyList<string> stageIds,
            string activeStageId,
            StageData stageData,
            RiverCrossingGameState gameState,
            IReadOnlyCollection<string> selectedEntities,
            IReadOnlyCollection<string> onboardPassengers,
            string lastMessage,
            IReadOnlyList<RouteData> availableRoutes)
        {
            StageIds = stageIds;
            ActiveStageId = activeStageId;
            StageData = stageData;
            GameState = gameState;
            SelectedEntities = selectedEntities;
            OnboardPassengers = onboardPassengers;
            LastMessage = lastMessage;
            AvailableRoutes = availableRoutes;
        }
    }

    public sealed class StageUIViewActions
    {
        public static StageUIViewActions None => new StageUIViewActions();

        public string StageToLoad { get; set; }
        public string ToggleSelectEntityId { get; set; }
        public string MoveRouteId { get; set; }
        public bool BoardSelected { get; set; }
        public bool UnboardSelected { get; set; }
        public bool ClearSelection { get; set; }
        public bool RestartStage { get; set; }
        public bool NextStage { get; set; }
    }
}
