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
        private readonly Dictionary<string, Button> entityButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, string> entityParents = new Dictionary<string, string>();
        private readonly Dictionary<string, RectTransform> locationPanels = new Dictionary<string, RectTransform>();
        private readonly List<Button> routeButtons = new List<Button>();

        private Transform leftContainer;
        private Transform rightContainer;
        private Transform boatContainer;

        private Text stageTitleText;
        private Text stageNameText;
        private Text movesText;
        private Text resultText;
        private Text messageText;
        private Text boatLocationText;
        private HorizontalLayoutGroup midLayoutGroup;
        private VerticalLayoutGroup mainLayoutGroup;
        private VerticalLayoutGroup bottomLayoutGroup;

        private RectTransform routeRowTransform;
        private RectTransform actionRowTransform;

        private Button restartButton;
        private Button nextStageButton;

        private Action<string> onEntitySelected;
        private Action<string> onMove;
        private Action onClearSelection;
        private Action onRestart;
        private Action onNextStage;

        public void Initialize(Transform parent,
            Action<string> onEntitySelected,
            Action<string> onMove,
            Action onClearSelection,
            Action onRestart,
            Action onNextStage)
        {
            this.onEntitySelected = onEntitySelected;
            this.onMove = onMove;
            this.onClearSelection = onClearSelection;
            this.onRestart = onRestart;
            this.onNextStage = onNextStage;

            BuildRoot(parent);
        }

        public void Render(StageUIViewContext context)
        {
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
            nextStageButton.interactable = context.GameState.IsCleared && !context.GameState.IsFailed && context.GameState.IsExactlyOptimalMoves();
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

            var left = CreateLocationArea("左岸", midLayout, "left");
            leftContainer = left;
            var river = CreateLocationArea("川", midLayout, "river");
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
            boatLocationText = CreateText("BoatLocation", boatContainer, "", 32, TextAnchor.MiddleCenter, 50);

            var right = CreateLocationArea("右岸", midLayout, "right");
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

            ApplyResponsiveLayout();
        }

        private void RenderEntities(StageUIViewContext context)
        {
            var entities = context.StageData.entities ?? Array.Empty<EntityData>();
            var activeIds = new HashSet<string>(entities.Select(x => x.entityId));

            var staleIds = entityButtons.Keys.Where(id => !activeIds.Contains(id)).ToList();
            foreach (var staleId in staleIds)
            {
                if (entityButtons.TryGetValue(staleId, out var staleButton) && staleButton != null)
                {
                    GameObject.Destroy(staleButton.gameObject);
                }

                entityButtons.Remove(staleId);
                entityParents.Remove(staleId);
            }

            var leftSiblingIndex = 0;
            var rightSiblingIndex = 0;

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

                if (!entityButtons.TryGetValue(entityId, out var button) || button == null)
                {
                    button = CreateButton(entityId, parent, entityId, () => onEntitySelected?.Invoke(entityId));
                    entityButtons[entityId] = button;
                    entityParents[entityId] = parentKey;
                }
                else if (!entityParents.TryGetValue(entityId, out var currentParentKey) || currentParentKey != parentKey)
                {
                    button.transform.SetParent(parent, false);
                    entityParents[entityId] = parentKey;
                }

                button.transform.SetSiblingIndex(siblingIndex);

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = isAtBoatLocation ? entityId : $"{entityId}（反対岸）";
                }

                button.interactable = isAtBoatLocation;
                if (context.SelectedEntities.Contains(entityId))
                {
                    button.image.color = new Color(1f, 0.9f, 0.3f, 1f);
                }
                else if (!isAtBoatLocation)
                {
                    button.image.color = new Color(0.55f, 0.55f, 0.55f, 0.8f);
                }
                else
                {
                    button.image.color = Color.white;
                }
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

        private static Transform CreateLocationArea(string label, Transform parent, string key)
        {
            var panel = CreateRect($"{key}Panel", parent, new Color(0.22f, 0.22f, 0.3f, 0.8f));
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
            CreateText("Label", panel, label, 34, TextAnchor.MiddleCenter, 52);
            return panel;
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
