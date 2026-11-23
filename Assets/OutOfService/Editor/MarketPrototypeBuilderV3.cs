using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using OutOfService;

public static class MarketPrototypeBuilderV3
{
    static Material floor, wall, shelf, shelfTop, red, blue, green, yellow, dark, glass, fabric, skin, cream, produce;
    static Transform root;

    [MenuItem("Out Of Service/Build Market Prototype V3")]
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
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MarketPrototype_v3.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/MarketPrototype_v3.unity", true) };
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
        skin = Mat("player hands", new Color(0.52f, 0.37f, 0.29f));
        cream = Mat("warm shop fixtures", new Color(0.45f, 0.39f, 0.29f));
        produce = Mat("produce green", new Color(0.19f, 0.38f, 0.12f));
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

    static GameObject Decor(string name, PrimitiveType primitive, Vector3 pos, Vector3 size, Material material, Transform parent = null)
    {
        GameObject obj = GameObject.CreatePrimitive(primitive);
        obj.name = name;
        obj.transform.SetParent(parent == null ? root : parent);
        obj.transform.position = pos;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
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
        Box("1F main floor", new Vector3(0, -0.25f, 0), new Vector3(100, 0.5f, 90), floor);
        // An atrium in the middle and a gap at the east side for two escalators.
        Box("2F west wing", new Vector3(-28.5f, 4.8f, 0), new Vector3(43, 0.4f, 90), floor);
        Box("2F central front", new Vector3(0, 4.8f, -27.5f), new Vector3(14, 0.4f, 35), floor);
        Box("2F central rear", new Vector3(0, 4.8f, 27.5f), new Vector3(14, 0.4f, 35), floor);
        Box("2F east wing", new Vector3(19, 4.8f, 0), new Vector3(24, 0.4f, 90), floor);
        Box("2F east outer strip", new Vector3(47.5f, 4.8f, 0), new Vector3(5, 0.4f, 90), floor);
        Box("2F escalator rear landing", new Vector3(38, 4.8f, 31), new Vector3(14, 0.4f, 28), floor);
        Box("2F escalator front landing", new Vector3(38, 4.8f, -31), new Vector3(14, 0.4f, 28), floor);
        Box("roof", new Vector3(0, 10.2f, 0), new Vector3(100, 0.4f, 90), dark);
        Box("west exterior wall", new Vector3(-50, 5, 0), new Vector3(0.5f, 10, 90), wall);
        Box("east exterior wall", new Vector3(50, 5, 0), new Vector3(0.5f, 10, 90), wall);
        Box("rear exterior wall", new Vector3(0, 5, 45), new Vector3(100, 10, 0.5f), wall);
        Box("front wall left", new Vector3(-26, 5, -45), new Vector3(48, 10, 0.5f), wall);
        Box("front wall right", new Vector3(26, 5, -45), new Vector3(48, 10, 0.5f), wall);
        Box("front doorway header", new Vector3(0, 6.5f, -45), new Vector3(4, 7, 0.5f), wall);
        for (int z = -30; z <= 30; z += 15)
        {
            Window(new Vector3(-49.69f, 7.3f, z), new Vector3(0.08f, 2.1f, 5));
            Window(new Vector3(49.69f, 7.3f, z), new Vector3(0.08f, 2.1f, 5));
        }
        for (int x = -42; x <= 42; x += 14)
            Window(new Vector3(x, 7.3f, -44.69f), new Vector3(5, 2.1f, 0.08f));
        for (int x = -40; x <= 40; x += 20)
            for (int z = -30; z <= 30; z += 20)
                Box("square support column", new Vector3(x, 5, z), new Vector3(0.8f, 10, 0.8f), wall);
        // Angled departments break the rectangular silhouette of the interior.
        Box("diagonal bakery partition", new Vector3(-33, 2.2f, -6), new Vector3(0.35f, 4.4f, 16), cream).transform.eulerAngles = new Vector3(0, 28, 0);
        Box("diagonal home-goods divider", new Vector3(29, 2.2f, 18), new Vector3(0.35f, 4.4f, 15), wall).transform.eulerAngles = new Vector3(0, -27, 0);
        for (int i = -7; i <= 7; i += 2)
            Box("atrium safety rail", new Vector3(i, 5.65f, -10), new Vector3(0.08f, 1.3f, 0.08f), shelf);
        Box("atrium front handrail", new Vector3(0, 6.3f, -10), new Vector3(14, 0.1f, 0.1f), shelf);
        Box("atrium rear handrail", new Vector3(0, 6.3f, 10), new Vector3(14, 0.1f, 0.1f), shelf);
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
        for (int row = 0; row < 3; row++)
            for (int item = -3; item <= 3; item++)
            {
                Material pack = (item + row) % 3 == 0 ? red : ((item + row) % 3 == 1 ? green : yellow);
                Decor("boxed Korean grocery product", PrimitiveType.Cube,
                    new Vector3(x + 0.46f, 1.08f + row * 0.58f, z + item * 0.82f),
                    new Vector3(0.35f, 0.4f, 0.48f), pack, group);
            }
    }

