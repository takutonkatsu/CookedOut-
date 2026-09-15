using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookedOut.Presentation
{
    public sealed class HomeScreenController : MonoBehaviour
    {
        public const string HomeSceneName = "Home";

        public static readonly string[] StageSceneNames =
        {
            "Tutorial_1_1",
            "Tutorial_1_2",
            "Tutorial_1_3",
            "Tutorial_1_4",
            "Tutorial_1_5",
            "Tutorial_1_6",
            "Tutorial_1_7",
            "Tutorial_1_8"
        };

        public void PlayStage(string sceneName)
        {
            if (!StageSceneNames.Contains(sceneName))
            {
                throw new ArgumentException("Unknown stage scene: " + sceneName, nameof(sceneName));
            }

            SceneManager.LoadScene(sceneName);
        }

        public static void ReturnHome()
        {
            SceneManager.LoadScene(HomeSceneName);
        }
    }

    public static class HomeSceneBootstrap
    {
        private readonly struct StageCard
        {
            public StageCard(string number, string title, string description, string sceneName, Color color)
            {
                Number = number;
                Title = title;
                Description = description;
                SceneName = sceneName;
                Color = color;
            }

            public string Number { get; }
            public string Title { get; }
            public string Description { get; }
            public string SceneName { get; }
            public Color Color { get; }
        }

        private static readonly StageCard[] Stages =
        {
            new StageCard("1-1", "サラダの基本", "切る  •  盛り付け  •  提供", "Tutorial_1_1",
                new Color(0.16f, 0.68f, 0.42f)),
            new StageCard("1-2", "スープ厨房", "切る  •  煮る  •  提供", "Tutorial_1_2",
                new Color(0.95f, 0.50f, 0.12f)),
            new StageCard("1-3", "投げて配達", "投げる  •  調理  •  配達", "Tutorial_1_3",
                new Color(0.55f, 0.24f, 0.68f)),
            new StageCard("1-4", "ダッシュ配達", "調理  •  ダッシュ  •  提供", "Tutorial_1_4",
                new Color(0.17f, 0.58f, 0.86f)),
            new StageCard("1-5", "フライパン調理", "切る  •  焼く  •  提供", "Tutorial_1_5",
                new Color(0.86f, 0.25f, 0.18f)),
            new StageCard("1-6", "火災から復旧", "消火  •  2種類の料理", "Tutorial_1_6",
                new Color(0.92f, 0.32f, 0.08f)),
            new StageCard("1-7", "皿を洗って再利用", "提供  •  洗浄  •  再利用", "Tutorial_1_7",
                new Color(0.10f, 0.67f, 0.72f)),
            new StageCard("1-8", "厨房と配達の実践", "代行  •  バイク  •  複合調理", "Tutorial_1_8",
                new Color(0.54f, 0.30f, 0.76f))
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneLoadHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureInstalled()
        {
            if (SceneManager.GetActiveScene().name != HomeScreenController.HomeSceneName ||
                UnityEngine.Object.FindFirstObjectByType<HomeScreenController>() != null)
            {
                return;
            }

            BuildHomeScreen();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureInstalled();
        }

        private static void BuildHomeScreen()
        {
            var root = new GameObject("CookedOut_Home");
            var controller = root.AddComponent<HomeScreenController>();

            var cameraObject = new GameObject("Home Camera");
            cameraObject.transform.SetParent(root.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = KitchenHudVisuals.DeepPlum;
            camera.cullingMask = 0;
            cameraObject.tag = "MainCamera";

            var canvasObject = new GameObject("Home Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(2436f, 1125f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var background = CreateImage(canvasObject.transform, "Background", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0.075f, 0.025f, 0.085f));
            CreateImage(background.transform, "Top Glow", new Vector2(0f, 0.70f), Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0.19f, 0.06f, 0.20f, 0.88f));
            CreateImage(background.transform, "Header Stripe", new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -11f), new Vector2(0f, 22f), new Color(0.98f, 0.49f, 0.10f));

            var safeAreaObject = new GameObject("Safe Area", typeof(RectTransform));
            safeAreaObject.transform.SetParent(canvasObject.transform, false);
            Stretch(safeAreaObject.GetComponent<RectTransform>());
            safeAreaObject.AddComponent<SafeAreaFitter>().ApplySafeArea();
            var safeArea = safeAreaObject.transform;

            var title = CreateText(safeArea, "Game Title", new Vector2(0.5f, 1f), new Vector2(0f, -104f),
                new Vector2(1600f, 120f), 84, TextAnchor.MiddleCenter, "COOKED OUT!");
            title.color = KitchenHudVisuals.Cream;
            title.fontStyle = FontStyle.Bold;
            var titleShadow = title.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            titleShadow.effectDistance = new Vector2(0f, -6f);

            var subtitle = CreateText(safeArea, "Stage Select", new Vector2(0.5f, 1f),
                new Vector2(0f, -185f), new Vector2(900f, 54f), 30, TextAnchor.MiddleCenter,
                "厨房を選ぶ");
            subtitle.color = new Color(0.72f, 0.80f, 0.83f);

            var positions = new[]
            {
                new Vector2(-840f, 105f),
                new Vector2(-280f, 105f),
                new Vector2(280f, 105f),
                new Vector2(840f, 105f),
                new Vector2(-840f, -205f),
                new Vector2(-280f, -205f),
                new Vector2(280f, -205f),
                new Vector2(840f, -205f)
            };
            for (var i = 0; i < Stages.Length; i++)
            {
                CreateStageButton(safeArea, controller, Stages[i], positions[i]);
            }

            var footer = CreateText(safeArea, "Footer", new Vector2(0.5f, 0f), new Vector2(0f, 34f),
                new Vector2(1300f, 48f), 23, TextAnchor.MiddleCenter,
                "ステージを選んで調理開始");
            footer.color = new Color(0.58f, 0.65f, 0.68f);

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.transform.SetParent(root.transform, false);
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }

        private static void CreateStageButton(
            Transform parent,
            HomeScreenController controller,
            StageCard stage,
            Vector2 position)
        {
            var card = CreateImage(parent, "Stage " + stage.Number + " Button",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(520f, 230f),
                new Color(0.13f, 0.055f, 0.14f, 0.98f));
            var cardImage = card.GetComponent<Image>();
            KitchenHudVisuals.ApplyRounded(cardImage);
            var shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.66f);
            shadow.effectDistance = new Vector2(0f, -10f);
            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(stage.Color.r, stage.Color.g, stage.Color.b, 0.72f);
            outline.effectDistance = new Vector2(3f, 3f);

            var accent = CreateImage(card.transform, "Accent", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(12f, 0f), new Vector2(24f, -24f), stage.Color);
            KitchenHudVisuals.ApplyRounded(accent.GetComponent<Image>());
            accent.GetComponent<Image>().raycastTarget = false;

            var badge = CreateImage(card.transform, "Stage Badge", new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(78f, 0f), new Vector2(112f, 112f), stage.Color);
            KitchenHudVisuals.ApplyCircle(badge.GetComponent<Image>());
            badge.GetComponent<Image>().raycastTarget = false;
            var number = CreateText(badge.transform, "Stage Number", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(104f, 66f), 36, TextAnchor.MiddleCenter, stage.Number);
            number.color = Color.white;
            number.fontStyle = FontStyle.Bold;

            var title = CreateText(card.transform, "Stage Title", new Vector2(0f, 0.5f),
                new Vector2(310f, 44f), new Vector2(320f, 54f), 29, TextAnchor.MiddleLeft, stage.Title);
            title.color = KitchenHudVisuals.Cream;
            title.fontStyle = FontStyle.Bold;
            var description = CreateText(card.transform, "Stage Description", new Vector2(0f, 0.5f),
                new Vector2(310f, -10f), new Vector2(320f, 44f), 18, TextAnchor.MiddleLeft,
                stage.Description);
            description.color = new Color(0.76f, 0.82f, 0.84f);
            var prompt = CreateText(card.transform, "Play Prompt", new Vector2(0f, 0.5f),
                new Vector2(310f, -64f), new Vector2(320f, 38f), 18, TextAnchor.MiddleLeft,
                "はじめる  ▶");
            prompt.color = stage.Color;
            prompt.fontStyle = FontStyle.Bold;

            var button = card.AddComponent<Button>();
            button.targetGraphic = cardImage;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1f, 1f, 0.92f, 1f),
                pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f),
                colorMultiplier = 1f,
                fadeDuration = 0.06f
            };
            var sceneName = stage.SceneName;
            button.onClick.AddListener(() => controller.PlayStage(sceneName));
            card.AddComponent<KitchenActionButtonView>().Initialize("STAGE " + stage.Number, KitchenActionIcon.None);
        }

        private static GameObject CreateImage(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            gameObject.GetComponent<Image>().color = color;
            return gameObject;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            int fontSize,
            TextAnchor alignment,
            string value)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = gameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
