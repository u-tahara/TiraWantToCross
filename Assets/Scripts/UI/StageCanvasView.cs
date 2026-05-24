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
            public Image ButtonBackground;
            public Image CircleBorderImage;
            public Text Label;
            public Text PortraitPlaceholderText;
            public Text CheckmarkText;
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
        private static Sprite circularBorderSprite;
        private static Sprite circularMaskSprite;
        private string routeTopologyCacheKey = string.Empty;

        private RectTransform boardPanel;
        private RectTransform playableBoardArea;
        private RectTransform headerOverlay;
        private RectTransform bottomOverlay;
        private RectTransform locationNodesRoot;
        private RectTransform routeLinesRoot;
        private RectTransform boatMarkerRoot;
        private Transform selectionIconsContainer;

        private Text stageNameText;
        private Text stageNumberText;
        private Text movesText;
        private Text optimalMovesText;
        private Text boatLocationText;
        private Image boatImage;
        private Text boatLabelText;
        private Sprite boatSprite;
        private VerticalLayoutGroup bottomLayoutGroup;
        private LayoutElement objectiveLayoutElement;
        private LayoutElement statusMessageLayoutElement;
        private LayoutElement selectionCountLayoutElement;
        private LayoutElement selectionIconPanelLayoutElement;
        private LayoutElement routeRowLayoutElement;

        private RectTransform routeRowTransform;
        private Text selectionCountText;
        private Text objectiveText;
        private Text statusMessageText;

        private Button restartButton;
        private Button ruleButton;
        private Button retireButton;
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
        private Action onDismissOperationError;
        private Action onOpenRule;
        private Action onRetire;
        private bool isRetireConfirmVisible;
        private Text stageSelectClearCountText;
        private Image stageSelectProgressFill;
        private Text stageDetailText;
        private Text stageDetailMetaText;
        private Button stageStartButton;
        private readonly List<Button> stageSelectNavButtons = new List<Button>();

        private enum PopupResultState
        {
            Playing,
            OperationError,
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
            Action onResetProgress,
            Action onDismissOperationError,
            Action onOpenRule,
            Action onRetire)
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
            this.onDismissOperationError = onDismissOperationError;
            this.onOpenRule = onOpenRule;
            this.onRetire = onRetire;

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
                if (stageNumberText != null) stageNumberText.text = "-";
                movesText.text = string.Empty;
                if (optimalMovesText != null) optimalMovesText.text = string.Empty;
                objectiveText.text = string.Empty;
                RenderStatusMessage(context, PopupResultState.Playing);
                return;
            }

            var stageNumber = ResolveStageIndex(context.StageIds, context.StageData.stageId) + 1;
            var stageLabel = stageNumber > 0 ? stageNumber.ToString() : "-";
            stageNameText.text = "ステージ";
            stageNumberText.text = stageLabel;
            movesText.text = "手数";
            optimalMovesText.text = $"{context.StageData.optimalMoves}";
            var moveText = $"{context.GameState.MoveCount}/{context.StageData.optimalMoves}";
            var moveValueText = movesText.transform.parent.Find("Value")?.GetComponent<Text>();
            if (moveValueText != null) moveValueText.text = moveText;
            objectiveText.text = BuildObjectiveText(context.StageData);
            boatLocationText.text = string.Empty;

            RenderBoardEntities(context);
            RenderSelectionIcons(context);
            RenderRoutes(context);

            var popupState = ResolvePopupState(context);
            RenderStatusMessage(context, popupState);
            RenderPopup(context, popupState);

            nextStageButton.gameObject.SetActive(false);
            nextStageButton.interactable = false;
        }

        private void BuildRoot(Transform parent)
        {
            var root = CreateRect("CanvasRoot", parent, new Color(0f, 0f, 0f, 0f));
            ApplyFullStretch(root);

            boardPanel = CreateRect("BoardLayer", root, new Color(0.66f, 0.89f, 0.98f, 1f));
            ApplyFullStretch(boardPanel, 0f, 0f, 0f, 0f);

            playableBoardArea = CreateRect("PlayableBoardArea", boardPanel, new Color(0f, 0f, 0f, 0f));
            playableBoardArea.anchorMin = new Vector2(0f, 0f);
            playableBoardArea.anchorMax = new Vector2(1f, 1f);
            playableBoardArea.pivot = new Vector2(0.5f, 0.5f);
            playableBoardArea.offsetMin = new Vector2(24f, 390f);
            playableBoardArea.offsetMax = new Vector2(-24f, -140f);

            routeLinesRoot = new GameObject("RouteLines", typeof(RectTransform)).GetComponent<RectTransform>();
            routeLinesRoot.SetParent(playableBoardArea, false);
            ApplyFullStretch(routeLinesRoot, 0f, 0f, 0f, 0f);

            locationNodesRoot = new GameObject("LocationNodes", typeof(RectTransform)).GetComponent<RectTransform>();
            locationNodesRoot.SetParent(playableBoardArea, false);
            ApplyFullStretch(locationNodesRoot, 0f, 0f, 0f, 0f);

            boatMarkerRoot = new GameObject("BoatMarker", typeof(RectTransform)).GetComponent<RectTransform>();
            boatMarkerRoot.SetParent(playableBoardArea, false);
            ApplyFullStretch(boatMarkerRoot, 0f, 0f, 0f, 0f);
            boatImage = CreateRect("BoatImage", boatMarkerRoot, new Color(0.75f, 0.55f, 0.2f, 1f)).GetComponent<Image>();
            var boatRect = boatImage.GetComponent<RectTransform>();
            boatRect.sizeDelta = new Vector2(150f, 90f);
            boatLabelText = CreateText("BoatVisualLabel", boatImage.transform, "BOAT", 24, TextAnchor.MiddleCenter, 80f);
            boatSprite = Resources.Load<Sprite>("Sprites/Boat/boat");
            boatLocationText = CreateText("BoatLocation", playableBoardArea, "", 28, TextAnchor.LowerCenter, 44f);

            headerOverlay = CreateRect("HeaderOverlay", root, new Color(0.98f, 0.95f, 0.88f, 0.98f));
            var header = headerOverlay;
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(0f, -140f);
            header.offsetMax = new Vector2(0f, 0f);
            var row = CreateHorizontalLayout("HeaderRow", header, 8f, true);
            var rowLayout = row.gameObject.GetComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(12, 12, 12, 12);
            rowLayout.childControlWidth = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childControlHeight = true;
            CreateHeaderInfoBlock(row, "StageBlock", "ステージ", out stageNameText, out stageNumberText, 150f, 46);
            CreateHeaderInfoBlock(row, "MoveBlock", "手数", out movesText, out var moveValueTextCreated, 170f, 32);
            moveValueTextCreated.name = "Value";
            CreateHeaderInfoBlock(row, "OptimalBlock", "最短手数", out var optimalTitleText, out optimalMovesText, 130f, 32);
            optimalTitleText.text = "最短手数";
            restartButton = CreateRoundHeaderButton(row, "ResetButton", "リセット", () => onRestart?.Invoke());
            ruleButton = CreateRoundHeaderButton(row, "RuleButton", "ルール", () => onOpenRule?.Invoke());
            retireButton = CreateRoundHeaderButton(row, "RetireButton", "リタイア", ShowRetireConfirmPopup);
            stageListButton = CreateRoundHeaderButton(row, "MenuButton", "メニュー", () => onOpenStageSelect?.Invoke());

            bottomOverlay = CreateRect("BottomOverlay", root, new Color(0.2f, 0.15f, 0.2f, 0.85f));
            var bottom = bottomOverlay;
            bottom.anchorMin = new Vector2(0f, 0f);
            bottom.anchorMax = new Vector2(1f, 0f);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.offsetMin = new Vector2(0f, 0f);
            bottom.offsetMax = new Vector2(0f, 390f);
            bottomLayoutGroup = bottom.gameObject.AddComponent<VerticalLayoutGroup>();
            bottomLayoutGroup.spacing = 10f;
            bottomLayoutGroup.padding = new RectOffset(12, 12, 12, 12);
            bottomLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            bottomLayoutGroup.childControlHeight = true;
            bottomLayoutGroup.childControlWidth = true;
            bottomLayoutGroup.childForceExpandHeight = false;
            bottomLayoutGroup.childForceExpandWidth = true;

            objectiveText = CreateText("Objective", bottom, "全員を最短手数で対岸へ運ぼう", 26, TextAnchor.MiddleLeft, 64f);
            objectiveLayoutElement = objectiveText.GetComponent<LayoutElement>();
            objectiveText.color = new Color(0.26f, 0.22f, 0.16f, 1f);
            objectiveLayoutElement.ignoreLayout = true;
            objectiveLayoutElement.preferredHeight = 0f;
            objectiveLayoutElement.minHeight = 0f;
            objectiveLayoutElement.flexibleHeight = 0f;
            objectiveText.gameObject.SetActive(false);
            statusMessageText = CreateText("StatusMessage", bottom, string.Empty, 24, TextAnchor.MiddleLeft, 0f);
            statusMessageLayoutElement = statusMessageText.GetComponent<LayoutElement>();
            statusMessageLayoutElement.ignoreLayout = true;
            statusMessageLayoutElement.preferredHeight = 0f;
            statusMessageLayoutElement.minHeight = 0f;
            statusMessageLayoutElement.flexibleHeight = 0f;
            statusMessageText.color = new Color(0.7f, 0.15f, 0.12f, 1f);
            statusMessageText.gameObject.SetActive(false);
            selectionCountText = CreateText("SelectionCountText", bottom, "0/0", 34, TextAnchor.MiddleCenter, 34f);
            selectionCountLayoutElement = selectionCountText.GetComponent<LayoutElement>();
            selectionCountText.color = new Color(0.22f, 0.22f, 0.22f, 1f);
            var iconPanel = CreateRect("SelectionIconPanel", bottom, new Color(0.98f, 0.96f, 0.9f, 1f));
            selectionIconPanelLayoutElement = iconPanel.gameObject.AddComponent<LayoutElement>();
            selectionIconPanelLayoutElement.preferredHeight = 274f;
            selectionIconPanelLayoutElement.minHeight = 258f;
            var selectionPanelLayout = iconPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            selectionPanelLayout.spacing = 10f;
            selectionPanelLayout.padding = new RectOffset(12, 12, 22, 22);
            selectionPanelLayout.childAlignment = TextAnchor.MiddleCenter;
            selectionPanelLayout.childControlWidth = true;
            selectionPanelLayout.childControlHeight = false;
            selectionPanelLayout.childForceExpandWidth = true;
            selectionPanelLayout.childForceExpandHeight = false;
            selectionCountText.transform.SetParent(iconPanel, false);
            selectionCountText.alignment = TextAnchor.MiddleCenter;
            var selectionCountRect = selectionCountText.GetComponent<RectTransform>();
            selectionCountRect.anchorMin = new Vector2(0.5f, 0.5f);
            selectionCountRect.anchorMax = new Vector2(0.5f, 0.5f);
            selectionCountRect.pivot = new Vector2(0.5f, 0.5f);
            selectionCountRect.anchoredPosition = Vector2.zero;
            selectionCountRect.sizeDelta = new Vector2(220f, 34f);

            var selectionIconsRoot = CreateRect("SelectionIcons", iconPanel, Color.clear);
            selectionIconsRoot.anchorMin = new Vector2(0.5f, 0.5f);
            selectionIconsRoot.anchorMax = new Vector2(0.5f, 0.5f);
            selectionIconsRoot.pivot = new Vector2(0.5f, 0.5f);
            selectionIconsRoot.anchoredPosition = Vector2.zero;
            selectionIconsRoot.sizeDelta = new Vector2(0f, 192f);
            var selectionIconsLayoutElement = selectionIconsRoot.gameObject.AddComponent<LayoutElement>();
            selectionIconsLayoutElement.preferredHeight = 192f;
            selectionIconsLayoutElement.flexibleHeight = 1f;
            selectionIconsContainer = selectionIconsRoot;

            var selectionIconsLayout = selectionIconsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            selectionIconsLayout.spacing = 16f;
            selectionIconsLayout.childAlignment = TextAnchor.MiddleCenter;
            selectionIconsLayout.childControlWidth = false;
            selectionIconsLayout.childForceExpandWidth = false;
            selectionIconsLayout.childControlHeight = false;
            selectionIconsLayout.childForceExpandHeight = false;
            routeRowTransform = CreateHorizontalLayout("RouteRow", bottom, 12f, false);
            routeRowLayoutElement = routeRowTransform.gameObject.AddComponent<LayoutElement>();
            routeRowLayoutElement.preferredHeight = 74f;
            routeRowLayoutElement.minHeight = 68f;
            for (var i = 0; i < 2; i++)
            {
                var routeButton = CreateButton($"Route{i + 1}", routeRowTransform, "Move", () => { }, 90f, 34);
                routeButtons.Add(routeButton);
            }

            nextStageButton = CreateButton("NextStage", bottom, "次のステージ", () => onNextStage?.Invoke(), 1f, 1);
            nextStageButton.gameObject.SetActive(false);

            BuildPopupOverlay(root);
            BuildStageSelect(root);
            ApplyResponsiveLayout();
        }

        private static RectTransform CreateHeaderInfoBlock(Transform parent, string name, string title, out Text titleText, out Text valueText, float width, int valueFontSize)
        {
            var block = CreateRect(name, parent, new Color(1f, 0.98f, 0.92f, 1f));
            var layoutElement = block.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.minWidth = width;
            layoutElement.preferredHeight = 112f;
            var layout = block.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.spacing = 2f;
            titleText = CreateText("Title", block, title, 20, TextAnchor.MiddleCenter, 28f);
            titleText.color = new Color(0.3f, 0.2f, 0.12f, 1f);
            valueText = CreateText("Value", block, "-", valueFontSize, TextAnchor.MiddleCenter, 58f);
            valueText.color = new Color(0.2f, 0.12f, 0.06f, 1f);
            return block;
        }

        private static Button CreateRoundHeaderButton(Transform parent, string name, string label, Action onClick)
        {
            var button = CreateButton(name, parent, label, onClick, 84f, 22);
            if (button.TryGetComponent<LayoutElement>(out var layout))
            {
                layout.preferredWidth = 112f;
                layout.minWidth = 98f;
                layout.preferredHeight = 84f;
                layout.minHeight = 78f;
            }
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.94f, 0.84f, 0.62f, 1f);
            }
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = new Color(0.32f, 0.2f, 0.1f, 1f);
            }
            return button;
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
                visuals.CircleBorderImage.enabled = false;
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
                var nodeScale = ResolveBoardNodeScale(locations.Length);
                if (locationNodeRoots.ContainsKey(location.locationId))
                {
                    var nextPosition = ResolveLocationNodePosition(i, locations.Length);
                    var existingNode = locationNodeRoots[location.locationId];
                    var delta = existingNode.anchoredPosition - nextPosition;
                    if (delta.sqrMagnitude > 0.01f)
                    {
                        existingNode.anchoredPosition = nextPosition;
                        shouldRebuildRoutes = true;
                    }

                    var latestName = string.IsNullOrWhiteSpace(location.displayName) ? location.locationId : location.displayName;
                    if (locationThemes.TryGetValue(location.locationId, out var existingTheme) && existingTheme.LabelText != null && existingTheme.LabelText.text != latestName)
                    {
                        existingTheme.LabelText.text = latestName;
                    }
                    shouldRebuildRoutes |= ApplyLocationNodeScale(existingNode, nodeScale);
                    continue;
                }

                var node = CreateRect($"{location.locationId}_Node", locationNodesRoot, new Color(0.78f, 0.92f, 0.62f, 1f));
                node.sizeDelta = new Vector2(220f * nodeScale, 240f * nodeScale);
                node.anchorMin = new Vector2(0.5f, 0.5f);
                node.anchorMax = new Vector2(0.5f, 0.5f);
                node.pivot = new Vector2(0.5f, 0.5f);
                node.anchoredPosition = ResolveLocationNodePosition(i, locations.Length);

                var countLabel = CreateText("Count", node, "0ひき", 24, TextAnchor.MiddleCenter, 32f);
                countLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 96f * nodeScale);
                countLabel.color = new Color(0.28f, 0.2f, 0.1f, 1f);
                locationCountTexts[location.locationId] = countLabel;

                var animalPanel = CreateRect("AnimalPanel", node, new Color(0.99f, 0.96f, 0.9f, 1f));
                animalPanel.sizeDelta = new Vector2(184f * nodeScale, 144f * nodeScale);
                animalPanel.anchorMin = new Vector2(0.5f, 0.5f);
                animalPanel.anchorMax = new Vector2(0.5f, 0.5f);
                animalPanel.pivot = new Vector2(0.5f, 0.5f);
                animalPanel.anchoredPosition = new Vector2(0f, 14f * nodeScale);

                var grid = CreateRect("Entities", animalPanel, new Color(0f, 0f, 0f, 0f));
                grid.anchorMin = new Vector2(0f, 0f);
                grid.anchorMax = new Vector2(1f, 1f);
                grid.pivot = new Vector2(0.5f, 0.5f);
                grid.offsetMin = new Vector2(8f * nodeScale, 8f * nodeScale);
                grid.offsetMax = new Vector2(-8f * nodeScale, -8f * nodeScale);
                var gridComp = grid.gameObject.AddComponent<GridLayoutGroup>();
                gridComp.cellSize = new Vector2(74f * nodeScale, 54f * nodeScale);
                gridComp.spacing = new Vector2(6f * nodeScale, 6f * nodeScale);
                gridComp.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridComp.constraintCount = 2;
                gridComp.childAlignment = TextAnchor.UpperCenter;
                locationEntityGrids[location.locationId] = grid;

                var name = string.IsNullOrWhiteSpace(location.displayName) ? location.locationId : location.displayName;
                var nameLabel = CreateText("Name", node, name, 26, TextAnchor.MiddleCenter, 38f);
                nameLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -96f * nodeScale);
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

        private static bool ApplyLocationNodeScale(RectTransform node, float nodeScale)
        {
            var changed = false;
            changed |= ApplySizeDelta(node, new Vector2(220f * nodeScale, 240f * nodeScale));

            var countLabelRect = node.Find("Count") as RectTransform;
            if (countLabelRect != null)
            {
                changed |= ApplyAnchoredPosition(countLabelRect, new Vector2(0f, 96f * nodeScale));
            }

            var animalPanel = node.Find("AnimalPanel") as RectTransform;
            if (animalPanel != null)
            {
                changed |= ApplySizeDelta(animalPanel, new Vector2(184f * nodeScale, 144f * nodeScale));
                changed |= ApplyAnchoredPosition(animalPanel, new Vector2(0f, 14f * nodeScale));

                var grid = animalPanel.Find("Entities") as RectTransform;
                if (grid != null)
                {
                    changed |= ApplyOffsetMin(grid, new Vector2(8f * nodeScale, 8f * nodeScale));
                    changed |= ApplyOffsetMax(grid, new Vector2(-8f * nodeScale, -8f * nodeScale));
                    var gridComp = grid.GetComponent<GridLayoutGroup>();
                    if (gridComp != null)
                    {
                        changed |= ApplyGridCellSize(gridComp, new Vector2(74f * nodeScale, 54f * nodeScale));
                        changed |= ApplyGridSpacing(gridComp, new Vector2(6f * nodeScale, 6f * nodeScale));
                    }
                }
            }

            var nameLabelRect = node.Find("Name") as RectTransform;
            if (nameLabelRect != null)
            {
                changed |= ApplyAnchoredPosition(nameLabelRect, new Vector2(0f, -96f * nodeScale));
            }

            return changed;
        }

        private static bool ApplySizeDelta(RectTransform rect, Vector2 next)
        {
            var current = rect.sizeDelta;
            if ((current - next).sqrMagnitude <= 0.01f)
            {
                return false;
            }

            rect.sizeDelta = next;
            return true;
        }

        private static bool ApplyAnchoredPosition(RectTransform rect, Vector2 next)
        {
            var current = rect.anchoredPosition;
            if ((current - next).sqrMagnitude <= 0.01f)
            {
                return false;
            }

            rect.anchoredPosition = next;
            return true;
        }

        private static bool ApplyOffsetMin(RectTransform rect, Vector2 next)
        {
            var current = rect.offsetMin;
            if ((current - next).sqrMagnitude <= 0.01f)
            {
                return false;
            }

            rect.offsetMin = next;
            return true;
        }

        private static bool ApplyOffsetMax(RectTransform rect, Vector2 next)
        {
            var current = rect.offsetMax;
            if ((current - next).sqrMagnitude <= 0.01f)
            {
                return false;
            }

            rect.offsetMax = next;
            return true;
        }

        private static bool ApplyGridCellSize(GridLayoutGroup gridComp, Vector2 next)
        {
            var current = gridComp.cellSize;
            if ((current - next).sqrMagnitude <= 0.01f)
            {
                return false;
            }

            gridComp.cellSize = next;
            return true;
        }

        private static bool ApplyGridSpacing(GridLayoutGroup gridComp, Vector2 next)
        {
            var current = gridComp.spacing;
            if ((current - next).sqrMagnitude <= 0.01f)
            {
                return false;
            }

            gridComp.spacing = next;
            return true;
        }

        private Vector2 ResolveLocationNodePosition(int index, int count)
        {
            var nodeScale = ResolveBoardNodeScale(count);
            if (count == 2) return new[] { new Vector2(-220f * nodeScale, 10f * nodeScale), new Vector2(220f * nodeScale, 10f * nodeScale) }[index];
            if (count == 3) return new[] { new Vector2(-260f * nodeScale, 10f * nodeScale), new Vector2(0f, 10f * nodeScale), new Vector2(260f * nodeScale, 10f * nodeScale) }[index];
            var col = index % 2;
            var row = index / 2;
            return new Vector2((col == 0 ? -180f : 180f) * nodeScale, (80f - row * 180f) * nodeScale);
        }

        private float ResolveBoardNodeScale(int count)
        {
            if (boardPanel == null)
            {
                return 1f;
            }

            var area = playableBoardArea != null ? playableBoardArea : boardPanel;
            var availableHeight = Mathf.Max(1f, area.rect.height - 24f);
            var availableWidth = Mathf.Max(1f, area.rect.width - 24f);
            var baseHeight = count <= 3 ? 320f : 500f;
            var baseWidth = count == 2 ? 620f : count == 3 ? 760f : 540f;
            var scaleByHeight = availableHeight / baseHeight;
            var scaleByWidth = availableWidth / baseWidth;
            return Mathf.Min(Mathf.Max(Mathf.Min(scaleByHeight, scaleByWidth), 0.01f), 1f);
        }

        private void RenderRouteLines(StageUIViewContext context)
        {
            var routes = context.StageData.routes ?? Array.Empty<RouteData>();
            var routeKeyParts = routes
                .Select(route => $"{route.routeId}:{route.from}->{route.to}")
                .OrderBy(x => x)
                .ToList();
            var nodePositionKeyParts = locationNodeRoots
                .OrderBy(pair => pair.Key)
                .Select(pair => $"{pair.Key}:{pair.Value.anchoredPosition.x:F1},{pair.Value.anchoredPosition.y:F1}")
                .ToList();
            var nextCacheKey = string.Join("|", routeKeyParts) + "#" + string.Join("|", nodePositionKeyParts);
            if (nextCacheKey == routeTopologyCacheKey)
            {
                return;
            }

            foreach (var line in routeLineVisuals)
            {
                GameObject.Destroy(line.gameObject);
            }
            routeLineVisuals.Clear();
            routeLinesRoot.SetAsFirstSibling();
            foreach (var route in routes)
            {
                if (!locationNodeRoots.TryGetValue(route.from, out var fromNode) || !locationNodeRoots.TryGetValue(route.to, out var toNode))
                {
                    continue;
                }
                var line = CreateRect($"{route.routeId}_Line", routeLinesRoot, new Color(0.95f, 0.94f, 0.82f, 0.9f));
                var diff = toNode.anchoredPosition - fromNode.anchoredPosition;
                var fromRadius = fromNode.rect.width * 0.5f;
                var toRadius = toNode.rect.width * 0.5f;
                var len = Mathf.Max(24f, diff.magnitude - (fromRadius + toRadius));
                var thickness = Mathf.Max(5f, 8f * ResolveBoardNodeScale(locationNodeRoots.Count));
                line.sizeDelta = new Vector2(len, thickness);
                line.anchorMin = new Vector2(0.5f, 0.5f);
                line.anchorMax = new Vector2(0.5f, 0.5f);
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
            selectionCountText.text = $"{context.SelectedEntities.Count}/{capacity}";
            var canSelectMore = context.SelectedEntities.Count < capacity;
            var isCompact = Screen.width < 1100f || (Screen.height > 0 && (float)Screen.width / Screen.height < 0.58f);
            var iconButtonSize = isCompact ? 216f : 240f;
            var selectionIconsRect = selectionIconsContainer as RectTransform;
            if (isCompact && selectionIconsRect != null)
            {
                var candidateCount = Mathf.Max(1, candidates.Count);
                var availableWidth = selectionIconsRect.rect.width;
                var spacingWidth = Mathf.Max(0, candidateCount - 1) * 16f;
                var fitSize = (availableWidth - spacingWidth) / candidateCount;
                iconButtonSize = Mathf.Min(iconButtonSize, fitSize);
                iconButtonSize = Mathf.Max(140f, iconButtonSize);
            }

            var circleSize = Mathf.Max(116f, iconButtonSize - 58f);

            for (var index = 0; index < candidates.Count; index++)
            {
                var entity = candidates[index];
                if (!selectionIconVisuals.TryGetValue(entity.entityId, out var visuals) || visuals?.Button == null)
                {
                    var capturedId = entity.entityId;
                    visuals = CreateEntityVisual(entity.entityId, selectionIconsContainer, () => onEntitySelected?.Invoke(capturedId), iconButtonSize, 24, true);
                    selectionIconVisuals[entity.entityId] = visuals;
                }
                ApplySelectionIconSizing(visuals, iconButtonSize, circleSize);

                TryApplyPortraitSprite(entity, visuals, CharacterSpriteUsage.Icon);
                visuals.Label.text = ResolveEntityDisplayName(entity);
                var isSelected = context.SelectedEntities.Contains(entity.entityId);
                var canInteract = isSelected || canSelectMore;
                visuals.Button.interactable = true;

                if (visuals.ButtonBackground != null)
                {
                    visuals.ButtonBackground.color = Color.clear;
                }

                if (visuals.CircleBorderImage != null)
                {
                    visuals.CircleBorderImage.enabled = true;
                    visuals.CircleBorderImage.color = isSelected
                        ? new Color(0.48f, 0.84f, 0.45f, 1f)
                        : canInteract
                            ? new Color(0.86f, 0.78f, 0.62f, 1f)
                            : new Color(0.78f, 0.78f, 0.78f, 1f);
                }

                if (visuals.CheckmarkText != null)
                {
                    visuals.CheckmarkText.enabled = isSelected;
                    visuals.CheckmarkText.text = "✓";
                }

                visuals.Label.color = isSelected
                    ? new Color(0.12f, 0.45f, 0.16f, 1f)
                    : canInteract
                        ? new Color(0.2f, 0.2f, 0.2f, 1f)
                        : new Color(0.42f, 0.42f, 0.42f, 1f);

                visuals.Button.transform.SetSiblingIndex(index);
            }
        }

        private static void ApplySelectionIconSizing(EntityVisualRefs visuals, float iconButtonSize, float circleSize)
        {
            if (visuals?.Button == null)
            {
                return;
            }

            var buttonRect = visuals.Button.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, iconButtonSize);
                buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, iconButtonSize);
            }

            if (visuals.Button.TryGetComponent<LayoutElement>(out var buttonLayout))
            {
                buttonLayout.preferredWidth = iconButtonSize;
                buttonLayout.preferredHeight = iconButtonSize;
                buttonLayout.minWidth = iconButtonSize;
                buttonLayout.minHeight = iconButtonSize;
                buttonLayout.flexibleWidth = 0f;
                buttonLayout.flexibleHeight = 0f;
            }

            var shell = visuals.Button.transform.Find("CircleRoot");
            var shellRect = shell as RectTransform;
            if (shellRect != null)
            {
                shellRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, circleSize);
                shellRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, circleSize);
            }

            if (shell != null && shell.TryGetComponent<LayoutElement>(out var shellLayout))
            {
                shellLayout.preferredWidth = circleSize;
                shellLayout.preferredHeight = circleSize;
                shellLayout.minWidth = circleSize;
                shellLayout.minHeight = circleSize;
                shellLayout.flexibleWidth = 0f;
                shellLayout.flexibleHeight = 0f;
            }

            if (visuals.PortraitImage != null && visuals.PortraitImage.transform is RectTransform portraitRect)
            {
                var portraitSize = Mathf.Max(92f, circleSize - 8f);
                portraitRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, portraitSize);
                portraitRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, portraitSize);
            }

            var portraitMask = visuals.Button.transform.Find("CircleRoot/CircleMask") as RectTransform;
            if (portraitMask != null)
            {
                var portraitMaskSize = Mathf.Max(96f, circleSize - 24f);
                portraitMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, portraitMaskSize);
                portraitMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, portraitMaskSize);
            }

            if (visuals.CircleBorderImage != null && visuals.CircleBorderImage.transform is RectTransform borderRect)
            {
                var borderSize = circleSize;
                borderRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, borderSize);
                borderRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, borderSize);
            }

            if (visuals.Label != null && visuals.Label.TryGetComponent<LayoutElement>(out var labelLayout))
            {
                const float labelPreferredHeight = 32f;
                const float baseLabelMinHeight = 28f;
                const float minReadableLabelHeight = 14f;
                const float buttonVerticalPaddingAndSpacing = 11f; // 上下padding(4+4) + 要素間spacing(3)

                var availableLabelHeight = iconButtonSize - circleSize - buttonVerticalPaddingAndSpacing;
                var adjustedMinHeight = Mathf.Clamp(availableLabelHeight, minReadableLabelHeight, baseLabelMinHeight);

                var adjustedPreferredHeight = Mathf.Clamp(labelPreferredHeight, adjustedMinHeight, Mathf.Max(adjustedMinHeight, availableLabelHeight));

                labelLayout.preferredHeight = adjustedPreferredHeight;
                labelLayout.minHeight = adjustedMinHeight;
                labelLayout.flexibleHeight = 0f;
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
                markerRect.anchorMin = new Vector2(0.5f, 0.5f);
                markerRect.anchorMax = new Vector2(0.5f, 0.5f);
                markerRect.pivot = new Vector2(0.5f, 0.5f);
                var locationCount = context?.StageData?.locations?.Length ?? locationNodeRoots.Count;
                var nodeScale = ResolveBoardNodeScale(locationCount);
                markerRect.anchoredPosition = node.anchoredPosition + new Vector2(0f, -86f * nodeScale);
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
                if (IsOperationErrorMessage(context.LastMessage))
                {
                    return PopupResultState.OperationError;
                }

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

            if (isRetireConfirmVisible)
            {
                popupOverlay.gameObject.SetActive(true);
                popupTitleText.text = "リタイア確認";
                popupMessageText.text = "広告を見ると、このステージをクリア扱いにできます。\nリタイアしますか？";
                ConfigurePopupButton(popupPrimaryButton, "広告を見てクリア", () =>
                {
                    isRetireConfirmVisible = false;
                    onRetire?.Invoke();
                });
                popupSecondaryButton.gameObject.SetActive(true);
                ConfigurePopupButton(popupSecondaryButton, "キャンセル", () => { isRetireConfirmVisible = false; });
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
                case PopupResultState.OperationError:
                    popupTitleText.text = "操作エラー";
                    popupMessageText.text = context.LastMessage;
                    ConfigurePopupButton(popupPrimaryButton, "OK", () => onDismissOperationError?.Invoke());
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

        private void ShowRetireConfirmPopup()
        {
            isRetireConfirmVisible = true;
        }


        private void RenderStatusMessage(StageUIViewContext context, PopupResultState popupState)
        {
            if (statusMessageText == null)
            {
                return;
            }

            // BottomPanel の縦レイアウトを固定するため、ステータスメッセージは常に非表示にする。
            statusMessageText.gameObject.SetActive(false);
            statusMessageText.text = string.Empty;
            if (statusMessageLayoutElement != null)
            {
                statusMessageLayoutElement.ignoreLayout = true;
                statusMessageLayoutElement.preferredHeight = 0f;
                statusMessageLayoutElement.minHeight = 0f;
                statusMessageLayoutElement.flexibleHeight = 0f;
            }
        }

        private static bool IsOperationErrorMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            if (message.StartsWith("Move成功:", StringComparison.Ordinal) ||
                message.StartsWith("初期化完了:", StringComparison.Ordinal) ||
                message.StartsWith("選択中:", StringComparison.Ordinal) ||
                message == "ステージ一覧を表示しています。" ||
                message == "選択解除しました。")
            {
                return false;
            }

            return true;
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


        private static int ResolveStageIndex(IReadOnlyList<string> stageIds, string stageId)
        {
            if (stageIds == null || string.IsNullOrWhiteSpace(stageId))
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
            text.alignment = circularStyle ? TextAnchor.UpperCenter : TextAnchor.MiddleCenter;
            text.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var buttonLayoutElement = button.GetComponent<LayoutElement>();
            var iconButtonSize = circularStyle ? Mathf.Max(preferredHeight, 148f) : preferredHeight;
            if (buttonLayoutElement != null && circularStyle)
            {
                buttonLayoutElement.preferredWidth = iconButtonSize;
                buttonLayoutElement.preferredHeight = iconButtonSize;
                buttonLayoutElement.minWidth = iconButtonSize;
                buttonLayoutElement.minHeight = iconButtonSize;
                buttonLayoutElement.flexibleWidth = 0f;
                buttonLayoutElement.flexibleHeight = 0f;
            }

            var buttonLayout = button.gameObject.AddComponent<VerticalLayoutGroup>();
            buttonLayout.padding = circularStyle ? new RectOffset(4, 4, 4, 4) : new RectOffset(4, 4, 4, 4);
            buttonLayout.spacing = circularStyle ? 3f : 2f;
            buttonLayout.childAlignment = circularStyle ? TextAnchor.UpperCenter : TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = false;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = false;

            var buttonImage = button.image;
            buttonImage.color = Color.clear;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.99f, 1f, 1f);
            colors.pressedColor = new Color(0.9f, 0.95f, 1f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            button.colors = colors;

            var shell = CreateRect("CircleRoot", button.transform, Color.clear);
            shell.transform.SetSiblingIndex(0);
            var shellLayout = shell.gameObject.AddComponent<LayoutElement>();
            var circleSize = circularStyle ? iconButtonSize - 36f : preferredHeight - 24f;
            shellLayout.preferredHeight = circleSize;
            shellLayout.preferredWidth = circleSize;
            shellLayout.minHeight = circleSize;
            shellLayout.minWidth = circleSize;
            shellLayout.flexibleWidth = 0f;
            shellLayout.flexibleHeight = 0f;
            var shellImage = shell.GetComponent<Image>();

            var portraitMask = CreateRect("CircleMask", shell.transform, Color.white);
            var portraitMaskImage = portraitMask.GetComponent<Image>();
            var portraitMaskComp = portraitMask.gameObject.AddComponent<Mask>();
            portraitMaskComp.showMaskGraphic = false;

            var portrait = CreateRect("PortraitImage", portraitMask.transform, new Color(0.7f, 0.85f, 0.95f, 1f));
            if (circularStyle)
            {
                portraitMask.anchorMin = new Vector2(0.5f, 0.5f);
                portraitMask.anchorMax = new Vector2(0.5f, 0.5f);
                portraitMask.pivot = new Vector2(0.5f, 0.5f);
                var portraitMaskSize = Mathf.Max(96f, circleSize - 24f);
                portraitMask.sizeDelta = new Vector2(portraitMaskSize, portraitMaskSize);

                portrait.anchorMin = new Vector2(0.5f, 0.5f);
                portrait.anchorMax = new Vector2(0.5f, 0.5f);
                portrait.pivot = new Vector2(0.5f, 0.5f);
                var portraitSize = Mathf.Max(92f, portraitMaskSize - 4f);
                portrait.sizeDelta = new Vector2(portraitSize, portraitSize);
            }
            else
            {
                ApplyFullStretch(portraitMask, 16f, 16f, 16f, 16f);
                ApplyFullStretch(portrait, 0f, 0f, 0f, 0f);
            }

            var portraitText = CreateText("PortraitText", portrait.transform, "IMG", 18, TextAnchor.MiddleCenter, shellLayout.preferredHeight - 32f);
            portraitText.color = new Color(0.15f, 0.2f, 0.25f, 0.9f);

            var border = CreateRect("CircleBorderImage", shell.transform, Color.white);
            if (circularStyle)
            {
                border.anchorMin = new Vector2(0.5f, 0.5f);
                border.anchorMax = new Vector2(0.5f, 0.5f);
                border.pivot = new Vector2(0.5f, 0.5f);
                border.sizeDelta = new Vector2(circleSize, circleSize);
            }
            else
            {
                ApplyFullStretch(border, -4f, -4f, -4f, -4f);
            }
            border.transform.SetAsLastSibling();
            var borderImage = border.GetComponent<Image>();
            borderImage.raycastTarget = false;
            borderImage.enabled = circularStyle;

            var checkmark = CreateText("Checkmark", shell.transform, "✓", 38, TextAnchor.MiddleCenter, 44f);
            var checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(1f, 0f);
            checkmarkRect.anchorMax = new Vector2(1f, 0f);
            checkmarkRect.pivot = new Vector2(1f, 0f);
            checkmarkRect.anchoredPosition = new Vector2(-8f, 4f);
            checkmarkRect.sizeDelta = new Vector2(44f, 44f);
            checkmark.color = new Color(0.16f, 0.68f, 0.24f, 1f);
            checkmark.raycastTarget = false;
            checkmark.enabled = false;

            if (circularStyle)
            {
                text.fontSize = Mathf.Max(labelFontSize, 24);
                text.resizeTextForBestFit = false;
                var textLayout = text.GetComponent<LayoutElement>();
                if (textLayout != null)
                {
                    textLayout.preferredHeight = 32f;
                    textLayout.minHeight = 28f;
                    textLayout.flexibleHeight = 0f;
                }
                shellImage.sprite = null;
                shellImage.type = Image.Type.Simple;
                shellImage.color = Color.clear;
                portraitMaskImage.sprite = GetCircularMaskSprite();
                portraitMaskImage.type = Image.Type.Simple;
                portraitMaskImage.color = Color.white;
                var portraitImage = portrait.GetComponent<Image>();
                portraitImage.sprite = null;
                portraitImage.type = Image.Type.Simple;
                portraitImage.color = Color.white;
                portraitImage.preserveAspect = true;
                borderImage.sprite = GetCircularBorderSprite();
                borderImage.type = Image.Type.Simple;
                borderImage.color = new Color(0.86f, 0.78f, 0.62f, 1f);
            }

            return new EntityVisualRefs
            {
                Button = button,
                PortraitImage = portrait.GetComponent<Image>(),
                ButtonBackground = shellImage,
                CircleBorderImage = borderImage,
                Label = text,
                PortraitPlaceholderText = portraitText,
                CheckmarkText = checkmark
            };
        }

        private void ApplyResponsiveLayout()
        {
            var logicalWidth = Screen.width;
            var logicalHeight = Screen.height;
            var aspect = logicalHeight > 0 ? (float)logicalWidth / logicalHeight : 1f;
            var compact = logicalWidth < 1100f || aspect < 0.58f;

            if (objectiveLayoutElement != null)
            {
                objectiveLayoutElement.ignoreLayout = true;
                objectiveLayoutElement.preferredHeight = 0f;
                objectiveLayoutElement.minHeight = 0f;
                objectiveLayoutElement.flexibleHeight = 0f;
            }

            if (selectionCountLayoutElement != null)
            {
                selectionCountLayoutElement.preferredHeight = compact ? 26f : 30f;
                selectionCountLayoutElement.minHeight = compact ? 24f : 28f;
            }

            if (selectionIconPanelLayoutElement != null)
            {
                selectionIconPanelLayoutElement.preferredHeight = compact ? 258f : 278f;
                selectionIconPanelLayoutElement.minHeight = compact ? 244f : 264f;
            }

            if (routeRowLayoutElement != null)
            {
                routeRowLayoutElement.preferredHeight = compact ? 72f : 80f;
                routeRowLayoutElement.minHeight = compact ? 68f : 76f;
            }

            var headerHeight = compact ? 132f : 140f;
            var bottomHeight = compact ? 398f : 438f;

            if (playableBoardArea != null)
            {
                playableBoardArea.offsetMin = new Vector2(24f, bottomHeight);
                playableBoardArea.offsetMax = new Vector2(-24f, -headerHeight);
            }

            if (headerOverlay != null)
            {
                headerOverlay.offsetMin = new Vector2(0f, -headerHeight);
                headerOverlay.offsetMax = new Vector2(0f, 0f);
            }

            if (bottomOverlay != null)
            {
                bottomOverlay.offsetMin = new Vector2(0f, 0f);
                bottomOverlay.offsetMax = new Vector2(0f, bottomHeight);
            }

            if (bottomLayoutGroup != null)
            {
                bottomLayoutGroup.spacing = compact ? 8f : 10f;
                bottomLayoutGroup.padding = compact ? new RectOffset(10, 10, 10, 10) : new RectOffset(12, 12, 12, 12);
                bottomLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
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

            ScaleText(stageNameText, compact ? 32 : 36);
            ScaleText(movesText, compact ? 28 : 32);
            ScaleText(selectionCountText, compact ? 30 : 34);
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

        private static Sprite GetCircularBorderSprite()
        {
            if (circularBorderSprite != null)
            {
                return circularBorderSprite;
            }

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "GeneratedCircularBorderSprite";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var center = (size - 1) * 0.5f;
            var radius = center - 2f;
            var borderThickness = 6f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var edgeDistance = Mathf.Abs(distance - radius);
                    var alpha = edgeDistance <= borderThickness * 0.5f ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            circularBorderSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            circularBorderSprite.name = "GeneratedCircularBorderSprite";
            return circularBorderSprite;
        }

        private static Sprite GetCircularMaskSprite()
        {
            if (circularMaskSprite != null)
            {
                return circularMaskSprite;
            }

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "GeneratedCircularMaskSprite";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var center = (size - 1) * 0.5f;
            var radius = center - 2f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = distance <= radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            circularMaskSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            circularMaskSprite.name = "GeneratedCircularMaskSprite";
            return circularMaskSprite;
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
