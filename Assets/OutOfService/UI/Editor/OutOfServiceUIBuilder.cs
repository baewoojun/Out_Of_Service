using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace OutOfService.EditorTools
{
    // 메뉴 Out Of Service ▸ UI ▸ Build Menus
    // 타이틀 / 일시정지 / 사망·탈출 프리팹, Title 씬, 한글 폰트 에셋을 만들고 빌드 씬 목록을 맞춘다.
    // 프리팹은 다시 만들면 덮어쓰므로, 손으로 다듬은 뒤에는 다시 돌리지 말 것.
    public static class OutOfServiceUIBuilder
    {
        const string Root = "Assets/OutOfService/UI";
        const string PrefabFolder = Root + "/Resources/OutOfServiceUI";
        const string FontPath = Root + "/Fonts/NotoSansKR.ttf";
        const string FontAssetPath = Root + "/Fonts/NotoSansKR SDF.asset";
        const string GlowMaterialPath = Root + "/Fonts/NotoSansKR SDF Glow.mat";
        const string TitleScenePath = "Assets/Scenes/Title.unity";
        const string LevelScenePath = "Assets/Scenes/MarketPrototype_v7.unity";

        static readonly Color Ink = new Color(0.94f, 0.93f, 0.88f);
        static readonly Color Muted = new Color(0.56f, 0.59f, 0.56f);
        static readonly Color Warm = new Color(0.82f, 0.73f, 0.50f);
        static readonly Color Blood = new Color(0.72f, 0.16f, 0.12f);
        static readonly Color PanelColor = new Color(0.032f, 0.044f, 0.046f, 0.97f);

        static TMP_FontAsset font;
        static Material glow;

        [MenuItem("Out Of Service/UI/Build Menus")]
        public static void BuildAll()
        {
            if (!EnsureTmpEssentials()) return;
            font = EnsureFontAsset();
            glow = EnsureGlowMaterial(font);
            Directory.CreateDirectory(PrefabFolder);

            SavePrefab(BuildPauseMenu(), "PauseMenu");
            SavePrefab(BuildEndScreen(), "EndScreen");
            var title = SavePrefab(BuildTitleMenu(), "TitleMenu");
            BuildTitleScene(title);
            UpdateBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] 타이틀·일시정지·사망/탈출 화면을 만들었습니다: " + PrefabFolder + ", " + TitleScenePath);
        }

        // 배치 모드용: unity run ... -executeMethod OutOfService.EditorTools.OutOfServiceUIBuilder.BuildFromCommandLine
        public static void BuildFromCommandLine()
        {
            if (!EnsureTmpEssentials()) { EditorApplication.Exit(3); return; }
            BuildAll();
        }

        // ───────────────────────── 준비물 ─────────────────────────

        static bool EnsureTmpEssentials()
        {
            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) return true;
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();
            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) return true;
            Debug.LogWarning("[UI] TMP 기본 리소스를 가져오는 중입니다. 가져오기가 끝나면 Build Menus 를 한 번 더 실행하세요.");
            return false;
        }

        static TMP_FontAsset EnsureFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing) return existing;
            var source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var asset = TMP_FontAsset.CreateFontAsset(source, 72, 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            asset.name = "NotoSansKR SDF";
            AssetDatabase.CreateAsset(asset, FontAssetPath);
            asset.atlasTextures[0].name = "NotoSansKR SDF Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = "NotoSansKR SDF Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        // 제목용: 글자 뒤에 핏빛이 번지는 머티리얼.
        static Material EnsureGlowMaterial(TMP_FontAsset asset)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath);
            if (!material)
            {
                material = new Material(asset.material) { name = "NotoSansKR SDF Glow" };
                AssetDatabase.CreateAsset(material, GlowMaterialPath);
            }
            material.CopyPropertiesFromMaterial(asset.material);
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", new Color(0.50f, 0.01f, 0.01f, 0.55f));
            material.SetFloat("_UnderlayOffsetX", 0f);
            material.SetFloat("_UnderlayOffsetY", -0.35f);
            material.SetFloat("_UnderlayDilate", 0.05f);
            material.SetFloat("_UnderlaySoftness", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ───────────────────────── 일시정지 ─────────────────────────

        static GameObject BuildPauseMenu()
        {
            var root = Canvas("PauseMenu", 100);
            var panel = Node("Panel", root.transform);
            Stretch(panel);
            var group = panel.AddComponent<CanvasGroup>();

            Image(Stretch(Node("Dim", panel.transform)), new Color(0.004f, 0.007f, 0.008f, 0.80f), true);

            var side = Node("Side", panel.transform);
            Place(side, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(640, 0));
            Image(side, PanelColor, true);
            var edge = Node("Edge", side.transform);
            Place(edge, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, new Vector2(3, 0));
            Image(edge, Blood);

            TopLeft(Text("Eyebrow", side.transform, "OUT OF SERVICE", 20, Muted, spacing: 40), 100, 150, 460, 30);
            TopLeft(Text("Heading", side.transform, "일시정지", 66, Ink, FontStyles.Bold, spacing: 6), 100, 186, 460, 86);
            var objective = TopLeft(Text("Objective", side.transform, "목표  ·  전원 퓨즈를 찾는다  (0/3)", 22, Warm), 100, 290, 460, 34);

            var list = VerticalList("Buttons", side.transform, 100, 380, 440);
            var resume = MenuItem(list, "Resume", "계속하기", out _);
            var settings = MenuItem(list, "Settings", "설정", out _);
            var restart = MenuItem(list, "Restart", "처음부터 다시", out _);
            var title = MenuItem(list, "Title", "타이틀로", out _);
            var quit = MenuItem(list, "Quit", "게임 종료", out _);

            var hint = Text("Hint", side.transform, "Esc  계속하기      ↑ ↓  선택      Enter  확인", 18, Muted);
            Place(hint.gameObject, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(100, 70), new Vector2(480, 30));

            var quote = Text("Broadcast", panel.transform, "“…오늘도 저희 매장을 찾아주셔서 감사합니다.”", 24, new Color(0.75f, 0.74f, 0.70f, 0.22f), FontStyles.Italic);
            quote.alignment = TextAlignmentOptions.BottomRight;
            Place(quote.gameObject, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-90, 70), new Vector2(900, 40));

            var menu = root.AddComponent<PauseMenu>();
            Set(menu, "group", group);
            Set(menu, "objective", objective);
            Set(menu, "resumeButton", resume);
            Set(menu, "settingsButton", settings);
            Set(menu, "restartButton", restart);
            Set(menu, "titleButton", title);
            Set(menu, "quitButton", quit);
            panel.SetActive(false);
            return root;
        }

        // ───────────────────────── 사망 · 탈출 ─────────────────────────

        static GameObject BuildEndScreen()
        {
            var root = Canvas("EndScreen", 90);
            var panel = Node("Panel", root.transform);
            Stretch(panel);
            var group = panel.AddComponent<CanvasGroup>();
            var backdrop = Image(Stretch(Node("Backdrop", panel.transform)), new Color(0.035f, 0.001f, 0.002f, 0.95f), true);

            var center = Node("Center", panel.transform);
            Place(center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1200, 720));

            var title = Text("Title", center.transform, "사망", 112, Ink, FontStyles.Bold, spacing: 14);
            title.alignment = TextAlignmentOptions.Center;
            title.fontSharedMaterial = glow;
            Place(title.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1200, 150));

            var line = Node("AccentLine", center.transform);
            Place(line, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -212), new Vector2(140, 3));
            var accent = Image(line, Blood);

            var subtitle = Text("Subtitle", center.transform, "마감 방송은 아직 끝나지 않았다.", 28, new Color(0.86f, 0.85f, 0.80f));
            subtitle.alignment = TextAlignmentOptions.Center;
            Place(subtitle.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -246), new Vector2(1200, 44));

            var stats = Text("Stats", center.transform, "버틴 시간  00:00      전원 퓨즈  0/3", 20, Muted, spacing: 4);
            stats.alignment = TextAlignmentOptions.Center;
            Place(stats.gameObject, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -310), new Vector2(1200, 32));

            var buttons = Node("Buttons", center.transform);
            Place(buttons, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -390), new Vector2(380, 200));
            var buttonGroup = buttons.AddComponent<CanvasGroup>();
            var layout = buttons.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            ConfigureList(layout);
            var retry = MenuItem(buttons.transform, "Retry", "다시 시작", out var retryLabel, centered: true);
            var toTitle = MenuItem(buttons.transform, "Title", "타이틀로", out _, centered: true);
            var quit = MenuItem(buttons.transform, "Quit", "게임 종료", out _, centered: true);

            var screen = root.AddComponent<EndScreen>();
            Set(screen, "group", group);
            Set(screen, "backdrop", backdrop);
            Set(screen, "accentLine", accent);
            Set(screen, "title", title);
            Set(screen, "subtitle", subtitle);
            Set(screen, "stats", stats);
            Set(screen, "buttonGroup", buttonGroup);
            Set(screen, "retryButton", retry);
            Set(screen, "titleButton", toTitle);
            Set(screen, "quitButton", quit);
            Set(screen, "retryLabel", retryLabel);
            panel.SetActive(false);
            return root;
        }

        // ───────────────────────── 타이틀 ─────────────────────────

        static GameObject BuildTitleMenu()
        {
            var root = Canvas("TitleMenu", 0);
            var screen = Node("Screen", root.transform);
            Stretch(screen);
            var screenGroup = screen.AddComponent<CanvasGroup>();

            Image(Stretch(Node("Background", screen.transform)), new Color(0.010f, 0.014f, 0.016f), true);
            var haze = Node("Haze", screen.transform);
            Place(haze, new Vector2(0, 0), new Vector2(0.62f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            Image(haze, new Color(0.035f, 0.048f, 0.05f, 0.9f));
            var hazeEdge = Node("HazeEdge", haze.transform);
            Place(hazeEdge, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), Vector2.zero, new Vector2(2, 0));
            Image(hazeEdge, new Color(Blood.r, Blood.g, Blood.b, 0.55f));
            var grain = Stretch(Node("Grain", screen.transform)).AddComponent<UnityEngine.UI.RawImage>();
            grain.raycastTarget = false;
            grain.color = Color.white;

            var left = Node("Content", screen.transform);
            Place(left, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(170, 40), new Vector2(980, 760));

            TopLeft(Text("Eyebrow", left.transform, "이마켓 야간 재고정리  ·  PM 11:00", 21, Muted, spacing: 8), 0, 40, 900, 32);
            var title = TopLeft(Text("Title", left.transform, "서비스 종료", 160, Ink, FontStyles.Bold, spacing: 8), -6, 84, 980, 200);
            title.fontSharedMaterial = glow;
            var rule = Node("Rule", left.transform);
            TopLeft(rule, 0, 296, 560, 4);
            Image(rule, Blood);
            var subtitle = TopLeft(Text("Subtitle", left.transform, "OUT OF SERVICE", 28, new Color(0.80f, 0.80f, 0.76f), spacing: 70), 0, 318, 900, 40);

            var list = VerticalList("Buttons", left.transform, 0, 440, 420);
            var start = MenuItem(list, "Start", "게임 시작", out _);
            var controls = MenuItem(list, "Controls", "조작 안내", out _);
            var quit = MenuItem(list, "Quit", "게임 종료", out _);

            var strip = Node("BroadcastStrip", screen.transform);
            Place(strip, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 84));
            Image(strip, new Color(0, 0, 0, 0.55f));
            var speaker = Text("Speaker", strip.transform, "●  매장 방송", 18, Blood, FontStyles.Bold, spacing: 6);
            Place(speaker.gameObject, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(170, 0), new Vector2(170, 40));
            speaker.alignment = TextAlignmentOptions.MidlineLeft;
            var broadcast = Text("Broadcast", strip.transform, "", 22, new Color(0.80f, 0.80f, 0.74f));
            Place(broadcast.gameObject, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(350, 0), new Vector2(-520, 40));
            broadcast.alignment = TextAlignmentOptions.MidlineLeft;
            broadcast.textWrappingMode = TextWrappingModes.NoWrap;
            broadcast.overflowMode = TextOverflowModes.Ellipsis;
            var version = Text("Version", strip.transform, "SW프로젝트 응용  ·  프로토타입", 16, new Color(Muted.r, Muted.g, Muted.b, 0.7f));
            Place(version.gameObject, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-60, 0), new Vector2(320, 30));
            version.alignment = TextAlignmentOptions.MidlineRight;

            // 조작 안내 창
            var panel = Node("ControlsPanel", screen.transform);
            Stretch(panel);
            var panelGroup = panel.AddComponent<CanvasGroup>();
            Image(Stretch(Node("Dim", panel.transform)), new Color(0, 0, 0, 0.72f), true);
            var box = Node("Box", panel.transform);
            Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780, 700));
            Image(box, PanelColor, true);
            var boxTop = Node("Top", box.transform);
            Place(boxTop, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 4));
            Image(boxTop, Blood);
            TopLeft(Text("Heading", box.transform, "조작 안내", 40, Ink, FontStyles.Bold, spacing: 4), 64, 52, 600, 56);
            var body = TopLeft(Text("Body", box.transform, "", 23, Ink), 64, 130, 652, 450);
            body.lineSpacing = 18;
            var closeHolder = Node("Close", box.transform);
            Place(closeHolder, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(52, 44), new Vector2(300, 58));
            var closeLayout = closeHolder.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            ConfigureList(closeLayout);
            var close = MenuItem(closeHolder.transform, "CloseButton", "닫기", out _);

            var blackout = Image(Stretch(Node("Blackout", root.transform)), Color.black);

            var menu = root.AddComponent<TitleMenu>();
            Set(menu, "screen", screenGroup);
            Set(menu, "blackout", blackout);
            Set(menu, "title", title);
            Set(menu, "subtitle", subtitle);
            Set(menu, "broadcast", broadcast);
            Set(menu, "grain", grain);
            Set(menu, "startButton", start);
            Set(menu, "controlsButton", controls);
            Set(menu, "quitButton", quit);
            Set(menu, "controlsPanel", panelGroup);
            Set(menu, "controlsBody", body);
            Set(menu, "controlsCloseButton", close);
            return root;
        }

        static void BuildTitleScene(GameObject titlePrefab)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // 지금 열려 있는 씬 구성(추가로 열린 씬 포함)을 기억했다가 끝나면 되돌린다.
            var previous = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;

            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            events.GetComponent<EventSystem>().sendNavigationEvents = true;

            var menu = (GameObject)PrefabUtility.InstantiatePrefab(titlePrefab, scene);
            menu.name = "TitleMenu";

            EditorSceneManager.SaveScene(scene, TitleScenePath);
            if (previous.Length > 0 && System.Array.TrueForAll(previous, s => File.Exists(s.path)))
                EditorSceneManager.RestoreSceneManagerSetup(previous);
        }

        static void UpdateBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(TitleScenePath, true),
                new EditorBuildSettingsScene(LevelScenePath, true),
            };
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.path != TitleScenePath && scene.path != LevelScenePath) scenes.Add(scene);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ───────────────────────── 부품 ─────────────────────────

        static UnityEngine.UI.Button MenuItem(Transform parent, string name, string text, out TMP_Text label, bool centered = false)
        {
            var go = Node(name, parent);
            go.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 58;
            var hit = Image(go, new Color(1, 1, 1, 0), true);

            var highlight = Node("Highlight", go.transform);
            Stretch(highlight);
            var highlightImage = Image(highlight, new Color(1f, 1f, 1f, 0.025f));

            var marker = Node("Marker", go.transform);
            Place(marker, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(4, 30));
            var markerImage = Image(marker, Blood);

            label = Text("Label", go.transform, text, 30, Muted);
            label.alignment = centered ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
            Place(label.gameObject, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(centered ? 0 : 26, 0), new Vector2(centered ? 0 : -26, 0));

            var button = go.AddComponent<UnityEngine.UI.Button>();
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            button.targetGraphic = hit;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Vertical };

            var menuButton = go.AddComponent<MenuButton>();
            Set(menuButton, "label", label);
            Set(menuButton, "marker", markerImage);
            Set(menuButton, "highlight", highlightImage);
            if (centered)
            {
                var so = new SerializedObject(menuButton);
                so.FindProperty("slide").floatValue = 0f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return button;
        }

        static Transform VerticalList(string name, Transform parent, float left, float top, float width)
        {
            var go = Node(name, parent);
            TopLeft(go, left, top, width, 360);
            ConfigureList(go.AddComponent<UnityEngine.UI.VerticalLayoutGroup>());
            return go.transform;
        }

        static void ConfigureList(UnityEngine.UI.VerticalLayoutGroup layout)
        {
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        static GameObject Canvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            return go;
        }

        static GameObject Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        static GameObject Stretch(GameObject go)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        static void Place(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static T TopLeft<T>(T component, float left, float top, float width, float height) where T : Component
        {
            TopLeft(component.gameObject, left, top, width, height);
            return component;
        }

        static GameObject TopLeft(GameObject go, float left, float top, float width, float height)
        {
            Place(go, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(left, -top), new Vector2(width, height));
            return go;
        }

        static UnityEngine.UI.Image Image(GameObject go, Color color, bool raycast = false)
        {
            var image = go.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color, FontStyles style = FontStyles.Normal, float spacing = 0)
        {
            var go = Node(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSharedMaterial = font.material;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.characterSpacing = spacing;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            return tmp;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null) { Debug.LogError($"[UI] {target.GetType().Name}.{field} 필드를 찾지 못했습니다."); return; }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject SavePrefab(GameObject root, string name)
        {
            string path = PrefabFolder + "/" + name + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
