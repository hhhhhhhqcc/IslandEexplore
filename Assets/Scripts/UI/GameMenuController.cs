using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Title, pause and settings UI assembled from the demo's pixel art.</summary>
[DisallowMultipleComponent]
public sealed class GameMenuController : MonoBehaviour
{
    private enum Page { None, Title, Pause, Settings, Help, Quit, ConfirmDisplay }
    public static GameMenuController Instance { get; private set; }
    private static int consumedEscapeFrame = -1, blockedInputFrame = -1;
    public static bool BlocksGameplayInput => Time.frameCount == blockedInputFrame ||
        (Instance != null && (!Instance.sessionStarted || Instance.page != Page.None));
    public static bool IsSessionStarted => Instance == null || Instance.sessionStarted;
    public static void ConsumeEscapeThisFrame() => consumedEscapeFrame = Time.frameCount;

    private const string VolumeKey = "IslandExplore.Settings.Volume";
    private const string WidthKey = "IslandExplore.Settings.Width";
    private const string HeightKey = "IslandExplore.Settings.Height";
    private const string FullscreenKey = "IslandExplore.Settings.Fullscreen";
    private static readonly Color Ink = new Color32(28, 41, 46, 255);
    private static readonly Color Paper = new Color32(241, 231, 197, 255);
    private static readonly Color Muted = new Color32(187, 199, 186, 255);
    private static readonly Color Wood = new Color32(97, 64, 45, 255);
    private static readonly Color Moss = new Color32(73, 108, 70, 255);
    private static readonly Color Gold = new Color32(225, 177, 89, 255);

    private readonly Dictionary<Page, RectTransform> pages = new Dictionary<Page, RectTransform>();
    private readonly Dictionary<Page, Selectable> firstSelection = new Dictionary<Page, Selectable>();
    private readonly List<UnityEngine.Object> generatedAssets = new List<UnityEngine.Object>();
    private readonly List<CanvasState> gameplayCanvases = new List<CanvasState>();
    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    private Page page = Page.Title, returnPage = Page.Title;
    private bool sessionStarted, chinese;
    private float resumeTimeScale = 1f;
    private Font font;
    private Sprite pixel;
    private Canvas menuCanvas;
    private GameObject scenery;
    private Image shade;
    private RectTransform floatingIsland;
    private Text startLabel, titleHint, quitMessage, volumeValue, settingsStatus, displayCountdown;
    private Slider volume;
    private Dropdown resolution;
    private Toggle fullscreen;
    private float confirmationDeadline, pendingVolume;
    private int lastCountdown = -1;
    private DisplayState previousDisplay;
    private Vector2Int pendingResolution;
    private bool pendingFullscreen;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

    private sealed class CanvasState
    {
        public CanvasGroup group;
        public float alpha;
        public bool interactable, blocksRaycasts;
    }
    private struct DisplayState
    {
        public int width, height;
        public FullScreenMode mode;
        public bool fullscreen;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        consumedEscapeFrame = blockedInputFrame = -1;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "SampleScene" || FindObjectOfType<GameMenuController>() != null) return;
        new GameObject("GameMenuController").AddComponent<GameMenuController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        resumeTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        blockedInputFrame = Time.frameCount;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.8f));
