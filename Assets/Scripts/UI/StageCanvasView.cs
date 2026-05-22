using System;
using System.Collections.Generic;
using System.Linq;
using TiraWantToCross.GameLogic;
using TiraWantToCross.Stage;
using TiraWantToCross.Prototype;
using UnityEngine;
using UnityEngine.UI;

namespace TiraWantToCross.UI
{
    public sealed class StageCanvasView
    {
        private sealed class EntityVisualRefs
        {
            public Button Button;
            public Image PortraitImage;
            public Image SelectionFrame;
            public Text Label;
            public Text PortraitPlaceholderText;
            public bool HasPortraitSprite;
        }

        private sealed class LocationVisualTheme
        {
            public string LocationId;
            public Image BackgroundImage;
            public Text LabelText;
            public Color BaseColor;
        }

        private readonly Dictionary<string, EntityVisualRefs> boardEntityVisuals = new Dictionary<string, EntityVisualRefs>();
        private readonly Dictionary<string, string> boardEntityParents = new Dictionary<string, string>();
        private readonly Dictionary<string, EntityVisualRefs> selectionIconVisuals = new Dictionary<string, EntityVisualRefs>();
        private readonly Dictionary<string, RectTransform> locationPanels = new Dictionary<string, RectTransform>();
        private readonly Dictionary<string, LocationVisualTheme> locationThemes = new Dictionary<string, LocationVisualTheme>();
        private readonly List<Button> routeButtons = new List<Button>();
        private readonly Dictionary<string, Sprite> boardSpriteCache = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Sprite> iconSpriteCache = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, RectTransform> locationNodeRoots = new Dictionary<string, RectTransform>();
        private readonly Dictionary<string, RectTransform> locationEntityGrids = new Dictionary<string, RectTransform>();
        private readonly Dictionary<string, Text> locationCountTexts = new Dictionary<string, Text>();
        private readonly List<RectTransform> routeLineVisuals = new List<RectTransform>();
        private string routeTopologyCacheKey = string.Empty;

        private RectTransform boardPanel;
        private RectTransform locationNodesRoot;
        private RectTransform routeLinesRoot;
        private RectTransform boatMarkerRoot;
        private Transform selectionIconsContainer;

        private Text stageTitleText;
        private Text stageNameText;
        private Text movesText;
        private Text resultText;
        private Text messageText;
        private Text boatLocationText;
        private Image boatImage;
        private Text boatLabelText;
        private Sprite boatSprite;
        private HorizontalLayoutGroup midLayoutGroup;
        private VerticalLayoutGroup mainLayoutGroup;
        private VerticalLayoutGroup bottomLayoutGroup;

        private RectTransform routeRowTransform;
        private RectTransform actionRowTransform;
        private Text selectionCountText;
        private Text objectiveText;

        private Button restartButton;
        private Button nextStageButton;
        private Button stageListButton;

        private RectTransform stageSelectRoot;
        private VerticalLayoutGroup stageSelectListLayout;
        private readonly List<Button> stageSelectButtons = new List<Button>();

        private RectTransform popupOverlay;
        private Text popupTitleText;
        private Text popupMessageText;
        private Button popupPrimaryButton;
        private Button popupSecondaryButton;

        private Action<string> onEntitySelected;
        private Action<string> onMove;
        private Action onClearSelection;
        private Action onRestart;
        private Action onNextStage;
        private Action onOpenStageSelect;
        private Action<string> onSelectStage;
        private Action onResetProgress;
        private Action onStartSelectedStage;
        private Text stageSelectClearCountText;
        private Image stageSelectProgressFill;
        private Text stageDetailText;
        private Text stageDetailMetaText;
        private Button stageStartButton;
        private readonly List<Button> stageSelectNavButtons = new List<Button>();

        private enum PopupResultState
        {
            Playing,
            ClearOptimal,
            ClearNotOptimal,
            Failed,
            AllStagesCleared
        }

        private enum CharacterSpriteUsage
        {
            Board,
            Icon
        }

        public void Initialize(Transform parent,
            Action<string> onEntitySelected,
            Action<string> onMove,
            Action onClearSelection,
            Action onRestart,
            Action onNextStage,
            Action onOpenStageSelect,
            Action<string> onSelectStage,
            Action onStartSelectedStage,
            Action onResetProgress)
        {
            this.onEntitySelected = onEntitySelected;
            this.onMove = onMove;
            this.onClearSelection = onClearSelection;
            this.onRestart = onRestart;
            this.onNextStage = onNextStage;
            this.onOpenStageSelect = onOpenStageSelect;
            this.onSelectStage = onSelectStage;
            this.onStartSelectedStage = onStartSelectedStage;
            this.onResetProgress = onResetProgress;

            BuildRoot(parent);
        }

        public void Render(StageUIViewContext context)
        {
            stageSelectRoot.gameObject.SetActive(context.IsStageSelectMode);
            if (context.IsStageSelectMode)
            {
                RenderStageSelect(context);
                return;
            }

            if (context.GameState == null || context.StageData == null)
            {
                stageNameText.text = "ステージ未読込";
                messageText.text = context.LastMessage;
                return;
            }

            stageNameText.text = $"{context.StageData.stageId} - {context.StageData.title}";
            stageTitleText.text = "Tira Want To Cross";
            movesText.text = $"moves: {context.GameState.MoveCount} / optimal: {context.StageData.optimalMoves}";
            resultText.text = BuildResultText(context.GameState);
            messageText.text = context.LastMessage;
            objectiveText.text = BuildObjectiveText(context.StageData);
            boatLocationText.text = $"ボート位置: {context.GameState.BoatLocation}";

            RenderBoardEntities(context);
            RenderSelectionIcons(context);
            RenderRoutes(context);

            var popupState = ResolvePopupState(context);
            RenderPopup(context, popupState);

            nextStageButton.gameObject.SetActive(false);
            nextStageButton.interactable = false;
        }

