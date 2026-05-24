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
        private enum ViewMode
        {
            StageSelect,
            Game
        }

        private readonly List<string> stageIds = new List<string>();
        private readonly Dictionary<string, StageData> stageDataById = new Dictionary<string, StageData>();
        private readonly List<string> selectedEntities = new List<string>();

        private RiverCrossingGameState gameState;
        private StageData stageData;
        private string activeStageId;
        private string selectedStageIdInSelect;
        private string lastMessage = "未実行";
        private StageUIView stageUIView;
        private StageCanvasView stageCanvasView;
        private string selectedRouteId;
        private ViewMode currentViewMode = ViewMode.StageSelect;
        private int highestUnlockedStageIndex;
        private const string HighestUnlockedStageIndexKey = "TiraWantToCross.HighestUnlockedStageIndex";
        private const string ClearedStageKeyPrefix = "TiraWantToCross.Cleared.";
        [SerializeField] private bool useLegacyOnGui;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!ShouldBootstrap()) return;
            var existing = FindAnyObjectByType<PrototypeStageUiController>();
            if (existing != null) return;

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
            highestUnlockedStageIndex = LoadHighestUnlockedStageIndex();

            if (stageIds.Count == 0)
            {
                gameState = null;
                stageData = null;
                lastMessage = "ステージが見つかりません。Resources/Data/Stages を確認してください。";
            }

            currentViewMode = ViewMode.StageSelect;
            if (stageIds.Count > 0)
            {
                selectedStageIdInSelect = stageIds[Mathf.Clamp(highestUnlockedStageIndex, 0, stageIds.Count - 1)];
            }
        }

        private void ReloadStageList()
        {
            stageIds.Clear();
            stageDataById.Clear();
            foreach (var loadedStage in StageDataLoader.LoadAllStages())
            {
                if (loadedStage == null || string.IsNullOrWhiteSpace(loadedStage.stageId)) continue;
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
            currentViewMode = ViewMode.Game;
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
            stageCanvasView.Initialize(
                canvas.transform,
                entityId => TogglePassengerSelection(entityId),
                ExecuteMoveFromSelectedRoute,
                ClearSelection,
                () => InitializeStage(activeStageId),
                TryLoadNextStage,
                OpenStageSelect,
                TrySelectStageFromList,
                StartSelectedStage,
                ResetProgress,
                DismissOperationError,
                OpenRuleGuide,
                OnRetireRequested);
        }

        private void Update()
        {
            if (stageCanvasView == null) return;
            var context = BuildViewContext();
            if (string.IsNullOrEmpty(selectedRouteId) && context.AvailableRoutes.Count > 0) selectedRouteId = context.AvailableRoutes[0].routeId;
            stageCanvasView.Render(context);
        }

        private StageUIViewContext BuildViewContext()
        {
            return new StageUIViewContext(stageIds, activeStageId, stageData, stageDataById, gameState, selectedEntities, lastMessage, ResolveAvailableRoutes(), currentViewMode == ViewMode.StageSelect, highestUnlockedStageIndex, selectedStageIdInSelect);
        }


        private void DismissOperationError()
        {
            lastMessage = string.Empty;
        }

        private void OpenStageSelect()
        {
            currentViewMode = ViewMode.StageSelect;
            if (!string.IsNullOrEmpty(activeStageId))
            {
                selectedStageIdInSelect = activeStageId;
            }
            selectedEntities.Clear();
            selectedRouteId = null;
            lastMessage = "ステージ一覧を表示しています。";
        }

        private void OpenRuleGuide()
        {
            // ルールボタンは現状ガイドUI未実装のため、操作エラーポップアップへ流れないようメッセージは更新しない。
            lastMessage = string.Empty;
        }

        private void OnRetireRequested()
        {
            if (gameState == null || string.IsNullOrEmpty(activeStageId))
            {
                lastMessage = "プレイ中のステージがありません。";
                return;
            }

            ShowRewardedAdForRetire(() => CompleteStageByRetire(activeStageId));
        }

        private void ShowRewardedAdForRetire(Action onRewardCompleted)
        {
            // TODO: 広告SDK導入後はここでリワード広告表示に差し替える
            onRewardCompleted?.Invoke();
        }

        private void CompleteStageByRetire(string stageId)
        {
            if (string.IsNullOrEmpty(stageId))
            {
                return;
            }

            PlayerPrefs.SetInt($"{ClearedStageKeyPrefix}{stageId}", 1);
            PlayerPrefs.Save();
            UnlockNextStageIfNeeded();
            OpenStageSelect();
            lastMessage = "広告視聴完了（仮）として、このステージをクリア扱いにしました。";
        }

        private void TrySelectStageFromList(string stageId)
        {
            var index = stageIds.IndexOf(stageId);
            if (index < 0) return;
            if (index > highestUnlockedStageIndex)
            {
                lastMessage = "このステージはまだロック中です。";
                return;
            }
            selectedStageIdInSelect = stageId;
            stageDataById.TryGetValue(stageId, out var selectedData);
            var title = selectedData?.title ?? stageId;
            lastMessage = $"選択中: {title}。下の「スタート」ボタンで開始します。";
        }

        private void StartSelectedStage()
        {
            if (string.IsNullOrEmpty(selectedStageIdInSelect))
            {
                lastMessage = "開始するステージを選択してください。";
                return;
            }

            var index = stageIds.IndexOf(selectedStageIdInSelect);
            if (index < 0 || index > highestUnlockedStageIndex)
            {
                lastMessage = "選択中ステージは未解放です。";
                return;
            }

            InitializeStage(selectedStageIdInSelect);
        }

        private void ExecuteMoveFromSelectedRoute(string routeId)
        {
            if (!string.IsNullOrEmpty(routeId)) selectedRouteId = routeId;
            var available = ResolveAvailableRoutes();
            if (available.Count == 0) { lastMessage = "利用可能なルートがありません。"; return; }
            if (string.IsNullOrEmpty(selectedRouteId) || available.All(x => x.routeId != selectedRouteId)) selectedRouteId = available[0].routeId;
            ExecuteMove(selectedRouteId);
        }

        private void TryLoadNextStage()
        {
            if (!CanGoToNextStage()) { lastMessage = "NextStageに進むには、最短手数でクリアする必要があります。"; return; }
            UnlockNextStageIfNeeded();
            LoadNextStage();
        }

        private void LoadNextStage()
        {
            if (stageIds.Count == 0) { lastMessage = "次に進めるステージがありません。"; return; }
            var currentIndex = stageIds.IndexOf(activeStageId);
            if (currentIndex < 0) { lastMessage = "現在のステージ位置を特定できません。"; return; }
            var next = currentIndex + 1;
            if (next >= stageIds.Count)
            {
                lastMessage = "全ステージクリア済みです。ステージ一覧から遊ぶステージを選んでください。";
                return;
            }
            InitializeStage(stageIds[next]);
        }

        private bool CanGoToNextStage() => gameState != null && gameState.IsCleared && !gameState.IsFailed && gameState.IsExactlyOptimalMoves();

        private void UnlockNextStageIfNeeded()
        {
            var currentIndex = stageIds.IndexOf(activeStageId);
            var nextIndex = currentIndex + 1;
            if (nextIndex >= 0 && nextIndex < stageIds.Count && highestUnlockedStageIndex < nextIndex)
            {
                highestUnlockedStageIndex = nextIndex;
                SaveHighestUnlockedStageIndex();
            }
        }

        private void TogglePassengerSelection(string entityId)
        {
            if (selectedEntities.Contains(entityId)) { selectedEntities.Remove(entityId); return; }
            if (gameState != null && selectedEntities.Count >= gameState.BoatCapacity) { lastMessage = $"このボートは{gameState.BoatCapacity}匹までだよ"; return; }
            selectedEntities.Add(entityId);
        }

        private void ExecuteMove(string routeId)
        {
            if (selectedEntities.Count == 0)
            {
                lastMessage = "ボートに乗せる動物を選んでね";
                return;
            }

            if (IsOperatorSelectionRequired() && !HasOperatorInSelection())
            {
                lastMessage = $"{BuildOperatorNamesText()}が乗っていないとボートを動かせないよ";
                return;
            }

            var result = gameState.TryMove(routeId, selectedEntities);
            if (!result.Succeeded) { lastMessage = $"Move失敗: {result.Message}"; Debug.LogWarning($"[PrototypeUI] {lastMessage}"); return; }

            selectedEntities.Clear();
            if (gameState.IsFailed && !string.IsNullOrWhiteSpace(gameState.LastFailMessage))
            {
                lastMessage = gameState.LastFailMessage;
                Debug.LogWarning($"[PrototypeUI] {lastMessage}");
                return;
            }
            if (CanGoToNextStage()) UnlockNextStageIfNeeded();
            if (CanGoToNextStage() && !string.IsNullOrEmpty(activeStageId))
            {
                PlayerPrefs.SetInt($"{ClearedStageKeyPrefix}{activeStageId}", 1);
                PlayerPrefs.Save();
            }
            lastMessage = $"Move成功: destination={result.Destination}, cleared={gameState.IsCleared}, failed={gameState.IsFailed}, moves={gameState.MoveCount}, exactOptimal={gameState.IsExactlyOptimalMoves()} / 移動後、自動で降船しました。";
            Debug.Log($"[PrototypeUI] {lastMessage}");
        }

        private void ClearSelection() { selectedEntities.Clear(); lastMessage = "選択解除しました。"; }


        private bool IsOperatorSelectionRequired()
        {
            return (stageData?.entities ?? Array.Empty<EntityData>()).Any(x => x.canOperateBoat);
        }

        private bool HasOperatorInSelection()
        {
            if (stageData?.entities == null)
            {
                return false;
            }

            foreach (var entityId in selectedEntities)
            {
                var entity = stageData.entities.FirstOrDefault(x => x.entityId == entityId);
                if (entity != null && entity.canOperateBoat)
                {
                    return true;
                }
            }

            return false;
        }

        private string BuildOperatorNamesText()
        {
            var operatorNames = (stageData?.entities ?? Array.Empty<EntityData>())
                .Where(x => x.canOperateBoat)
                .Select(ResolveEntityDisplayName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToArray();

            return operatorNames.Length == 0 ? "操船できる動物" : string.Join("・", operatorNames);
        }

        private static string ResolveEntityDisplayName(EntityData entity)
        {
            if (!string.IsNullOrWhiteSpace(entity?.displayName))
            {
                return entity.displayName;
            }

            return entity?.entityId ?? string.Empty;
        }

        private List<RouteData> ResolveAvailableRoutes()
        {
            var list = new List<RouteData>();
            if (gameState == null || stageData == null) return list;
            foreach (var route in stageData.routes ?? Array.Empty<RouteData>())
            {
                if (!string.IsNullOrEmpty(ResolveDestination(route, gameState.BoatLocation))) list.Add(route);
            }
            return list;
        }


        private int LoadHighestUnlockedStageIndex()
        {
            if (stageIds.Count == 0) return -1;

            var defaultValue = 0;
            var savedIndex = PlayerPrefs.GetInt(HighestUnlockedStageIndexKey, defaultValue);
            var clampedIndex = Mathf.Clamp(savedIndex, defaultValue, stageIds.Count - 1);
            if (clampedIndex != savedIndex)
            {
                PlayerPrefs.SetInt(HighestUnlockedStageIndexKey, clampedIndex);
                PlayerPrefs.Save();
            }

            return clampedIndex;
        }

        private void SaveHighestUnlockedStageIndex()
        {
            if (stageIds.Count == 0) return;

            var clampedIndex = Mathf.Clamp(highestUnlockedStageIndex, 0, stageIds.Count - 1);
            highestUnlockedStageIndex = clampedIndex;
            PlayerPrefs.SetInt(HighestUnlockedStageIndexKey, clampedIndex);
            PlayerPrefs.Save();
        }

        private void ResetProgress()
        {
            PlayerPrefs.DeleteKey(HighestUnlockedStageIndexKey);
            foreach (var stageId in stageIds)
            {
                PlayerPrefs.DeleteKey($"{ClearedStageKeyPrefix}{stageId}");
            }
            PlayerPrefs.Save();
            highestUnlockedStageIndex = stageIds.Count > 0 ? 0 : -1;
            selectedStageIdInSelect = stageIds.Count > 0 ? stageIds[0] : null;
            lastMessage = "進行状況をリセットしました。Stage 1のみ解放しています。";
        }
        private static string ResolveDestination(RouteData route, string currentBoatLocation)
        {
            if (route.from == currentBoatLocation) return route.to;
            if (route.bidirectional && route.to == currentBoatLocation) return route.from;
            return null;
        }
    }
}
