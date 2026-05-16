using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TiraWantToCross.GameLogic;
using TiraWantToCross.Stage;
using UnityEngine;

namespace TiraWantToCross.Prototype
{
    public sealed class PrototypeStageUiController : MonoBehaviour
    {
        private readonly string[] stageIds = { "stage_001", "stage_002", "stage_003" };
        private readonly List<string> selectedEntities = new List<string>();
        private readonly List<string> onboardPassengers = new List<string>();

        private RiverCrossingGameState gameState;
        private StageData stageData;
        private string activeStageId = "stage_001";
        private string lastMessage = "未実行";
        private Vector2 scrollPosition;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!ShouldBootstrap())
            {
                return;
            }

            var existing = FindAnyObjectByType<PrototypeStageUiController>();
            if (existing != null)
            {
                return;
            }

            var go = new GameObject(nameof(PrototypeStageUiController));
            DontDestroyOnLoad(go);
            go.AddComponent<PrototypeStageUiController>();
        }

        private static bool ShouldBootstrap()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }

        private void Start()
        {
            InitializeStage(activeStageId);
        }

        private void InitializeStage(string stageId)
        {
            stageData = StageDataLoader.LoadByStageId(stageId);
            if (stageData == null)
            {
                gameState = null;
                lastMessage = $"ステージ読み込み失敗: {stageId}";
                return;
            }

            activeStageId = stageId;
            gameState = new RiverCrossingGameState(stageData);
            selectedEntities.Clear();
            onboardPassengers.Clear();
            lastMessage = $"初期化完了: {stageData.stageId} ({stageData.title})";
            Debug.Log($"[PrototypeUI] {lastMessage}");
        }

        private void OnGUI()
        {
            const int panelWidth = 560;
            var area = new Rect(16, 16, panelWidth, Screen.height - 32);
            GUILayout.BeginArea(area, GUI.skin.box);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            DrawStageSelector();
            GUILayout.Space(8);

            if (gameState == null || stageData == null)
            {
                GUILayout.Label("ゲーム状態を作成できていません。");
                GUILayout.Label(lastMessage);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            DrawStateSummary();
            GUILayout.Space(8);
            DrawEntityControls();
            GUILayout.Space(8);
            DrawBoatControls();
            GUILayout.Space(8);
            DrawResultSection();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawStageSelector()
        {
            GUILayout.Label("[Stage Select]");
            GUILayout.BeginHorizontal();
            foreach (var stageId in stageIds)
            {
                var style = new GUIStyle(GUI.skin.button);
                if (stageId == activeStageId)
                {
                    style.normal.textColor = Color.green;
                }

                if (GUILayout.Button(stageId, style, GUILayout.Height(28)))
                {
                    InitializeStage(stageId);
                }
            }

            GUILayout.EndHorizontal();
        }

        private void DrawStateSummary()
        {
            GUILayout.Label("[Current State]");
            GUILayout.Label($"stage: {stageData.stageId} / title: {stageData.title}");
            GUILayout.Label($"boat location: {gameState.BoatLocation}");
            GUILayout.Label($"boat capacity: {stageData.boat.capacity}");
            GUILayout.Label($"moves: {gameState.MoveCount} / optimal: {stageData.optimalMoves}");
            GUILayout.Label($"cleared: {gameState.IsCleared} / failed: {gameState.IsFailed} / exactOptimal: {gameState.IsExactlyOptimalMoves()}");
            GUILayout.Label($"onboard passengers: {(onboardPassengers.Count == 0 ? "(none)" : string.Join(", ", onboardPassengers))}");

            var locationsText = new StringBuilder();
            foreach (var location in stageData.locations ?? Array.Empty<LocationData>())
            {
                var entities = gameState.EntityLocations
                    .Where(x => x.Value == location.locationId)
                    .Select(x => x.Key)
                    .OrderBy(x => x)
                    .ToArray();

                locationsText.AppendLine($"- {location.locationId} ({location.displayName}): [{string.Join(", ", entities)}]");
            }

            GUILayout.TextArea(locationsText.ToString(), GUILayout.MinHeight(90));
        }

        private void DrawEntityControls()
        {
            GUILayout.Label("[Entity Select / Board / Unboard]");

            foreach (var entity in stageData.entities ?? Array.Empty<EntityData>())
            {
                GUILayout.BeginHorizontal();

                var isSelected = selectedEntities.Contains(entity.entityId);
                if (GUILayout.Button(isSelected ? $"解除 {entity.entityId}" : $"選択 {entity.entityId}", GUILayout.Width(140)))
                {
                    TogglePassengerSelection(entity.entityId);
                }

                GUILayout.Label($"loc: {gameState.EntityLocations[entity.entityId]}", GUILayout.Width(150));
                GUILayout.Label(entity.canOperateBoat ? "operator" : "passenger");
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("選択キャラを乗船", GUILayout.Height(30)))
            {
                BoardSelectedEntities();
            }

            if (GUILayout.Button("選択キャラを降ろす", GUILayout.Height(30)))
            {
                UnboardSelectedEntities();
            }

            if (GUILayout.Button("全選択解除", GUILayout.Height(30)))
            {
                selectedEntities.Clear();
                lastMessage = "選択解除しました。";
            }

            GUILayout.EndHorizontal();
        }

        private void DrawBoatControls()
        {
            GUILayout.Label("[Boat Move]");
            var availableRoutes = ResolveAvailableRoutes();
            if (availableRoutes.Count == 0)
            {
                GUILayout.Label("現在地から利用可能な route がありません。");
                return;
            }

            foreach (var route in availableRoutes)
            {
                var destination = ResolveDestination(route, gameState.BoatLocation);
                if (GUILayout.Button($"Move ({route.routeId}): {gameState.BoatLocation} -> {destination}", GUILayout.Height(36)))
                {
                    ExecuteMove(route.routeId);
                }
            }
        }

        private void DrawResultSection()
        {
            GUILayout.Label("[Result]");
            GUILayout.TextArea(lastMessage, GUILayout.MinHeight(60));
        }

        private void TogglePassengerSelection(string entityId)
        {
            if (selectedEntities.Contains(entityId))
            {
                selectedEntities.Remove(entityId);
                return;
            }

            selectedEntities.Add(entityId);
        }


        private void BoardSelectedEntities()
        {
            var boarded = 0;
            foreach (var entityId in selectedEntities)
            {
                if (onboardPassengers.Contains(entityId))
                {
                    continue;
                }

                if (onboardPassengers.Count >= stageData.boat.capacity)
                {
                    break;
                }

                if (gameState.EntityLocations[entityId] == gameState.BoatLocation)
                {
                    onboardPassengers.Add(entityId);
                    boarded += 1;
                }
            }

            lastMessage = boarded > 0 ? $"{boarded}体を乗船させました。" : "乗船できるキャラがいません。";
        }

        private void UnboardSelectedEntities()
        {
            var unboarded = 0;
            foreach (var entityId in selectedEntities)
            {
                if (onboardPassengers.Remove(entityId))
                {
                    unboarded += 1;
                }
            }

            lastMessage = unboarded > 0 ? $"{unboarded}体を降ろしました。" : "降ろせるキャラがいません。";
        }
        private void ExecuteMove(string routeId)
        {
            var result = gameState.TryMove(routeId, onboardPassengers);
            if (!result.Succeeded)
            {
                lastMessage = $"Move失敗: {result.Message}";
                Debug.LogWarning($"[PrototypeUI] {lastMessage}");
                return;
            }

            lastMessage = $"Move成功: destination={result.Destination}, cleared={gameState.IsCleared}, failed={gameState.IsFailed}, moves={gameState.MoveCount}, exactOptimal={gameState.IsExactlyOptimalMoves()}";
            Debug.Log($"[PrototypeUI] {lastMessage}");
        }

        private List<RouteData> ResolveAvailableRoutes()
        {
            var list = new List<RouteData>();
            foreach (var route in stageData.routes ?? Array.Empty<RouteData>())
            {
                if (!string.IsNullOrEmpty(ResolveDestination(route, gameState.BoatLocation)))
                {
                    list.Add(route);
                }
            }

            return list;
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
}
