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

        private readonly Dictionary<string, EntityVisualRefs> entityVisuals = new Dictionary<string, EntityVisualRefs>();
        private readonly Dictionary<string, string> entityParents = new Dictionary<string, string>();
        private readonly Dictionary<string, RectTransform> locationPanels = new Dictionary<string, RectTransform>();
        private readonly Dictionary<string, LocationVisualTheme> locationThemes = new Dictionary<string, LocationVisualTheme>();
        private readonly List<Button> routeButtons = new List<Button>();
        private readonly Dictionary<string, Sprite> portraitSpriteCache = new Dictionary<string, Sprite>();

        private Transform leftContainer;
        private Transform rightContainer;
        private Transform boatContainer;

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
            boatLocationText.text = $"ボート位置: {context.GameState.BoatLocation}";

            RenderEntities(context);
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
            var midLayout = CreateHorizontalLayout("MidLayout", mid, 12f, true);
            midLayoutGroup = midLayout.GetComponent<HorizontalLayoutGroup>();

            var left = CreateLocationArea("左岸", midLayout, "left", new Color(0.3f, 0.38f, 0.26f, 0.9f));
            leftContainer = left;
            var river = CreateLocationArea("川", midLayout, "river", new Color(0.2f, 0.35f, 0.55f, 0.9f));
            boatContainer = CreateRect("BoatContainer", river, new Color(0.6f, 0.5f, 0.2f, 0.9f));
            var boatLayout = boatContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            boatLayout.childControlHeight = true;
            boatLayout.childControlWidth = true;
            boatLayout.childForceExpandHeight = false;
            boatLayout.childForceExpandWidth = true;
            boatLayout.childAlignment = TextAnchor.UpperCenter;
            boatLayout.spacing = 8f;
            boatLayout.padding = new RectOffset(12, 12, 12, 12);
            var boatAreaLayout = boatContainer.gameObject.AddComponent<LayoutElement>();
            boatAreaLayout.minHeight = 240f;
            boatAreaLayout.flexibleHeight = 1f;
            boatImage = CreateRect("BoatImage", boatContainer, new Color(0.75f, 0.55f, 0.2f, 1f)).GetComponent<Image>();
            var boatImageLayout = boatImage.gameObject.AddComponent<LayoutElement>();
            boatImageLayout.preferredHeight = 150f;
            boatImageLayout.minHeight = 100f;
            boatLabelText = CreateText("BoatVisualLabel", boatImage.transform, "Boat << LEFT", 28, TextAnchor.MiddleCenter, 150f);
            boatLabelText.color = new Color(0.15f, 0.1f, 0.08f, 1f);
            boatLocationText = CreateText("BoatLocation", boatContainer, "", 32, TextAnchor.MiddleCenter, 50);
            boatSprite = Resources.Load<Sprite>("Sprites/Boat/boat");

            var right = CreateLocationArea("右岸", midLayout, "right", new Color(0.4f, 0.3f, 0.2f, 0.9f));
            rightContainer = right;

            locationPanels["left"] = left as RectTransform;
            locationPanels["right"] = right as RectTransform;
            locationPanels["river"] = river as RectTransform;

            var bottom = CreatePanel("BottomPanel", main, new Color(0.2f, 0.15f, 0.2f, 0.8f), 420f, 360f);
            bottomLayoutGroup = bottom.gameObject.AddComponent<VerticalLayoutGroup>();
            bottomLayoutGroup.spacing = 12f;
            bottomLayoutGroup.padding = new RectOffset(12, 12, 12, 12);
            bottomLayoutGroup.childControlHeight = true;
            bottomLayoutGroup.childControlWidth = true;
            bottomLayoutGroup.childForceExpandHeight = false;
            bottomLayoutGroup.childForceExpandWidth = true;

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

        private void RenderEntities(StageUIViewContext context)
        {
            var entities = context.StageData.entities ?? Array.Empty<EntityData>();
            var activeIds = new HashSet<string>(entities.Select(x => x.entityId));

            var staleIds = entityVisuals.Keys.Where(id => !activeIds.Contains(id)).ToList();
            foreach (var staleId in staleIds)
            {
                if (entityVisuals.TryGetValue(staleId, out var staleVisuals) && staleVisuals?.Button != null)
                {
                    GameObject.Destroy(staleVisuals.Button.gameObject);
                }

                entityVisuals.Remove(staleId);
                entityParents.Remove(staleId);
            }

            var leftSiblingIndex = 1;
            var rightSiblingIndex = 1;

            foreach (var entity in entities)
            {
                var entityId = entity.entityId;
                if (!context.GameState.EntityLocations.TryGetValue(entityId, out var location))
                {
                    continue;
                }

                var parent = location == "left" ? leftContainer : rightContainer;
                var parentKey = location == "left" ? "left" : "right";
                var siblingIndex = parentKey == "left" ? leftSiblingIndex++ : rightSiblingIndex++;
                var isAtBoatLocation = location == context.GameState.BoatLocation;

                if (!entityVisuals.TryGetValue(entityId, out var visuals) || visuals?.Button == null)
                {
                    visuals = CreateEntityVisual(entityId, parent, () => onEntitySelected?.Invoke(entityId));
                    entityVisuals[entityId] = visuals;
                    entityParents[entityId] = parentKey;
                }
                else if (!entityParents.TryGetValue(entityId, out var currentParentKey) || currentParentKey != parentKey)
                {
                    visuals.Button.transform.SetParent(parent, false);
                    entityParents[entityId] = parentKey;
                }

                visuals.Button.transform.SetSiblingIndex(siblingIndex);
                TryApplyPortraitSprite(entity, visuals);
                visuals.Label.text = isAtBoatLocation ? entityId : $"{entityId}（反対岸）";
                visuals.Button.interactable = isAtBoatLocation;
                if (context.SelectedEntities.Contains(entityId))
                {
                    visuals.Button.image.color = new Color(0.9f, 0.82f, 0.3f, 1f);
                    visuals.PortraitImage.color = visuals.HasPortraitSprite
                        ? Color.white
                        : new Color(0.98f, 0.95f, 0.6f, 1f);
                    visuals.SelectionFrame.enabled = true;
                    visuals.SelectionFrame.color = new Color(1f, 0.96f, 0.45f, 1f);
                }
                else if (!isAtBoatLocation)
                {
                    visuals.Button.image.color = new Color(0.45f, 0.45f, 0.5f, 0.9f);
                    visuals.PortraitImage.color = visuals.HasPortraitSprite
                        ? new Color(0.72f, 0.72f, 0.72f, 1f)
                        : new Color(0.64f, 0.64f, 0.64f, 1f);
                    visuals.SelectionFrame.enabled = false;
                }
                else
                {
                    visuals.Button.image.color = new Color(0.88f, 0.88f, 0.94f, 1f);
                    visuals.PortraitImage.color = visuals.HasPortraitSprite
                        ? Color.white
                        : new Color(0.7f, 0.85f, 0.95f, 1f);
                    visuals.SelectionFrame.enabled = false;
                }
            }
            UpdateLocationVisualTheme(context.GameState.BoatLocation);
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

            UpdateBoatVisual(context.GameState.BoatLocation);
        }


        private void UpdateBoatVisual(string boatLocation)
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
                    boatImage.color = boatLocation == "left"
                        ? new Color(0.75f, 0.55f, 0.2f, 1f)
                        : new Color(0.55f, 0.75f, 0.25f, 1f);
                }
            }

            if (boatLabelText != null)
            {
                boatLabelText.enabled = boatSprite == null;
                boatLabelText.text = boatLocation == "left" ? "Boat << LEFT" : "Boat RIGHT >>";
            }
        }

        private void TryApplyPortraitSprite(EntityData entity, EntityVisualRefs visuals)
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

            var cacheKey = BuildPortraitCacheKey(entity);
            if (!portraitSpriteCache.TryGetValue(cacheKey, out var sprite))
            {
                var spriteResourceId = !string.IsNullOrWhiteSpace(entity.spriteId) ? entity.spriteId : entity.entityId;
                sprite = Resources.Load<Sprite>($"Sprites/Characters/{spriteResourceId}");
                portraitSpriteCache[cacheKey] = sprite;
            }
            if (sprite == null)
            {
                ApplyPortraitFallback(visuals);
                return;
            }

            visuals.HasPortraitSprite = true;
            visuals.PortraitImage.sprite = sprite;
            visuals.PortraitImage.type = Image.Type.Simple;
            visuals.PortraitImage.preserveAspect = true;
            if (visuals.PortraitPlaceholderText != null)
            {
                visuals.PortraitPlaceholderText.enabled = false;
            }
        }


        private static string BuildPortraitCacheKey(EntityData entity)
        {
            var spriteResourceId = !string.IsNullOrWhiteSpace(entity.spriteId) ? entity.spriteId : entity.entityId;
            return $"{entity.entityId}:{spriteResourceId}";
        }

        private static void ApplyPortraitFallback(EntityVisualRefs visuals)
        {
            visuals.HasPortraitSprite = false;
            visuals.PortraitImage.sprite = null;
            visuals.PortraitImage.type = Image.Type.Simple;
            visuals.PortraitImage.preserveAspect = true;
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
            stageDetailText.text = $"ステージ{index + 1}\n{stageData.title}\n{ResolveStageDescription(stageId)}\nクリア条件: 全員を右岸へ運ぶ";
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

        private static string ResolveStageDescription(string stageId)
        {
            return stageId switch
            {
                "stage_001" => "2匹を最短手数で運ぼう",
                "stage_002" => "3匹を上手に運ぼう",
                "stage_003" => "漕げるチラだけで運ぼう",
                "stage_004" => "4匹を最短手数で運ぼう",
                _ => "最短手数を目指して川を渡ろう"
            };
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


        private static EntityVisualRefs CreateEntityVisual(string entityId, Transform parent, Action onClick)
        {
            var button = CreateButton(entityId, parent, string.Empty, onClick, 90f, 20);
            var text = button.GetComponentInChildren<Text>();
            text.alignment = TextAnchor.LowerCenter;

            var portrait = CreateRect("Portrait", button.transform, new Color(0.7f, 0.85f, 0.95f, 1f));
            portrait.transform.SetSiblingIndex(0);
            var portraitLayout = portrait.gameObject.AddComponent<LayoutElement>();
            portraitLayout.preferredHeight = 50f;
            portraitLayout.minHeight = 40f;

            var portraitText = CreateText("PortraitText", portrait.transform, "IMG", 18, TextAnchor.MiddleCenter, 50f);
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
