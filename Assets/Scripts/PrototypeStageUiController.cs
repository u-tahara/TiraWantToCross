using System;
using System.Collections.Generic;
using System.Linq;
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
        private StageUIView stageUIView;

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
            stageUIView = new StageUIView();
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
            RefreshView();
        }

        private void OnGUI()
        {
            if (stageUIView == null)
            {
                return;
            }

            var context = BuildViewContext();
            var actions = stageUIView.Refresh(context);

            if (actions.StageToLoad != null)
            {
                InitializeStage(actions.StageToLoad);
                return;
            }

            if (!string.IsNullOrEmpty(actions.ToggleSelectEntityId))
            {
                TogglePassengerSelection(actions.ToggleSelectEntityId);
            }

            if (actions.BoardSelected)
            {
                BoardSelectedEntities();
            }

            if (actions.UnboardSelected)
            {
                UnboardSelectedEntities();
            }

            if (actions.ClearSelection)
            {
                selectedEntities.Clear();
                lastMessage = "選択解除しました。";
            }

            if (!string.IsNullOrEmpty(actions.MoveRouteId))
            {
                ExecuteMove(actions.MoveRouteId);
            }

            if (actions.RestartStage)
            {
                InitializeStage(activeStageId);
            }

            if (actions.NextStage)
            {
                LoadNextStage();
            }
        }

        private StageUIViewContext BuildViewContext()
        {
            return new StageUIViewContext(
                stageIds,
                activeStageId,
                stageData,
                gameState,
                selectedEntities,
                onboardPassengers,
                lastMessage,
                ResolveAvailableRoutes());
        }

        private void RefreshView()
        {
            stageUIView?.Refresh(BuildViewContext());
        }

        private void LoadNextStage()
        {
            var currentIndex = Array.IndexOf(stageIds, activeStageId);
            if (currentIndex < 0)
            {
                InitializeStage(stageIds[0]);
                return;
            }

            var next = (currentIndex + 1) % stageIds.Length;
            InitializeStage(stageIds[next]);
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
            if (gameState == null || stageData == null)
            {
                return list;
            }

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