#if !UNITY_EDITOR
        if (PlayerPrefs.HasKey(WidthKey))
            Screen.SetResolution(Mathf.Max(800, PlayerPrefs.GetInt(WidthKey)),
                Mathf.Max(600, PlayerPrefs.GetInt(HeightKey)),
                PlayerPrefs.GetInt(FullscreenKey, 1) != 0 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
#endif
    }

    private void Start()
    {
        ResolveFont();
        pixel = MakeSprite(1, 1, new[] { Color.white });
        CaptureGameplayCanvases();
        BuildCanvas();
        BuildScenery();
        BuildTitle();
        BuildPause();
        BuildSettings();
        BuildHelp();
        BuildQuit();
        BuildDisplayConfirmation();
        Show(Page.Title);
    }

    private void LateUpdate()
    {
        if (floatingIsland != null && scenery.activeSelf)
            floatingIsland.anchoredPosition = new Vector2(-270f, -32f + Mathf.Sin(Time.unscaledTime * 1.1f) * 5f);
        if (page == Page.ConfirmDisplay)
        {
            int remaining = Mathf.Max(0, Mathf.CeilToInt(confirmationDeadline - Time.unscaledTime));
            if (remaining != lastCountdown)
            {
                lastCountdown = remaining;
                displayCountdown.text = L(remaining + " 秒后自动恢复原显示设置", "Reverting display settings in " + remaining + " seconds");
            }
            if (remaining == 0) RejectDisplay();
        }
        if (!Input.GetKeyDown(KeyCode.Escape) || consumedEscapeFrame == Time.frameCount) return;
        // Dropdown.OnCancel hides its list over 0.15 real-time seconds.
        // Consume Escape while that list still exists, before navigating back.
        if (page == Page.Settings && resolution.transform.Find("Dropdown List") != null)
        {
            resolution.Hide();
            ConsumeEscapeThisFrame();
            return;
        }
        switch (page)
        {
            case Page.None: Show(Page.Pause); break;
            case Page.Pause: Resume(); break;
            case Page.Title: AskQuit(Page.Title); break;
            case Page.Settings: BackFromSettings(); break;
            case Page.Help: Show(returnPage); break;
            case Page.Quit: Show(returnPage); break;
            case Page.ConfirmDisplay: RejectDisplay(); break;
        }
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && sessionStarted && page == Page.None && menuCanvas != null) Show(Page.Pause);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        if (page == Page.ConfirmDisplay)
        {
#if !UNITY_EDITOR
            Screen.SetResolution(previousDisplay.width, previousDisplay.height, previousDisplay.mode);
#endif
        }
        foreach (CanvasState state in gameplayCanvases)
        {
            if (state.group == null) continue;
            state.group.alpha = state.alpha;
            state.group.interactable = state.interactable;
            state.group.blocksRaycasts = state.blocksRaycasts;
        }
        Time.timeScale = resumeTimeScale;
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.8f));
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        foreach (UnityEngine.Object asset in generatedAssets) if (asset != null) Destroy(asset);
        Instance = null;
    }

    private string L(string zh, string en) => chinese ? zh : en;

    private void ResolveFont()
    {
        string[] candidates = { "Microsoft YaHei", "Microsoft YaHei UI", "微软雅黑", "SimHei", "Microsoft JhengHei",
            "PingFang SC", "Noto Sans CJK SC", "Noto Sans SC", "WenQuanYi Micro Hei" };
        var installed = new HashSet<string>(Font.GetOSInstalledFontNames(), StringComparer.OrdinalIgnoreCase);
        foreach (string name in candidates)
        {
            if (!installed.Contains(name)) continue;
            Font candidate = Font.CreateDynamicFontFromOSFont(name, 24);
            if (candidate != null && candidate.HasCharacter('岛') && candidate.HasCharacter('设'))
            {
                font = candidate;
                generatedAssets.Add(font);
                chinese = true;
                return;
            }
            if (candidate != null) Destroy(candidate);
        }
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        chinese = false;
    }

    private void CaptureGameplayCanvases()
    {
        foreach (Canvas canvas in FindObjectsOfType<Canvas>())
        {
            if (canvas.rootCanvas != canvas) continue;
            CanvasGroup group = canvas.GetComponent<CanvasGroup>();
            if (group == null) group = canvas.gameObject.AddComponent<CanvasGroup>();
            gameplayCanvases.Add(new CanvasState { group = group, alpha = group.alpha,
                interactable = group.interactable, blocksRaycasts = group.blocksRaycasts });
        }
    }

    private void BuildCanvas()
    {
        var root = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        menuCanvas = root.GetComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 2000;
        menuCanvas.pixelPerfect = true;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        if (EventSystem.current == null)
            new GameObject("MenuEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform, false);
        scenery = Stretch(root.transform, "TitleScenery").gameObject;
        Image background = scenery.AddComponent<Image>();
        background.sprite = MakeSky();
        background.raycastTarget = false;
        shade = Stretch(root.transform, "InputShield").gameObject.AddComponent<Image>();
        shade.sprite = pixel;
        shade.color = Color.clear;
        shade.raycastTarget = true;
    }

    private Sprite MakeSky()
    {
        const int width = 160, height = 90;
        var colors = new Color[width * height];
        var random = new System.Random(41);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            Color c = Color.Lerp(new Color32(39, 68, 76, 255), new Color32(16, 30, 49, 255), y / (float)height);
            if (y > 25 && random.Next(280) == 0) c = new Color32(131, 156, 146, 255);
            if (y < 14 && ((x * 17 + y * 7) % 41) < 2) c = new Color32(48, 82, 85, 255);
            colors[y * width + x] = c;
        }
        return MakeSprite(width, height, colors);
    }

    private Sprite MakeSprite(int width, int height, Color[] colors)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "MenuPixelTexture";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colors);
        texture.Apply(false, false);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1f);
        generatedAssets.Add(sprite);
        generatedAssets.Add(texture);
        return sprite;
    }

    private void BuildScenery()
    {
        Label(scenery.transform, "Eyebrow", L("采集 · 建造 · 探索", "GATHER / BUILD / EXPLORE"), new Vector2(-270, 240), new Vector2(570, 35), 20, Gold);
        Label(scenery.transform, "GameTitle", L("浮岛开拓者", "ISLAND EXPLORER"), new Vector2(-270, 183), new Vector2(600, 75), chinese ? 62 : 43, Paper, FontStyle.Bold);
        Label(scenery.transform, "Subtitle", L("从一座小岛，走向更远的世界。", "A small island. A world to discover."), new Vector2(-270, 125), new Vector2(570, 35), 23, Muted);
        floatingIsland = Rect(scenery.transform, "FloatingIsland", new Vector2(-270, -32), new Vector2(540, 320));
        Fill(floatingIsland, "IslandShadow", new Vector2(0, -102), new Vector2(330, 18), new Color(0, 0, 0, 0.17f));
        Fill(floatingIsland, "CliffBottom", new Vector2(4, -67), new Vector2(220, 48), new Color32(52, 42, 40, 255));
        Fill(floatingIsland, "CliffMiddle", new Vector2(0, -35), new Vector2(324, 68), new Color32(84, 58, 44, 255));
        Fill(floatingIsland, "CliffRim", new Vector2(0, -7), new Vector2(372, 48), Wood);
        Fill(floatingIsland, "GrassEdge", new Vector2(0, 12), new Vector2(372, 42), new Color32(42, 75, 42, 255));
        Fill(floatingIsland, "GrassTop", new Vector2(0, 37), new Vector2(350, 55), new Color32(77, 122, 56, 255));
        Fill(floatingIsland, "GrassStep", new Vector2(0, 65), new Vector2(298, 18), new Color32(92, 139, 63, 255));
        for (int i = 0; i < 15; i++)
            Fill(floatingIsland, "GrassPixel" + i, new Vector2(-140 + i * 19, 29 + (i * 13 % 32)), new Vector2(8, 4),
                i % 2 == 0 ? new Color32(115, 150, 75, 255) : Moss);
        SpriteDB db = FindObjectOfType<TileManager>()?.spriteDB;
        if (db != null)
        {
            Art(floatingIsland, "TreeLeft", db.tree, new Vector2(-112, 105), new Vector2(76, 133));
            Art(floatingIsland, "TreeBack", db.tree, new Vector2(-28, 117), new Vector2(60, 105));
            Art(floatingIsland, "Cabin", db.storageBox, new Vector2(111, 92), new Vector2(80, 80));
            Art(floatingIsland, "Stone", db.stone, new Vector2(-40, 45), new Vector2(44, 44));
            Art(floatingIsland, "Furnace", db.furnace, new Vector2(53, 62), new Vector2(55, 55));
        }
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) Art(floatingIsland, "Explorer", player.idleSprite, new Vector2(9, 78), new Vector2(56, 56));
        Label(scenery.transform, "AdventureCaption", L("拾起工具，搭起通往下一座岛的桥。", "Pick up your tools. Build your next bridge."), new Vector2(-270, -218), new Vector2(590, 35), 20, Muted);
        Label(scenery.transform, "Footer", L("浮岛探索 Demo    ·    让冒险从这里开始", "ISLAND DEMO    /    YOUR ADVENTURE STARTS HERE"), new Vector2(0, -324), new Vector2(1080, 28), 15, Muted);
    }

    private void BuildTitle()
    {
        RectTransform card = Panel(NewPage(Page.Title), "TitleCard", new Vector2(330, 0), new Vector2(360, 460));
        Label(card, "Heading", L("新的旅程", "YOUR JOURNEY"), new Vector2(0, 170), new Vector2(306, 42), 29, Paper, FontStyle.Bold);
        Label(card, "HeadingHint", L("下一座岛，等你发现。", "Your next island awaits."), new Vector2(0, 125), new Vector2(300, 30), 19, Muted);
        Button start = MenuButton(card, "Start", L("开始探险", "START EXPLORING"), new Vector2(0, 60), new Vector2(292, 54), Moss, Resume);
        startLabel = start.GetComponentInChildren<Text>();
        MenuButton(card, "Settings", L("设置", "SETTINGS"), new Vector2(0, -8), new Vector2(292, 48), Ink, () => OpenSettings(Page.Title));
        MenuButton(card, "Help", L("操作说明", "HOW TO PLAY"), new Vector2(0, -70), new Vector2(292, 48), Ink, () => OpenHelp(Page.Title));
        MenuButton(card, "Quit", L("退出游戏", "QUIT GAME"), new Vector2(0, -132), new Vector2(292, 48), Wood, () => AskQuit(Page.Title));
        titleHint = Label(card, "SessionHint", L("WASD 移动  ·  鼠标交互", "WASD to move / mouse to interact"), new Vector2(0, -187), new Vector2(318, 32), 15, Muted);
        firstSelection[Page.Title] = start;
    }

    private void BuildPause()
    {
        RectTransform card = Panel(NewPage(Page.Pause), "PauseCard", Vector2.zero, new Vector2(420, 538));
        Label(card, "Heading", L("歇一会儿", "TAKE A BREATHER"), new Vector2(0, 205), new Vector2(370, 50), 35, Paper, FontStyle.Bold);
        Label(card, "Hint", L("小岛会在这里等你。", "Your island will be right here."), new Vector2(0, 157), new Vector2(370, 32), 20, Muted);
        firstSelection[Page.Pause] = MenuButton(card, "Resume", L("继续探险", "RESUME"), new Vector2(0, 88), new Vector2(328, 52), Moss, Resume);
        MenuButton(card, "Settings", L("设置", "SETTINGS"), new Vector2(0, 22), new Vector2(328, 48), Ink, () => OpenSettings(Page.Pause));
        MenuButton(card, "Help", L("操作说明", "HOW TO PLAY"), new Vector2(0, -40), new Vector2(328, 48), Ink, () => OpenHelp(Page.Pause));
        MenuButton(card, "Title", L("返回标题", "BACK TO TITLE"), new Vector2(0, -102), new Vector2(328, 48), Ink, () => Show(Page.Title));
        MenuButton(card, "Quit", L("退出游戏", "QUIT GAME"), new Vector2(0, -164), new Vector2(328, 48), Wood, () => AskQuit(Page.Pause));
        Label(card, "Shortcut", L("Esc 继续探险", "Esc to resume"), new Vector2(0, -226), new Vector2(370, 30), 17, Muted);
    }

    private void BuildSettings()
    {
        RectTransform card = Panel(NewPage(Page.Settings), "SettingsCard", Vector2.zero, new Vector2(650, 600));
        Label(card, "Heading", L("调整你的旅程", "MAKE YOURSELF AT HOME"), new Vector2(0, 235), new Vector2(592, 48), chinese ? 34 : 28, Paper, FontStyle.Bold);
        Fill(card, "Rule", new Vector2(0, 194), new Vector2(554, 2), Wood);
        Label(card, "VolumeLabel", L("主音量", "MASTER VOLUME"), new Vector2(-127, 157), new Vector2(298, 32), 22, Paper, FontStyle.Normal, TextAnchor.MiddleLeft);
        volumeValue = Label(card, "VolumeValue", "80%", new Vector2(225, 157), new Vector2(100, 32), 21, Gold);
        volume = MakeSlider(card, new Vector2(0, 118), new Vector2(550, 24));
        volume.onValueChanged.AddListener(value => { AudioListener.volume = value; volumeValue.text = Mathf.RoundToInt(value * 100f) + "%"; });
        Label(card, "ResolutionLabel", L("窗口分辨率", "WINDOW RESOLUTION"), new Vector2(-127, 57), new Vector2(298, 32), 22, Paper, FontStyle.Normal, TextAnchor.MiddleLeft);
        BuildResolutionList();
        resolution = MakeDropdown(card, new Vector2(0, 14), new Vector2(550, 44));
        var options = new List<string>();
        foreach (Vector2Int size in resolutions) options.Add(size.x + " × " + size.y);
        resolution.AddOptions(options);
        fullscreen = MakeToggle(card, L("全屏显示", "FULLSCREEN"), new Vector2(0, -63), new Vector2(550, 42));
        Label(card, "FullscreenHint", L("窗口模式下，分辨率决定窗口大小。", "Window resolution controls the size of the game window."), new Vector2(0, -108), new Vector2(554, 56), 18, Muted);
        settingsStatus = Label(card, "Status", L("设置保存在当前设备。", "Settings are saved on this device."), new Vector2(0, -163), new Vector2(560, 50), 18, Gold);
        MenuButton(card, "Apply", L("应用并保存", "APPLY & SAVE"), new Vector2(139, -235), new Vector2(266, 48), Moss, ApplySettings);
        MenuButton(card, "Back", L("返回", "BACK"), new Vector2(-139, -235), new Vector2(266, 48), Ink, BackFromSettings);
        firstSelection[Page.Settings] = volume;
    }

    private void BuildResolutionList()
    {
        foreach (Resolution supported in Screen.resolutions) AddResolution(supported.width, supported.height);
        AddResolution(960, 600); AddResolution(1280, 720); AddResolution(1600, 900); AddResolution(1920, 1080);
        AddResolution(Screen.width, Screen.height);
        AddResolution(PlayerPrefs.GetInt(WidthKey, Screen.width), PlayerPrefs.GetInt(HeightKey, Screen.height));
        resolutions.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
    }

    private void AddResolution(int width, int height)
    {
        if (width < 800 || height < 600) return;
        var size = new Vector2Int(width, height);
        if (!resolutions.Contains(size)) resolutions.Add(size);
    }

    private void BuildHelp()
    {
        RectTransform card = Panel(NewPage(Page.Help), "HelpCard", Vector2.zero, new Vector2(710, 610));
        Label(card, "Heading", L("小岛探险手册", "EXPLORER'S FIELD GUIDE"), new Vector2(0, 242), new Vector2(650, 50), 34, Paper, FontStyle.Bold);
        Fill(card, "Rule", new Vector2(0, 201), new Vector2(612, 2), Wood);
        string[,] rows = {
            { "WASD / ↑↓←→", L("移动", "Move") },
            { "1 / 2 / 3 / 4", L("剑 / 镐 / 斧 / 铲", "Sword / pickaxe / axe / shovel") },
            { L("左键 / 空格", "Left click / Space"), L("朝鼠标方向采集或攻击", "Gather or attack toward the cursor") },
            { "E", L("打开背包", "Open inventory") },
            { "B", L("建造；1 熔炉 / 2 仓库 / 3 桥", "Build: 1 furnace / 2 storage / 3 bridge") },
            { L("右键熔炉", "Right click furnace"), L("靠近后打开冶炼配方", "Open recipes when nearby") },
            { L("左键相邻岛屿", "Click a nearby island"), L("消耗金币解锁", "Spend gold to unlock") },
            { "Esc", L("关闭当前界面 / 暂停", "Close the current panel / pause") }
        };
        for (int i = 0; i < rows.GetLength(0); i++)
        {
            float y = 157 - i * 42;
            Label(card, "Key" + i, rows[i, 0], new Vector2(-199, y), new Vector2(215, 36), 18, Gold, FontStyle.Bold, TextAnchor.MiddleLeft);
            Label(card, "Action" + i, rows[i, 1], new Vector2(116, y), new Vector2(380, 36), 18, Paper, FontStyle.Normal, TextAnchor.MiddleLeft);
        }
        Label(card, "Tip", L("先采集，再解锁岛屿；搭好桥，才走得过去。", "Gather, unlock, then build a bridge to your next island."), new Vector2(0, -197), new Vector2(628, 35), 18, Muted);
        firstSelection[Page.Help] = MenuButton(card, "Back", L("我知道了", "GOT IT"), new Vector2(0, -249), new Vector2(320, 48), Moss, () => Show(returnPage));
    }

    private void BuildQuit()
    {
        RectTransform card = Panel(NewPage(Page.Quit), "QuitCard", Vector2.zero, new Vector2(570, 316));
        Label(card, "Heading", L("离开这座小岛？", "LEAVING THE ISLAND?"), new Vector2(0, 90), new Vector2(510, 52), 31, Paper, FontStyle.Bold);
        quitMessage = Label(card, "Message", "", new Vector2(0, 18), new Vector2(490, 75), 21, Muted);
        firstSelection[Page.Quit] = MenuButton(card, "Cancel", L("再待一会儿", "STAY A WHILE"), new Vector2(-129, -92), new Vector2(238, 48), Moss, () => Show(returnPage));
        MenuButton(card, "Confirm", L("确认退出", "QUIT GAME"), new Vector2(129, -92), new Vector2(238, 48), Wood, QuitGame);
    }

    private void BuildDisplayConfirmation()
    {
        RectTransform card = Panel(NewPage(Page.ConfirmDisplay), "DisplayConfirmation", Vector2.zero, new Vector2(590, 320));
        Label(card, "Heading", L("保留新的显示设置？", "KEEP THESE DISPLAY SETTINGS?"), new Vector2(0, 92), new Vector2(536, 60), chinese ? 29 : 25, Paper, FontStyle.Bold);
        displayCountdown = Label(card, "Countdown", "", new Vector2(0, 12), new Vector2(536, 62), 21, Gold);
        firstSelection[Page.ConfirmDisplay] = MenuButton(card, "Keep", L("保留", "KEEP"), new Vector2(134, -94), new Vector2(244, 48), Moss, ConfirmDisplay);
        MenuButton(card, "Revert", L("恢复原设置", "REVERT"), new Vector2(-134, -94), new Vector2(244, 48), Ink, RejectDisplay);
    }

    private void Resume() { sessionStarted = true; Show(Page.None); }
    private void OpenSettings(Page origin) { returnPage = origin; PrepareSettings(); Show(Page.Settings); }

    private void PrepareSettings()
    {
        float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.8f));
        volume.SetValueWithoutNotify(savedVolume);
        volumeValue.text = Mathf.RoundToInt(savedVolume * 100f) + "%";
        AudioListener.volume = savedVolume;
        int width = PlayerPrefs.GetInt(WidthKey, Screen.width), height = PlayerPrefs.GetInt(HeightKey, Screen.height);
        int index = resolutions.FindIndex(size => size.x == width && size.y == height);
        resolution.SetValueWithoutNotify(Mathf.Max(0, index));
        resolution.RefreshShownValue();
        fullscreen.SetIsOnWithoutNotify(PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0);
        settingsStatus.text = L("设置保存在当前设备。", "Settings are saved on this device.");
    }

    private void BackFromSettings()
    {
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.8f));
        Show(returnPage);
    }

    private void ApplySettings()
    {
        pendingResolution = resolutions[resolution.value];
        pendingFullscreen = fullscreen.isOn;
        pendingVolume = volume.value;
#if UNITY_EDITOR
        SaveSettings();
        settingsStatus.text = L("已保存；显示设置将在导出的游戏中生效。", "Saved. Display changes apply in the built game.");
#else
        previousDisplay = new DisplayState { width = Screen.width, height = Screen.height,
            mode = Screen.fullScreenMode, fullscreen = Screen.fullScreen };
        bool changed = pendingFullscreen != Screen.fullScreen || (!pendingFullscreen &&
            (pendingResolution.x != Screen.width || pendingResolution.y != Screen.height));
        if (!changed) { SaveSettings(); settingsStatus.text = L("设置已保存。", "Settings saved."); return; }
        Screen.SetResolution(pendingResolution.x, pendingResolution.y,
            pendingFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        confirmationDeadline = Time.unscaledTime + 15f;
        lastCountdown = -1;
        Show(Page.ConfirmDisplay);
#endif
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat(VolumeKey, pendingVolume);
        PlayerPrefs.SetInt(WidthKey, pendingResolution.x);
        PlayerPrefs.SetInt(HeightKey, pendingResolution.y);
        PlayerPrefs.SetInt(FullscreenKey, pendingFullscreen ? 1 : 0);
        PlayerPrefs.Save();
        AudioListener.volume = pendingVolume;
    }

    private void ConfirmDisplay()
    {
        SaveSettings();
        Show(Page.Settings);
        settingsStatus.text = L("设置已保存。", "Settings saved.");
    }

    private void RejectDisplay()
    {
#if !UNITY_EDITOR
        Screen.SetResolution(previousDisplay.width, previousDisplay.height, previousDisplay.mode);
#endif
        PrepareSettings();
        // Screen.SetResolution takes effect at the end of the frame.
        // Restore controls from the snapshot rather than the temporary Screen state.
        int previousIndex = resolutions.FindIndex(size => size.x == previousDisplay.width && size.y == previousDisplay.height);
        if (previousIndex >= 0) resolution.SetValueWithoutNotify(previousIndex);
        resolution.RefreshShownValue();
        fullscreen.SetIsOnWithoutNotify(previousDisplay.fullscreen);
        Show(Page.Settings);
        settingsStatus.text = L("已恢复原显示设置。", "Previous display settings restored.");
    }

    private void OpenHelp(Page origin) { returnPage = origin; Show(Page.Help); }

    private void AskQuit(Page origin)
    {
        returnPage = origin;
        quitMessage.text = sessionStarted ? L("当前探险尚无存档。\n退出会结束本次旅程。", "This demo has no saved progress.\nQuitting ends your current adventure.")
            : L("确定退出游戏吗？", "Are you sure you want to quit?");
#if UNITY_WEBGL && !UNITY_EDITOR
        quitMessage.text = L("浏览器版请关闭当前标签页退出。", "Close this browser tab to leave the game.");
#endif
        Show(Page.Quit);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        Show(Page.Title);
#else
        Application.Quit();
#endif
    }

    private void Show(Page next)
    {
        blockedInputFrame = Time.frameCount;
        if (page == Page.None && next != Page.None && Time.timeScale > 0f) resumeTimeScale = Time.timeScale;
        page = next;
        if (next != Page.None) CloseGameplayPanels();
        foreach (var entry in pages) entry.Value.gameObject.SetActive(entry.Key == next);
        menuCanvas.gameObject.SetActive(next != Page.None);
        bool titleBackground = !sessionStarted || next == Page.Title ||
            ((next == Page.Settings || next == Page.Help || next == Page.Quit || next == Page.ConfirmDisplay) && returnPage == Page.Title);
        scenery.SetActive(titleBackground);
        shade.color = titleBackground ? Color.clear : new Color(0.025f, 0.055f, 0.08f, 0.8f);
        foreach (CanvasState state in gameplayCanvases)
        {
            if (state.group == null) continue;
            state.group.alpha = titleBackground ? 0f : state.alpha;
            state.group.interactable = next == Page.None && state.interactable;
            state.group.blocksRaycasts = next == Page.None && state.blocksRaycasts;
        }
        Time.timeScale = next == Page.None ? resumeTimeScale : 0f;
        startLabel.text = sessionStarted ? L("继续探险", "CONTINUE EXPLORING") : L("开始探险", "START EXPLORING");
        titleHint.text = sessionStarted ? L("本次进度保留至关闭游戏", "This adventure lasts until you quit") : L("WASD 移动  ·  鼠标交互", "WASD to move / mouse to interact");
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            if (firstSelection.TryGetValue(next, out Selectable selection)) EventSystem.current.SetSelectedGameObject(selection.gameObject);
        }
    }

    private void CloseGameplayPanels()
    {
        InventoryUI inventory = FindObjectOfType<InventoryUI>();
        if (inventory != null && inventory.IsOpen) inventory.Close();
        CraftingUI crafting = FindObjectOfType<CraftingUI>();
        if (crafting != null && crafting.IsOpen) crafting.Close();
        BuildingPlacer building = FindObjectOfType<BuildingPlacer>();
        if (building != null && building.IsBuildingMode) building.ExitBuildingMode();
    }

    private RectTransform NewPage(Page value)
    {
        RectTransform root = Stretch(menuCanvas.transform, value + "Page");
        pages.Add(value, root);
        root.gameObject.SetActive(false);
        return root;
    }

    private RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform));
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    private RectTransform Stretch(Transform parent, string name)
    {
        RectTransform rt = Rect(parent, name, Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private Image Fill(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        Image image = Rect(parent, name, position, size).gameObject.AddComponent<Image>();
        image.sprite = pixel; image.color = color; image.raycastTarget = false;
        return image;
    }

    private void Art(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        if (sprite == null) return;
        Image image = Fill(parent, name, position, size, Color.white);
        image.sprite = sprite; image.preserveAspect = true;
    }

    private RectTransform Panel(Transform parent, string name, Vector2 position, Vector2 size)
    {
        Fill(parent, name + "Shadow", position + new Vector2(7, -8), size, new Color(0, 0, 0, 0.28f));
        Image border = Fill(parent, name, position, size, new Color32(37, 30, 28, 255));
        border.raycastTarget = true;
        RectTransform rt = border.rectTransform;
        Fill(rt, "WoodFrame", Vector2.zero, size - Vector2.one * 6, Wood);
        Fill(rt, "Inset", Vector2.zero, size - Vector2.one * 14, new Color32(34, 48, 48, 255));
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
            Fill(rt, "BrassCorner", new Vector2(x * (size.x / 2 - 9), y * (size.y / 2 - 9)), new Vector2(4, 4), Gold);
        return rt;
    }

    private Text Label(Transform parent, string name, string content, Vector2 position, Vector2 size, int fontSize,
        Color color, FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        Text text = Rect(parent, name, position, size).gameObject.AddComponent<Text>();
        text.font = font; text.fontSize = fontSize; text.fontStyle = style; text.color = color; text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false; text.supportRichText = false; text.text = content;
        return text;
    }

    private Button MenuButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color, Action clicked)
    {
        Image edge = Fill(parent, name, position, size, new Color32(16, 25, 28, 255));
        edge.raycastTarget = true;
        Image face = Fill(edge.transform, "Face", new Vector2(0, 2), size - new Vector2(6, 8), Color.white);
        face.raycastTarget = true;
        Button button = edge.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        ColorBlock states = button.colors;
        states.normalColor = color; states.highlightedColor = Color.Lerp(color, Paper, 0.2f);
        states.selectedColor = Color.Lerp(color, Gold, 0.32f); states.pressedColor = Color.Lerp(color, Color.black, 0.22f);
        states.fadeDuration = 0.08f; button.colors = states;
        Label(edge.transform, "Label", label, new Vector2(0, 2), size - new Vector2(18, 10), 23, Paper);
        button.onClick.AddListener(() => clicked());
        return button;
    }

    private Slider MakeSlider(Transform parent, Vector2 position, Vector2 size)
    {
        Image background = Fill(parent, "VolumeSlider", position, size, new Color32(17, 29, 31, 255));
        background.raycastTarget = true;
        Slider slider = background.gameObject.AddComponent<Slider>();
        RectTransform fillArea = Stretch(background.transform, "FillArea");
        fillArea.offsetMin = new Vector2(4, 6); fillArea.offsetMax = new Vector2(-4, -6);
        Image fill = Stretch(fillArea, "Fill").gameObject.AddComponent<Image>();
        fill.sprite = pixel; fill.color = Gold; fill.raycastTarget = false;
        RectTransform handleArea = Stretch(background.transform, "HandleArea");
        handleArea.offsetMin = new Vector2(10, 0); handleArea.offsetMax = new Vector2(-10, 0);
        Image handle = Fill(handleArea, "Handle", Vector2.zero, new Vector2(18, 32), Paper);
        handle.raycastTarget = true;
        slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
        slider.minValue = 0; slider.maxValue = 1; slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private Toggle MakeToggle(Transform parent, string label, Vector2 position, Vector2 size)
    {
        RectTransform root = Rect(parent, "FullscreenToggle", position, size);
        Image box = Fill(root, "Box", new Vector2(-size.x / 2 + 18, 0), new Vector2(32, 32), Paper);
        box.raycastTarget = true;
        Image check = Fill(box.transform, "Check", Vector2.zero, new Vector2(20, 20), Moss);
        Toggle toggle = root.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = box; toggle.graphic = check;
        Text labelText = Label(root, "Label", label, new Vector2(27, 0), size - new Vector2(70, 0), 22, Paper, FontStyle.Normal, TextAnchor.MiddleLeft);
        labelText.raycastTarget = true;
        return toggle;
    }

    private Dropdown MakeDropdown(Transform parent, Vector2 position, Vector2 size)
    {
        Image background = Fill(parent, "ResolutionDropdown", position, size, Paper);
        background.raycastTarget = true;
        Dropdown dropdown = background.gameObject.AddComponent<Dropdown>();
        dropdown.targetGraphic = background;
        Text caption = Label(background.transform, "Caption", "", new Vector2(-15, 0), size - new Vector2(54, 4), 21, Ink, FontStyle.Normal, TextAnchor.MiddleLeft);
        Label(background.transform, "Arrow", "▼", new Vector2(size.x / 2 - 23, 0), new Vector2(30, 32), 18, Wood);
        RectTransform template = Rect(background.transform, "Template", Vector2.zero, new Vector2(0, 180));
        template.anchorMin = new Vector2(0, 0); template.anchorMax = new Vector2(1, 0);
        template.pivot = new Vector2(0.5f, 1); template.anchoredPosition = new Vector2(0, -3);
        Image templateBg = template.gameObject.AddComponent<Image>(); templateBg.sprite = pixel; templateBg.color = Paper;
        ScrollRect scroll = template.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
        scroll.scrollSensitivity = 35f;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        RectTransform viewport = Stretch(template, "Viewport");
        Image maskImage = viewport.gameObject.AddComponent<Image>(); maskImage.sprite = pixel;
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        RectTransform content = Rect(viewport, "Content", Vector2.zero, new Vector2(0, 40));
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
        RectTransform item = Rect(content, "Item", Vector2.zero, new Vector2(0, 40));
        item.anchorMin = new Vector2(0, 0.5f); item.anchorMax = new Vector2(1, 0.5f);
        Image itemBg = item.gameObject.AddComponent<Image>(); itemBg.sprite = pixel; itemBg.color = Paper;
        Toggle itemToggle = item.gameObject.AddComponent<Toggle>(); itemToggle.targetGraphic = itemBg;
        itemToggle.graphic = Fill(item, "Check", new Vector2(-size.x / 2 + 12, 0), new Vector2(8, 8), Moss);
        Text itemText = Label(item, "Label", "", new Vector2(8, 0), new Vector2(size.x - 46, 38), 21, Ink, FontStyle.Normal, TextAnchor.MiddleLeft);
        scroll.viewport = viewport; scroll.content = content;
        dropdown.template = template; dropdown.captionText = caption; dropdown.itemText = itemText;
        template.gameObject.SetActive(false);
        return dropdown;
    }
}
