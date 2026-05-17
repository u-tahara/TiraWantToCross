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
        private readonly List<string> stageIds = new List<string>();
        private readonly Dictionary<string, StageData> stageDataById = new Dictionary<string, StageData>();
        private readonly List<string> selectedEntities = new List<string>();
        private readonly List<string> onboardPassengers = new List<string>();

        private RiverCrossingGameState gameState;
        private StageData stageData;
        private string activeStageId;
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
            ReloadStageList();

            if (stageIds.Count == 0)
            {
                gameState = null;
                stageData = null;
                lastMessage = "ステージが見つかりません。Resources/Data/Stages を確認してください。";
                return;
            }

            activeStageId = stageIds[0];
            InitializeStage(activeStageId);
        }

        private void ReloadStageList()
        {
            stageIds.Clear();
            stageDataById.Clear();

            var loadedStages = StageDataLoader.LoadAllStages();
            foreach (var loadedStage in loadedStages)
            {
                if (loadedStage == null || string.IsNullOrWhiteSpace(loadedStage.stageId))
                {
                    continue;
                }

                stageDataById[loadedStage.stageId] = loadedStage;
            }

            stageIds.AddRange(stageDataById.Keys.OrderBy(stageId => stageId, StringComparer.Ordinal));
        }

        private void InitializeStage(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId))
            {
                gameState = null;
                lastMessage = "ステージIDが未指定です。";
                return;
            }

            stageData = stageDataById.TryGetValue(stageId, out var loadedStage)
                ? loadedStage
                : StageDataLoader.LoadByStageId(stageId);

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

        private void LoadNextStage()
        {
            if (stageIds.Count == 0)
            {
                lastMessage = "次に進めるステージがありません。";
                return;
            }

            var currentIndex = stageIds.IndexOf(activeStageId);
            if (currentIndex < 0)
            {
                InitializeStage(stageIds[0]);
                return;
            }

            var next = (currentIndex + 1) % stageIds.Count;
            InitializeStage(stageIds[next]);
        }

        private void TogglePassengerSelection(string entityId)
        {
            if (selectedEntities.Contains(entityId))
            {
                selectedEntities.Remove(entityId);
                return;
            }

            if (gameState != null && selectedEntities.Count >= gameState.BoatCapacity)
            {
                lastMessage = $"これ以上乗せられません。（定員: {gameState.BoatCapacity}）";
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

                if (onboardPassengers.Count >= gameState.BoatCapacity)
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