    static void BuildGroundFloor()
    {
        Box("entrance mat", new Vector3(0, 0.02f, -39), new Vector3(7, 0.04f, 5), dark);
        for (int x = -32; x <= 32; x += 8)
        {
            if (Mathf.Abs(x) < 5) continue;
            Box("checkout counter", new Vector3(x, 0.55f, -30), new Vector3(2.3f, 1.1f, 5), shelf);
            Box("cash register", new Vector3(x, 1.18f, -31), new Vector3(0.65f, 0.22f, 0.8f), blue);
            Box("checkout number light", new Vector3(x, 3.1f, -30), new Vector3(0.6f, 0.4f, 0.3f), red);
        }
        for (int x = -8; x <= 19; x += 9)
            for (int z = -12; z <= 24; z += 9)
                Shelf("grocery aisle " + x + " " + z, x, z, z < 0 ? red : green);
        for (int x = -38; x <= -22; x += 8)
            for (int z = -15; z <= 15; z += 15)
                ProduceIsland(x, z);
        for (int x = -38; x <= 26; x += 8)
            Box("refrigerated display", new Vector3(x, 1.25f, 42), new Vector3(5.5f, 2.5f, 1.2f), blue);
        for (int x = 28; x <= 42; x += 7)
            Shelf("household products " + x, x, 9, blue);
        Box("stockroom wall left", new Vector3(22, 2.3f, 32), new Vector3(19, 4.6f, 0.3f), wall);
        Box("stockroom wall right", new Vector3(42, 2.3f, 32), new Vector3(12, 4.6f, 0.3f), wall);
        Box("stockroom doorway header", new Vector3(34, 3.8f, 32), new Vector3(4, 1.6f, 0.3f), wall);
        for (int x = 25; x <= 43; x += 6)
            Box("stockroom pallet and cartons", new Vector3(x, 0.6f, 38), new Vector3(3.5f, 1.2f, 3), cream);
        for (int i = 0; i < 8; i++)
            Box("shopping cart frame", new Vector3(-43 + i * 0.8f, 0.55f, -32 + i * 0.5f), new Vector3(0.7f, 0.65f, 1.1f), shelfTop);
        Box("customer service counter", new Vector3(-35, 0.65f, -31), new Vector3(8, 1.3f, 2), cream);
        Box("bakery counter", new Vector3(-35, 0.65f, 13), new Vector3(8, 1.3f, 3), cream).transform.eulerAngles = new Vector3(0, 24, 0);
        for (int i = -3; i <= 3; i++)
            Decor("bakery loaf", PrimitiveType.Sphere, new Vector3(-35 + i, 1.45f, 13), new Vector3(0.7f, 0.32f, 0.55f), yellow);
        Label("FOOD  /  MARKET", new Vector3(0, 3.7f, -25), Vector3.zero, 1f);
        Label("CUSTOMER SERVICE", new Vector3(-35, 3.4f, -29), Vector3.zero);
        Label("BAKERY", new Vector3(-35, 3.5f, 18), Vector3.zero);
        Label("STOCKROOM", new Vector3(34, 3.5f, 31.7f), new Vector3(0, 180, 0));
    }