        private void BuildRoot(Transform parent)
        {
            var root = CreateRect("CanvasRoot", parent, new Color(0f, 0f, 0f, 0f));
            ApplyFullStretch(root);

            var main = CreateVerticalLayout("Main", root, 18f);
            ApplyFullStretch(main, 24f, 24f, 24f, 24f);
            mainLayoutGroup = main.GetComponent<VerticalLayoutGroup>();

            var top = CreatePanel("TopPanel", main, new Color(0.15f, 0.2f, 0.25f, 0.8f), 260f, 220f);
            var topLayout = top.gameObject.AddComponent<VerticalLayoutGroup>();
            topLayout.spacing = 8f;
            topLayout.padding = new RectOffset(16, 16, 16, 16);
            topLayout.childControlHeight = true;
            topLayout.childControlWidth = true;
            topLayout.childForceExpandHeight = false;
            topLayout.childForceExpandWidth = true;
            topLayout.childAlignment = TextAnchor.UpperCenter;
            stageTitleText = CreateText("Title", top, "", 46, TextAnchor.MiddleCenter, 70);
            stageNameText = CreateText("StageName", top, "", 42, TextAnchor.MiddleCenter, 88);
            movesText = CreateText("Moves", top, "", 36, TextAnchor.MiddleCenter, 66);

            var mid = CreatePanel("MiddlePanel", main, new Color(0.15f, 0.15f, 0.2f, 0.7f), 0f, 560f, 1f);
            boardPanel = CreateRect("BoardPanel", mid, new Color(0.66f, 0.89f, 0.98f, 1f));
            var boardLayout = boardPanel.gameObject.AddComponent<LayoutElement>();
            boardLayout.flexibleHeight = 1f;
            boardLayout.minHeight = 560f;
            ApplyFullStretch(boardPanel, 8f, 8f, 8f, 8f);
            locationNodesRoot = new GameObject("LocationNodesRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            locationNodesRoot.SetParent(boardPanel, false);
            ApplyFullStretch(locationNodesRoot, 20f, 20f, 20f, 120f);
            routeLinesRoot = new GameObject("RouteLinesRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            routeLinesRoot.SetParent(boardPanel, false);
            ApplyFullStretch(routeLinesRoot, 20f, 20f, 20f, 120f);
            boatMarkerRoot = new GameObject("BoatMarker", typeof(RectTransform)).GetComponent<RectTransform>();
            boatMarkerRoot.SetParent(boardPanel, false);
            ApplyFullStretch(boatMarkerRoot, 20f, 20f, 20f, 20f);
            boatImage = CreateRect("BoatImage", boatMarkerRoot, new Color(0.75f, 0.55f, 0.2f, 1f)).GetComponent<Image>();
            var boatRect = boatImage.GetComponent<RectTransform>();
            boatRect.sizeDelta = new Vector2(150f, 90f);
            boatLabelText = CreateText("BoatVisualLabel", boatImage.transform, "BOAT", 24, TextAnchor.MiddleCenter, 80f);
            boatSprite = Resources.Load<Sprite>("Sprites/Boat/boat");
            boatLocationText = CreateText("BoatLocation", boardPanel, "", 28, TextAnchor.LowerCenter, 44f);

            var bottom = CreatePanel("BottomPanel", main, new Color(0.2f, 0.15f, 0.2f, 0.8f), 420f, 360f);
            bottomLayoutGroup = bottom.gameObject.AddComponent<VerticalLayoutGroup>();
            bottomLayoutGroup.spacing = 12f;
            bottomLayoutGroup.padding = new RectOffset(12, 12, 12, 12);
            bottomLayoutGroup.childControlHeight = true;
            bottomLayoutGroup.childControlWidth = true;
            bottomLayoutGroup.childForceExpandHeight = false;
            bottomLayoutGroup.childForceExpandWidth = true;

            selectionCountText = CreateText("SelectionCount", bottom, "のせる動物 0/0", 30, TextAnchor.MiddleLeft, 52f);
            selectionCountText.color = new Color(0.22f, 0.22f, 0.22f, 1f);
            var iconPanel = CreateRect("SelectionIconPanel", bottom, new Color(0.98f, 0.96f, 0.9f, 1f));
            var iconPanelLayoutElement = iconPanel.gameObject.AddComponent<LayoutElement>();
            iconPanelLayoutElement.preferredHeight = 140f;
            iconPanelLayoutElement.minHeight = 120f;
            selectionIconsContainer = CreateHorizontalLayout("SelectionIcons", iconPanel, 12f, true);
            routeRowTransform = CreateHorizontalLayout("RouteRow", bottom, 12f, false);
            var routeRowLayout = routeRowTransform.gameObject.AddComponent<LayoutElement>();
            routeRowLayout.preferredHeight = 122f;
            routeRowLayout.minHeight = 102f;
            for (var i = 0; i < 2; i++)
            {
                var routeButton = CreateButton($"Route{i + 1}", routeRowTransform, "Move", () => { }, 98, 36);
                routeButtons.Add(routeButton);
            }

            actionRowTransform = CreateHorizontalLayout("ActionRow", bottom, 12f, false);
            var actionRowLayout = actionRowTransform.gameObject.AddComponent<LayoutElement>();
            actionRowLayout.preferredHeight = 112f;
            actionRowLayout.minHeight = 92f;
            CreateButton("ClearSelection", actionRowTransform, "選択解除", () => onClearSelection?.Invoke(), 90, 32);
            restartButton = CreateButton("Restart", actionRowTransform, "Restart", () => onRestart?.Invoke(), 90, 34);
            nextStageButton = CreateButton("NextStage", actionRowTransform, "NextStage", () => onNextStage?.Invoke(), 90, 32);
            stageListButton = CreateButton("StageSelect", actionRowTransform, "ステージ一覧へ", () => onOpenStageSelect?.Invoke(), 90, 30);
            objectiveText = CreateText("Objective", bottom, "全員を最短手数で対岸へ運ぼう", 28, TextAnchor.MiddleCenter, 56f);
            objectiveText.color = new Color(0.26f, 0.22f, 0.16f, 1f);

            var resultPanel = CreatePanel("ResultPanel", main, new Color(0.1f, 0.3f, 0.2f, 0.7f), 180f, 140f);
            var resultLayout = resultPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            resultLayout.spacing = 6f;
            resultLayout.padding = new RectOffset(16, 16, 12, 12);
            resultLayout.childControlHeight = true;
            resultLayout.childControlWidth = true;
            resultLayout.childForceExpandHeight = false;
            resultLayout.childForceExpandWidth = true;
            resultLayout.childAlignment = TextAnchor.UpperCenter;
            resultText = CreateText("Result", resultPanel, "", 38, TextAnchor.MiddleCenter, 74);
            messageText = CreateText("Message", resultPanel, "", 30, TextAnchor.MiddleCenter, 64);

            BuildPopupOverlay(root);
            BuildStageSelect(root);
            ApplyResponsiveLayout();
        }

        private void RenderBoardEntities(StageUIViewContext context)
        {
            BuildLocationNodes(context);
            var entities = context.StageData.entities ?? Array.Empty<EntityData>();
            var activeIds = new HashSet<string>(entities.Select(x => x.entityId));

            var staleIds = boardEntityVisuals.Keys.Where(id => !activeIds.Contains(id)).ToList();
            foreach (var staleId in staleIds)
            {
                if (boardEntityVisuals.TryGetValue(staleId, out var staleVisuals) && staleVisuals?.Button != null)
                {
                    GameObject.Destroy(staleVisuals.Button.gameObject);
                }

                boardEntityVisuals.Remove(staleId);
                boardEntityParents.Remove(staleId);
            }

            foreach (var entity in entities)
            {
                var entityId = entity.entityId;
                if (!context.GameState.EntityLocations.TryGetValue(entityId, out var location))
                {
                    continue;
                }

                if (!locationEntityGrids.TryGetValue(location, out var parent))
                {
                    continue;
                }
                var parentKey = location;

                if (!boardEntityVisuals.TryGetValue(entityId, out var visuals) || visuals?.Button == null)
                {
                    visuals = CreateEntityVisual(entityId, parent, () => { }, 58f, 16, false);
                    boardEntityVisuals[entityId] = visuals;
                    boardEntityParents[entityId] = parentKey;
                }
                else if (!boardEntityParents.TryGetValue(entityId, out var currentParentKey) || currentParentKey != parentKey)
                {
                    visuals.Button.transform.SetParent(parent, false);
                    boardEntityParents[entityId] = parentKey;
                }

                TryApplyPortraitSprite(entity, visuals, CharacterSpriteUsage.Board);
                visuals.Label.text = ResolveEntityDisplayName(entity);
                visuals.Button.image.color = Color.white;
                visuals.SelectionFrame.enabled = false;
            }
            foreach (var location in context.StageData.locations ?? Array.Empty<LocationData>())
            {
                var count = entities.Count(e => context.GameState.EntityLocations.TryGetValue(e.entityId, out var loc) && loc == location.locationId);
                if (locationCountTexts.TryGetValue(location.locationId, out var countText))
                {
                    countText.text = $"{count}ひき";
                }
            }
            UpdateLocationVisualTheme(context.GameState.BoatLocation);
        }

        private void BuildLocationNodes(StageUIViewContext context)
        {
            var locations = context.StageData.locations ?? Array.Empty<LocationData>();
            var shouldRebuildRoutes = false;
            var activeIds = new HashSet<string>(locations.Select(x => x.locationId));
            foreach (var staleId in locationNodeRoots.Keys.Where(x => !activeIds.Contains(x)).ToList())
            {
                GameObject.Destroy(locationNodeRoots[staleId].gameObject);
                locationNodeRoots.Remove(staleId);
                locationEntityGrids.Remove(staleId);
                locationCountTexts.Remove(staleId);
                locationThemes.Remove(staleId);
                shouldRebuildRoutes = true;
            }

            for (var i = 0; i < locations.Length; i++)
            {
                var location = locations[i];
                if (locationNodeRoots.ContainsKey(location.locationId))
                {
                    var nextPosition = ResolveLocationNodePosition(i, locations.Length);
                    var existingNode = locationNodeRoots[location.locationId];
                    if (existingNode.anchoredPosition != nextPosition)
                    {
                        existingNode.anchoredPosition = nextPosition;
                        shouldRebuildRoutes = true;
                    }
                    continue;
                }

                var node = CreateRect($"{location.locationId}_Node", locationNodesRoot, new Color(0.78f, 0.92f, 0.62f, 1f));
                node.sizeDelta = new Vector2(220f, 240f);
                node.anchorMin = new Vector2(0f, 0f);
                node.anchorMax = new Vector2(0f, 0f);
                node.pivot = new Vector2(0.5f, 0.5f);
                node.anchoredPosition = ResolveLocationNodePosition(i, locations.Length);

                var countLabel = CreateText("Count", node, "0ひき", 24, TextAnchor.MiddleCenter, 32f);
                countLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 96f);
                countLabel.color = new Color(0.28f, 0.2f, 0.1f, 1f);
                locationCountTexts[location.locationId] = countLabel;

                var animalPanel = CreateRect("AnimalPanel", node, new Color(0.99f, 0.96f, 0.9f, 1f));
                animalPanel.sizeDelta = new Vector2(180f, 140f);
                animalPanel.anchorMin = new Vector2(0.5f, 0.5f);
                animalPanel.anchorMax = new Vector2(0.5f, 0.5f);
                animalPanel.pivot = new Vector2(0.5f, 0.5f);
                animalPanel.anchoredPosition = new Vector2(0f, 14f);
                var grid = CreateHorizontalLayout("Entities", animalPanel, 8f, true);
                var gridLayout = grid.GetComponent<HorizontalLayoutGroup>();
                gridLayout.childControlWidth = false;
                gridLayout.childForceExpandWidth = false;
                gridLayout.padding = new RectOffset(10, 10, 10, 10);
                var gridComp = grid.gameObject.AddComponent<GridLayoutGroup>();
                gridComp.cellSize = new Vector2(74f, 56f);
                gridComp.spacing = new Vector2(8f, 8f);
                gridComp.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridComp.constraintCount = 2;
                GameObject.Destroy(gridLayout);
                locationEntityGrids[location.locationId] = grid;

                var name = string.IsNullOrWhiteSpace(location.displayName) ? location.locationId : location.displayName;
                var nameLabel = CreateText("Name", node, name, 26, TextAnchor.MiddleCenter, 38f);
                nameLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -96f);
                nameLabel.color = new Color(0.24f, 0.16f, 0.08f, 1f);

                locationThemes[location.locationId] = new LocationVisualTheme
                {
                    LocationId = location.locationId,
                    BackgroundImage = node.GetComponent<Image>(),
                    LabelText = nameLabel,
                    BaseColor = new Color(0.78f, 0.92f, 0.62f, 1f)
                };
                locationNodeRoots[location.locationId] = node;
                shouldRebuildRoutes = true;
            }

            if (shouldRebuildRoutes)
            {
                routeTopologyCacheKey = string.Empty;
            }

            RenderRouteLines(context);
        }

