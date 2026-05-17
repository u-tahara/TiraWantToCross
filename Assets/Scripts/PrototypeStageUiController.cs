using System;
using System.Collections.Generic;
using System.Linq;
using TiraWantToCross.GameLogic;
using TiraWantToCross.Stage;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TiraWantToCross.UI;

namespace TiraWantToCross.Prototype
{
    public sealed class PrototypeStageUiController : MonoBehaviour
    {
        private readonly List<string> stageIds = new List<string>();
        private readonly Dictionary<string, StageData> stageDataById = new Dictionary<string, StageData>();
        private readonly List<string> selectedEntities = new List<string>();

        private RiverCrossingGameState gameState;
        private StageData stageData;
        private string activeStageId;
        private string lastMessage = "未実行";
        private StageUIView stageUIView;
        private StageCanvasView stageCanvasView;
        private string selectedRouteId;
        [SerializeField] private bool useLegacyOnGui;

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
            SetupCanvasUI();
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
            selectedRouteId = null;
            lastMessage = $"初期化完了: {stageData.stageId} ({stageData.title})";
        }


        private void SetupCanvasUI()
        {
            var canvasGo = new GameObject("PrototypeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasGo);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(eventSystemGo);
            }

            stageCanvasView = new StageCanvasView();
            stageCanvasView.Initialize(canvas.transform,
                entityId => TogglePassengerSelection(entityId),
                ExecuteMoveFromSelectedRoute,
                ClearSelection,
                () => InitializeStage(activeStageId),
                TryLoadNextStage);
        }

        private void Update()
        {
            if (stageCanvasView == null)
            {
                return;
            }

            var context = BuildViewContext();
            if (string.IsNullOrEmpty(selectedRouteId) && context.AvailableRoutes.Count > 0)
            {
                selectedRouteId = context.AvailableRoutes[0].routeId;
            }

            stageCanvasView.Render(context);
        }

        private void ExecuteMoveFromSelectedRoute(string routeId)
        {
            if (!string.IsNullOrEmpty(routeId))
            {
                selectedRouteId = routeId;
            }

            var available = ResolveAvailableRoutes();
            if (available.Count == 0)
            {
                lastMessage = "利用可能なルートがありません。";
                return;
            }

            if (string.IsNullOrEmpty(selectedRouteId) || available.All(x => x.routeId != selectedRouteId))
            {
                selectedRouteId = available[0].routeId;
            }

            ExecuteMove(selectedRouteId);
        }

        private void OnGUI()
        {
            if (!useLegacyOnGui || stageUIView == null)
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

            if (actions.ClearSelection)
            {
                ClearSelection();
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
                TryLoadNextStage();
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
                lastMessage,
                ResolveAvailableRoutes());
        }

        private void TryLoadNextStage()
        {
            if (!CanGoToNextStage())
            {
                lastMessage = "NextStageに進むには、最短手数でクリアする必要があります。";
                return;
            }

            LoadNextStage();
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

        private bool CanGoToNextStage()
        {
            return gameState != null
                && gameState.IsCleared
                && !gameState.IsFailed
                && gameState.IsExactlyOptimalMoves();
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

        private void ExecuteMove(string routeId)
        {
            var result = gameState.TryMove(routeId, selectedEntities);
            if (!result.Succeeded)
            {
                lastMessage = $"Move失敗: {result.Message}";
                Debug.LogWarning($"[PrototypeUI] {lastMessage}");
                return;
            }

            selectedEntities.Clear();
            lastMessage = $"Move成功: destination={result.Destination}, cleared={gameState.IsCleared}, failed={gameState.IsFailed}, moves={gameState.MoveCount}, exactOptimal={gameState.IsExactlyOptimalMoves()} / 移動後、自動で降船しました。";
            Debug.Log($"[PrototypeUI] {lastMessage}");
        }

        private void ClearSelection()
        {
            selectedEntities.Clear();
            lastMessage = "選択解除しました。";
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