    static void ProduceIsland(float x, float z)
    {
        GameObject stand = Box("produce island", new Vector3(x, 0.6f, z), new Vector3(4.2f, 1.2f, 4.2f), cream);
        stand.transform.eulerAngles = new Vector3(0, 18, 0);
        for (int i = 0; i < 9; i++)
        {
            float px = x + (i % 3 - 1) * 0.85f;
            float pz = z + (i / 3 - 1) * 0.85f;
            Decor("fruit and vegetables", PrimitiveType.Sphere, new Vector3(px, 1.35f, pz),
                new Vector3(0.65f, 0.45f, 0.65f), i % 3 == 0 ? produce : (i % 3 == 1 ? red : yellow));
        }
    }

    static void BuildUpperFloor()
    {
        for (int x = -40; x <= 20; x += 10)
            for (int z = -29; z <= 27; z += 11)
            {
                if (Mathf.Abs(x) < 8 && Mathf.Abs(z) < 13) continue;
                Transform group = new GameObject("clothing rack").transform;
                group.SetParent(root);
                Box("rack base", new Vector3(x, 5.3f, z), new Vector3(3.5f, 0.5f, 5), shelf, group);
                Box("hanger rail", new Vector3(x, 7.0f, z), new Vector3(0.12f, 0.12f, 5), shelfTop, group);
                for (int i = -2; i <= 2; i++)
                    Box("hanging clothes", new Vector3(x, 6.3f, z + i * 0.9f), new Vector3(1.1f, 1.2f, 0.55f), i % 2 == 0 ? fabric : blue, group);
            }
        for (int x = -43; x <= -23; x += 5)
        {
            Box("fitting room divider", new Vector3(x, 7.3f, 37), new Vector3(0.2f, 4.5f, 10), wall);
            Box("fitting room curtain", new Vector3(x + 2.5f, 7.2f, 32), new Vector3(4.7f, 3.8f, 0.1f), fabric);
        }
        Box("staff office wall", new Vector3(13, 7.5f, 35), new Vector3(18, 5, 0.3f), wall);
        Box("staff office divider", new Vector3(22, 7.5f, 39), new Vector3(0.3f, 5, 10), wall);
        Box("staff door header", new Vector3(17, 8.8f, 35), new Vector3(2.5f, 2.4f, 0.3f), wall);
        for (int z = -7; z <= 7; z += 2)
        {
            Box("atrium left rail post", new Vector3(-7, 5.65f, z), new Vector3(0.08f, 1.3f, 0.08f), shelf);
            Box("atrium right rail post", new Vector3(7, 5.65f, z), new Vector3(0.08f, 1.3f, 0.08f), shelf);
        }
        Box("atrium left handrail", new Vector3(-7, 6.3f, 0), new Vector3(0.1f, 0.1f, 20), shelf);
        Box("atrium right handrail", new Vector3(7, 6.3f, 0), new Vector3(0.1f, 0.1f, 20), shelf);
        Label("CLOTHING  /  2F", new Vector3(-18, 8.8f, -35), Vector3.zero, 1f);
        Label("FITTING ROOM", new Vector3(-33, 8.8f, 30), Vector3.zero);
    }

    static void BuildRamp()
    {
        foreach (float x in new[] { 34.5f, 41.5f })
        {
            GameObject ramp = Box("escalator walkable slope", new Vector3(x, 2.5f, 0), new Vector3(3.5f, 0.35f, 34.4f), dark);
            ramp.transform.eulerAngles = new Vector3(-8.35f, 0, 0);
            for (int i = 0; i < 26; i++)
            {
                float t = (i + 0.5f) / 26f;
                Decor("escalator metal tread", PrimitiveType.Cube,
                    new Vector3(x, 0.15f + 4.7f * t, -16.5f + 33f * t),
                    new Vector3(3.3f, 0.08f, 0.76f), i % 2 == 0 ? shelfTop : floor);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject rail = Box("escalator glass side", new Vector3(x + side * 1.9f, 3.2f, 0), new Vector3(0.15f, 1.1f, 34.4f), glass);
                rail.transform.eulerAngles = new Vector3(-8.35f, 0, 0);
                GameObject handrail = Box("escalator moving handrail", new Vector3(x + side * 1.9f, 3.8f, 0), new Vector3(0.22f, 0.16f, 34.4f), shelf);
                handrail.transform.eulerAngles = new Vector3(-8.35f, 0, 0);
            }
        }
        Label("2F  ESCALATOR", new Vector3(38, 3.4f, -22), Vector3.zero);
    }