        private static Vector2 ResolveLocationNodePosition(int index, int count)
        {
            if (count == 2) return new[] { new Vector2(-220f, 10f), new Vector2(220f, 10f) }[index];
            if (count == 3) return new[] { new Vector2(-260f, 10f), new Vector2(0f, 10f), new Vector2(260f, 10f) }[index];
            var col = index % 2;
            var row = index / 2;
            return new Vector2(col == 0 ? -180f : 180f, 80f - row * 180f);
        }

        private void RenderRouteLines(StageUIViewContext context)
        {
            var routes = context.StageData.routes ?? Array.Empty<RouteData>();
            var routeKeyParts = routes
                .Select(route => $"{route.routeId}:{route.from}->{route.to}")
                .OrderBy(x => x)
                .ToList();
            var nextCacheKey = string.Join("|", routeKeyParts);
            if (nextCacheKey == routeTopologyCacheKey)
            {
                return;
            }

            foreach (var line in routeLineVisuals)
            {
                GameObject.Destroy(line.gameObject);
            }
            routeLineVisuals.Clear();
            foreach (var route in routes)
            {
                if (!locationNodeRoots.TryGetValue(route.from, out var fromNode) || !locationNodeRoots.TryGetValue(route.to, out var toNode))
                {
                    continue;
                }
                var line = CreateRect($"{route.routeId}_Line", routeLinesRoot, new Color(0.95f, 0.94f, 0.82f, 0.9f));
                var diff = toNode.anchoredPosition - fromNode.anchoredPosition;
                var len = Mathf.Max(24f, diff.magnitude - 220f);
                line.sizeDelta = new Vector2(len, 8f);
                line.anchorMin = new Vector2(0f, 0f);
                line.anchorMax = new Vector2(0f, 0f);
                line.pivot = new Vector2(0.5f, 0.5f);
                line.anchoredPosition = (fromNode.anchoredPosition + toNode.anchoredPosition) * 0.5f;
                line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
                routeLineVisuals.Add(line);
            }
            routeTopologyCacheKey = nextCacheKey;
        }

