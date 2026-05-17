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
        private readonly List<Button> stageButtons = new List<Button>();
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

        private Button boardButton;
        private Button unboardButton;
        private Button moveButton;
        private Button restartButton;
        private Button nextStageButton;

        private Action<string> onStageSelected;
        private Action<string> onEntitySelected;
        private Action onBoard;
        private Action onUnboard;
        private Action<string> onMove;
        private Action onRestart;
        private Action onNextStage;

        public void Initialize(Transform parent,
            Action<string> onStageSelected,
            Action<string> onEntitySelected,
            Action onBoard,
            Action onUnboard,
            Action<string> onMove,
            Action onRestart,
            Action onNextStage)
        {
            this.onStageSelected = onStageSelected;
            this.onEntitySelected = onEntitySelected;
            this.onBoard = onBoard;
            this.onUnboard = onUnboard;
            this.onMove = onMove;
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

            RenderStageButtons(context);
            RenderEntities(context);
            RenderRoutes(context);
            nextStageButton.interactable = context.GameState.IsCleared && !context.GameState.IsFailed && context.GameState.IsExactlyOptimalMoves();
        }

        private void BuildRoot(Transform parent)
        {
            var root = CreateRect("CanvasRoot", parent, new Color(0f, 0f, 0f, 0f));
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var main = CreateVerticalLayout("Main", root, 12f);
            main.offsetMin = new Vector2(20, 20);
            main.offsetMax = new Vector2(-20, -20);

            var top = CreatePanel("TopPanel", main, new Color(0.15f, 0.2f, 0.25f, 0.8f), 180);
            stageTitleText = CreateText("Title", top, "", 30, TextAnchor.UpperCenter);
            stageNameText = CreateText("StageName", top, "", 26, TextAnchor.UpperCenter);
            movesText = CreateText("Moves", top, "", 24, TextAnchor.UpperCenter);

            var mid = CreatePanel("MiddlePanel", main, new Color(0.15f, 0.15f, 0.2f, 0.7f), 540);
            var midLayout = CreateHorizontalLayout("MidLayout", mid, 10f);

            var left = CreateLocationArea("左岸", midLayout, "left");
            leftContainer = left;
            var river = CreateLocationArea("川", midLayout, "river");
            boatContainer = CreateRect("BoatContainer", river, new Color(0.6f, 0.5f, 0.2f, 0.9f));
            var boatLayout = boatContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            boatLayout.childControlHeight = false;
            boatLayout.childControlWidth = true;
            boatLayout.childForceExpandHeight = false;
            boatLayout.childAlignment = TextAnchor.UpperCenter;
            boatLocationText = CreateText("BoatLocation", boatContainer, "", 20, TextAnchor.MiddleCenter);

            var right = CreateLocationArea("右岸", midLayout, "right");
            rightContainer = right;

            locationPanels["left"] = left as RectTransform;
            locationPanels["right"] = right as RectTransform;
            locationPanels["river"] = river as RectTransform;

            var bottom = CreatePanel("BottomPanel", main, new Color(0.2f, 0.15f, 0.2f, 0.8f), 280);
            var stageRow = CreateHorizontalLayout("StageRow", bottom, 8f);
            for (var i = 0; i < 4; i++)
            {
                var button = CreateButton($"Stage{i + 1}", stageRow, $"Stage{i + 1}", () => { });
                stageButtons.Add(button);
            }

            var routeRow = CreateHorizontalLayout("RouteRow", bottom, 8f);
            for (var i = 0; i < 2; i++)
            {
                var routeButton = CreateButton($"Route{i + 1}", routeRow, "Move", () => { });
                routeButtons.Add(routeButton);
            }

            var actionRow = CreateHorizontalLayout("ActionRow", bottom, 8f);
            boardButton = CreateButton("Board", actionRow, "乗船", () => onBoard?.Invoke());
            unboardButton = CreateButton("Unboard", actionRow, "降船", () => onUnboard?.Invoke());
            moveButton = CreateButton("Move", actionRow, "移動", () => onMove?.Invoke(null));
            restartButton = CreateButton("Restart", actionRow, "Restart", () => onRestart?.Invoke());
            nextStageButton = CreateButton("NextStage", actionRow, "NextStage", () => onNextStage?.Invoke());

            var resultPanel = CreatePanel("ResultPanel", main, new Color(0.1f, 0.3f, 0.2f, 0.7f), 120);
            resultText = CreateText("Result", resultPanel, "", 26, TextAnchor.MiddleCenter);
            messageText = CreateText("Message", resultPanel, "", 20, TextAnchor.MiddleCenter);
        }

        private void RenderStageButtons(StageUIViewContext context)
        {
            for (var i = 0; i < stageButtons.Count; i++)
            {
                if (i >= context.StageIds.Count)
                {
                    stageButtons[i].gameObject.SetActive(false);
                    continue;
                }

                var stageId = context.StageIds[i];
                stageButtons[i].gameObject.SetActive(true);
                stageButtons[i].GetComponentInChildren<Text>().text = stageId;
                stageButtons[i].image.color = stageId == context.ActiveStageId ? new Color(0.3f, 0.8f, 0.4f, 1f) : Color.white;
                stageButtons[i].onClick.RemoveAllListeners();
                stageButtons[i].onClick.AddListener(() => onStageSelected?.Invoke(stageId));
            }
        }

        private void RenderEntities(StageUIViewContext context)
        {
            foreach (Transform child in leftContainer) { if (child.name != "Label") GameObject.Destroy(child.gameObject); }
            foreach (Transform child in rightContainer) { if (child.name != "Label") GameObject.Destroy(child.gameObject); }
            foreach (Transform child in boatContainer) { if (child.name != "BoatLocation") GameObject.Destroy(child.gameObject); }

            var entities = context.StageData.entities ?? Array.Empty<EntityData>();
            foreach (var entity in entities.OrderBy(x => x.entityId))
            {
                var location = context.GameState.EntityLocations[entity.entityId];
                var parent = context.OnboardPassengers.Contains(entity.entityId)
                    ? boatContainer
                    : location == "left" ? leftContainer : rightContainer;

                var button = CreateButton(entity.entityId, parent, entity.entityId, () => onEntitySelected?.Invoke(entity.entityId));
                if (context.SelectedEntities.Contains(entity.entityId))
                {
                    button.image.color = new Color(1f, 0.9f, 0.3f, 1f);
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
                routeButtons[i].GetComponentInChildren<Text>().text = $"{context.GameState.BoatLocation}→{destination}";
                routeButtons[i].onClick.RemoveAllListeners();
                routeButtons[i].onClick.AddListener(() => onMove?.Invoke(route.routeId));
            }

            moveButton.interactable = context.OnboardPassengers.Count > 0 && context.AvailableRoutes.Count > 0;
        }

        private static string BuildResultText(RiverCrossingGameState gameState)
        {
            if (gameState.IsFailed) return "FAILED";
            if (gameState.IsCleared && gameState.IsExactlyOptimalMoves()) return "CLEAR";
            if (gameState.IsCleared) return "NOT OPTIMAL";
            return "PLAYING";
        }

        private static RectTransform CreateRect(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return go.GetComponent<RectTransform>();
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color, float preferredHeight)
        {
            var rect = CreateRect(name, parent, color);
            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            return rect;
        }

        private static RectTransform CreateVerticalLayout(string name, Transform parent, float spacing)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return rect;
        }

        private static RectTransform CreateHorizontalLayout(string name, Transform parent, float spacing)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
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
            panel.gameObject.AddComponent<LayoutElement>().preferredWidth = 300;
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            CreateText("Label", panel, label, 24, TextAnchor.MiddleCenter);
            return panel;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = Color.white;
            go.AddComponent<LayoutElement>().preferredHeight = 40;
            return txt;
        }

        private static Button CreateButton(string name, Transform parent, string text, Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = Color.white;
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            go.AddComponent<LayoutElement>().preferredHeight = 56;

            CreateText("Text", go.transform, text, 22, TextAnchor.MiddleCenter).color = Color.black;
            return btn;
        }
    }
}