    static void BuildLighting()
    {
        GameObject sun = new GameObject("dim ambient directional light");
        sun.transform.SetParent(root);
        sun.transform.rotation = Quaternion.Euler(55, -25, 0);
        Light ambient = sun.AddComponent<Light>();
        ambient.type = LightType.Directional;
        ambient.intensity = 0.005f;
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.008f, 0.009f, 0.012f);
        RenderSettings.reflectionIntensity = 0.08f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.08f, 0.10f, 0.11f);
        RenderSettings.fogDensity = 0.013f;
        for (int level = 0; level < 2; level++)
            for (int x = -40; x <= 40; x += 16)
                for (int z = -36; z <= 36; z += 12)
                {
                    if (level == 1 && x > 30 && z > -17 && z < 17) continue;
                    if (level == 1 && Mathf.Abs(x) < 8 && Mathf.Abs(z) < 12) continue;
                    float y = level == 0 ? 4.45f : 9.65f;
                    Box("fluorescent fixture", new Vector3(x, y, z), new Vector3(0.35f, 0.12f, 4.2f), shelfTop);
                    GameObject lightObj = new GameObject("fluorescent light");
                    lightObj.transform.SetParent(root);
                    lightObj.transform.position = new Vector3(x, y - 0.18f, z);
                    Light lamp = lightObj.AddComponent<Light>();
                    lamp.type = LightType.Point;
                    lamp.range = 7f;
                    lamp.intensity = ((x / 8 + z / 6) % 3 == 0) ? 0.48f : 0.025f;
                    lamp.color = new Color(0.78f, 0.89f, 1f);
                    if ((x + z) % 3 == 0) lightObj.AddComponent<MarketFlicker>();
                }
    }

    static void BuildPlayer()
    {
        GameObject player = new GameObject("Player - first person");
        player.transform.position = new Vector3(0, 0.9f, -38);
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
        flashlight.range = 25;
        flashlight.spotAngle = 58;
        flashlight.intensity = 6f;
        flashlight.enabled = false;
        movement.flashlight = flashlight;
        BuildHeldModels(cameraObj.transform, movement);
    }

    static GameObject Held(Transform parent, string name, PrimitiveType shape, Vector3 localPos, Vector3 localSize, Material material, Vector3 localRotation)
    {
        GameObject obj = GameObject.CreatePrimitive(shape);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPos;
        obj.transform.localEulerAngles = localRotation;
        obj.transform.localScale = localSize;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        obj.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }

    static void BuildHeldModels(Transform camera, MarketPlayer movement)
    {
        Transform bare = new GameObject("Held - bare hands").transform;
        bare.SetParent(camera, false);
        Held(bare, "left sleeve", PrimitiveType.Cube, new Vector3(-0.42f, -0.46f, 0.66f), new Vector3(0.22f, 0.4f, 0.22f), dark, new Vector3(20, -15, -20));
        Held(bare, "left hand", PrimitiveType.Cube, new Vector3(-0.37f, -0.26f, 0.8f), new Vector3(0.18f, 0.22f, 0.17f), skin, new Vector3(20, -15, -20));
        Held(bare, "right sleeve", PrimitiveType.Cube, new Vector3(0.42f, -0.46f, 0.66f), new Vector3(0.22f, 0.4f, 0.22f), dark, new Vector3(20, 15, 20));
        Held(bare, "right hand", PrimitiveType.Cube, new Vector3(0.37f, -0.26f, 0.8f), new Vector3(0.18f, 0.22f, 0.17f), skin, new Vector3(20, 15, 20));
        movement.bareHandsModel = bare.gameObject;

        Transform torch = new GameObject("Held - flashlight").transform;
        torch.SetParent(camera, false);
        Held(torch, "right sleeve", PrimitiveType.Cube, new Vector3(0.42f, -0.46f, 0.63f), new Vector3(0.22f, 0.4f, 0.22f), dark, new Vector3(25, 10, 20));
        Held(torch, "gripping hand", PrimitiveType.Cube, new Vector3(0.38f, -0.25f, 0.77f), new Vector3(0.2f, 0.2f, 0.19f), skin, Vector3.zero);
        Held(torch, "flashlight metal body", PrimitiveType.Cylinder, new Vector3(0.38f, -0.16f, 0.89f), new Vector3(0.11f, 0.23f, 0.11f), shelfTop, new Vector3(90, 0, 0));
        Held(torch, "flashlight front rim", PrimitiveType.Cylinder, new Vector3(0.38f, -0.16f, 1.10f), new Vector3(0.16f, 0.04f, 0.16f), dark, new Vector3(90, 0, 0));
        Held(torch, "flashlight lens", PrimitiveType.Cylinder, new Vector3(0.38f, -0.16f, 1.14f), new Vector3(0.12f, 0.012f, 0.12f), yellow, new Vector3(90, 0, 0));
        movement.flashlightModel = torch.gameObject;

        Transform calculator = new GameObject("Held - calculator").transform;
        calculator.SetParent(camera, false);
        Held(calculator, "left sleeve", PrimitiveType.Cube, new Vector3(-0.38f, -0.48f, 0.66f), new Vector3(0.22f, 0.35f, 0.2f), dark, new Vector3(20, 0, -20));
        Held(calculator, "holding hand", PrimitiveType.Cube, new Vector3(-0.31f, -0.28f, 0.76f), new Vector3(0.2f, 0.2f, 0.2f), skin, Vector3.zero);
        Held(calculator, "calculator body", PrimitiveType.Cube, new Vector3(-0.3f, -0.12f, 0.86f), new Vector3(0.32f, 0.46f, 0.07f), shelf, new Vector3(-12, 0, -8));
        Held(calculator, "calculator display", PrimitiveType.Cube, new Vector3(-0.3f, 0.03f, 0.81f), new Vector3(0.25f, 0.09f, 0.01f), green, new Vector3(-12, 0, -8));
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
                Held(calculator, "calculator key", PrimitiveType.Cube,
                    new Vector3(-0.4f + col * 0.1f, -0.09f - row * 0.09f, 0.81f),
                    new Vector3(0.07f, 0.05f, 0.015f), row == 2 ? red : shelfTop, Vector3.zero);
        movement.calculatorModel = calculator.gameObject;
        Light viewLight = new GameObject("subtle hands visibility").AddComponent<Light>();
        viewLight.transform.SetParent(camera, false);
        viewLight.transform.localPosition = new Vector3(0, -0.35f, 0.55f);
        viewLight.type = LightType.Point;
        viewLight.range = 1.3f;
        viewLight.intensity = 0.35f;
    }

    static void BuildObjectives()
    {
        GameObject exit = Box("LOCKED FRONT EXIT - E", new Vector3(0, 1.5f, -44.7f), new Vector3(3.8f, 3, 0.2f), glass);
        exit.AddComponent<MarketInteractable>().kind = MarketInteractable.Kind.Exit;
        Label("EXIT  /  POWER OFF", new Vector3(0, 3.6f, -44.4f), Vector3.zero);
        AddFuse("Fuse A - cold display", new Vector3(-37, 1.2f, 39));
        AddFuse("Fuse B - stockroom", new Vector3(38, 1.2f, 39));
        AddFuse("Fuse C - clothing floor", new Vector3(-37, 6.1f, 27));
        AddBattery(new Vector3(22, 1f, -31));
        AddBattery(new Vector3(-16, 1f, 8));
        AddBattery(new Vector3(34, 1f, 22));
        AddBattery(new Vector3(18, 6f, 25));
        AddBattery(new Vector3(-32, 6f, -25));
        GameObject note = Box("story note - E", new Vector3(4, 1.25f, -36), new Vector3(0.7f, 0.5f, 0.15f), yellow);
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
        threat.transform.position = new Vector3(-23, 5.9f, -28);
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

