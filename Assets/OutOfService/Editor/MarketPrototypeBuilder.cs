using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using OutOfService;

public static class MarketPrototypeBuilder
{
    static Material floor, wall, shelf, shelfTop, red, blue, green, yellow, dark, glass, fabric;
    static Transform root;

    [MenuItem("Out Of Service/Test Damage")]
    public static void TestDamage()
    {
        if (Application.isPlaying && MarketPlayer.Instance != null)
            MarketPlayer.Instance.TakeDamage(25f);
        else
            Debug.Log("Start Play mode before testing damage.");
    }

    [MenuItem("Out Of Service/Build Market Prototype")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        root = new GameObject("Market Prototype - replace primitive art later").transform;
        MakeMaterials();
        BuildShell();
        BuildGroundFloor();
        BuildUpperFloor();
        BuildRamp();
        BuildLighting();
        BuildPlayer();
        BuildObjectives();
        BuildThreat();
        new GameObject("Game Logic").AddComponent<MarketGame>();
        new GameObject("Fluorescent Hum").AddComponent<MarketAmbience>();
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MarketPrototype_v2.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/MarketPrototype_v2.unity", true) };
        Selection.activeGameObject = root.gameObject;
        Debug.Log("MarketPrototype built. Press Play: WASD / Shift / Mouse / E / F / Esc.");
    }