        private void RenderSelectionIcons(StageUIViewContext context)
        {
            var entities = context.StageData.entities ?? Array.Empty<EntityData>();
            var candidates = entities.Where(x => context.GameState.EntityLocations.TryGetValue(x.entityId, out var location) && location == context.GameState.BoatLocation).ToList();
            var activeIds = new HashSet<string>(candidates.Select(x => x.entityId));
            var staleIds = selectionIconVisuals.Keys.Where(id => !activeIds.Contains(id)).ToList();
            foreach (var staleId in staleIds)
            {
                if (selectionIconVisuals.TryGetValue(staleId, out var staleVisuals) && staleVisuals?.Button != null)
                {
                    GameObject.Destroy(staleVisuals.Button.gameObject);
                }
                selectionIconVisuals.Remove(staleId);
            }

            var capacity = Mathf.Max(1, context.GameState.BoatCapacity);
            selectionCountText.text = $"のせる動物 {context.SelectedEntities.Count}/{capacity}";
            var canSelectMore = context.SelectedEntities.Count < capacity;

            for (var index = 0; index < candidates.Count; index++)
            {
                var entity = candidates[index];
                if (!selectionIconVisuals.TryGetValue(entity.entityId, out var visuals) || visuals?.Button == null)
                {
                    var capturedId = entity.entityId;
                    visuals = CreateEntityVisual(entity.entityId, selectionIconsContainer, () => onEntitySelected?.Invoke(capturedId), 120f, 20, true);
                    selectionIconVisuals[entity.entityId] = visuals;
                }

                TryApplyPortraitSprite(entity, visuals, CharacterSpriteUsage.Icon);
                visuals.Label.text = ResolveEntityDisplayName(entity);
                var isSelected = context.SelectedEntities.Contains(entity.entityId);
                visuals.SelectionFrame.enabled = isSelected;
                visuals.SelectionFrame.color = new Color(0.3f, 0.8f, 0.32f, 1f);
                var canInteract = isSelected || canSelectMore;
                visuals.Button.interactable = true;
                visuals.Button.image.color = isSelected
                    ? new Color(0.82f, 0.97f, 0.84f, 1f)
                    : canInteract
                        ? new Color(1f, 1f, 1f, 1f)
                        : new Color(0.92f, 0.92f, 0.92f, 1f);
                visuals.Button.transform.SetSiblingIndex(index);
            }
        }

        private void RenderRoutes(StageUIViewContext context)
        {
            for (var i = 0; i < routeButtons.Count; i++)
            {
                if (i >= context.AvailableRoutes.Count)
                {
                    routeButtons[i].gameObject.SetActive(false);
                    continue;
                }

                var route = context.AvailableRoutes[i];
                var destination = route.from == context.GameState.BoatLocation ? route.to : route.from;
                routeButtons[i].gameObject.SetActive(true);
                routeButtons[i].GetComponentInChildren<Text>().text = $"{BuildLocationDisplayName(destination)}へ移動";
                routeButtons[i].onClick.RemoveAllListeners();
                routeButtons[i].onClick.AddListener(() => onMove?.Invoke(route.routeId));
                routeButtons[i].interactable = CanMoveSelectedEntities(context);
            }

            UpdateBoatVisual(context.GameState.BoatLocation, context);
        }


