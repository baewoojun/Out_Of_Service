using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using OutOfService;

public static class MarketPrototypeBuilderV5
{
    static Material floor, wall, shelf, shelfTop, red, entranceRed, blue, green, yellow, dark, glass, fabric, skin, cream, produce, bottleClear, priceWhite;
    static Transform root;
    static Mesh bottleRowMesh;

    [MenuItem("Out Of Service/Preview Shelf V5")]
    public static void PreviewShelf()
    {
        if (!Application.isPlaying || MarketPlayer.Instance == null) return;
        MarketPlayer player = MarketPlayer.Instance;
        player.transform.position = new Vector3(-5.5f, 0.9f, -12f);
        player.transform.rotation = Quaternion.Euler(0, 270, 0);
        player.viewCamera.transform.localRotation = Quaternion.identity;
        player.flashlight.enabled = true;
    }

    [MenuItem("Out Of Service/Preview Entrance V5")]
    public static void PreviewEntrance()
    {
        if (!Application.isPlaying || MarketPlayer.Instance == null) return;
        MarketPlayer player = MarketPlayer.Instance;
        player.transform.position = new Vector3(0, 0.9f, -37.8f);
        player.transform.rotation = Quaternion.Euler(0, 180, 0);
        player.viewCamera.transform.localRotation = Quaternion.identity;
        player.flashlight.enabled = true;
    }