    static Material Mat(string name, Color color, float metallic = 0, float smooth = 0.3f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = name, color = color };
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smooth);
        return material;
    }

    static void MakeMaterials()
    {
        floor = Mat("polished grey floor", new Color(0.48f, 0.5f, 0.52f), 0.15f, 0.7f);
        wall = Mat("warm off-white walls", new Color(0.77f, 0.78f, 0.72f));
        shelf = Mat("shelf charcoal", new Color(0.16f, 0.18f, 0.2f), 0.3f);
        shelfTop = Mat("shelf light trim", new Color(0.67f, 0.7f, 0.68f), 0.15f);
        red = Mat("market red", new Color(0.58f, 0.08f, 0.08f));
        blue = Mat("utility blue", new Color(0.08f, 0.26f, 0.4f));
        green = Mat("food green", new Color(0.13f, 0.32f, 0.18f));
        yellow = Mat("objective yellow", new Color(0.95f, 0.73f, 0.12f));
        dark = Mat("dark ceiling", new Color(0.10f, 0.11f, 0.12f));
        glass = Mat("opaque reflective night glass", new Color(0.012f, 0.018f, 0.026f), 0.65f, 0.94f);
        fabric = Mat("clothing colours", new Color(0.34f, 0.36f, 0.43f));
    }

    static GameObject Box(string name, Vector3 pos, Vector3 size, Material material, Transform parent = null)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent == null ? root : parent);
        obj.transform.position = pos;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    static void Label(string text, Vector3 pos, Vector3 rotation, float size = 0.75f)
    {
        GameObject obj = new GameObject("Sign - " + text);
        obj.transform.SetParent(root);
        obj.transform.position = pos;
        obj.transform.eulerAngles = rotation;
        TextMesh mesh = obj.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = 60;
        mesh.characterSize = size / 6f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.color = Color.white;
    }

    static void BuildShell()
    {
        Box("1F ground floor", new Vector3(0, -0.25f, 0), new Vector3(60, 0.5f, 60), floor);
        Box("2F west slab", new Vector3(-5, 4.8f, 0), new Vector3(50, 0.4f, 60), floor);
        Box("2F east rear slab", new Vector3(25, 4.8f, -22), new Vector3(10, 0.4f, 16), floor);
        Box("2F east front slab", new Vector3(25, 4.8f, 21), new Vector3(10, 0.4f, 18), floor);
        Box("roof", new Vector3(0, 10.2f, 0), new Vector3(60, 0.4f, 60), dark);
        Box("west exterior wall", new Vector3(-30, 5, 0), new Vector3(0.5f, 10, 60), wall);
        Box("east exterior wall", new Vector3(30, 5, 0), new Vector3(0.5f, 10, 60), wall);
        Box("rear exterior wall", new Vector3(0, 5, 30), new Vector3(60, 10, 0.5f), wall);
        Box("front wall left", new Vector3(-16, 5, -30), new Vector3(28, 10, 0.5f), wall);
        Box("front wall right", new Vector3(16, 5, -30), new Vector3(28, 10, 0.5f), wall);
        Box("front doorway header", new Vector3(0, 6.5f, -30), new Vector3(4, 7, 0.5f), wall);
        for (int z = -18; z <= 18; z += 12)
        {
            Window(new Vector3(-29.69f, 7.3f, z), new Vector3(0.08f, 2.1f, 5));
            Window(new Vector3(29.69f, 7.3f, z), new Vector3(0.08f, 2.1f, 5));
        }
        for (int x = -22; x <= 22; x += 11)
            Window(new Vector3(x, 7.3f, -29.69f), new Vector3(5, 2.1f, 0.08f));
        for (int x = -24; x <= 24; x += 12)
            for (int z = -20; z <= 20; z += 20)
                Box("square support column", new Vector3(x, 5, z), new Vector3(0.8f, 10, 0.8f), wall);
    }

    static void Window(Vector3 position, Vector3 size)
    {
        Box("opaque window - no exterior view", position, size, glass);
        Vector3 glintSize = size.x < size.z ? new Vector3(0.09f, 0.035f, size.z * 0.65f) : new Vector3(size.x * 0.65f, 0.035f, 0.09f);
        Vector3 offset = size.x < size.z ? new Vector3(position.x < 0 ? 0.06f : -0.06f, 0.45f, 0) : new Vector3(0, 0.45f, 0.06f);
        Box("window reflected light streak", position + offset, glintSize, shelfTop);
    }

    static void Shelf(string name, float x, float z, Material accent)
    {
        Transform group = new GameObject(name).transform;
        group.SetParent(root);
        Box("base", new Vector3(x, 0.35f, z), new Vector3(1.4f, 0.7f, 7), shelf, group);
        Box("left upright", new Vector3(x - 0.65f, 1.5f, z), new Vector3(0.12f, 2.3f, 7), shelf, group);
        Box("right upright", new Vector3(x + 0.65f, 1.5f, z), new Vector3(0.12f, 2.3f, 7), shelf, group);
        foreach (float y in new[] { 0.85f, 1.4f, 2f, 2.55f })
            Box("product shelf", new Vector3(x, y, z), new Vector3(1.3f, 0.08f, 7), shelfTop, group);
        Box("aisle header", new Vector3(x, 2.9f, z), new Vector3(1.5f, 0.35f, 7), accent, group);
    }

    static void BuildGroundFloor()
    {
        Box("entrance mat", new Vector3(0, 0.02f, -25), new Vector3(5, 0.04f, 5), dark);
        for (int x = -18; x <= 18; x += 9)
            Box("checkout counter", new Vector3(x, 0.55f, -18), new Vector3(2.3f, 1.1f, 5), shelf);
        for (int x = -22; x <= 14; x += 9)
            for (int z = -5; z <= 13; z += 9)
                Shelf("food aisle " + x + " " + z, x, z, z < 0 ? red : green);
        for (int x = -22; x <= 12; x += 8)
            Box("refrigerated display", new Vector3(x, 1.25f, 27), new Vector3(5.5f, 2.5f, 1.2f), blue);
        Box("stockroom partition left", new Vector3(-19, 2.3f, 20), new Vector3(16, 4.6f, 0.3f), wall);
        Box("stockroom partition right", new Vector3(-2, 2.3f, 20), new Vector3(10, 4.6f, 0.3f), wall);
        Box("stockroom door header", new Vector3(-8.5f, 3.8f, 20), new Vector3(3, 1.6f, 0.3f), wall);
        Label("FOOD  /  MARKET", new Vector3(0, 3.7f, -14.3f), Vector3.zero, 1f);
        Label("STOCKROOM", new Vector3(-8.5f, 3.5f, 19.7f), new Vector3(0, 180, 0));
    }

    static void BuildUpperFloor()
    {
        for (int x = -21; x <= 12; x += 11)
            for (int z = -19; z <= 18; z += 12)
            {
                Transform group = new GameObject("clothing rack").transform;
                group.SetParent(root);
                Box("rack base", new Vector3(x, 5.3f, z), new Vector3(3.5f, 0.5f, 5), shelf, group);
                Box("hanger rail", new Vector3(x, 7.0f, z), new Vector3(0.12f, 0.12f, 5), shelfTop, group);
                for (int i = -2; i <= 2; i++)
                    Box("hanging clothes", new Vector3(x, 6.3f, z + i * 0.9f), new Vector3(1.1f, 1.2f, 0.55f), i % 2 == 0 ? fabric : blue, group);
            }
        Box("staff office wall", new Vector3(-21, 7.5f, 22), new Vector3(18, 5, 0.3f), wall);
        Box("staff office divider", new Vector3(-12, 7.5f, 25.5f), new Vector3(0.3f, 5, 9), wall);
        Box("staff door header", new Vector3(-17, 8.8f, 22), new Vector3(2.5f, 2.4f, 0.3f), wall);
        Label("CLOTHING  /  2F", new Vector3(-4, 8.8f, -24), Vector3.zero, 1f);
    }

    static void BuildRamp()
    {
        GameObject ramp = Box("service ramp - temporary stair route", new Vector3(25, 2.5f, 0), new Vector3(5, 0.4f, 27.5f), floor);
        ramp.transform.eulerAngles = new Vector3(-10.4f, 0, 0);
        Box("ramp west railing", new Vector3(22.3f, 3.2f, 0), new Vector3(0.2f, 1.2f, 27), red).transform.eulerAngles = new Vector3(-10.4f, 0, 0);
        Box("ramp east railing", new Vector3(27.7f, 3.2f, 0), new Vector3(0.2f, 1.2f, 27), red).transform.eulerAngles = new Vector3(-10.4f, 0, 0);
        Label("2F UP", new Vector3(25, 2.5f, -17), Vector3.zero);
    }

    static void BuildLighting()
    {
        GameObject sun = new GameObject("dim ambient directional light");
        sun.transform.SetParent(root);
        sun.transform.rotation = Quaternion.Euler(55, -25, 0);
        Light ambient = sun.AddComponent<Light>();
        ambient.type = LightType.Directional;
        ambient.intensity = 0.025f;
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.025f, 0.028f, 0.034f);
        RenderSettings.reflectionIntensity = 0.08f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.08f, 0.10f, 0.11f);
        RenderSettings.fogDensity = 0.013f;
        for (int level = 0; level < 2; level++)
            for (int x = -22; x <= 22; x += 11)
                for (int z = -22; z <= 22; z += 11)
                {
                    if (level == 1 && x > 18 && z > -12 && z < 12) continue;
                    float y = level == 0 ? 4.45f : 9.65f;
                    Box("fluorescent fixture", new Vector3(x, y, z), new Vector3(0.35f, 0.12f, 4.2f), shelfTop);
                    GameObject lightObj = new GameObject("fluorescent light");
                    lightObj.transform.SetParent(root);
                    lightObj.transform.position = new Vector3(x, y - 0.18f, z);
                    Light lamp = lightObj.AddComponent<Light>();
                    lamp.type = LightType.Point;
                    lamp.range = 7.5f;
                    lamp.intensity = ((x + z) % 2 == 0) ? 0.9f : 0.16f;
                    lamp.color = new Color(0.78f, 0.89f, 1f);
                    if ((x + z) % 3 == 0) lightObj.AddComponent<MarketFlicker>();
                }
    }

    static void BuildPlayer()
    {
        GameObject player = new GameObject("Player - first person");
        player.transform.position = new Vector3(0, 0.9f, -24);
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.stepOffset = 0.35f;
        cc.slopeLimit = 45;
        MarketPlayer movement = player.AddComponent<MarketPlayer>();
        GameObject cameraObj = new GameObject("Player Camera");
        cameraObj.transform.SetParent(player.transform);
        cameraObj.transform.localPosition = new Vector3(0, 0.65f, 0);
        Camera camera = cameraObj.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.fieldOfView = 75;
        movement.viewCamera = camera;
        cameraObj.AddComponent<AudioListener>();
        Light flashlight = cameraObj.AddComponent<Light>();
        flashlight.type = LightType.Spot;
        flashlight.range = 24;
        flashlight.spotAngle = 58;
        flashlight.intensity = 4.5f;
        flashlight.enabled = false;
        movement.flashlight = flashlight;
    }

    static void BuildObjectives()
    {
        GameObject exit = Box("LOCKED FRONT EXIT - E", new Vector3(0, 1.5f, -29.7f), new Vector3(3.8f, 3, 0.2f), glass);
        exit.AddComponent<MarketInteractable>().kind = MarketInteractable.Kind.Exit;
        Label("EXIT  /  POWER OFF", new Vector3(0, 3.6f, -29.4f), Vector3.zero);
        AddFuse("Fuse A - cold display", new Vector3(11, 1.2f, 25));
        AddFuse("Fuse B - stockroom", new Vector3(-23, 1.2f, 26));
        AddFuse("Fuse C - clothing floor", new Vector3(-24, 6.1f, 19));
        AddBattery(new Vector3(18, 1f, -22));
        AddBattery(new Vector3(-13, 1f, 8));
        AddBattery(new Vector3(11, 6f, 17));
        GameObject note = Box("story note - E", new Vector3(4, 1.25f, -22), new Vector3(0.7f, 0.5f, 0.15f), yellow);
        var interact = note.AddComponent<MarketInteractable>();
        interact.kind = MarketInteractable.Kind.Note;
        interact.note = "마감 방송이 멈추지 않는다. 퓨즈 세 개를 찾아 정문 전원을 복구해라.";
    }

    static void AddBattery(Vector3 pos)
    {
        GameObject battery = Box("battery pickup - E", pos, new Vector3(0.45f, 0.7f, 0.45f), blue);
        battery.AddComponent<MarketInteractable>().kind = MarketInteractable.Kind.Battery;
        Box("battery top", pos + Vector3.up * 0.4f, new Vector3(0.2f, 0.12f, 0.2f), yellow, battery.transform);
    }

    static void BuildThreat()
    {
        GameObject threat = new GameObject("temporary clothing-floor mannequin enemy");
        threat.transform.position = new Vector3(-5, 5.9f, -24);
        threat.AddComponent<MarketThreat>();
        threat.transform.SetParent(root);
        Box("mannequin body", threat.transform.position + Vector3.up * 0.8f, new Vector3(0.75f, 1.6f, 0.5f), dark, threat.transform);
        Box("mannequin head", threat.transform.position + Vector3.up * 1.8f, new Vector3(0.55f, 0.55f, 0.55f), wall, threat.transform);
        Box("red face mark", threat.transform.position + new Vector3(0, 1.8f, 0.29f), new Vector3(0.32f, 0.1f, 0.06f), red, threat.transform);
        GameObject eye = new GameObject("mannequin red glow");
        eye.transform.SetParent(threat.transform);
        eye.transform.localPosition = new Vector3(0, 1.8f, 0.4f);
        Light glow = eye.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = 3;
        glow.intensity = 1.5f;
        glow.color = Color.red;
    }

    static void AddFuse(string name, Vector3 pos)
    {
        GameObject fuse = Box(name, pos, new Vector3(0.7f, 0.7f, 0.7f), yellow);
        fuse.AddComponent<MarketInteractable>().kind = MarketInteractable.Kind.Fuse;
        GameObject glow = new GameObject("fuse glow");
        glow.transform.SetParent(fuse.transform);
        glow.transform.localPosition = Vector3.zero;
        Light lamp = glow.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.range = 4;
        lamp.intensity = 3;
        lamp.color = new Color(1, 0.75f, 0.1f);
    }
}