        private void UpdateBoatVisual(string boatLocation, StageUIViewContext context)
        {
            if (boatImage != null)
            {
                if (boatSprite != null)
                {
                    boatImage.sprite = boatSprite;
                    boatImage.type = Image.Type.Simple;
                    boatImage.preserveAspect = true;
                    boatImage.color = Color.white;
                }
                else
                {
                    boatImage.sprite = null;
                    boatImage.color = new Color(0.75f, 0.55f, 0.2f, 1f);
                }
            }

            if (boatLabelText != null)
            {
                boatLabelText.enabled = boatSprite == null;
                boatLabelText.text = "BOAT";
            }

            if (locationNodeRoots.TryGetValue(boatLocation, out var node))
            {
                var markerRect = boatImage.GetComponent<RectTransform>();
                markerRect.anchorMin = new Vector2(0f, 0f);
                markerRect.anchorMax = new Vector2(0f, 0f);
                markerRect.pivot = new Vector2(0.5f, 0.5f);
                markerRect.anchoredPosition = node.anchoredPosition + new Vector2(0f, -110f);
            }
        }

        private void TryApplyPortraitSprite(EntityData entity, EntityVisualRefs visuals, CharacterSpriteUsage usage)
        {
            if (visuals == null || visuals.PortraitImage == null)
            {
                return;
            }

            if (entity == null || string.IsNullOrEmpty(entity.entityId))
            {
                ApplyPortraitFallback(visuals);
                return;
            }

            var sprite = ResolveCharacterSprite(entity, usage);
            if (sprite == null)
            {
                ApplyPortraitFallback(visuals);
                return;
            }

            visuals.HasPortraitSprite = true;
            visuals.PortraitImage.sprite = sprite;
            visuals.PortraitImage.type = Image.Type.Simple;
            visuals.PortraitImage.preserveAspect = true;
            visuals.PortraitImage.color = Color.white;
            if (visuals.PortraitPlaceholderText != null)
            {
                visuals.PortraitPlaceholderText.enabled = false;
            }
        }


        private static string BuildPortraitCacheKey(EntityData entity)
        {
            var spriteBaseId = ResolveSpriteBaseId(entity);
            return $"{entity.entityId}:{spriteBaseId}";
        }