    [MenuItem("Out Of Service/Build Market Prototype V5")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        root = new GameObject("Market Prototype V5 - Korean departments").transform;
        MakeMaterials();
        BuildShell();
        BuildCeilingDetails();
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
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MarketPrototype_v5.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/MarketPrototype_v5.unity", true) };
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
        entranceRed = Mat("entrance lit red", new Color(0.63f, 0.065f, 0.065f));
        entranceRed.EnableKeyword("_EMISSION");
        entranceRed.SetColor("_EmissionColor", new Color(0.35f, 0.012f, 0.012f));
        blue = Mat("utility blue", new Color(0.08f, 0.26f, 0.4f));
        green = Mat("food green", new Color(0.13f, 0.32f, 0.18f));
        yellow = Mat("objective yellow", new Color(0.95f, 0.73f, 0.12f));
        dark = Mat("dark ceiling", new Color(0.10f, 0.11f, 0.12f));
        glass = Mat("opaque reflective night glass", new Color(0.012f, 0.018f, 0.026f), 0.65f, 0.94f);
        fabric = Mat("clothing colours", new Color(0.34f, 0.36f, 0.43f));
        skin = Mat("player hands", new Color(0.52f, 0.37f, 0.29f));
        cream = Mat("warm shop fixtures", new Color(0.45f, 0.39f, 0.29f));
        produce = Mat("produce green", new Color(0.19f, 0.38f, 0.12f));
        bottleClear = Mat("bottle clear blue", new Color(0.23f, 0.54f, 0.71f), 0.08f, 0.8f);
        priceWhite = Mat("price label white", new Color(0.89f, 0.89f, 0.84f));
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

    static void Frustum(List<Vector3> vertices, List<int> triangles, float zCenter, float y0, float y1, float r0, float r1)
    {
        int start = vertices.Count;
        for (int ring = 0; ring < 2; ring++)
            for (int side = 0; side < 8; side++)
            {
                float angle = side * Mathf.PI * 2f / 8f;
                float radius = ring == 0 ? r0 : r1;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, ring == 0 ? y0 : y1, zCenter + Mathf.Sin(angle) * radius));
            }
        for (int side = 0; side < 8; side++)
        {
            int next = (side + 1) % 8;
            triangles.Add(start + side); triangles.Add(start + 8 + side); triangles.Add(start + 8 + next);
            triangles.Add(start + side); triangles.Add(start + 8 + next); triangles.Add(start + next);
        }
    }

    static Mesh BottleRowMesh()
    {
        if (bottleRowMesh != null) return bottleRowMesh;
        const string meshPath = "Assets/OutOfService/Generated/BottleRow12.asset";
        bottleRowMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (bottleRowMesh != null) return bottleRowMesh;
        var vertices = new List<Vector3>();
        var bodies = new List<int>();
        var labels = new List<int>();
        var caps = new List<int>();
        for (int i = 0; i < 12; i++)
        {
            float z = (i - 5.5f) * 0.51f;
            Frustum(vertices, bodies, z, 0, 0.36f, 0.12f, 0.12f);
            Frustum(vertices, bodies, z, 0.36f, 0.46f, 0.12f, 0.065f);
            Frustum(vertices, labels, z, 0.12f, 0.27f, 0.126f, 0.126f);
            Frustum(vertices, caps, z, 0.46f, 0.54f, 0.068f, 0.068f);
        }
        bottleRowMesh = new Mesh { name = "low-poly bottle row 12" };
        bottleRowMesh.SetVertices(vertices);
        bottleRowMesh.subMeshCount = 3;
        bottleRowMesh.SetTriangles(bodies, 0);
        bottleRowMesh.SetTriangles(labels, 1);
        bottleRowMesh.SetTriangles(caps, 2);
        bottleRowMesh.RecalculateNormals();
        if (!AssetDatabase.IsValidFolder("Assets/OutOfService/Generated"))
            AssetDatabase.CreateFolder("Assets/OutOfService", "Generated");
        AssetDatabase.CreateAsset(bottleRowMesh, meshPath);
        return bottleRowMesh;
    }

    static void BottleRow(string name, Vector3 position, Material drink, Transform parent = null, float rotationY = 0)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent == null ? root : parent);
        obj.transform.position = position;
        obj.transform.eulerAngles = new Vector3(0, rotationY, 0);
        obj.AddComponent<MeshFilter>().sharedMesh = BottleRowMesh();
        obj.AddComponent<MeshRenderer>().sharedMaterials = new[] { drink, priceWhite, red };
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
        BuildEntrance();
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

    static void BuildEntrance()
    {
        // The player starts inside. The closed automatic doors and their reflections sit behind them.
        Box("entrance red fascia", new Vector3(0, 3.55f, -43.65f), new Vector3(12.5f, 1.05f, 0.24f), entranceRed);
        Label("Homeplus", new Vector3(0, 3.55f, -43.42f), new Vector3(0, 180, 0), 0.9f);
        Box("entrance top steel track", new Vector3(0, 2.84f, -43.75f), new Vector3(12.4f, 0.12f, 0.35f), shelfTop);
        Box("entrance threshold", new Vector3(0, 0.04f, -43.73f), new Vector3(12.4f, 0.08f, 0.5f), shelfTop);
        for (int i = -3; i <= 3; i++)
        {
            float x = i * 1.65f;
            Box("automatic door dark reflective glass", new Vector3(x, 1.44f, -43.76f), new Vector3(1.57f, 2.7f, 0.08f), glass);
            Box("automatic door aluminium stile", new Vector3(x - 0.79f, 1.44f, -43.66f), new Vector3(0.065f, 2.76f, 0.1f), shelfTop);
            Box("red safety stripe", new Vector3(x, 1.36f, -43.63f), new Vector3(1.53f, 0.13f, 0.025f), red);
            Box("door reflection", new Vector3(x + 0.3f, 2.15f, -43.62f), new Vector3(0.025f, 0.45f, 0.018f), priceWhite);
        }
        Box("entrance side poster left", new Vector3(-7.0f, 1.6f, -43.56f), new Vector3(1.7f, 2.3f, 0.12f), entranceRed);
        Box("entrance side poster right", new Vector3(7.0f, 1.6f, -43.56f), new Vector3(1.7f, 2.3f, 0.12f), entranceRed);
        Label("행사 안내", new Vector3(-7.0f, 2.2f, -43.40f), new Vector3(0, 180, 0), 0.35f);
        Label("SALE", new Vector3(7.0f, 2.2f, -43.40f), new Vector3(0, 180, 0), 0.45f);
        for (int x = -4; x <= 4; x += 8)
        {
            Box("queue post", new Vector3(x, 0.5f, -40.5f), new Vector3(0.08f, 1.0f, 0.08f), shelfTop);
            Decor("queue post round foot", PrimitiveType.Cylinder, new Vector3(x, 0.04f, -40.5f), new Vector3(0.32f, 0.08f, 0.32f), shelfTop);
        }
        Box("entrance mat logo stripe", new Vector3(0, 0.05f, -39), new Vector3(5.8f, 0.01f, 0.35f), red);
    }

    static void Shelf(string name, float x, float z, Material accent)
    {
        Transform group = new GameObject(name).transform;
        group.SetParent(root);
        Box("recessed dark plinth", new Vector3(x, 0.18f, z), new Vector3(1.55f, 0.36f, 7.1f), shelf, group);
        Box("metal back panel", new Vector3(x, 1.5f, z), new Vector3(0.09f, 2.5f, 7.1f), priceWhite, group);
        Box("front end upright", new Vector3(x, 1.5f, z - 3.5f), new Vector3(1.55f, 2.7f, 0.09f), shelfTop, group);
        Box("rear end upright", new Vector3(x, 1.5f, z + 3.5f), new Vector3(1.55f, 2.7f, 0.09f), shelfTop, group);
        for (int tier = 0; tier < 5; tier++)
        {
            float shelfY = 0.57f + tier * 0.51f;
            Box("thin metal shelf " + tier, new Vector3(x, shelfY, z), new Vector3(1.55f, 0.055f, 7.1f), shelfTop, group);
            foreach (int side in new[] { -1, 1 })
            {
                float frontX = x + side * 0.78f;
                Box("white shelf-edge price rail", new Vector3(frontX, shelfY + 0.06f, z), new Vector3(0.035f, 0.11f, 7.1f), priceWhite, group);
                bool bottled = (Mathf.RoundToInt(x + z) + tier) % 3 != 0;
                if (bottled)
                {
                    Material drink = (tier + side + 6) % 3 == 0 ? bottleClear : ((tier + side + 6) % 3 == 1 ? green : yellow);
                    BottleRow("twelve capped drink bottles", new Vector3(x + side * 0.41f, shelfY + 0.035f, z), drink, group);
                }
                else
                {
                    for (int item = -5; item <= 5; item++)
                        Decor("small food package", PrimitiveType.Cube,
                            new Vector3(x + side * 0.41f, shelfY + 0.23f, z + item * 0.58f),
                            new Vector3(0.33f, 0.38f, 0.48f), item % 2 == 0 ? red : blue, group);
                }
                for (int price = -2; price <= 2; price++)
                    Decor("price ticket", PrimitiveType.Cube,
                        new Vector3(frontX + side * 0.025f, shelfY + 0.05f, z + price * 1.35f),
                        new Vector3(0.04f, 0.085f, 0.46f), price % 3 == 0 ? red : priceWhite, group);
                if (tier == 2)
                {
                    Box("red 1+1 sale tag", new Vector3(frontX + side * 0.09f, shelfY - 0.13f, z - 1.4f),
                        new Vector3(0.055f, 0.32f, 0.56f), red, group);
                }
            }
        }
        Box("slim red aisle fascia", new Vector3(x, 3.05f, z - 3.54f), new Vector3(1.75f, 0.3f, 0.09f), accent, group);
        Box("endcap shelf", new Vector3(x, 1.3f, z - 3.65f), new Vector3(1.35f, 0.1f, 0.45f), shelfTop, group);
    }

    static void AisleSign(int number, Vector3 position)
    {
        Box("hanging aisle sign red header", position, new Vector3(1.45f, 0.43f, 0.12f), red);
        Box("hanging aisle sign white body", position + Vector3.down * 0.48f, new Vector3(1.45f, 0.55f, 0.12f), priceWhite);
        Label(number.ToString("00"), position + new Vector3(0, 0, -0.09f), Vector3.zero, 0.75f);
        for (int i = 0; i < 3; i++)
            Box("category print line", position + new Vector3(0, -0.34f - i * 0.14f, -0.08f), new Vector3(0.85f, 0.025f, 0.015f), shelf);
        Box("sign ceiling hanger", position + Vector3.up * 0.78f, new Vector3(0.035f, 1.15f, 0.035f), shelfTop);
    }

    static void BuildGroundFloor()
    {
        Box("entrance mat", new Vector3(0, 0.02f, -39), new Vector3(7, 0.04f, 5), dark);
        for (int x = -32; x <= 32; x += 8)
        {
            if (Mathf.Abs(x) < 5) continue;
            CheckoutLane(x, (x + 40) / 8);
        }
        string[] aisleNames = { "라면 · 즉석밥", "과자 · 시리얼", "생수 · 탄산", "커피 · 차" };
        for (int x = -8; x <= 19; x += 9)
            for (int z = -12; z <= 24; z += 9)
            {
                int category = (x + 8) / 9;
                Shelf(aisleNames[category] + " aisle " + x + " " + z, x, z, z < 0 ? red : green);
                if (z == -12)
                {
                    AisleSign(10 + category, new Vector3(x, 3.48f, z - 4));
                    Label(aisleNames[category], new Vector3(x, 2.96f, z - 4.12f), Vector3.zero, 0.32f);
                }
            }
        for (int x = -38; x <= -22; x += 8)
            for (int z = -15; z <= 15; z += 15)
                ProduceIsland(x, z);
        for (int x = -38; x <= 26; x += 8)
            FridgeBank(x, 42, (x + 38) / 8);
        BuildProduceWall();
        BuildFrozenBanks();
        for (int x = 28; x <= 42; x += 7)
            Shelf("household products " + x, x, 9, blue);
        SelfCheckout(new Vector3(40, 0, -30));
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
        Label("식품 · 생활", new Vector3(0, 3.7f, -25), Vector3.zero, 1f);
        Label("CUSTOMER SERVICE", new Vector3(-35, 3.4f, -29), Vector3.zero);
        Label("BAKERY", new Vector3(-35, 3.5f, 18), Vector3.zero);
        Label("STOCKROOM", new Vector3(34, 3.5f, 31.7f), new Vector3(0, 180, 0));
    }

    static void CheckoutLane(float x, int number)
    {
        Box("checkout lane base", new Vector3(x, 0.48f, -30), new Vector3(2.3f, 0.96f, 5.7f), shelf);
        Box("moving black conveyor belt", new Vector3(x, 0.99f, -30.7f), new Vector3(2.05f, 0.06f, 3.5f), dark);
        Box("red checkout edge", new Vector3(x - 1.12f, 1.03f, -30), new Vector3(0.1f, 0.15f, 5.7f), red);
        Box("red checkout edge", new Vector3(x + 1.12f, 1.03f, -30), new Vector3(0.1f, 0.15f, 5.7f), red);
        Box("barcode scanner", new Vector3(x, 1.07f, -28.2f), new Vector3(1.0f, 0.12f, 0.6f), blue);
        Box("register screen stem", new Vector3(x + 0.7f, 1.31f, -28.4f), new Vector3(0.08f, 0.5f, 0.08f), shelfTop);
        Box("register dark display", new Vector3(x + 0.7f, 1.62f, -28.4f), new Vector3(0.75f, 0.5f, 0.08f), dark);
        Box("register screen light", new Vector3(x + 0.7f, 1.62f, -28.34f), new Vector3(0.53f, 0.3f, 0.02f), blue);
        Box("bagging tray", new Vector3(x, 1.03f, -27.55f), new Vector3(1.8f, 0.09f, 0.85f), shelfTop);
        Box("checkout number sign", new Vector3(x, 3.5f, -30), new Vector3(0.8f, 0.6f, 0.12f), red);
        Label(number.ToString("00"), new Vector3(x, 3.5f, -30.1f), Vector3.zero, 0.57f);
    }

    static void SelfCheckout(Vector3 position)
    {
        Transform kiosk = new GameObject("self-checkout kiosk").transform;
        kiosk.SetParent(root);
        Box("rounded-style pale housing", position + new Vector3(0, 1.4f, 0), new Vector3(1.25f, 2.8f, 0.75f), priceWhite, kiosk);
        Box("large black touchscreen", position + new Vector3(0, 1.78f, -0.4f), new Vector3(0.95f, 1.18f, 0.08f), dark, kiosk);
        Box("screen blue reflection", position + new Vector3(0, 1.83f, -0.45f), new Vector3(0.74f, 0.75f, 0.015f), blue, kiosk);
        Box("card terminal", position + new Vector3(-0.7f, 1.1f, -0.45f), new Vector3(0.28f, 0.42f, 0.2f), shelf, kiosk);
        Box("receipt slot", position + new Vector3(0, 0.65f, -0.44f), new Vector3(0.6f, 0.08f, 0.04f), dark, kiosk);
        Box("bagging platform", position + new Vector3(1.25f, 0.72f, 0), new Vector3(1.2f, 0.13f, 1.5f), shelfTop, kiosk);
        Box("status lamp", position + new Vector3(0, 3.0f, 0), new Vector3(0.35f, 0.28f, 0.35f), green, kiosk);
    }

    static void FridgeBank(float x, float z, int section)
    {
        string[] sections = { "생수", "탄산음료", "주스", "우유", "요구르트", "두부", "반찬", "냉장식품", "치즈" };
        string title = sections[Mathf.Clamp(section, 0, sections.Length - 1)];
        Transform bank = new GameObject("냉장 - " + title).transform;
        bank.SetParent(root);
        Box("black cooler cabinet", new Vector3(x, 1.35f, z), new Vector3(5.8f, 2.7f, 1.35f), dark, bank);
        Box("cool white back panel", new Vector3(x, 1.35f, z - 0.15f), new Vector3(5.5f, 2.35f, 0.08f), priceWhite, bank);
        for (int tier = 0; tier < 4; tier++)
        {
            float y = 0.42f + tier * 0.54f;
            Box("cooler shelf", new Vector3(x, y, z - 0.51f), new Vector3(5.55f, 0.06f, 0.56f), shelfTop, bank);
            Material productColor = section == 0 ? bottleClear : section == 1 ? red : section == 2 ? yellow : section == 3 ? priceWhite : section == 4 ? green : cream;
            if (section <= 4)
                BottleRow(title + " bottles", new Vector3(x, y + 0.04f, z - 0.69f), productColor, bank, 90f);
            else
                for (int item = -5; item <= 5; item++)
                    Decor(title + " chilled packs", PrimitiveType.Cube, new Vector3(x + item * 0.47f, y + 0.24f, z - 0.7f),
                        new Vector3(0.34f, 0.37f, 0.29f), item % 3 == 0 ? yellow : productColor, bank);
        }
        for (int door = -2; door <= 2; door++)
            Box("cooler glass door frame", new Vector3(x + door * 1.13f, 1.35f, z - 0.72f), new Vector3(0.065f, 2.55f, 0.06f), shelfTop, bank);
        Box("cooler LED header", new Vector3(x, 2.71f, z - 0.69f), new Vector3(5.8f, 0.11f, 0.08f), priceWhite, bank);
        Box("cooler red price banner", new Vector3(x, 2.91f, z - 0.68f), new Vector3(5.8f, 0.3f, 0.07f), red, bank);
        Label(title, new Vector3(x, 2.93f, z - 0.74f), Vector3.zero, 0.4f);
        Label("1,980", new Vector3(x - 1.3f, 0.15f, z - 0.89f), Vector3.zero, 0.24f);
        GameObject lightObj = new GameObject("cold cabinet interior light");
        lightObj.transform.SetParent(bank);
        lightObj.transform.position = new Vector3(x, 2.2f, z - 0.85f);
        Light glow = lightObj.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = 4.5f;
        glow.intensity = 0.34f;
        glow.color = new Color(0.55f, 0.79f, 1f);
    }

    static void BuildCeilingDetails()
    {
        for (int level = 0; level < 2; level++)
        {
            float y = level == 0 ? 4.48f : 9.92f;
            for (int x = -48; x <= 48; x += 8)
                Decor("exposed ceiling beam", PrimitiveType.Cube, new Vector3(x, y, 0), new Vector3(0.09f, 0.12f, 89), shelfTop);
            for (int z = -42; z <= 42; z += 8)
                Decor("exposed ceiling crossbar", PrimitiveType.Cube, new Vector3(0, y, z), new Vector3(99, 0.12f, 0.09f), shelfTop);
        }
    }

    static void BuildProduceWall()
    {
        string[] groups = { "잎채소", "버섯", "뿌리채소", "과일", "쌈채소" };
        Material[] colors = { produce, cream, yellow, red, green };
        for (int section = 0; section < groups.Length; section++)
        {
            float z = -22 + section * 11f;
            Transform wallStand = new GameObject("청과 벽면 - " + groups[section]).transform;
            wallStand.SetParent(root);
            Box("woodgrain fascia", new Vector3(-46.5f, 3.25f, z), new Vector3(0.25f, 0.85f, 10.3f), cream, wallStand);
            Box("dark refrigerated base", new Vector3(-46.2f, 0.38f, z), new Vector3(2.5f, 0.75f, 10.3f), dark, wallStand);
            for (int tier = 0; tier < 3; tier++)
            {
                float y = 0.82f + tier * 0.72f;
                var tray = Box("sloping produce shelf", new Vector3(-45.8f, y, z), new Vector3(1.72f, 0.09f, 10.1f), shelfTop, wallStand);
                tray.transform.eulerAngles = new Vector3(0, 0, 12);
                Box("shelf front lip", new Vector3(-44.85f, y - 0.14f, z), new Vector3(0.08f, 0.2f, 10.1f), shelfTop, wallStand);
                for (int item = -8; item <= 8; item++)
                {
                    Vector3 p = new Vector3(-45.5f, y + 0.19f, z + item * 0.55f);
                    PrimitiveType shape = section == 0 || section == 4 ? PrimitiveType.Capsule : PrimitiveType.Sphere;
                    Vector3 scale = section == 0 || section == 4 ? new Vector3(0.32f, 0.28f, 0.42f) : new Vector3(0.4f, 0.34f, 0.4f);
                    Decor(groups[section] + " individual item", shape, p, scale, colors[section], wallStand);
                }
            }
            Label(groups[section], new Vector3(-44.86f, 3.28f, z), new Vector3(0, 90, 0), 0.64f);
            Label(section % 2 == 0 ? "2,980" : "3,990", new Vector3(-44.77f, 0.78f, z - 2), new Vector3(0, 90, 0), 0.27f);
        }
    }

    static void BuildFrozenBanks()
    {
        string[] types = { "냉동만두", "아이스크림", "냉동피자", "냉동채소", "냉동육류" };
        for (int section = 0; section < types.Length; section++)
        {
            float x = -38 + section * 9.1f;
            Transform bank = new GameObject("냉동 - " + types[section]).transform;
            bank.SetParent(root);
            Box("frozen cabinet dark body", new Vector3(x, 1.4f, 31), new Vector3(7.4f, 2.8f, 1.6f), dark, bank);
            Box("frozen pale interior", new Vector3(x, 1.35f, 30.45f), new Vector3(7.05f, 2.4f, 0.16f), priceWhite, bank);
            for (int tier = 0; tier < 4; tier++)
            {
                float y = 0.45f + tier * 0.52f;
                Box("freezer shelf", new Vector3(x, y, 30.15f), new Vector3(7.05f, 0.06f, 0.55f), shelfTop, bank);
                for (int item = -7; item <= 7; item++)
                {
                    Material pack = (section + item + 30) % 3 == 0 ? blue : (section % 2 == 0 ? red : yellow);
                    Decor(types[section] + " Korean retail pack", PrimitiveType.Cube,
                        new Vector3(x + item * 0.44f, y + 0.20f, 30.07f),
                        new Vector3(0.37f, 0.32f, 0.24f), pack, bank);
                }
            }
            for (int door = -3; door <= 3; door++)
                Box("frosted door steel stile", new Vector3(x + door * 1.07f, 1.4f, 30.03f), new Vector3(0.07f, 2.6f, 0.08f), shelfTop, bank);
            Box("cold LED", new Vector3(x, 2.78f, 30.01f), new Vector3(7.35f, 0.09f, 0.08f), priceWhite, bank);
            Box("freezer blue category header", new Vector3(x, 3.04f, 30.02f), new Vector3(7.4f, 0.42f, 0.08f), blue, bank);
            Label(types[section], new Vector3(x, 3.05f, 29.96f), Vector3.zero, 0.54f);
        }
    }

    static void ProduceIsland(float x, float z)
    {
        string[] names = { "사과", "배", "감귤", "양파", "감자", "대파", "토마토", "오이", "고구마" };
        int index = Mathf.Clamp(Mathf.RoundToInt((x + 38) / 8f) * 3 + Mathf.RoundToInt((z + 15) / 15f), 0, names.Length - 1);
        string itemName = names[index];
        Transform stand = new GameObject("청과 매대 - " + itemName).transform;
        stand.SetParent(root);
        Box("low pallet plinth", new Vector3(x, 0.23f, z), new Vector3(4.15f, 0.46f, 4.15f), cream, stand);
        for (int quadrant = 0; quadrant < 4; quadrant++)
        {
            float dx = quadrant % 2 == 0 ? -1.02f : 1.02f;
            float dz = quadrant < 2 ? -1.02f : 1.02f;
            GameObject tray = Box("sloped timber produce tray", new Vector3(x + dx, 0.85f, z + dz),
                new Vector3(1.9f, 0.16f, 1.9f), cream, stand);
            tray.transform.eulerAngles = new Vector3(dz < 0 ? 12 : -12, 0, dx < 0 ? -12 : 12);
            for (int i = 0; i < 9; i++)
            {
                float px = x + dx + (i % 3 - 1) * 0.45f;
                float pz = z + dz + (i / 3 - 1) * 0.45f;
                PrimitiveType shape = index == 5 || index == 7 ? PrimitiveType.Capsule : PrimitiveType.Sphere;
                Vector3 scale = index == 5 || index == 7 ? new Vector3(0.22f, 0.45f, 0.22f) : new Vector3(0.38f, 0.32f, 0.38f);
                Material color = index == 0 || index == 6 ? red : index == 1 || index == 2 || index == 4 ? yellow : index == 3 || index == 8 ? cream : produce;
                Decor(itemName + " individual produce", shape, new Vector3(px, 1.15f, pz), scale, color, stand);
            }
        }
        Box("red sale placard", new Vector3(x, 1.9f, z - 2.1f), new Vector3(0.95f, 0.6f, 0.08f), red, stand);
        Label(itemName, new Vector3(x, 1.96f, z - 2.16f), Vector3.zero, 0.32f);
        Label(index % 2 == 0 ? "3,980" : "2,490", new Vector3(x, 1.67f, z - 2.16f), Vector3.zero, 0.22f);
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
        RenderSettings.fogColor = new Color(0.045f, 0.060f, 0.068f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.038f;
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