        private Sprite ResolveCharacterSprite(EntityData entity, CharacterSpriteUsage usage)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.entityId))
            {
                return null;
            }

            var cacheKey = BuildPortraitCacheKey(entity);
            var cache = usage == CharacterSpriteUsage.Board ? boardSpriteCache : iconSpriteCache;
            if (cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var sprite = TryLoadCharacterSprite(entity, usage);

            cache[cacheKey] = sprite;
            return sprite;
        }

        private static Sprite TryLoadCharacterSprite(EntityData entity, CharacterSpriteUsage usage)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.entityId))
            {
                return null;
            }

            var spriteBaseId = ResolveSpriteBaseId(entity);
            foreach (var path in BuildCharacterSpriteResourcePaths(spriteBaseId, usage))
            {
                var sprite = Resources.Load<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            if (!string.IsNullOrWhiteSpace(entity.spriteId))
            {
                return null;
            }

            foreach (var path in BuildCharacterSpriteResourcePaths(entity.entityId, usage))
            {
                var sprite = Resources.Load<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static string ResolveSpriteBaseId(EntityData entity)
        {
            if (entity == null)
            {
                return string.Empty;
            }

            return !string.IsNullOrWhiteSpace(entity.spriteId) ? entity.spriteId : entity.entityId;
        }

        private static IEnumerable<string> BuildCharacterSpriteResourcePaths(string baseId, CharacterSpriteUsage usage)
        {
            if (string.IsNullOrWhiteSpace(baseId))
            {
                yield break;
            }

            var usageSuffix = usage == CharacterSpriteUsage.Board ? "board" : "icon";
            yield return $"Sprites/Characters/{baseId}/{baseId}_{usageSuffix}";
            yield return $"Sprites/Characters/{baseId}/{baseId}";
            yield return $"Sprites/Characters/{baseId}_{usageSuffix}";
            yield return $"Sprites/Characters/{baseId}";
        }

        private static void ApplyPortraitFallback(EntityVisualRefs visuals)
        {
            visuals.HasPortraitSprite = false;
            visuals.PortraitImage.sprite = null;
            visuals.PortraitImage.type = Image.Type.Simple;
            visuals.PortraitImage.preserveAspect = true;
            visuals.PortraitImage.color = new Color(0.7f, 0.85f, 0.95f, 1f);
            if (visuals.PortraitPlaceholderText != null)
            {
                visuals.PortraitPlaceholderText.enabled = true;
            }
        }

        private void UpdateLocationVisualTheme(string boatLocation)
        {
            foreach (var theme in locationThemes.Values)
            {
                var isBoatHere = theme.LocationId == boatLocation;
                var color = isBoatHere ? theme.BaseColor * 1.2f : theme.BaseColor;
                color.a = theme.BaseColor.a;
                theme.BackgroundImage.color = color;
                theme.LabelText.color = isBoatHere ? new Color(1f, 0.96f, 0.65f, 1f) : Color.white;
            }
        }

        private static bool CanMoveSelectedEntities(StageUIViewContext context)
        {
            if (context.SelectedEntities.Count == 0)
            {
                return false;
            }

            return context.SelectedEntities.All(entityId =>
                context.GameState.EntityLocations.TryGetValue(entityId, out var location) &&
                location == context.GameState.BoatLocation);
        }

        private static string BuildLocationDisplayName(string locationId)
        {
            return locationId switch
            {
                "left" => "左岸",
                "right" => "右岸",
                _ => locationId
            };
        }

        private static string BuildResultText(RiverCrossingGameState gameState)
        {
            if (gameState.IsFailed) return "FAILED";
            if (gameState.IsCleared && gameState.IsExactlyOptimalMoves()) return "CLEAR";
            if (gameState.IsCleared) return "NOT OPTIMAL";
            return "PLAYING";
        }



        private PopupResultState ResolvePopupState(StageUIViewContext context)
        {
            if (context?.GameState == null || context.StageData == null)
            {
                return PopupResultState.Playing;
            }

            if (context.GameState.IsFailed)
            {
                return PopupResultState.Failed;
            }

            if (!context.GameState.IsCleared)
            {
                return PopupResultState.Playing;
            }

            if (!context.GameState.IsExactlyOptimalMoves())
            {
                return PopupResultState.ClearNotOptimal;
            }

            return IsLastStage(context) ? PopupResultState.AllStagesCleared : PopupResultState.ClearOptimal;
        }

        private static bool IsLastStage(StageUIViewContext context)
        {
            if (context.StageIds == null || context.StageIds.Count == 0)
            {
                return false;
            }

            return context.ActiveStageId == context.StageIds[context.StageIds.Count - 1];
        }

        private void RenderPopup(StageUIViewContext context, PopupResultState state)
        {
            if (popupOverlay == null)
            {
                return;
            }

            var showPopup = state != PopupResultState.Playing;
            popupOverlay.gameObject.SetActive(showPopup);
            if (!showPopup)
            {
                return;
            }

            popupSecondaryButton.gameObject.SetActive(false);

            switch (state)
            {
                case PopupResultState.ClearOptimal:
                    popupTitleText.text = "クリア！";
                    popupMessageText.text = "最短手数でクリアしました！";
                    ConfigurePopupButton(popupPrimaryButton, "次のステージへ", () => onNextStage?.Invoke());
                    break;
                case PopupResultState.ClearNotOptimal:
                    popupTitleText.text = "手数オーバー";
                    popupMessageText.text = "最短手数ではありません。もう一度挑戦しましょう。";
                    ConfigurePopupButton(popupPrimaryButton, "リスタート", () => onRestart?.Invoke());
                    break;
                case PopupResultState.Failed:
                    popupTitleText.text = "失敗";
                    popupMessageText.text = string.IsNullOrWhiteSpace(context.LastMessage)
                        ? "条件違反です。もう一度挑戦しましょう。"
                        : $"条件違反です。もう一度挑戦しましょう。\n{context.LastMessage}";
                    ConfigurePopupButton(popupPrimaryButton, "リスタート", () => onRestart?.Invoke());
                    break;
                case PopupResultState.AllStagesCleared:
                    popupTitleText.text = "全ステージクリア！";
                    popupMessageText.text = "ここまでのステージをすべてクリアしました！";
                    ConfigurePopupButton(popupPrimaryButton, "ステージ一覧へ", () => onOpenStageSelect?.Invoke());
                    break;
            }
        }

        private static void ConfigurePopupButton(Button button, string label, Action onClick)
        {
            button.GetComponentInChildren<Text>().text = label;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }


        private void RenderStageSelect(StageUIViewContext context)
        {
            popupOverlay.gameObject.SetActive(false);
            EnsureStageSelectButtons(8);
            var clearCount = CountClearedStages(context);
            var total = context.StageIds.Count;
            stageSelectClearCountText.text = $"クリア数 {clearCount}/{Mathf.Max(total, 1)}";
            if (stageSelectProgressFill != null)
            {
                stageSelectProgressFill.fillAmount = total > 0 ? (float)clearCount / total : 0f;
            }

            for (var i = 0; i < stageSelectButtons.Count; i++)
            {
                var button = stageSelectButtons[i];
                if (i >= 8)
                {
                    button.gameObject.SetActive(false);
                    continue;
                }

                if (i >= context.StageIds.Count)
                {
                    button.gameObject.SetActive(true);
                    button.interactable = false;
                    button.GetComponentInChildren<Text>().text = "Coming Soon";
                    continue;
                }

                var stageId = context.StageIds[i];
                var unlocked = i <= context.HighestUnlockedStageIndex;
                var selected = stageId == context.SelectedStageIdInSelect;
                context.StageDataById.TryGetValue(stageId, out var stageData);
                var optimal = stageData?.optimalMoves ?? 0;
                var cleared = IsStageCleared(stageId, stageData);
                button.gameObject.SetActive(true);
                button.interactable = unlocked;
                var label = button.GetComponentInChildren<Text>();
                label.text = $"STAGE {i + 1}\n最短 {optimal}手  {(cleared ? "CLEAR" : unlocked ? "OPEN" : "LOCK")}";
                button.image.color = !unlocked
                    ? new Color(0.6f, 0.6f, 0.6f, 0.9f)
                    : selected
                        ? new Color(0.98f, 0.92f, 0.54f, 1f)
                        : new Color(0.86f, 0.95f, 0.82f, 1f);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onSelectStage?.Invoke(stageId));
            }

            foreach (var navButton in stageSelectNavButtons)
            {
                navButton.onClick.RemoveAllListeners();
            }

            RenderSelectedStageDetail(context);
        }

        private void BuildStageSelect(Transform parent)
        {
            stageSelectRoot = CreateRect("StageSelectRoot", parent, new Color(0.97f, 0.94f, 0.86f, 1f));
            ApplyFullStretch(stageSelectRoot);
            stageSelectRoot.SetAsLastSibling();

            var layout = stageSelectRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var header = CreateRect("StageSelectHeader", stageSelectRoot, new Color(0.85f, 0.94f, 0.78f, 1f));
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 180f;
            var headerLayout = header.gameObject.AddComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset(18, 18, 16, 16);
            headerLayout.spacing = 8f;
            stageSelectClearCountText = CreateText("ClearCount", header, "クリア数 0/0", 34, TextAnchor.MiddleLeft, 46f);
            CreateText("StageSelectTitle", header, "ステージ選択", 52, TextAnchor.MiddleCenter, 62f);
            var progressBg = CreateRect("ProgressBg", header, new Color(0.88f, 0.84f, 0.68f, 1f));
            progressBg.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
            stageSelectProgressFill = CreateRect("ProgressFill", progressBg, new Color(0.52f, 0.76f, 0.42f, 1f)).GetComponent<Image>();
            stageSelectProgressFill.type = Image.Type.Filled;
            stageSelectProgressFill.fillMethod = Image.FillMethod.Horizontal;
            stageSelectProgressFill.fillAmount = 0f;
            ApplyFullStretch(stageSelectProgressFill.rectTransform, 2f, 2f, 2f, 2f);

            var tools = CreateHorizontalLayout("HeaderButtons", header, 10f, false);
            stageSelectNavButtons.Add(CreateButton("BackButton", tools, "もどる", () => { }, 56f, 24));
            stageSelectNavButtons.Add(CreateButton("RuleButton", tools, "ルール", () => { }, 56f, 24));
            stageSelectNavButtons.Add(CreateButton("MenuButton", tools, "メニュー", () => { }, 56f, 24));

            var list = CreateRect("StageSelectList", stageSelectRoot, new Color(0.95f, 0.9f, 0.78f, 1f));
            var listElement = list.gameObject.AddComponent<LayoutElement>();
            listElement.preferredHeight = 700f;
            stageSelectListLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            stageSelectListLayout.spacing = 12f;
            stageSelectListLayout.padding = new RectOffset(18, 18, 18, 18);
            stageSelectListLayout.childControlHeight = true;
            stageSelectListLayout.childControlWidth = true;
            stageSelectListLayout.childForceExpandHeight = false;
            stageSelectListLayout.childForceExpandWidth = true;

            var detail = CreateRect("StageDetail", stageSelectRoot, new Color(0.89f, 0.96f, 0.86f, 1f));
            detail.gameObject.AddComponent<LayoutElement>().preferredHeight = 360f;
            var detailLayout = detail.gameObject.AddComponent<VerticalLayoutGroup>();
            detailLayout.padding = new RectOffset(18, 18, 14, 14);
            detailLayout.spacing = 8f;
            stageDetailText = CreateText("StageDetailText", detail, "", 32, TextAnchor.UpperLeft, 190f);
            stageDetailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            stageDetailMetaText = CreateText("StageDetailMeta", detail, "", 30, TextAnchor.UpperLeft, 96f);
            stageStartButton = CreateButton("StartButton", detail, "スタート", () => onStartSelectedStage?.Invoke(), 74f, 34);

            CreateButton("ResetProgress", stageSelectRoot, "進行状況リセット", () => onResetProgress?.Invoke(), 88f, 28);
        }

        private void EnsureStageSelectButtons(int requiredCount)
        {
            while (stageSelectButtons.Count < requiredCount)
            {
                var index = stageSelectButtons.Count;
                var button = CreateButton($"StageSelectButton{index + 1}", stageSelectListLayout.transform, "", () => { }, 110f, 28);
                stageSelectButtons.Add(button);
            }
        }

        private void RenderSelectedStageDetail(StageUIViewContext context)
        {
            var stageId = context.SelectedStageIdInSelect;
            if (string.IsNullOrEmpty(stageId) || !context.StageDataById.TryGetValue(stageId, out var stageData))
            {
                stageDetailText.text = "ステージを選択してください。";
                stageDetailMetaText.text = string.Empty;
                stageStartButton.interactable = false;
                return;
            }
            var index = FindStageIndex(context.StageIds, stageId);
            var unlocked = index >= 0 && index <= context.HighestUnlockedStageIndex;
            stageDetailText.text = $"ステージ{index + 1}\n{stageData.title}\n{ResolveStageDescription(stageData)}\nクリア条件: 全員を右岸へ運ぶ";
            stageDetailMetaText.text = $"最短手数: {stageData.optimalMoves}手\n評価: {(IsStageCleared(stageId, stageData) ? "CLEAR" : "未クリア")}";
            stageStartButton.interactable = unlocked;
        }

        private static int FindStageIndex(IReadOnlyList<string> stageIds, string stageId)
        {
            if (stageIds == null || string.IsNullOrEmpty(stageId))
            {
                return -1;
            }

            for (var i = 0; i < stageIds.Count; i++)
            {
                if (stageIds[i] == stageId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string ResolveStageDescription(StageData stageData)
        {
            if (!string.IsNullOrWhiteSpace(stageData?.uiText?.stageSelectDescription))
            {
                return stageData.uiText.stageSelectDescription;
            }

            return "このステージをクリアしよう";
        }

        private static string BuildObjectiveText(StageData stageData)
        {
            var objective = stageData?.uiText?.objective;
            if (string.IsNullOrWhiteSpace(objective))
            {
                objective = "全員を目的地へ運ぼう";
            }

            var tip = stageData?.uiText?.tip;
            if (string.IsNullOrWhiteSpace(tip))
            {
                tip = "条件を守って最短手数を目指そう";
            }

            return $"{objective}\n{tip}";
        }

        private static string ResolveEntityDisplayName(EntityData entity)
        {
            if (!string.IsNullOrWhiteSpace(entity?.displayName))
            {
                return entity.displayName;
            }

            return entity?.entityId ?? string.Empty;
        }

        private static bool IsStageCleared(string stageId, StageData stageData)
        {
            if (stageData == null || string.IsNullOrWhiteSpace(stageId)) return false;
            return PlayerPrefs.GetInt($"TiraWantToCross.Cleared.{stageId}", 0) == 1;
        }

        private static int CountClearedStages(StageUIViewContext context)
        {
            return context.StageIds.Count(id => context.StageDataById.TryGetValue(id, out var data) && IsStageCleared(id, data));
        }
        private void BuildPopupOverlay(Transform parent)
        {
            popupOverlay = CreateRect("PopupOverlay", parent, new Color(0f, 0f, 0f, 0.66f));
            ApplyFullStretch(popupOverlay);
            popupOverlay.SetAsLastSibling();

            var popupPanel = CreateRect("PopupPanel", popupOverlay, new Color(0.15f, 0.17f, 0.23f, 0.97f));
            var panelRect = popupPanel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(860f, 760f);

            var panelLayout = popupPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            panelLayout.spacing = 24f;
            panelLayout.padding = new RectOffset(32, 32, 32, 32);
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            popupTitleText = CreateText("PopupTitle", popupPanel, "", 52, TextAnchor.MiddleCenter, 120f);
            popupMessageText = CreateText("PopupMessage", popupPanel, "", 34, TextAnchor.MiddleCenter, 280f);
            popupMessageText.horizontalOverflow = HorizontalWrapMode.Wrap;
            popupMessageText.verticalOverflow = VerticalWrapMode.Truncate;

            popupPrimaryButton = CreateButton("PopupPrimaryButton", popupPanel, "", () => { }, 110f, 34);
            popupSecondaryButton = CreateButton("PopupSecondaryButton", popupPanel, "", () => { }, 96f, 30);
            popupOverlay.gameObject.SetActive(false);
        }

        private static void ApplyCenterDefaults(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void ApplyFullStretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }
        private static RectTransform CreateRect(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            var rect = go.GetComponent<RectTransform>();
            ApplyCenterDefaults(rect);
            return rect;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color, float preferredHeight, float minHeight = -1f, float flexibleHeight = 0f)
        {
            var rect = CreateRect(name, parent, color);
            var layout = rect.gameObject.AddComponent<LayoutElement>();
            if (preferredHeight > 0f)
            {
                layout.preferredHeight = preferredHeight;
            }

            if (minHeight >= 0f)
            {
                layout.minHeight = minHeight;
            }

            layout.flexibleHeight = flexibleHeight;
            return rect;
        }

        private static RectTransform CreateVerticalLayout(string name, Transform parent, float spacing, bool stretch = false)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            if (stretch)
            {
                ApplyFullStretch(rect);
            }
            else
            {
                ApplyCenterDefaults(rect);
            }
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return rect;
        }

        private static RectTransform CreateHorizontalLayout(string name, Transform parent, float spacing, bool stretch = false)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            if (stretch)
            {
                ApplyFullStretch(rect);
            }
            else
            {
                ApplyCenterDefaults(rect);
            }
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return rect;
        }

        private Transform CreateLocationArea(string label, Transform parent, string key, Color baseColor)
        {
            var panel = CreateRect($"{key}Panel", parent, baseColor);
            var layoutElement = panel.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 0f;
            layoutElement.minWidth = 220f;
            layoutElement.flexibleWidth = 1f;
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var labelText = CreateText("Label", panel, label, 34, TextAnchor.MiddleCenter, 52);
            locationThemes[key] = new LocationVisualTheme
            {
                LocationId = key,
                BackgroundImage = panel.GetComponent<Image>(),
                LabelText = labelText,
                BaseColor = baseColor
            };
            return panel;
        }


        private static EntityVisualRefs CreateEntityVisual(string entityId, Transform parent, Action onClick, float preferredHeight, int labelFontSize, bool circularStyle)
        {
            var button = CreateButton(entityId, parent, string.Empty, onClick, preferredHeight, labelFontSize);
            var text = button.GetComponentInChildren<Text>();
            text.alignment = TextAnchor.LowerCenter;
            text.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            if (circularStyle)
            {
                button.image.color = new Color(1f, 1f, 1f, 1f);
            }

            var portrait = CreateRect("Portrait", button.transform, new Color(0.7f, 0.85f, 0.95f, 1f));
            portrait.transform.SetSiblingIndex(0);
            var portraitLayout = portrait.gameObject.AddComponent<LayoutElement>();
            portraitLayout.preferredHeight = circularStyle ? preferredHeight - 42f : 50f;
            portraitLayout.minHeight = circularStyle ? 64f : 40f;

            var portraitText = CreateText("PortraitText", portrait.transform, "IMG", 18, TextAnchor.MiddleCenter, portraitLayout.preferredHeight);
            portraitText.color = new Color(0.15f, 0.2f, 0.25f, 0.9f);

            var frame = CreateRect("SelectionFrame", button.transform, new Color(1f, 0.95f, 0.45f, 0.95f));
            frame.transform.SetAsLastSibling();
            var frameImage = frame.GetComponent<Image>();
            frameImage.raycastTarget = false;
            frameImage.enabled = false;

            return new EntityVisualRefs
            {
                Button = button,
                PortraitImage = portrait.GetComponent<Image>(),
                SelectionFrame = frameImage,
                Label = text,
                PortraitPlaceholderText = portraitText
            };
        }

        private void ApplyResponsiveLayout()
        {
            var logicalWidth = Screen.width;
            var logicalHeight = Screen.height;
            var aspect = logicalHeight > 0 ? (float)logicalWidth / logicalHeight : 1f;
            var compact = logicalWidth < 1100f || aspect < 0.58f;

            if (mainLayoutGroup != null)
            {
                mainLayoutGroup.spacing = compact ? 12f : 18f;
                mainLayoutGroup.padding = compact ? new RectOffset(12, 12, 12, 12) : new RectOffset(24, 24, 24, 24);
            }

            if (midLayoutGroup != null)
            {
                midLayoutGroup.spacing = compact ? 6f : 12f;
                midLayoutGroup.padding = compact ? new RectOffset(4, 4, 4, 4) : new RectOffset(8, 8, 8, 8);
            }

            if (bottomLayoutGroup != null)
            {
                bottomLayoutGroup.spacing = compact ? 8f : 12f;
                bottomLayoutGroup.padding = compact ? new RectOffset(8, 8, 8, 8) : new RectOffset(12, 12, 12, 12);
            }

            foreach (var panel in locationPanels.Values)
            {
                var le = panel.GetComponent<LayoutElement>();
                if (le == null)
                {
                    continue;
                }

                le.minWidth = compact ? 170f : 220f;
                le.preferredWidth = 0f;
                le.flexibleWidth = 1f;
            }

            ScaleText(stageTitleText, compact ? 38 : 48);
            ScaleText(stageNameText, compact ? 42 : 52);
            ScaleText(movesText, compact ? 32 : 40);
            ScaleText(resultText, compact ? 34 : 40);
            ScaleText(messageText, compact ? 26 : 32);
            ScaleText(boatLocationText, compact ? 26 : 32);
        }

        private void ScaleText(Text text, int fontSize)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = fontSize;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor anchor, float preferredHeight = 40f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = Color.white;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.minHeight = Mathf.Max(32f, preferredHeight * 0.8f);
            return txt;
        }

        private static Button CreateButton(string name, Transform parent, string text, Action onClick, float preferredHeight = 56f, int fontSize = 22)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = Color.white;
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.minHeight = Mathf.Max(32f, preferredHeight * 0.8f);

            CreateText("Text", go.transform, text, fontSize, TextAnchor.MiddleCenter, preferredHeight).color = Color.black;
            return btn;
        }
    }
}
