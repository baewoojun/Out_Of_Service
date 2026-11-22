using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using OutOfService;

public static partial class MarketPrototypeBuilderV7
{
    static Material floor, wall, shelf, shelfTop, red, entranceRed, blue, green, yellow, dark, glass, fabric, skin, cream, produce, bottleClear, priceWhite, wornFloor, wornMetal, fluorescent, signWhite, signRed, bagFilm, mistWhite;
    static Material cabinetGlass;
    static Transform root;
    static Mesh bottleRowMesh;
    static Mesh pouchMesh;
    static Material[] producePalette;
    static readonly Dictionary<string, Mesh> handmadeMeshes = new Dictionary<string, Mesh>();
    static readonly Dictionary<int, Material> packageMaterials = new Dictionary<int, Material>();
    static readonly Dictionary<string, Material> modelMaterials = new Dictionary<string, Material>();

    [MenuItem("Out Of Service/Preview Coolers V7")]
    public static void PreviewCoolers()
    {
        if (!Application.isPlaying || MarketPlayer.Instance == null) return;
        MarketPlayer player = MarketPlayer.Instance;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = new Vector3(7f, 0.9f, 38.8f);
        player.transform.rotation = Quaternion.identity;
        player.viewCamera.transform.localRotation = Quaternion.identity;
        if (controller != null) controller.enabled = true;
        player.flashlight.enabled = true;
    }

    [MenuItem("Out Of Service/Preview Dairy V7")]
    public static void PreviewDairy()
    {
        if (!Application.isPlaying || MarketPlayer.Instance == null) return;
        MarketPlayer player = MarketPlayer.Instance;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = new Vector3(-17f, 0.9f, 38.8f);
        player.transform.rotation = Quaternion.identity;
        player.viewCamera.transform.localRotation = Quaternion.identity;
        if (controller != null) controller.enabled = true;
        player.flashlight.enabled = true;
    }

    [MenuItem("Out Of Service/Preview Freezer V7")]
    public static void PreviewFreezer()
    {
        if (!Application.isPlaying || MarketPlayer.Instance == null) return;
        MarketPlayer player = MarketPlayer.Instance;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = new Vector3(-3f, 0.9f, 35.5f);
        player.transform.rotation = Quaternion.identity;
        player.viewCamera.transform.LookAt(new Vector3(-3f, 1.6f, 31f));
        player.enabled = false;
        if (controller != null) controller.enabled = true;
        player.flashlight.enabled = true;
    }

    [MenuItem("Out Of Service/Preview Open Cooler Door V7")]
    public static void PreviewOpenCoolerDoor()
    {
        PreviewCoolers();
        if (!Application.isPlaying || MarketPlayer.Instance == null) return;
        MarketPlayer.Instance.enabled = false;
        GameObject door = GameObject.Find("생수 · 탄산 냉장고 문 3 - E to open");
        if (door != null) door.GetComponent<MarketSwingDoor>().Toggle();
        else Debug.LogError("Cooler door missing from scene");
    }

    [MenuItem("Out Of Service/Preview Shelf V7")]
    public static void PreviewShelf()
    {
        if (!Application.isPlaying || MarketPlayer.Instance == null) return;
        MarketPlayer player = MarketPlayer.Instance;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = new Vector3(-5.5f, 0.9f, -12f);
        player.transform.rotation = Quaternion.Euler(0, 270, 0);
        player.viewCamera.transform.LookAt(new Vector3(-8, 1.55f, -12f));
        player.enabled = false;
        if (controller != null) controller.enabled = true;
        player.flashlight.enabled = true;
    }

    [MenuItem("Out Of Service/Preview Real Produce V7")]
    public static void PreviewRealProduce()
    {
        if (!Application.isPlaying || MarketPlayer.Instance == null) return;
        MarketPlayer player = MarketPlayer.Instance;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = new Vector3(-38f, 0.9f, -18.4f);
        player.transform.rotation = Quaternion.identity;
        player.viewCamera.transform.LookAt(new Vector3(-38f, 1.1f, -15f));
        player.enabled = false;
        if (controller != null) controller.enabled = true;
        player.flashlight.enabled = true;
    }

    [MenuItem("Out Of Service/Preview Vegetable Pile V7")]
    public static void PreviewVegetablePile()
    {
        if (!Application.isPlaying || MarketPlayer.Instance == null) return;
        MarketPlayer player = MarketPlayer.Instance;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = new Vector3(-30f, 0.9f, -18.4f);
        player.transform.rotation = Quaternion.identity;
        player.viewCamera.transform.LookAt(new Vector3(-30f, 1.1f, -15f));
        player.enabled = false;
        if (controller != null) controller.enabled = true;
        player.flashlight.enabled = true;
    }

    [MenuItem("Out Of Service/Preview Entrance V7")]
    public static void PreviewEntrance()
    {
        if (!Application.isPlaying || MarketPlayer.Instance == null) return;
        MarketPlayer player = MarketPlayer.Instance;
        player.transform.position = new Vector3(0, 0.9f, -37.8f);
        player.transform.rotation = Quaternion.Euler(0, 180, 0);
        player.viewCamera.transform.localRotation = Quaternion.identity;
        player.flashlight.enabled = true;
    }

    [MenuItem("Out Of Service/Preview Break Room V7")]
    public static void PreviewBreakRoom()
    {
        if (!Application.isPlaying || MarketPlayer.Instance == null) return;
        MarketPlayer player = MarketPlayer.Instance;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = new Vector3(17, 5.9f, 35);
        player.transform.rotation = Quaternion.Euler(0, 0, 0);
        player.viewCamera.transform.localRotation = Quaternion.identity;
        if (controller != null) controller.enabled = true;
        player.flashlight.enabled = true;
        Debug.Log("Break room preview position: " + player.transform.position);
    }

    [MenuItem("Out Of Service/Preview Escalator V7")]
    public static void PreviewEscalator()
    {
        if (!Application.isPlaying || MarketPlayer.Instance == null) return;
        MarketPlayer player = MarketPlayer.Instance;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = new Vector3(34.5f, 0.9f, -22f);
        player.transform.rotation = Quaternion.identity;
        player.viewCamera.transform.localRotation = Quaternion.identity;
        if (controller != null) controller.enabled = true;
        player.flashlight.enabled = true;
    }

    [MenuItem("Out Of Service/Preview Market Signs V7")]
    public static void PreviewMarketSigns()
    {
        if (!Application.isPlaying || MarketPlayer.Instance == null) return;
        MarketPlayer player = MarketPlayer.Instance;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = new Vector3(-8, 0.9f, -19);
        player.transform.rotation = Quaternion.identity;
        player.enabled = false;
        player.viewCamera.transform.LookAt(new Vector3(-8, 3.2f, -16.07f));
        if (controller != null) controller.enabled = true;
        GameObject printed = GameObject.Find("printed front aisle_10");
        Debug.Log("Sign preview at " + player.transform.position + ", camera " + player.viewCamera.transform.rotation
            + ", sign texture " + (printed == null ? "missing" : printed.GetComponent<Renderer>().sharedMaterial.mainTexture?.name));
    }

    [MenuItem("Out Of Service/Build Market Prototype V7")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        root = new GameObject("Market Prototype V7 - textured horror environment").transform;
        MakeMaterials();
        BuildShell();
        BuildCeilingDetails();
        BuildGroundFloor();
        BuildUpperFloor();
        BuildRamp();
        BuildThreeFloorExtension();
        BuildingArchitecturalFinish();
        BuildRetailCirculation();
        BuildLighting();
        BuildPlayer();
        BuildObjectives();
        BuildThreat();
        ShopSurfaceFinish();
        SpatialGraphicsFinish();
        ApplyAfterHoursLightBalance();
        BuildIndirectBounce();
        BakeShopReflections();
        BatchSmallProps();
        new GameObject("Game Logic").AddComponent<MarketGame>();
        new GameObject("Startup render preparation").AddComponent<MarketRenderPreparation>();
        new GameObject("Fluorescent Hum").AddComponent<MarketAmbience>();
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MarketPrototype_v7.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/MarketPrototype_v7.unity", true) };
        Selection.activeGameObject = root.gameObject;
        Debug.Log("MarketPrototype V7 built. Press Play: WASD / Shift / Space / Mouse / E / F / Esc.");
    }

    static Material Mat(string name, Color color, float metallic = 0, float smooth = 0.3f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = name, color = color, enableInstancing = true };
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smooth);
        return material;
    }

    static Material UnlitMat(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader) { name = name };
        material.SetColor("_BaseColor", color);
        return material;
    }

    static Material TransparentMat(string name, Color color)
    {
        Material material = Mat(name, color, 0.05f, 0.82f);
        material.SetFloat("_Surface", 1);
        material.SetFloat("_Blend", 0);
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetShaderPassEnabled("ShadowCaster", false);
        material.renderQueue = (int)RenderQueue.Transparent;
        return material;
    }

    static Material Pbr(string name, string assetName, Color tint, Vector2 tiling, float metallic, float smoothness)
    {
        Material material = Mat(name, tint, metallic, smoothness);
        string folder = "Assets/OutOfService/Art/Textures/";
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + assetName + "_Diffuse.jpg");
        string normalPath = folder + assetName + "_nor_dx.jpg";
        TextureImporter importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        if (albedo != null)
        {
            material.SetTexture("_BaseMap", albedo);
            material.SetTextureScale("_BaseMap", tiling);
        }
        if (normal != null)
        {
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 0.65f);
            material.SetTextureScale("_BumpMap", tiling);
        }
        return material;
    }

    static void MakeMaterials()
    {
        freshMaterials.Clear();
        floorMaterials.Clear();
        packageMaterials.Clear();
        retailSurfaces.Clear();
        modelMaterials.Clear();
        floor = Mat("polished grey supermarket porcelain tile", new Color(0.86f, 0.88f, 0.9f), 0f, 0.34f);
        Texture2D tilePrint = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Textures/supermarket_gray_tile_v2.png");
        if (tilePrint != null)
        {
            floor.SetTexture("_BaseMap", tilePrint);
            floor.SetTextureScale("_BaseMap", new Vector2(25, 23));
        }
        FloorFinish();
        wall = Pbr("worn painted wall PBR", "painted_plaster_wall", new Color(0.78f, 0.77f, 0.72f), new Vector2(8, 3), 0f, 0.23f);
        wornFloor = Pbr("staff room worn concrete PBR", "worn_concrete_floor", new Color(0.76f, 0.75f, 0.73f), new Vector2(5, 4), 0f, 0.2f);
        wornMetal = Pbr("rusted stockroom metal PBR", "metal_plate_02", new Color(0.7f, 0.7f, 0.68f), new Vector2(2, 2), 0.65f, 0.3f);
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
        producePalette = new[] {
            Mat("apple red", new Color(0.58f, 0.09f, 0.07f)),
            Mat("Korean pear golden", new Color(0.68f, 0.54f, 0.27f), 0f, 0.12f),
            Mat("tangerine orange", new Color(0.94f, 0.37f, 0.055f), 0f, 0.18f),
            Mat("onion papery tan", new Color(0.60f, 0.40f, 0.20f), 0f, 0.12f),
            Mat("potato earth brown", new Color(0.39f, 0.29f, 0.17f), 0f, 0.11f),
            Mat("scallion leaf green", new Color(0.10f, 0.36f, 0.14f)),
            Mat("tomato ripe red", new Color(0.67f, 0.095f, 0.055f), 0f, 0.45f),
            Mat("cucumber dark green", new Color(0.11f, 0.29f, 0.10f), 0f, 0.27f),
            Mat("sweet potato red brown", new Color(0.34f, 0.16f, 0.15f)),
            Mat("lettuce fresh green", new Color(0.28f, 0.49f, 0.15f)),
            Mat("mushroom beige", new Color(0.57f, 0.49f, 0.38f))
        };
        foreach(int kind in new[]{2,4,6,7,9})
            producePalette[kind].mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Packs/ProduceSkin"+kind+".png");
        bottleClear = Mat("bottle clear blue", new Color(0.23f, 0.54f, 0.71f), 0.08f, 0.8f);
        priceWhite = Mat("price label white", new Color(0.89f, 0.89f, 0.84f));
        fluorescent = Mat("aged fluorescent diffuser", new Color(0.78f, 0.84f, 0.79f));
        fluorescent.EnableKeyword("_EMISSION");
        fluorescent.SetColor("_EmissionColor", new Color(0.45f, 0.55f, 0.53f));
        signWhite = UnlitMat("illuminated store sign white", new Color(0.86f, 0.86f, 0.82f));
        signRed = UnlitMat("illuminated store sign red", new Color(0.64f, 0.035f, 0.055f));
        produceFilm = TransparentMat("thin produce retail film",new Color(.85f,.96f,.87f,.055f));
        produceFilm.SetFloat("_Smoothness",.25f);
        produceFilm.DisableKeyword("_ENVIRONMENTREFLECTIONS_ON");
        produceFilm.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        produceFilm.SetFloat("_SpecularHighlights",0f);
        bagFilm = TransparentMat("clear produce bags and freezer doors", new Color(0.77f, 0.88f, 0.9f, 0.20f));
        bagFilm.SetFloat("_Smoothness", .52f);
        cabinetGlass = TransparentMat("Anti-reflective display case glazing", new Color(.49f, .65f, .69f, .045f));
        cabinetGlass.SetFloat("_SpecularHighlights", 0f);
        cabinetGlass.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        mistWhite = TransparentMat("cold condensation", new Color(0.85f, 0.94f, 1f, 0.18f));
    }

    static Material PackMaterial(int productIndex, int variant = 0)
    {
        int key = productIndex * 6 + variant;
        if (packageMaterials.TryGetValue(key, out Material cached)) return cached;
        Material wrapper = Mat("Korean supermarket pack " + productIndex + " variant " + variant, Color.white, 0f, productIndex == 4 ? .55f : productIndex == 0 || productIndex == 5 || productIndex == 20 ? .38f : productIndex == 28 ? .10f : .20f);
        string path = "Assets/OutOfService/Art/Packs/pack_" + productIndex.ToString("D2") + "_" + variant + ".png";
        Texture2D artwork = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (artwork == null) Debug.LogError("Missing package artwork: " + path);
        else wrapper.SetTexture("_BaseMap", artwork);
        packageMaterials.Add(key, wrapper);
        return wrapper;
    }

    static int ShelfArtIndex(int category)
    {
        if (category == 21) return 28; // paper goods
        if (category == 22) return 27; // laundry products
        if (category == 24) return 29; // pet food
        return category;
    }

    static GameObject Box(string name, Vector3 pos, Vector3 size, Material material, Transform parent = null)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent == null ? root : parent);
        obj.transform.position = pos;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = material == floor && size.y < .6f ? SizedFloor(size) : material;
        return obj;
    }

    static GameObject Pickable(string name, PrimitiveType shape, Vector3 pos, Vector3 size, Material material, Transform parent)
    {
        GameObject item = GameObject.CreatePrimitive(shape);
        item.name = name;
        item.transform.SetParent(parent);
        item.transform.position = pos;
        item.transform.localScale = size;
        item.GetComponent<Renderer>().sharedMaterial = material;
        Rigidbody body = item.AddComponent<Rigidbody>();
        body.isKinematic = true;
        item.AddComponent<MarketProduct>();
        return item;
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

    static Bounds ModelBounds(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    static GameObject RealModel(string id, Vector3 floorCenter, float targetHeight, Transform parent)
    {
        string folder = "Assets/OutOfService/Art/Models/" + id + "/";
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(folder + id + ".fbx");
        if (source == null) { Debug.LogError("Missing CC0 model: " + id); return null; }
        GameObject instance = Object.Instantiate(source);
        instance.name = "Poly Haven CC0 - " + id;
        instance.transform.SetParent(parent == null ? root : parent);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        Bounds bounds = ModelBounds(instance);
        if (bounds.size.y < 0.001f) { Object.DestroyImmediate(instance); return null; }
        float scale = targetHeight / bounds.size.y;
        instance.transform.localScale *= scale;
        bounds = ModelBounds(instance);
        instance.transform.position = floorCenter - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        if (!modelMaterials.TryGetValue(id, out Material material))
        {
            material = Mat("Poly Haven " + id, Color.white, id.Contains("shelves") ? 0.65f : 0.03f,
                id.Contains("shelves") ? 0.44f : 0.24f);
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + id + "_diff_1k.jpg");
            if (albedo != null) material.SetTexture("_BaseMap", albedo);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + id + "_nor_gl_1k.exr");
            if (normal != null) { material.SetTexture("_BumpMap", normal); material.SetFloat("_BumpScale", .6f); material.EnableKeyword("_NORMALMAP"); }
            modelMaterials.Add(id, material);
        }
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterial = material;
        bool bulkFruit=id=="food_pears_asian_01" || id=="sweet_potato" || id=="yellow_onion";
        if(bulkFruit)
        {
            int part=0;
            foreach(var filter in instance.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.sharedMesh!=null) filter.sharedMesh=ClusterRetailMesh(filter.sharedMesh,id+"_"+part,10);
                part++;
            }
        }
        else AddRetailModelLOD(instance, id);
        return instance;
    }

    static Mesh SoftPouchMesh()
    {
        if (pouchMesh != null) return pouchMesh;
        const string path = "Assets/OutOfService/Generated/SoftRetailPouchReadable.asset";
        pouchMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (pouchMesh != null) return pouchMesh;
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        for (int face = 0; face < 2; face++)
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                {
                    float bulge = row == 1 && col == 1 ? 0.5f : row == 1 || col == 1 ? 0.38f : 0.17f;
                    vertices.Add(new Vector3((face == 0 ? 1 : -1) * bulge, (row - 1) * 0.5f, (col - 1) * 0.5f));
                    uv.Add(new Vector2(face == 0 ? col * .5f : 1f-col * .5f, row * .5f));
                }
        for (int face = 0; face < 2; face++)
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 2; col++)
                {
                    int a = face * 9 + row * 3 + col;
                    int b = a + 1;
                    int c = a + 3;
                    int d = c + 1;
                    if (face == 0)
                    {
                        triangles.Add(a); triangles.Add(c); triangles.Add(b);
                        triangles.Add(b); triangles.Add(c); triangles.Add(d);
                    }
                    else
                    {
                        triangles.Add(a); triangles.Add(b); triangles.Add(c);
                        triangles.Add(b); triangles.Add(d); triangles.Add(c);
                    }
                }
        int[] edge = { 0, 1, 2, 5, 8, 7, 6, 3 };
        for (int i = 0; i < edge.Length; i++)
        {
            int next = (i + 1) % edge.Length;
            triangles.Add(edge[i]); triangles.Add(edge[next]); triangles.Add(9 + edge[i]);
            triangles.Add(edge[next]); triangles.Add(9 + edge[next]); triangles.Add(9 + edge[i]);
        }
        pouchMesh = new Mesh { name = "pillow shaped retail bag" };
        pouchMesh.SetVertices(vertices);
        pouchMesh.SetUVs(0, uv);
        pouchMesh.SetTriangles(triangles, 0);
        pouchMesh.RecalculateNormals();
        if (!AssetDatabase.IsValidFolder("Assets/OutOfService/Generated"))
            AssetDatabase.CreateFolder("Assets/OutOfService", "Generated");
        AssetDatabase.CreateAsset(pouchMesh, path);
        return pouchMesh;
    }

    static Mesh TurnedPackageMesh(string id, float[] heights, float[] radii)
    {
        if (handmadeMeshes.TryGetValue(id, out Mesh cached)) return cached;
        string path = "Assets/OutOfService/Generated/Handmade_" + id + ".asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) { handmadeMeshes.Add(id, mesh); return mesh; }
        const int sides = 24;
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        for (int ring = 0; ring < heights.Length; ring++)
            for (int side = 0; side <= sides; side++)
            {
                float angle = side * Mathf.PI * 2f / sides;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radii[ring], heights[ring],
                    Mathf.Sin(angle) * radii[ring]));
                uv.Add(new Vector2((float)side / sides, heights[ring]));
            }
        for (int ring = 0; ring < heights.Length - 1; ring++)
            for (int side = 0; side < sides; side++)
            {
                int a = ring * (sides + 1) + side;
                int b = a + 1;
                int c = a + sides + 1;
                int d = c + 1;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        mesh = new Mesh { name = "handmade " + id };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        if (!AssetDatabase.IsValidFolder("Assets/OutOfService/Generated"))
            AssetDatabase.CreateFolder("Assets/OutOfService", "Generated");
        AssetDatabase.CreateAsset(mesh, path);
        handmadeMeshes.Add(id, mesh);
        return mesh;
    }

    static void CartonFace(List<Vector3> vertices, List<Vector2> uv, List<int> triangles,
        params Vector3[] corners)
    {
        int first = vertices.Count;
        Vector3 faceNormal=Vector3.Cross(corners[1]-corners[0],corners[2]-corners[0]).normalized;
        for (int i = 0; i < corners.Length; i++)
        {
            vertices.Add(corners[i]);
            Vector3 p=corners[i];
            float u=Mathf.Abs(faceNormal.z)>Mathf.Abs(faceNormal.x) ? (faceNormal.z<0?p.x+.5f:.5f-p.x) : (faceNormal.x>0?p.z+.5f:.5f-p.z);
            uv.Add(new Vector2(u, p.y/.77f));
        }
        for (int i = 1; i < corners.Length - 1; i++)
        {
            triangles.Add(first);
            triangles.Add(first + i);
            triangles.Add(first + i + 1);
        }
    }

    static Mesh HandmadeMilkCartonMesh()
    {
        const string id = "folded_milk_carton_readable";
        if (handmadeMeshes.TryGetValue(id, out Mesh cached)) return cached;
        string path = "Assets/OutOfService/Generated/Handmade_" + id + ".asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) { handmadeMeshes.Add(id, mesh); return mesh; }
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        Vector3 b0 = new Vector3(-.5f, 0, -.5f), b1 = new Vector3(.5f, 0, -.5f);
        Vector3 b2 = new Vector3(.5f, 0, .5f), b3 = new Vector3(-.5f, 0, .5f);
        Vector3 s0 = new Vector3(-.5f, .77f, -.5f), s1 = new Vector3(.5f, .77f, -.5f);
        Vector3 s2 = new Vector3(.5f, .77f, .5f), s3 = new Vector3(-.5f, .77f, .5f);
        Vector3 r0 = new Vector3(0, 1f, -.5f), r1 = new Vector3(0, 1f, .5f);
        CartonFace(vertices, uv, triangles, b0, s0, s1, b1);
        CartonFace(vertices, uv, triangles, b3, b2, s2, s3);
        CartonFace(vertices, uv, triangles, b0, b3, s3, s0);
        CartonFace(vertices, uv, triangles, b1, s1, s2, b2);
        CartonFace(vertices, uv, triangles, s0, r0, s1);
        CartonFace(vertices, uv, triangles, s3, s2, r1);
        CartonFace(vertices, uv, triangles, s0, s3, r1, r0);
        CartonFace(vertices, uv, triangles, s1, r0, r1, s2);
        CartonFace(vertices, uv, triangles, b0, b1, b2, b3);
        mesh = new Mesh { name = "handmade folded milk carton" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        if (!AssetDatabase.IsValidFolder("Assets/OutOfService/Generated"))
            AssetDatabase.CreateFolder("Assets/OutOfService", "Generated");
        AssetDatabase.CreateAsset(mesh, path);
        handmadeMeshes.Add(id, mesh);
        return mesh;
    }

    static float ProduceNoise(int seed, int channel)
    {
        return Mathf.PerlinNoise(seed * 0.37f + channel * 3.11f, seed * 0.19f + channel * 1.73f);
    }

    static Mesh OrganicProduceMesh(int kind, int variant)
    {
        if(kind==4 || kind==6 || kind==7 || kind==12)return HarvestBody(kind,variant);
        string id = (kind==12 ? "pepper_crown_v2_" : "organic_produce_") + kind + "_" + variant;
        if (handmadeMeshes.TryGetValue(id, out Mesh cached)) return cached;
        string path = "Assets/OutOfService/Generated/Handmade_" + id + ".asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) { handmadeMeshes.Add(id, mesh); return mesh; }
        const int rings = 13, sides = 16;
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        for (int ring = 0; ring <= rings; ring++)
        {
            float theta = Mathf.PI * (1f - (float)ring / rings);
            float y = Mathf.Cos(theta) * 0.5f;
            for (int side = 0; side <= sides; side++)
            {
                float angle = side * Mathf.PI * 2f / sides;
                float wobble = 1f + (ProduceNoise(variant * 31 + ring * 5 + side, kind) - 0.5f) * 0.115f;
                float shape = 1f;
                if (kind == 1) shape = y > 0 ? 1f - y * 0.76f : 1f - y * 0.16f; // Korean pear
                if (kind == 3) shape = y > 0 ? 1f - y * 0.66f : 1f + y * 0.12f; // onion
                if (kind == 4 || kind == 8) shape = 0.92f + Mathf.Sin(angle * 3f + variant) * 0.07f; // tuber
                if (kind == 6) shape = 1f + Mathf.Cos(angle * 6f + variant) * 0.055f; // tomato lobes
                if (kind == 12) shape=(1f+.135f*Mathf.Cos(angle*4+variant*.17f))*(1f+y*.29f);
                if (kind == 9) shape = 1f + Mathf.Sin(angle * 7f + ring * 0.55f) * 0.12f; // leafy head
                float radius = Mathf.Sin(theta) * 0.5f * shape * wobble;
                float contourY=y;
                if(kind==13)
                {
                    radius*=1f+.19f*y+.035f*Mathf.Cos(5*angle+variant);
                    contourY-=Mathf.Sign(y)*.095f*Mathf.Pow(Mathf.Abs(2*y),8);
                }
                if(kind==12)
                {
                    float cap=Mathf.Clamp01((Mathf.Abs(y)-.31f)/.19f);
                    contourY-=Mathf.Sign(y)*cap*cap*.13f;
                    radius*=1f+.12f*Mathf.Cos(angle*4f)*Mathf.Abs(y)*2f;
                }
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, contourY,
                    Mathf.Sin(angle) * radius));
                uv.Add(new Vector2((float)side / sides, (float)ring / rings));
            }
        }
        for (int ring = 0; ring < rings; ring++)
            for (int side = 0; side < sides; side++)
            {
                int a = ring * (sides + 1) + side;
                int b = a + 1;
                int c = a + sides + 1;
                int d = c + 1;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        mesh = new Mesh { name = "hand shaped produce " + kind + " variation " + variant };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        if (!AssetDatabase.IsValidFolder("Assets/OutOfService/Generated"))
            AssetDatabase.CreateFolder("Assets/OutOfService", "Generated");
        AssetDatabase.CreateAsset(mesh, path);
        handmadeMeshes.Add(id, mesh);
        return mesh;
    }

    static Mesh HandmadeBottleMesh()
    {
        return TurnedPackageMesh("ribbed_drink_bottle",
            new[] { 0f, 0.025f, 0.07f, 0.16f, 0.22f, 0.29f, 0.36f, 0.43f, 0.50f, 0.57f,
                0.68f, 0.76f, 0.83f, 0.87f, 0.92f, 0.97f, 1f },
            new[] { 0.35f, 0.48f, 0.50f, 0.49f, 0.50f, 0.48f, 0.50f, 0.48f, 0.50f, 0.49f,
                0.47f, 0.40f, 0.28f, 0.19f, 0.17f, 0.17f, 0.17f });
    }

    static Mesh HandmadeCupMesh()
    {
        return TurnedPackageMesh("tapered_noodle_cup",
            new[] { 0f, 0.02f, 0.07f, 0.20f, 0.46f, 0.72f, 0.92f, 0.98f, 1f },
            new[] { 0.34f, 0.36f, 0.38f, 0.39f, 0.43f, 0.46f, 0.49f, 0.50f, 0.50f });
    }

    static Mesh HandmadeCanMesh()
    {
        // Both ends must be sealed: the old lathe stopped at the rim and looked
        // like an opened, empty can from the normal standing camera.
        return TurnedPackageMesh("sealed_recessed_lid_can",
            new[] {0f,0f,.015f,.04f,.09f,.91f,.96f,.985f,1f,1f,.982f,.982f},
            new[] {0f,.46f,.50f,.49f,.47f,.47f,.49f,.50f,.48f,.43f,.43f,0f});
    }

    static GameObject HandmadeProp(string name, Mesh mesh, Vector3 floorPosition, Vector3 dimensions,
        Material material, Transform parent)
    {
        GameObject prop = new GameObject(name);
        prop.transform.SetParent(parent == null ? root : parent);
        prop.transform.position = floorPosition;
        prop.transform.localScale = dimensions;
        prop.AddComponent<MeshFilter>().sharedMesh = mesh;
        prop.AddComponent<MeshRenderer>().sharedMaterial = material;
        return prop;
    }

    static void CabinetDoor(string name, Transform bank, float hingeX, float z, float width, float height, bool frosted)
    {
        Transform hinge = new GameObject(name + " - E to open").transform;
        hinge.SetParent(bank);
        hinge.position = new Vector3(hingeX, 0, z);
        hinge.gameObject.AddComponent<MarketSwingDoor>();
        float middle = hingeX + width * 0.5f;
        var panel=Box("moving glass panel", new Vector3(middle, height * 0.5f, z),
            new Vector3(width - 0.07f, height - 0.13f, 0.035f), cabinetGlass, hinge);
        panel.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        Box("moving left stile", new Vector3(hingeX + 0.025f, height * 0.5f, z),
            new Vector3(0.05f, height, 0.07f), shelfTop, hinge);
        Box("moving right stile", new Vector3(hingeX + width - 0.025f, height * 0.5f, z),
            new Vector3(0.05f, height, 0.07f), shelfTop, hinge);
        Box("moving top rail", new Vector3(middle, height - 0.035f, z),
            new Vector3(width, 0.07f, 0.07f), shelfTop, hinge);
        Box("moving lower rail", new Vector3(middle, 0.035f, z),
            new Vector3(width, 0.07f, 0.07f), shelfTop, hinge);
        Box("door rubber gasket left", new Vector3(hingeX + 0.07f, height * 0.5f, z + 0.035f),
            new Vector3(0.022f, height - 0.15f, 0.016f), dark, hinge);
        Box("door rubber gasket right", new Vector3(hingeX + width - 0.07f, height * 0.5f, z + 0.035f),
            new Vector3(0.022f, height - 0.15f, 0.016f), dark, hinge);
        Box("door rubber gasket top", new Vector3(middle, height - 0.08f, z + 0.035f),
            new Vector3(width - 0.15f, 0.022f, 0.016f), dark, hinge);
        Box("door rubber gasket bottom", new Vector3(middle, 0.08f, z + 0.035f),
            new Vector3(width - 0.15f, 0.022f, 0.016f), dark, hinge);
        Box("moving pull handle", new Vector3(hingeX + width - 0.15f, 1.3f, z - 0.10f),
            new Vector3(0.045f, 0.58f, 0.07f), wornMetal, hinge);
        Decor("upper hinge pin", PrimitiveType.Cylinder,
            new Vector3(hingeX, height - 0.09f, z - 0.03f), new Vector3(0.08f, 0.03f, 0.08f), wornMetal, hinge);
        Decor("lower hinge pin", PrimitiveType.Cylinder,
            new Vector3(hingeX, 0.09f, z - 0.03f), new Vector3(0.08f, 0.03f, 0.08f), wornMetal, hinge);
        if (frosted)
            Decor("door upper condensation", PrimitiveType.Cube,
                new Vector3(middle, height - 0.22f, z - 0.023f),
                new Vector3(width - 0.12f, 0.23f, 0.008f), mistWhite, hinge);
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
        DepthTestText(obj);
    }

    static void PrintedSign(string key, Vector3 position, float width, float height)
    {
        string path = "Assets/OutOfService/Art/Signs/" + key + ".png";
        Texture2D print = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (print == null) { Debug.LogError("Missing market sign texture: " + path); return; }
        Material ink = Mat("printed market sign " + key,Color.white,0,.23f);
        ink.EnableKeyword("_EMISSION");
        ink.SetColor("_EmissionColor",Color.white*(key=="entrance_brand"?.14f:.035f));
        ink.SetTexture("_EmissionMap",print);
        ink.SetTexture("_BaseMap", print);
        ink.mainTexture = print;
        ink.SetFloat("_Cull", 2);
        Decor("printed sign frame " + key, PrimitiveType.Cube, position, new Vector3(width + 0.08f, height + 0.08f, 0.12f), shelf);
        GameObject front = Decor("printed front " + key, PrimitiveType.Quad, position + Vector3.back * 0.14f,
            new Vector3(width, height, 1), ink);
        front.transform.eulerAngles = Vector3.zero;
        GameObject back = Decor("printed back " + key, PrimitiveType.Quad, position + Vector3.forward * 0.14f,
            new Vector3(width, height, 1), ink);
        back.transform.eulerAngles = new Vector3(0, 180, 0);
    }

    static void WallWriting(string text, Vector3 pos, float size)
    {
        GameObject obj = new GameObject("painted wall writing - " + text);
        obj.transform.SetParent(root);
        obj.transform.position = pos;
        TextMesh lettering = obj.AddComponent<TextMesh>();
        lettering.text = text;
        lettering.fontSize = 60;
        lettering.characterSize = size / 6f;
        lettering.anchor = TextAnchor.MiddleCenter;
        lettering.color = new Color(0.055f, 0.045f, 0.04f);
        DepthTestText(obj);
    }

    static void BuildShell()
    {
        BuildGroundSlabs();
        // An atrium in the middle and a gap at the east side for two escalators.
        Box("2F west wing", new Vector3(-28.5f, 4.8f, 6), new Vector3(43, 0.4f, 78), floor);
        FloorRect("2F west front apron",-50,-7,-45,-42,5);
        FloorRect("2F west stair inner apron",-43,-7,-42,-33,5);
        FloorRect("2F west stair outer rim",-50,-49,-42,-33,5);
        Box("2F central front", new Vector3(0, 4.8f, -27.5f), new Vector3(14, 0.4f, 35), floor);
        Box("2F central rear", new Vector3(0, 4.8f, 27.5f), new Vector3(14, 0.4f, 35), floor);
        Box("2F east wing", new Vector3(19, 4.8f, 0), new Vector3(24, 0.4f, 90), floor);
        FloorRect("2F east outer strip",45,50,-33,45,5);
        FloorRect("2F east stair outer rim",49,50,-42,-33,5);
        FloorRect("2F east front apron",31,50,-45,-42,5);
        Box("2F escalator rear landing", new Vector3(38, 4.8f, 31), new Vector3(14, 0.4f, 28), floor);
        FloorRect("2F escalator front landing",31,45,-33,-17,5);
        FloorRect("2F east stair inner apron",31,43,-42,-33,5);
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
        // Perimeter departments open onto the continuous customer circulation loop.
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
        PrintedSign("entrance_brand", new Vector3(0, 3.55f, -43.42f), 12.3f, 0.92f);
        PrintedSign("entrance_brand", new Vector3(0, 4.02f, -33.2f), 5.8f, 0.75f);
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

    static bool IsLiquid(int category)
    {
        return category >= 10 && category <= 14 || category == 17 || category == 19 || category == 22;
    }

    static Mesh RetailBottleProfile(int form)
    {
        // Profiles run from the base to the mouth; each package has a different shoulder and neck.
        switch (form)
        {
            case 0: return TurnedPackageMesh("retail_water_pet", new[] {0f,.025f,.08f,.16f,.18f,.32f,.34f,.48f,.50f,.68f,.72f,.84f,.91f,1f},
                new[] {0f,.44f,.49f,.48f,.44f,.44f,.49f,.49f,.44f,.44f,.46f,.28f,.16f,.16f});
            case 1: return TurnedPackageMesh("retail_cola_contour", new[] {0f,.035f,.12f,.28f,.46f,.63f,.74f,.85f,.91f,1f},
                new[] {0f,.42f,.48f,.42f,.36f,.44f,.50f,.31f,.15f,.15f});
            case 2: return TurnedPackageMesh("retail_sport_wide_neck", new[] {0f,.03f,.10f,.19f,.22f,.62f,.68f,.79f,.85f,1f},
                new[] {0f,.43f,.49f,.49f,.45f,.45f,.49f,.30f,.25f,.25f});
            case 3: return TurnedPackageMesh("retail_tea_fluted", new[] {0f,.03f,.12f,.15f,.26f,.29f,.40f,.43f,.62f,.74f,.85f,.90f,1f},
                new[] {0f,.43f,.48f,.44f,.48f,.44f,.48f,.44f,.45f,.40f,.24f,.16f,.16f});
            case 4: return TurnedPackageMesh("retail_glass_sauce", new[] {0f,.025f,.08f,.62f,.69f,.79f,.85f,1f},
                new[] {0f,.40f,.44f,.44f,.40f,.23f,.18f,.18f});
            case 5: return TurnedPackageMesh("retail_detergent_jug", new[] {0f,.025f,.10f,.55f,.70f,.82f,.89f,1f},
                new[] {0f,.40f,.49f,.49f,.43f,.29f,.22f,.22f});
            default: return TurnedPackageMesh("retail_short_jar", new[] {0f,.025f,.08f,.76f,.82f,.90f,1f},
                new[] {0f,.43f,.48f,.48f,.44f,.43f,.43f});
        }
    }

    static void RetailPrint(string name, Material artwork, Vector3 p, int side, float depth, float h, float w, Transform group)
    {
        Decor(name + " front label", PrimitiveType.Quad, p + new Vector3(side * (depth * .5f + .004f), h * .50f, 0),
            new Vector3(w * .91f, h * .72f, 1), artwork, group).transform.eulerAngles = new Vector3(0, -side * 90, 0);
    }

    static void RetailItem(string name,int category,int variant,Vector3 p,int side,Transform group)
    {
        if(category<=2) PantryFacing(name,category,variant,p,side,group,false);
        else AisleFacing(name,category,variant,p,side,group,false);
    }

    static void Shelf(string name, float x, float z, Material accent, int category = -1)
    {
        Transform group = new GameObject(name + " - category fixture").transform;
        group.SetParent(root);
        bool pallet = category == 10 || category == 21 || category == 24;
        bool pegboard = category == 9;
        bool wireBasket = category == 7;
        bool bottles = IsLiquid(category);
        int tiers = category==2 ? 8 : category<=1 ? 7 : pallet ? 3 : category == 8 || category == 22 ? 4 :
            wireBasket ? 6 : 5;
        float spacing = category==2 ? .30f : category<=1 ? .35f : pallet ? 0.67f : category == 8 || category == 22 ? 0.64f :
            wireBasket ? 0.43f : 0.51f;
        Box(pallet ? "red pallet base" : "recessed metal plinth", new Vector3(x, 0.17f, z),
            new Vector3(pallet ? 1.82f : 1.55f, 0.34f, 7.1f), pallet ? red : shelf, group);
        Box(pallet ? "pallet wire back" : pegboard ? "hanging packet pegboard" : "perforated shelf back",
            new Vector3(x, 1.40f, z), new Vector3(0.055f, 2.45f, 7.1f),
            pallet || pegboard ? shelf : priceWhite, group);
        foreach (int end in new[] { -1, 1 })
        {
            MerchBox("fixture flush end panel", new Vector3(x, 1.42f, z + end * 3.5f),
                new Vector3(1.58f, 2.6f, 0.035f), priceWhite, group,true);
            foreach(int side in new[]{-1,1})
                Decor("end panel folded edge",PrimitiveType.Cube,new Vector3(x+side*.77f,1.42f,z+end*3.526f),new Vector3(.028f,2.6f,.022f),shelfTop,group);
        }
        ShelfConstruction(x,z,group,pallet);
        if(z==-14 && (category==0 || category==5 || category==10 || category==15)) FeaturedEndcap(x,z,category,group);
        if(category==22) CleaningEndcap(x,z-3.94f,group);
        for (int tier = 0; tier < tiers; tier++)
        {
            float y = 0.54f + tier * spacing;
            Box(pallet ? "wire pallet deck" : wireBasket ? "chocolate basket tray" : "adjustable metal shelf",
                    new Vector3(x, y, z), new Vector3(pallet ? 1.78f : 1.55f, 0.05f, 7.1f),
                    pallet ? wornMetal : shelfTop, group);
            ShelfDeckDetails(x,y,z,group,pallet);
            foreach (int side in new[] { -1, 1 })
            {
                float frontX = x + side * (pallet ? 0.89f : 0.78f);
                Box("shelf edge price rail",
                    new Vector3(frontX, y + .038f, z),
                    new Vector3(.038f,.056f,7.0f),
                    pegboard ? wornMetal : priceWhite, group);
                if (wireBasket || bottles && !pallet)
                {
                    Box(wireBasket ? "basket front wire" : "bottle guard wire",
                        new Vector3(frontX, y + (wireBasket ? .11f : .072f), z),
                        new Vector3(.012f,.012f,7.0f), wornMetal, group);
                    for (int divider = -2; divider <= 2; divider++)
                        Decor("stock bay divider", PrimitiveType.Cube,
                            new Vector3(x + side * 0.42f, y + 0.060f, z + divider * 1.35f),
                            new Vector3(0.65f, 0.057f, 0.008f), wornMetal, group);
                }
                bool shipping=category==0 && tier==tiers-1;
                int count = shipping?8:category==0?25:category==1?32:category==2?36:
                    category==21?10:category==22?17:category==24?21:category==11 && tier==0?36:bottles?54:category==4?52:category==7?44:category==9?38:30;
                bool compactStock=(bottles || category==4 || category==5 || category==7) && !pallet && category!=22;
                int depthRows = category==21 || category==24 ? 1 : compactStock?3:2;
                float stride = 6.40f / count;
                for (int item = 0; item < count; item++)
                {
                    if ((item * 7 + tier * 3 + category + 40) % 53 == 0) continue;
                    int variant = ((item / 3) + tier * 2 + side + category + 12) % 6;
                    float along = z - 3.20f + (item + 0.5f) * stride;
                    if(category>2 && item%3==0 && !(category==11 && tier==0))
                        ProductTicket(category,variant,new Vector3(frontX+side*.021f,y+.038f,along),side,group);
                    if(category<=2 && !shipping && item%3==0)
                    {
                        ProductTicket(category,variant,new Vector3(frontX+side*.021f,y+.038f,along),side,group);
                    }
                    for (int depth = 0; depth < depthRows; depth++)
                    {
                        if (tier == 1 && item == 0 && depth == 0) continue; // Interactive front facing below.
                        float inset = shipping?.38f:category<=2?(depth==0?.64f:.425f):
                            depthRows == 1 ? (pallet ? 0.52f : 0.48f) : compactStock?.58f-depth*.175f:depth == 0 ? 0.58f : 0.22f;
                        Vector3 p = new Vector3(x + side * inset, y + .025f, along);
                        if(shipping)
                        {
                            if(depth==0)
                            {
                                var kraft=RetailSurface("noodle case kraft",new Color(.64f,.47f,.30f),0,.12f);
                                Decor("ramen shipping case",PrimitiveType.Cube,p+Vector3.up*.16f,new Vector3(.52f,.32f,.67f),kraft,group);
                                RetailPrint(name,PantryInk("NoodleCase",variant,.08f),p,side,.52f,.32f,.63f,group);
                            }
                        }
                        else if(category==11 && tier==0)
                        {
                            if(item%2==0 && depth<2) SodaPack(p,variant,side,group);
                        }
                        else {
                            RetailItem(name, category, variant, p, side, group);
                            if(depth==0) CatalogView(ShelfArtIndex(category),variant,p,side*Vector3.right,group,
                                10*Mathf.Abs(tier-1)+Mathf.Abs(item-count/2)*.05f+(side<0?.1f:0));
                        }
                    }
                    if(pegboard && item%4==0)
                        Decor("wire lane separator",PrimitiveType.Cube,new Vector3(x+side*.41f,y+.051f,along-.10f),
                            new Vector3(.64f,.045f,.006f),wornMetal,group);
                }
                if(category==11 && tier==0)
                {
                    for(int item=0;item<count;item+=6){
                        int variant=((item/3)+tier*2+side+category+12)%6;
                        MerchArt("six can pack price",180+variant,new Vector3(frontX+side*.021f,y+.038f,z-3.2f+(item+.5f)*stride),new Vector2(.185f,.058f),new Vector3(0,-side*90,0),group,true);
                    }
                }
                if(tier==1 && category<=2)
                    PantryFacing(name,category,(tier*2+side+category+12)%6,
                        new Vector3(x+side*.64f,y+.025f,z-3.20f+.5f*stride),side,group,true);
                else if (tier == 1)
                    AisleFacing(name,category,(tier*2+side+category+12)%6,
                        new Vector3(x+side*(depthRows==1?(pallet?.52f:.48f):.58f),y+.026f,z-3.20f+.5f*stride),side,group,true);
            }
        }
        Box("category colour fascia", new Vector3(x, 3.04f, z - 3.54f),
            new Vector3(1.78f, 0.29f, 0.09f), category >= 10 && category <= 14 ? blue : accent, group);
        if (pallet)
            for (int rib = 0; rib < 5; rib++)
                Box("pallet front rib", new Vector3(x, 0.2f, z - 2.7f + rib * 1.35f),
                    new Vector3(1.80f, 0.13f, 0.055f), wornMetal, group);
    }

    static void AisleSign(int number, string title, string detail, Vector3 position)
    {
        PrintedSign("aisle_" + number, position + Vector3.down * 0.35f, 4.6f, 1.20f);
        for (int side = -1; side <= 1; side += 2)
            Decor("aisle sign suspension rod", PrimitiveType.Cylinder, position + new Vector3(side * 1.7f, 0.51f, 0), new Vector3(0.025f, 0.72f, 0.025f), wornMetal);
    }

    static void DepartmentSign(string key, string title, string subtitle, Vector3 position, float width = 5.8f)
    {
        PrintedSign(key, position, width, 0.95f);
        for (int side = -1; side <= 1; side += 2)
            Decor("department sign ceiling support", PrimitiveType.Cylinder, position + new Vector3(side * (width * 0.36f), 0.7f, 0), new Vector3(0.03f, 0.75f, 0.03f), wornMetal);
    }

    static void BuildGroundFloor()
    {
        Box("entrance mat", new Vector3(0, 0.02f, -39), new Vector3(7, 0.04f, 5), dark);
        for (int x = -32; x <= 32; x += 8)
        {
            if (Mathf.Abs(x) < 5 || x == -32) continue;
            CheckoutLane(x, (x + 40) / 8);
        }
        string[] aisleNames = { "면 · 간편식", "과자 · 간식", "음료 · 생수", "커피 · 조미료" };
        string[] aisleDetails = { "라면 / 즉석밥 / 통조림", "스낵 / 비스킷 / 시리얼", "생수 / 탄산 / 주스", "커피 / 차 / 양념" };
        string[,] categories = {
            { "봉지라면", "컵라면", "즉석밥", "국 · 찌개", "통조림" },
            { "스낵", "비스킷", "초콜릿", "시리얼", "견과류" },
            { "생수", "탄산음료", "스포츠음료", "과일주스", "차음료" },
            { "원두커피", "믹스커피", "커피음료", "전통차", "양념 · 소스" }
        };
        float[] aisleSegments = { -14, -6, 7, 15, 23 };
        for (int x = -8; x <= 19; x += 9)
            for (int segment = 0; segment < aisleSegments.Length; segment++)
            {
                float z = aisleSegments[segment];
                int category = (x + 8) / 9;
                string product = categories[category, segment];
                Shelf(product, x, z, z < 0 ? red : green, category * 5 + segment);
                if (segment == 0)
                {
                    AisleSign(10 + category, aisleNames[category], aisleDetails[category], new Vector3(x, 4.15f, z - 4));
                }
            }
        for (int x = -38; x <= -22; x += 8)
            for (int z = -15; z <= 15; z += 15)
                ProduceIsland(x, z);
        PepperPromotion(new Vector3(-30,0,-8));
        // Chilled staples sit together; water and all drinks are beside the beverage aisle.
        for (int section = 2; section <= 4; section++)
            FridgeBank(-17 + (section - 2) * 8, 42, section);
        FridgeBank(7, 42, 0);
        FridgeBank(15, 42, 1);
        BuildProduceWall();
        BuildFrozenBanks();
        BuildMeatAndSeafood();
        Shelf("세제 · 섬유유연제", 26, 7, blue, 22);
        Shelf("화장지 · 위생용품", 26, 15, blue, 21);
        Shelf("반려동물 용품", 26, 23, green, 24);
        SelfCheckout(new Vector3(40, 0, -30));
        BuildBackRooms();
        BuildHardwareDepartment();
        for (int i = 0; i < 7; i++) ShoppingCart(new Vector3(-27+i*.65f,0,-39));
        // Customer service now belongs to 2F; this footprint becomes a family room.
        BakeryStock();
        DepartmentSign("food", "식품 · 생활 · 가전", "1F  FOOD / LIVING / APPLIANCES", new Vector3(0, 3.7f, -23), 7.2f);
        DepartmentSign("bakery", "베이커리", "빵 · 디저트", new Vector3(-35, 3.65f, -20));
        DepartmentSign("produce", "채소 · 과일", "청과 / 버섯 / 포장채소", new Vector3(-35, 3.65f, -9));
        DepartmentSign("frozen_food", "냉동식품", "만두 / 아이스크림 / 간편식", new Vector3(1, 3.65f, 27), 7f);
        DepartmentSign("beverages", "음료 · 생수", "생수 / 탄산 / 주스 / 차", new Vector3(11, 3.65f, 37), 7f);
        DepartmentSign("meat", "정육", "국내산 / 수입육 / 양념육", new Vector3(-42, 3.65f, 25));
        DepartmentSign("seafood", "수산", "생선 / 해산물 / 손질", new Vector3(-27, 3.65f, 25));
        DepartmentSign("living", "생활용품", "세제 · 위생 · 반려동물", new Vector3(26, 3.65f, 1));
    }

    static void CheckoutLane(float x, int number)
    {
        MerchBox("checkout lane base",new Vector3(x,.49f,-29.75f),new Vector3(1.26f,.98f,4.9f),shelf,null,true);
        MerchBox("moving black conveyor belt",new Vector3(x,.99f,-30.65f),new Vector3(1.07f,.06f,2.95f),dark,null,true);
        foreach(int side in new[]{-1,1}){
            MerchBox("red checkout edge",new Vector3(x+side*.61f,1.0f,-29.75f),new Vector3(.06f,.10f,4.9f),red);
            for(int panel=0;panel<3;panel++)
                Decor("checkout service panel seam",PrimitiveType.Cube,new Vector3(x+side*.632f,.48f,-31.5f+panel*1.63f),new Vector3(.002f,.73f,.010f),dark);
        }
        MerchBox("bagging tray",new Vector3(x,1.03f,-27.85f),new Vector3(1.12f,.07f,.95f),wornMetal,null,true);
        CheckoutFittings(x);
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

    static void BuildBackRooms()
    {
        // Two walk-in rooms behind the sales floor. Both doors have a clear three metre opening.
        Box("backroom front wall west", new Vector3(23.5f, 2.25f, 32), new Vector3(7, 4.5f, 0.25f), wall);
        Box("backroom front wall middle", new Vector3(34.5f, 2.25f, 32), new Vector3(9, 4.5f, 0.25f), wall);
        Box("backroom front wall east", new Vector3(45.5f, 2.25f, 32), new Vector3(7, 4.5f, 0.25f), wall);
        Box("stockroom door lintel", new Vector3(28.5f, 3.6f, 32), new Vector3(3, 1.8f, 0.25f), wall);
        Box("freezer door lintel", new Vector3(40.5f, 3.6f, 32), new Vector3(3, 1.8f, 0.25f), wall);
        Box("room western side wall", new Vector3(20, 2.2f, 38), new Vector3(0.25f, 4.4f, 12), wall);
        Box("stockroom freezer divider", new Vector3(36.5f, 2.2f, 38), new Vector3(0.25f, 4.4f, 12), wall);
        Box("cold room insulated inner wall", new Vector3(48.6f, 2.2f, 38), new Vector3(0.25f, 4.4f, 12), priceWhite);
        Box("stockroom sliding door parked open", new Vector3(26.5f, 1.45f, 31.78f), new Vector3(1.8f, 2.9f, 0.14f), shelfTop);
        Box("freezer sliding door parked open", new Vector3(38.2f, 1.45f, 31.78f), new Vector3(1.2f, 2.9f, 0.14f), priceWhite);
        Label("물품창고", new Vector3(28.5f, 3.5f, 31.8f), Vector3.zero, 0.55f);
        Label("냉동창고", new Vector3(40.5f, 3.5f, 31.8f), Vector3.zero, 0.55f);
        for (int row = 0; row < 3; row++)
        {
            float z = 35 + row * 3.1f;
            RealModel("steel_frame_shelves_02", new Vector3(23, 0, z), 2.7f, root);
            RealModel("steel_frame_shelves_02", new Vector3(33, 0, z), 2.7f, root);
            for (int tier = 0; tier < 3; tier++)
                for (int side = 0; side < 2; side++)
                    for (int box = 0; box < 3; box++)
                    {
                        Vector3 cratePosition = new Vector3((side == 0 ? 23 : 33) + (box - 1) * 0.9f, 0.21f + tier * 0.78f, z);
                        if (box == 1 && tier == 0)
                            RealModel("cardboard_box_01", cratePosition, 0.55f, root);
                        else
                            Decor("물류 상자", PrimitiveType.Cube, cratePosition + Vector3.up * 0.27f,
                                new Vector3(0.72f, 0.55f, 0.7f), box % 2 == 0 ? cream : yellow);
                    }
            Box("freezer metal rack", new Vector3(39, 1.3f, z + 0.2f), new Vector3(2.1f, 2.6f, 1.2f), shelfTop);
            Box("freezer metal rack", new Vector3(46, 1.3f, z + 0.2f), new Vector3(2.1f, 2.6f, 1.2f), shelfTop);
            for (int side = 0; side < 2; side++)
                for (int tier = 0; tier < 3; tier++)
                    Decor("냉동 식품 상자", PrimitiveType.Cube,
                        new Vector3(side == 0 ? 39 : 46, 0.55f + tier * 0.75f, z + 0.2f),
                        new Vector3(1.5f, 0.55f, 0.72f), tier % 2 == 0 ? blue : priceWhite);
        }
        Box("cold room blue floor strip", new Vector3(42.5f, 0.025f, 37), new Vector3(11, 0.05f, 0.18f), blue);
        GameObject coldLight = new GameObject("freezer cold maintenance light");
        coldLight.transform.SetParent(root);
        coldLight.transform.position = new Vector3(43, 3.7f, 38);
        Light lamp = coldLight.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.range = 8f;
        lamp.intensity = 0.32f;
        lamp.color = new Color(0.48f, 0.71f, 1f);
    }

    static void StockDairyShelf(Transform bank, float x, float y, float z, int tier)
    {
        // Each door bay has one facing group, with reserve stock directly behind it.
        for (int bay = 0; bay < 5; bay++)
        {
            int kind = tier == 0 ? 0 : tier == 1 ? (bay < 3 ? 0 : 1) :
                tier == 2 ? (bay < 2 ? 1 : 2) : (bay == 0 ? 2 : 3);
            bool carton = kind < 2;
            float width = kind == 0 ? .14f : kind == 1 ? .10f : kind == 2 ? .078f : .11f;
            float height = kind == 0 ? .32f : kind == 1 ? .17f : kind == 2 ? .15f : .10f;
            Mesh shape = carton ? HandmadeMilkCartonMesh() : kind == 3 ? HandmadeCupMesh() :
                TurnedPackageMesh("cultured_milk_waist", new[]{0f,.04f,.34f,.62f,.76f,.84f,1f},
                    new[]{.39f,.49f,.47f,.36f,.47f,.30f,.30f});
            int recipe=kind==0?(bay<3?0:1):kind==1?3:kind==2?(bay%2==0?4:2):5;
            Material label=PackMaterial(26,recipe);
            if(recipe==4)
                shape=TurnedPackageMesh("banana_milk_small_belly",new[]{0f,.06f,.28f,.53f,.66f,.84f,.96f,1f},
                    new[]{.30f,.35f,.48f,.50f,.43f,.29f,.29f,.31f});
            string stockKey = "Dairy stock material " + kind;
            if (!modelMaterials.TryGetValue(stockKey, out Material stock))
            {
                Color[] tones = {new Color(.88f,.93f,.84f),new Color(1f,.79f,.77f),
                    new Color(.93f,.79f,.59f),new Color(.14f,.25f,.40f)};
                stock = Mat(stockKey, tones[kind], 0, carton ? .26f : .42f);
                modelMaterials[stockKey] = stock;
            }
            int lanes = Mathf.FloorToInt(.91f / (width + .012f));
            for (int lane = 0; lane < lanes; lane++)
            for (int depth = 0; depth < 2; depth++)
            {
                if (tier == 2 && bay == 1 && lane == 1 && depth == 0) continue;
                Vector3 foot = new Vector3(x + (bay - 2) * 1.09f + (lane - (lanes-1)*.5f)*(width+.012f),
                    y, z - .69f + depth * (width + .018f));
                GameObject pack = HandmadeProp("들녘낙농 chilled pack", shape, foot,
                    new Vector3(width,height,width), stock, bank);
                if(depth==0) CatalogView(26,recipe,foot,Vector3.back,bank,Mathf.Abs(lane-lanes/2));
                bool pickup = tier == 1 && bay == 2 && lane == lanes/2 && depth == 0;
                if (pickup)
                {
                    var hit = pack.AddComponent<BoxCollider>();
                    hit.center = Vector3.up * .5f;
                    pack.AddComponent<Rigidbody>().isKinematic = true;
                    pack.AddComponent<MarketProduct>();
                }
                if(carton){
                    var facing=Decor("Printed dairy facing",PrimitiveType.Quad,foot+new Vector3(0,height*.40f,-width*.505f),
                        new Vector3(width*.98f,height*.73f,1),label,bank);
                    facing.transform.SetParent(pack.transform,true);
                }else{
                    pack.GetComponent<MeshFilter>().sharedMesh=PrintedMold(shape);
                    pack.GetComponent<MeshRenderer>().sharedMaterial=label;
                }
                if (carton)
                {
                    var fin = Decor("Heat-sealed gable fin", PrimitiveType.Cube, foot + Vector3.up*(height+.005f),
                        new Vector3(.005f,.012f,width),stock,bank);
                    fin.transform.SetParent(pack.transform,true);
                }
                else
                {
                    var foil = Decor("crimped foil seal", PrimitiveType.Cylinder, foot + Vector3.up*(height+.002f),
                        new Vector3(width*(kind==2 ? .64f : 1.03f),.002f,width*(kind==2 ? .64f : 1.03f)),wornMetal,bank);
                    foil.transform.SetParent(pack.transform,true);
                }
                if(pickup)PrintHeldUnit(pack.transform,26,recipe);
            }
        }
    }

    static void FridgeBank(float x, float z, int section)
    {
        string[] sections = { "생수 · 탄산", "주스 · 차음료", "우유 · 요구르트", "두부 · 반찬", "치즈 · 냉장식품" };
        string title = sections[Mathf.Clamp(section, 0, sections.Length - 1)];
        Transform bank = new GameObject("냉장 - " + title).transform;
        bank.SetParent(root);
        Box("black cooler cabinet", new Vector3(x, 1.35f, z + .25f), new Vector3(5.8f, 2.7f, .72f), dark, bank);
        Box("cool white back panel", new Vector3(x, 1.35f, z - 0.15f), new Vector3(5.5f, 2.35f, 0.08f), priceWhite, bank);
        Box("cooler insulated left end", new Vector3(x - 2.87f, 1.37f, z - 0.44f),
            new Vector3(0.12f, 2.74f, 1.1f), shelfTop, bank);
        Box("cooler insulated right end", new Vector3(x + 2.87f, 1.37f, z - 0.44f),
            new Vector3(0.12f, 2.74f, 1.1f), shelfTop, bank);
        Box("cooler recessed foot grille", new Vector3(x, 0.12f, z - 0.88f),
            new Vector3(5.66f, 0.18f, 0.045f), wornMetal, bank);
        for (int vent = -16; vent <= 16; vent++)
            Box("cooler ventilation slit", new Vector3(x + vent * 0.16f, 0.12f, z - 0.91f),
                new Vector3(0.075f, 0.032f, 0.012f), dark, bank);
        for (int tier = 0; tier < (section == 2 ? 6 : 4); tier++)
        {
            float y = section == 2 ? .32f + Mathf.Min(tier,2)*.47f + Mathf.Max(0,tier-2)*.34f : .42f + tier*.54f;
            Box("cooler shelf", new Vector3(x, y, z - 0.51f), new Vector3(5.55f, 0.06f, 0.56f), shelfTop, bank);
            Box("cooler shelf white price rail", new Vector3(x, y + 0.015f, z - 0.795f),
                new Vector3(5.5f, 0.085f, 0.022f), priceWhite, bank);
            Box("cooler shelf red price stripe", new Vector3(x, y + 0.046f, z - 0.810f),
                new Vector3(5.5f, 0.016f, 0.009f), red, bank);
            if (section == 2)
            {
                StockDairyShelf(bank, x, y + .03f, z, tier);
                continue;
            }
            int count = section<=1 ? 30 : 24;
            for(int item=0;item<count;item++)
            {
                bool pickup=tier==1 && item==count/2;
                if(!pickup && (item+tier*7+section*11)%61==0)continue;
                int variant=(item/4+tier*2+section)%6;
                int index=section==0?(item<count/2?10:11):section==1?(item<count/2?13:14):21+section;
                float along=x-2.52f+(item+.5f)*5.04f/count;
                for(int depth=0;depth<2;depth++)
                {
                    Vector3 foot=new Vector3(along,y+.031f,z-(depth==0?.68f:.44f));
                    if(section<2)SellUnit(title,foot,bank,0,pickup && depth==0,index,variant,
                        unit=>DrinkFacing(index,variant,foot,PackMaterial(index,variant),unit));
                    else ColdFacing(index,variant,foot,bank,pickup && depth==0,title);
                    if(depth==0 && section>=2) CatalogView(index,variant,foot,Vector3.back,bank,Mathf.Abs(tier-1)*2+Mathf.Abs(Mathf.Repeat((along-x+2.72f)/1.09f,1f)-.5f)*10);
                }
            }
        }
        for (int door = 0; door < 5; door++)
            CabinetDoor(title + " 냉장고 문 " + (door + 1), bank, x - 2.72f + door * 1.09f,
                z - 0.85f, 1.09f, 2.55f, false);
        for (int mullion = 0; mullion <= 5; mullion++)
            Box("cooler vertical LED reveal", new Vector3(x - 2.72f + mullion * 1.09f, 1.35f, z - 0.80f),
                new Vector3(0.025f, 2.32f, 0.022f), priceWhite, bank);
        Box("cooler LED header", new Vector3(x, 2.71f, z - 0.69f), new Vector3(5.8f, 0.11f, 0.08f), priceWhite, bank);
        Box("cooler red price banner", new Vector3(x, 2.91f, z - 0.68f), new Vector3(5.8f, 0.3f, 0.07f), red, bank);
        Label(title, new Vector3(x, 2.93f, z - 0.74f), Vector3.zero, 0.4f);
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
            {
                if (level == 0 && (x == 32 || x == 40)) continue;
                Decor("exposed ceiling beam", PrimitiveType.Cube, new Vector3(x, y, 0), new Vector3(0.09f, 0.12f, 89), shelfTop);
            }
            for (int z = -42; z <= 42; z += 8)
            {
                if (level == 0 && z >= -18 && z <= 14)
                {
                    Decor("ceiling crossbar before escalator", PrimitiveType.Cube, new Vector3(-9, y, z), new Vector3(80, 0.12f, 0.09f), shelfTop);
                    Decor("ceiling crossbar past escalator", PrimitiveType.Cube, new Vector3(47.5f, y, z), new Vector3(5, 0.12f, 0.09f), shelfTop);
                }
                else Decor("exposed ceiling crossbar", PrimitiveType.Cube, new Vector3(0, y, z), new Vector3(99, 0.12f, 0.09f), shelfTop);
            }
        }
    }

    static void BuildProduceWall()
    {
        string[] groups = { "잎채소", "버섯", "손질채소", "과일", "샐러드" };
        for (int section = 0; section < groups.Length; section++)
        {
            // The nursing room occupies the west wall's southern end. Keep a
            // compact leaf bay north of its outward door swing, not through it.
            float z = section==0 ? -18.7f : -22 + section * 11f;
            float span=section==0?3.8f:10.3f,spacing=section==0?.50f:.55f;
            int firstItem=section==0?-3:-8,lastItem=section==0?3:8;
            Transform wallStand = new GameObject("청과 벽면 - " + groups[section]).transform;
            wallStand.SetParent(root);
            Box("woodgrain fascia", new Vector3(-46.5f, 3.25f, z), new Vector3(0.25f, 0.85f, span), cream, wallStand);
            Label(section==4 ? "샐러드  /  간편채소" : section==0 ? "잎채소 · 쌈채소" : groups[section],new Vector3(-46.31f,3.23f,z),new Vector3(0,-90,0),section==0?.30f:.46f);
            Box("dark refrigerated base", new Vector3(-46.2f, 0.38f, z), new Vector3(2.5f, 0.75f, span), dark, wallStand);
            if(section==1 || section==2)
            {
                Decor("chiller under-canopy housing",PrimitiveType.Cube,new Vector3(-45.42f,2.96f,z),
                    new Vector3(.41f,.065f,9.8f),shelfTop,wallStand);
                var tube=RetailSurface("low power chiller diffuser",new Color(.58f,.64f,.64f),0,.34f);
                tube.EnableKeyword("_EMISSION");
                tube.SetColor("_EmissionColor",new Color(.105f,.126f,.128f));
                Decor("emergency circuit diffuser",PrimitiveType.Cube,new Vector3(-45.35f,2.92f,z),
                    new Vector3(.064f,.018f,3.14f),tube,wallStand);
                var lamp=new GameObject("stock case night light").AddComponent<Light>();
                lamp.transform.SetParent(wallStand);
                lamp.transform.position=new Vector3(-44.99f,2.75f,z);
                lamp.type=LightType.Point;lamp.range=3.8f;lamp.intensity=.62f;
                lamp.color=new Color(.81f,.89f,.91f);lamp.shadows=LightShadows.None;
            }
            for (int tier = 0; tier < 3; tier++)
            {
                float y = 0.82f + tier * 0.72f;
                Material lining=RetailSurface("matte graphite chiller liner",new Color(.16f,.18f,.17f),0,.13f);
                var tray = Box("sloping produce shelf", new Vector3(-45.8f, y, z), new Vector3(1.72f, 0.09f, span-.2f), lining, wallStand);
                tray.transform.eulerAngles = new Vector3(0, 0, -9);
                Box("shelf front lip", new Vector3(-44.85f, y - 0.14f, z), new Vector3(0.08f, 0.08f, span-.2f), lining, wallStand);
                bool packedTier=section==1 || section==2 || (section==0 && tier>=1);
                if(packedTier)
                {
                    var rest=Decor("inclined chiller stock support",PrimitiveType.Cube,
                        new Vector3(-45.14f,y+.055f,z),new Vector3(.018f,.246f,span-.95f),lining,wallStand);
                    rest.transform.rotation=Quaternion.Euler(0,0,36);
                }
                if(section==4)
                    Decor("rear salad shelf riser",PrimitiveType.Cube,new Vector3(-45.70f,y+.104f,z),
                        new Vector3(.36f,.13f,9.35f),lining,wallStand);
                for (int item = firstItem; item <= lastItem; item++)
                {
                    int produceKind = section == 0 || section == 4 ? 9 : section == 1 ? 10 :
                        section == 2 ? 4 : new[] { 0, 1, 2, 11 }[(item + 8) % 4];
                    for (int cluster = 0; cluster < 2; cluster++)
                    {
                        int seed = section * 1000 + tier * 100 + (item + 8) * 2 + cluster;
                        Vector3 p = new Vector3(-45.55f + cluster * 0.43f + (ProduceNoise(seed, 1) - .5f) * .12f,
                            y + 0.18f + (ProduceNoise(seed, 2) - .5f) * .13f,
                            z + item * spacing + (ProduceNoise(seed, 3) - .5f) * .15f);
                        if(packedTier)
                        {
                            int kind=section==1 ? Mathf.Min(4,(item+8)*5/17) : section==2 ? 6+Mathf.Min(2,(item+8)*3/17) : 5;
                            bool firstFacing=item==firstItem || (section==1 && (item+7)*5/17!=(item+8)*5/17) ||
                                (section==2 && (item+7)*3/17!=(item+8)*3/17);
                            if(cluster==0 && firstFacing)
                            {
                                float bayZ=z+item*spacing;
                                var ticket=RetailSurface("chiller shelf price "+kind,Color.white,0,.14f);
                                if(!ticket.mainTexture)ticket.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(
                                    "Assets/OutOfService/Art/Prices/fresh_"+kind+".png");
                                Decor("price rail ticket "+freshPackNames[kind],PrimitiveType.Quad,new Vector3(-44.807f,y-.14f,bayZ),
                                    new Vector3(.212f,.078f,1),ticket,wallStand).transform.rotation=Quaternion.Euler(0,-90,0);
                                var divider=Decor("stock divider",PrimitiveType.Cube,new Vector3(-45.36f,y+.048f,bayZ-.268f),
                                    new Vector3(.79f,.107f,.012f),lining,wallStand);
                                divider.transform.rotation=Quaternion.Euler(0,0,-9);
                            }
                            float shelfX=-45.12f;
                            float surface=y+.045f-(shelfX+45.8f)*Mathf.Tan(9*Mathf.Deg2Rad);
                            p=new Vector3(shelfX,surface+.13f,z+item*spacing+(cluster-.5f)*.265f);
                            ChilledProducePack(p,kind,seed,wallStand);
                            continue;
                        }
                        if(section==4)
                        {
                            p.x=cluster==0?-45.70f:-45.17f;
                            p.y=cluster==0?y+.303f:y+.045f-(p.x+45.8f)*Mathf.Tan(9*Mathf.Deg2Rad)+.129f;
                            SaladFacing(p,seed,wallStand);
                            continue;
                        }
                        ProducePiece(produceKind, groups[section], p, wallStand);
                    }
                }
            }
            if (section == 0 || section == 4)
                ColdMist("vegetable chiller low mist", new Vector3(-44.6f, 2.45f, z), new Vector3(0.45f, 0.08f, Mathf.Min(6.5f,span-.5f)), section==0?2f:5f);
        }
    }

    static void ColdMist(string name, Vector3 position, Vector3 area, float rate)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(root);
        obj.transform.position = position;
        ParticleSystem system = obj.AddComponent<ParticleSystem>();
        var main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
        main.startColor = new Color(0.8f, 0.9f, 0.95f, 0.12f);
        main.maxParticles = 60;
        var emission = system.emission;
        emission.rateOverTime = rate;
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = area;
        system.GetComponent<ParticleSystemRenderer>().sharedMaterial = mistWhite;
    }

    static void BuildMeatAndSeafood()
    {
        BuildFreshRoom("정육", -42f, false);
        BuildFreshRoom("수산", -27f, true);
    }

    static void BuildFreshRoom(string section, float x, bool seafood)
    {
        Transform area = new GameObject(section + " display, cutting room and walk-in").transform;
        area.SetParent(root);
        Material accent = seafood ? blue : red;
        Box(section + " refrigerated display case", new Vector3(x, 0.45f, 28.3f), new Vector3(11.5f, 0.9f, 2.2f), shelf, area);
        Box(section + " chilled display tray", new Vector3(x, 0.96f, 27.7f), new Vector3(10.9f, 0.12f, 0.92f), shelfTop, area);
        Decor(section + " sloped display glass", PrimitiveType.Cube,
            new Vector3(x, 1.25f, 27.18f), new Vector3(11.3f, 0.62f, 0.035f), cabinetGlass, area).transform.eulerAngles = new Vector3(-18, 0, 0);
        for (int row = 0; row < 2; row++)
            for (int item = -7; item <= 7; item++)
            {
                Vector3 p = new Vector3(x + item * 0.69f, 1.07f, 27.38f + row * 0.54f);
                FreshPack(p, seafood, (item + 7) / 5, row * 31 + item + 10, area, item < -2);
            }
        Box(section + " case red lower trim", new Vector3(x, 0.24f, 27.17f), new Vector3(11.5f, 0.18f, 0.08f), accent, area);
        for(int bay=0;bay<3;bay++){
            var p=new Vector3(x-3.6f+bay*3.6f,1.65f,28.4f);
            Decor(section+" case LED bracket",PrimitiveType.Cube,p+new Vector3(1.62f,-.31f,0),new Vector3(.035f,.62f,.05f),shelfTop,area);
            Decor(section+" case LED rail",PrimitiveType.Cube,p,new Vector3(3.35f,.045f,.09f),fluorescent,area);
            var lamp=new GameObject(section+" case task light").AddComponent<Light>();
            lamp.transform.SetParent(area);lamp.transform.position=p;
            lamp.transform.LookAt(p+new Vector3(0,-.7f,-.55f));
            lamp.type=LightType.Spot;lamp.spotAngle=135;lamp.innerSpotAngle=65;
            lamp.range=2.2f;lamp.intensity=.55f;lamp.color=seafood?new Color(.79f,.88f,1):new Color(1,.90f,.82f);
            lamp.shadows=LightShadows.None;
        }
        // Customer side remains open; the staff cutting room is separated behind the display.
        Box(section + " prep front wall left", new Vector3(x - 2.4f, 2.2f, 32), new Vector3(9.2f, 4.4f, 0.22f), wall, area);
        Box(section + " prep front wall right", new Vector3(x + 5.4f, 2.2f, 32), new Vector3(3.2f, 4.4f, 0.22f), wall, area);
        Box(section + " prep door lintel", new Vector3(x + 3f, 3.55f, 32), new Vector3(1.6f, 1.7f, 0.25f), wall, area);
        Box(section + " staff door frame left", new Vector3(x + 2.17f, 1.35f, 31.87f), new Vector3(0.09f, 2.7f, 0.13f), shelfTop, area);
        Box(section + " staff door frame right", new Vector3(x + 3.83f, 1.35f, 31.87f), new Vector3(0.09f, 2.7f, 0.13f), shelfTop, area);
        FreshStaffDoor(section + " staff door", new Vector3(x + 2.2f, 0, 31.9f), 1.6f, area);
        Box(section + " prep west wall", new Vector3(x - 7f, 2.2f, 38), new Vector3(0.22f, 4.4f, 12), wall, area);
        Box(section + " prep east wall", new Vector3(x + 7f, 2.2f, 38), new Vector3(0.22f, 4.4f, 12), wall, area);
        Box(section + " prep non-slip floor", new Vector3(x, 0.035f, 38), new Vector3(13.8f, 0.07f, 11.7f), PrepFloor(), area);
        BuildPrepDetails(x, seafood, area);
        Box(section + " walk-in divider left", new Vector3(x - 4.2f, 2.2f, 39.2f), new Vector3(5.6f, 4.4f, 0.22f), priceWhite, area);
        Box(section + " walk-in divider right", new Vector3(x + 4.2f, 2.2f, 39.2f), new Vector3(5.6f, 4.4f, 0.22f), priceWhite, area);
        Box(section + " walk-in insulated lintel", new Vector3(x, 3.75f, 39.2f), new Vector3(2.8f, 1.3f, 0.22f), priceWhite, area);
        Box(section + " walk-in door parked open", new Vector3(x - 2.0f, 1.35f, 39.02f), new Vector3(0.72f, 2.7f, 0.15f), priceWhite, area);
        Box(section + " walk-in cold threshold", new Vector3(x, 0.06f, 39.2f), new Vector3(2.8f, 0.12f, 0.35f), blue, area);
        for (int side = -1; side <= 1; side += 2)
            for (int tier = 0; tier < 2; tier++)
            {
                Box(section + " walk-in metal shelf", new Vector3(x + side * 4.7f, 0.7f + tier * 0.95f, 41.6f),
                    new Vector3(2.6f, 0.08f, 3.2f), shelfTop, area);
            }
        ColdMist(section + " walk-in cold vapour", new Vector3(x, 2.7f, 41.5f), new Vector3(2.8f, 0.3f, 3.0f), 3f);
        GameObject lightObject = new GameObject(section + " prep room fluorescent light");
        lightObject.transform.SetParent(area);
        lightObject.transform.position = new Vector3(x, 3.9f, 35.5f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 8f;
        light.intensity = 0.4f;
        light.color = seafood ? new Color(0.76f, 0.9f, 1f) : new Color(0.98f, 0.88f, 0.8f);
    }

    static void BuildFrozenBanks()
    {
        string[] types = { "냉동만두", "아이스크림", "냉동간편식", "냉동채소" };
        for (int section = 0; section < types.Length; section++)
        {
            float x = -12 + section * 9f;
            Transform bank = new GameObject("냉동 - " + types[section]).transform;
            bank.SetParent(root);
            Box("frozen cabinet dark body", new Vector3(x, 1.4f, 31.25f), new Vector3(7.4f, 2.8f, .80f), dark, bank);
            Box("frozen pale interior", new Vector3(x, 1.35f, 30.52f), new Vector3(7.05f, 2.4f, 0.16f), priceWhite, bank);
            Box("freezer recessed machine grille", new Vector3(x, 0.12f, 29.88f),
                new Vector3(7.1f, 0.19f, 0.04f), wornMetal, bank);
            for (int vent = -21; vent <= 21; vent++)
                Box("freezer machine vent", new Vector3(x + vent * 0.155f, 0.12f, 29.855f),
                    new Vector3(0.073f, 0.035f, 0.012f), dark, bank);
            for (int tier = 0; tier < 4; tier++)
            {
                float y = 0.45f + tier * 0.52f;
                Box("freezer shelf", new Vector3(x, y, 30.15f), new Vector3(7.05f, 0.06f, 0.55f), shelfTop, bank);
                Box("freezer shelf tag rail", new Vector3(x, y + 0.015f, 29.875f),
                    new Vector3(7.0f, 0.08f, 0.018f), priceWhite, bank);
                const int facings=25;
                for(int item=0;item<facings;item++)
                {
                    bool pickup=tier==1 && item==facings/2;
                    if(!pickup && (item*3+tier+section+21)%71==0)continue;
                    int variant=(item/4+tier*2+section)%6;
                    for(int row=0;row<2;row++)
                    {
                        Vector3 foot=new Vector3(x-3.28f+(item+.5f)*6.56f/facings,y+.031f,30.05f+row*.21f);
                        ColdFacing(20+section,variant,foot,bank,pickup && row==0,types[section]);
                        if(row==0) CatalogView(20+section,variant,foot,Vector3.back,bank,Mathf.Abs(tier-1)*2+Mathf.Abs(Mathf.Repeat((foot.x-x+3.2f)/1.067f,1f)-.5f)*10);
                    }
                }
            }
            for (int door = 0; door < 6; door++)
            {
                CabinetDoor(types[section] + " 냉동고 문 " + (door + 1), bank,
                    x - 3.2f + door * 1.067f, 29.91f, 1.067f, 2.65f, true);
            }
            Box("freezer insulated side panel left", new Vector3(x - 3.66f, 1.4f, 30.11f), new Vector3(0.12f, 2.8f, 1.65f), priceWhite, bank);
            Box("freezer insulated side panel right", new Vector3(x + 3.66f, 1.4f, 30.11f), new Vector3(0.12f, 2.8f, 1.65f), priceWhite, bank);
            Box("cold LED", new Vector3(x, 2.78f, 30.01f), new Vector3(7.35f, 0.09f, 0.08f), priceWhite, bank);
            Box("freezer blue category header", new Vector3(x, 3.04f, 30.02f), new Vector3(7.4f, 0.42f, 0.08f), blue, bank);
            bank.RotateAround(new Vector3(x,0,31),Vector3.up,180);
            Label(types[section],new Vector3(x,3.05f,32.04f),new Vector3(0,180,0),.54f);
        }
    }

    static void ProduceIsland(float x, float z)
    {
        string[] names = { "사과", "배", "감귤", "양파", "감자", "대파", "토마토", "오이", "고구마" };
        int index = Mathf.Clamp(Mathf.RoundToInt((x + 38) / 8f) * 3 + Mathf.RoundToInt((z + 15) / 15f), 0, names.Length - 1);
        string itemName = names[index];
        Transform stand = new GameObject("청과 매대 - " + itemName).transform;
        stand.SetParent(root);
        TimberIsland(x,z,stand);
        Box("recessed produce island plinth", new Vector3(x, 0.23f, z),
            new Vector3(4.15f, 0.46f, 4.15f), dark, stand);
        for (int quadrant = 0; quadrant < 4; quadrant++)
        {
            float dx = quadrant % 2 == 0 ? -1.02f : 1.02f;
            float dz = quadrant < 2 ? -1.02f : 1.02f;
            Box("black plastic produce crate floor", new Vector3(x + dx, 0.85f, z + dz),
                new Vector3(1.9f, 0.16f, 1.9f), dark, stand);
            foreach (int edge in new[] { -1, 1 })
            {
                Box("produce crate long rim", new Vector3(x + dx + edge * .91f, .99f, z + dz),
                    new Vector3(.075f, .18f, 1.9f), shelf, stand);
                Box("produce crate short rim", new Vector3(x + dx, .99f, z + dz + edge * .91f),
                    new Vector3(1.9f, .18f, .075f), shelf, stand);
            }
            StockProduceCrate(index, itemName, new Vector3(x+dx,.93f,z+dz), quadrant, stand);
        }
        Box("produce tag support", new Vector3(x, 1.26f, z - 1.88f), new Vector3(0.035f, 0.56f, 0.035f), wornMetal, stand);
        PrintedSign("produce_price_" + index, new Vector3(x, 1.73f, z - 1.88f), 0.62f, 0.41f);
    }

    static void ProducePiece(int index, string name, Vector3 p, Transform parent)
    {
        int seed = Mathf.Abs(Mathf.RoundToInt(p.x * 83f + p.z * 97f + p.y * 71f));
        if(index==6){HarvestTomato(p,seed,parent);return;}
        if(index==5){HarvestScallions(p,seed,parent);return;}
        float scale = .76f + ProduceNoise(seed, 4) * .48f;
        float yaw = ProduceNoise(seed, 5) * 360f;
        string realistic = index == 0 ? "food_apple_01" :
            index == 1 ? "food_pears_asian_01" :
            index == 3 ? "yellow_onion" :
            index == 8 ? "sweet_potato" :
            index == 11 ? "bananas" : null;
        if (realistic != null)
        {
            float itemHeight = index == 11 ? .24f : index == 8 ? .17f : index == 1 ? .148f : .124f;
            GameObject model = RealModel(realistic, p + Vector3.down * (itemHeight * .5f * scale), itemHeight * scale, parent);
            if (model != null)
            {
                model.transform.eulerAngles = new Vector3(ProduceNoise(seed, 6) * 24f - 12f, yaw,
                    ProduceNoise(seed, 7) * 24f - 12f);
                return;
            }
        }
        if (index == 5)
        {
            for (int stalk = 0; stalk < 4; stalk++)
            {
                Vector3 basePoint=p+new Vector3((stalk-1.5f)*.029f,0,-.25f+stalk*.01f);
                Vector3 joint=basePoint+new Vector3(0,.013f,.205f);
                RailSegment("blanched scallion shank",basePoint,joint,.014f,priceWhite,parent);
                for(int blade=0;blade<2;blade++)
                    RailSegment("split onion blade",joint,joint+new Vector3((blade-.5f)*.06f,.028f,.27f+.033f*stalk),.010f,producePalette[5],parent);
            }
            Decor("scallion paper band",PrimitiveType.Cube,p+new Vector3(0,.014f,-.09f),new Vector3(.124f,.037f,.041f),yellow,parent);            return;
        }
        if (index == 9)
        {
            for(int leaf=0;leaf<7;leaf++)
            {
                float angle=(yaw+leaf*137.5f)*Mathf.Deg2Rad;
                Vector3 offset=new Vector3(Mathf.Sin(angle)*.012f,leaf*.002f,Mathf.Cos(angle)*.012f);
                var blade=HandmadeProp("ruffled lettuce blade",LettuceLeaf(leaf%3),p+offset,
                    new Vector3(.14f,.14f,.20f-leaf*.009f)*scale,HarvestLeafSkin(),parent);
                blade.transform.rotation=Quaternion.Euler(-13-leaf*7,angle*Mathf.Rad2Deg,0);
            }
            return;
        }
        if (index == 10)
        {
            for (int mushroom = 0; mushroom < 3; mushroom++)
            {
                Vector3 offset = new Vector3((mushroom - 1) * .13f, mushroom % 2 * .035f, 0);
                Decor("mushroom stalk", PrimitiveType.Cylinder, p + offset - Vector3.up * .09f,
                    new Vector3(.065f, .12f, .065f), priceWhite, parent);
                GameObject cap = HandmadeProp("mushroom domed cap", OrganicProduceMesh(10, (seed + mushroom) % 5),
                    p + offset + Vector3.up * .035f,
                    new Vector3(.20f * scale, .12f * scale, .18f * scale), producePalette[10], parent);
                cap.transform.eulerAngles = new Vector3(0, yaw + mushroom * 29f, 0);
            }
            return;
        }
        Vector3 dimensions = index == 1 ? new Vector3(.38f, .43f, .37f) :
            index == 2 ? new Vector3(.095f, .078f, .092f) :
            index == 3 ? new Vector3(.38f, .37f, .39f) :
            index == 4 ? new Vector3(.145f, .089f, .105f) :
            index == 6 ? new Vector3(.104f, .089f, .11f) :
            index == 7 ? new Vector3(.049f, .257f, .054f) :
            index == 9 ? new Vector3(.44f, .42f, .47f) :
            new Vector3(.36f, .33f, .36f);
        GameObject piece = HandmadeProp(name + " irregular individual produce",
            OrganicProduceMesh(index, seed % 5), p, dimensions * scale,
            index==4 || index==7 ? HarvestSkin(index) : producePalette[Mathf.Clamp(index, 0, producePalette.Length - 1)], parent);
        piece.transform.eulerAngles = index == 7 ?
            new Vector3(68f + ProduceNoise(seed, 6) * 17f, yaw, 0) :
            new Vector3(ProduceNoise(seed, 6) * 25f - 12f, yaw,
                ProduceNoise(seed, 7) * 25f - 12f);
        if (index == 1 || index == 2 || index == 6)
            Decor(name + " small stem", PrimitiveType.Cylinder,
                p + Vector3.up * (dimensions.y * scale * .48f),
                new Vector3(.012f, .015f, .012f), index == 3 ? cream : green, parent);
        if (index == 6)
            Decor("tomato green calyx", PrimitiveType.Sphere,
                p + Vector3.up * (dimensions.y * scale * .48f),
                new Vector3(.034f, .009f, .034f), green, parent);
    }

    static void BuildUpperFloor()
    {
        for (int x = -40; x <= 20; x += 10)
            foreach (int z in new[]{-29,-18,-7,4,15,25})
            {
                if (x >= -10 && x <= 0 && Mathf.Abs(z) < 13) continue;
                if (x >= 10 && z == -29) continue; // Service counter approach and waiting area.
                string clothing = x <= -20 ? (z < 0 ? "여성의류" : "아동의류") : (z < 0 ? "남성의류" : "신발 · 잡화");
                Transform group = new GameObject(clothing + " rack").transform;
                group.SetParent(root);
                if (clothing == "신발 · 잡화") ShoeDisplay(x, z, group); else ClothingRack(x + 2.5f, z, clothing, group);
                if (z == -29 || z == 4) Label(clothing, new Vector3(x, 7.45f, z - 2.5f), Vector3.zero, 0.4f);
            }
        for (int x = -43; x <= -23; x += 5)
        {
            Box("fitting room divider", new Vector3(x, 7.3f, 37), new Vector3(0.2f, 4.5f, 10), wall);
            Box("fitting room curtain", new Vector3(x + 2.5f, 7.2f, 32), new Vector3(4.7f, 3.8f, 0.1f), fabric);
        }
        BuildBreakRoom();
        for (int z = -7; z <= 7; z += 2)
        {
            Box("atrium left rail post", new Vector3(-7, 5.65f, z), new Vector3(0.08f, 1.3f, 0.08f), shelf);
            Box("atrium right rail post", new Vector3(7, 5.65f, z), new Vector3(0.08f, 1.3f, 0.08f), shelf);
        }
        Box("atrium left handrail", new Vector3(-7, 6.3f, 0), new Vector3(0.1f, 0.1f, 20), shelf);
        Box("atrium right handrail", new Vector3(7, 6.3f, 0), new Vector3(0.1f, 0.1f, 20), shelf);
        DepartmentSign("fashion", "패션 · 고객센터", "2F  FASHION & SERVICE", new Vector3(-18, 9.05f, -35), 7f);
        DepartmentSign("clothing", "여성 · 남성의류", "여성 / 남성 / 아동", new Vector3(-30, 9.05f, -17), 7f);
        DepartmentSign("accessories", "신발 · 잡화", "가방 / 신발 / 액세서리", new Vector3(-18, 9.05f, 19), 6.5f);
        DepartmentSign("fitting", "탈의실", "FITTING ROOM", new Vector3(-33, 9.05f, 29));
    }

    static void BuildBreakRoom()
    {
        // Staff-only room on the upper floor, with an open doorway facing the clothing floor.
        Box("break room front wall left", new Vector3(12, 7.25f, 31), new Vector3(6, 4.5f, 0.25f), wall);
        Box("break room front wall right", new Vector3(23, 7.25f, 31), new Vector3(8, 4.5f, 0.25f), wall);
        Box("break room doorway lintel", new Vector3(17, 8.55f, 31), new Vector3(4, 1.9f, 0.25f), wall);
        Box("break room west wall", new Vector3(9, 7.25f, 38), new Vector3(0.25f, 4.5f, 14), wall);
        Box("break room east wall", new Vector3(27, 7.25f, 38), new Vector3(0.25f, 4.5f, 14), wall);
        Label("직원 휴게실", new Vector3(17, 8.55f, 30.83f), Vector3.zero, 0.65f);
        Box("break room worn concrete floor", new Vector3(18, 5.03f, 38), new Vector3(16, 0.04f, 11), wornFloor);
        Box("stained wall lower rail", new Vector3(18, 5.52f, 44.69f), new Vector3(17.6f, 0.95f, 0.04f), cream);
        Box("emergency wall light case", new Vector3(11, 8.5f, 44.55f), new Vector3(0.5f, 0.32f, 0.12f), red);
        WallWriting("영업 종료", new Vector3(18, 7.15f, 44.59f), 1.3f);
        for (int mark = 0; mark < 8; mark++)
        {
            float x = 11.5f + mark * 1.9f;
            Decor("wall damp stain", PrimitiveType.Cube, new Vector3(x, 7.7f + (mark % 3) * 0.3f, 44.62f),
                new Vector3(0.17f + (mark % 2) * 0.2f, 0.75f, 0.015f), dark);
            Decor("scattered employee papers", PrimitiveType.Cube, new Vector3(14.7f + mark * 0.4f, 5.06f, 37.3f + mark * 0.12f),
                new Vector3(0.24f, 0.008f, 0.34f), priceWhite);
        }
        Box("staff table top", new Vector3(16, 5.75f, 38), new Vector3(4, 0.12f, 2), cream);
        for (int x = 14; x <= 18; x += 4)
            for (int z = 36; z <= 40; z += 4)
                Box("staff chair", new Vector3(x, 5.35f, z), new Vector3(0.7f, 0.7f, 0.7f), blue);
        for (int x = 11; x <= 25; x += 2)
        {
            Box("employee locker", new Vector3(x, 6.0f, 43), new Vector3(1.6f, 2.0f, 0.75f), wornMetal);
            Box("employee locker vent", new Vector3(x, 6.45f, 42.58f), new Vector3(0.9f, 0.04f, 0.025f), dark);
        }
        Box("microwave body", new Vector3(23, 6.1f, 35), new Vector3(0.9f, 0.55f, 0.6f), shelfTop);
        Box("microwave dark window", new Vector3(23, 6.12f, 34.69f), new Vector3(0.55f, 0.35f, 0.03f), glass);
        Box("vending machine", new Vector3(24.8f, 6.2f, 35), new Vector3(1.3f, 2.45f, 0.8f), red);
        Box("vending machine glass", new Vector3(24.8f, 6.5f, 34.56f), new Vector3(0.98f, 1.15f, 0.04f), glass);
        Box("break room fluorescent housing", new Vector3(18, 9.1f, 38), new Vector3(3.2f, 0.12f, 0.45f), shelfTop);
        Box("break room fluorescent diffuser", new Vector3(18, 9.02f, 38), new Vector3(3.0f, 0.02f, 0.32f), priceWhite);
        GameObject roomLight = new GameObject("break room emergency light");
        roomLight.transform.SetParent(root);
        roomLight.transform.position = new Vector3(18, 8.9f, 38);
        Light lamp = roomLight.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.range = 14f;
        lamp.intensity = 0.9f;
        lamp.color = new Color(0.86f, 0.92f, 1f);
    }

    static void BuildHardwareDepartment()
    {
        Transform previous=root;
        root=new GameObject("1F hardware and home care").transform;root.SetParent(previous);
        string[] types = { "수공구", "전동공구", "나사 · 철물", "전기 · 조명", "청소도구", "안전용품" };
        for (int section = 0; section < types.Length; section++)
        {
            float x = 32f+(section%3)*7f;
            float z = -20f+(section/3)*10f;
            Transform display = new GameObject("공구 전시 - " + types[section]).transform;
            display.SetParent(root);
            Box("dark tool pegboard", new Vector3(x, 6.35f, z), new Vector3(0.22f, 2.2f, 7.2f), shelf, display);
            Box("hardware red end sign", new Vector3(x, 7.48f, z - 3.7f), new Vector3(2.2f, 0.45f, 0.12f), red, display);
            Label(types[section], new Vector3(x, 7.5f, z - 3.77f), Vector3.zero, 0.43f);
            for (int side = -1; side <= 1; side += 2)
            {
                float px = x + side * 0.55f;
                for (int item = -5; item <= 5; item++)
                {
                    float pz = z + item * 0.55f;
                    if (section == 4)
                    {
                        RailSegment("brush pole",new Vector3(px,5.94f,pz),new Vector3(px,6.85f,pz),.022f,wornMetal,display);
                        Decor("brush block",PrimitiveType.Cube,new Vector3(px,5.94f,pz),new Vector3(.16f,.07f,.32f),blue,display);
                        for(int tuft=-2;tuft<=2;tuft++)
                            Decor("brush tuft",PrimitiveType.Capsule,new Vector3(px,5.88f,pz+tuft*.055f),new Vector3(.065f,.045f,.056f),cream,display);
                    }
                    else if (section == 0)
                    {
                        Decor(types[section] + " handle", PrimitiveType.Cylinder, new Vector3(px, 6.2f, pz),
                            new Vector3(0.12f, 0.55f, 0.12f), item % 2 == 0 ? cream : blue, display);
                        Decor(types[section] + " metal head", PrimitiveType.Cube, new Vector3(px, 6.83f, pz),
                            new Vector3(0.35f, 0.18f, 0.18f), shelfTop, display);
                    }
                    else if (section == 1)
                    {
                        Decor("hand drill body", PrimitiveType.Cube, new Vector3(px, 6.3f, pz), new Vector3(0.38f, 0.3f, 0.34f), red, display);
                        RailSegment("drill chuck",new Vector3(px,6.31f,pz-.16f),new Vector3(px,6.31f,pz-.24f),.065f,wornMetal,display);
                        Decor("slide-in battery",PrimitiveType.Cube,new Vector3(px,5.91f,pz),new Vector3(.22f,.10f,.24f),dark,display);
                        Decor("drill grip", PrimitiveType.Cube, new Vector3(px, 6.05f, pz), new Vector3(0.16f, 0.34f, 0.17f), dark, display);
                    }
                    else
                    {
                        Vector3 card=new Vector3(px,6.65f,pz);
                        bool carton=section==3;
                        int variant=((item+5)/3)%2;
                        int print=(section==2?0:carton?2:4)+variant;
                        float height=section==2?.184f:carton?.148f:.219f;
                        float width=section==2?.131f:carton?.083f:.187f;
                        card.y=6.8f-height*.5f;
                        CatalogView(30,print,card-Vector3.up*height*.5f,Vector3.right*side,display,Mathf.Abs(item));
                        float depth=carton?.083f:.006f;
                        Decor(carton?"folded lamp carton":"die cut hanging card",PrimitiveType.Cube,card,new Vector3(depth,height,width),cream,display);
                        var panel=Decor("hardware retail print",PrimitiveType.Quad,card+Vector3.right*side*(depth*.5f+.0005f),
                            new Vector3(width-.001f,height-.001f,1),PantryInk("HardwareCard",print,.19f),display);
                        panel.transform.rotation=Quaternion.Euler(0,-side*90,0);
                        Vector3 front=card+Vector3.right*side*.022f;
                        if(section==2)
                        {
                            for(int n=0;n<5;n++)
                            {
                                var screw=front+new Vector3(0,-.028f,(n-2)*.023f);
                                float length=variant==0?.025f:.032f;
                                RailSegment("plated screw shaft",screw,screw+Vector3.up*length,.004f,wornMetal,display);
                                Decor("pan head",PrimitiveType.Cylinder,screw+Vector3.up*length,new Vector3(.0085f,.0012f,.0085f),shelfTop,display);
                            }
                            Decor("clear screw blister",PrimitiveType.Cube,front+Vector3.right*side*.005f+Vector3.down*.008f,new Vector3(.014f,.060f,.110f),produceFilm,display);
                        }
                        else if(section==3)
                        {
                            Decor("carton hanging tab",PrimitiveType.Cube,card+Vector3.up*(height*.5f+.009f),new Vector3(.004f,.024f,.050f),cream,display);
                        }
                        else
                        {
                            for(int n=-1;n<=1;n+=2){
                                Decor("protective goggle lens",PrimitiveType.Sphere,front+Vector3.forward*n*.036f,new Vector3(.016f,.050f,.077f),variant==0?WaterPET():glass,display);
                                RailSegment("folded temple",front+new Vector3(side*.002f,.011f,n*.073f),front+new Vector3(side*.004f,-.014f,n*.019f),.006f,dark,display);
                                RailSegment("molded brow",front+new Vector3(side*.008f,.016f,0),front+new Vector3(side*.005f,.02f,n*.073f),.005f,dark,display);
                            }
                            Decor("safety goggle bridge",PrimitiveType.Cube,front,new Vector3(.025f,.018f,.034f),dark,display);
                        }
                    }
                    Decor("tool peg hook",PrimitiveType.Cube,new Vector3(px,6.8f,pz),new Vector3(.16f,.009f,.009f),wornMetal,display);
                }
            }
            Box("tool low stock shelf", new Vector3(x, 5.25f, z), new Vector3(1.7f, 0.5f, 7.3f), cream, display);
        }
        DepartmentSign("hardware", "공구 · 생활용품", "공구 / 전기 / 청소", new Vector3(39, 8.65f, -25), 7f);
        root.position=Vector3.down*5;root=previous;
    }

    static void BuildRamp()
    {
        // Five metre rise, ten metre run: conventional stepped escalators rather than a long travelator.
        foreach (float z in new[] { -11f, 11f })
            Box("2F escalator landing infill", new Vector3(38,4.8f,z),new Vector3(14,.4f,12),floor);
        Box("2F escalator west opening edge",new Vector3(32.25f,4.8f,0),new Vector3(2.5f,.4f,10),floor);
        Box("2F escalator central opening infill",new Vector3(38,4.8f,0),new Vector3(5.1f,.4f,10),floor);
        Box("2F escalator east opening edge",new Vector3(43.75f,4.8f,0),new Vector3(2.5f,.4f,10),floor);
        foreach(float x in new[]{34.5f,41.5f})
        {
            GameObject slope=Box("escalator walkable slope",new Vector3(x,2.45f,0),new Vector3(1.48f,.22f,11.18f),wornMetal);
            slope.transform.eulerAngles=new Vector3(-26.565f,0,0);
            for(int i=0;i<34;i++)
            {
                float z=-5f+(i+.5f)*10f/34f;
                float y=(z+5f)*.5f+.13f;
                Decor("escalator horizontal tread",PrimitiveType.Cube,new Vector3(x,y,z),new Vector3(1.40f,.045f,.29f),wornMetal);
                Decor("escalator vertical riser",PrimitiveType.Cube,new Vector3(x,y-.065f,z-.145f),new Vector3(1.40f,.15f,.025f),shelfTop);
                for(int rib=-6;rib<=6;rib++)
                    Decor("step anti slip groove",PrimitiveType.Cube,new Vector3(x+rib*.10f,y+.025f,z),new Vector3(.012f,.009f,.28f),shelfTop);
            }
            foreach(int side in new[]{-1,1})
            {
                float edge=x+side*.83f;
                var skirt=Decor("escalator brushed skirt",PrimitiveType.Cube,new Vector3(edge,2.55f,0),new Vector3(.10f,.32f,11.18f),shelfTop);
                skirt.transform.eulerAngles=new Vector3(-26.565f,0,0);
                var balustrade=Box("escalator safety balustrade",new Vector3(edge,3.05f,0),new Vector3(.08f,.85f,11.18f),glass);
                balustrade.transform.eulerAngles=new Vector3(-26.565f,0,0);
                var rail=Decor("escalator rubber handrail",PrimitiveType.Cube,new Vector3(edge,3.50f,0),new Vector3(.13f,.10f,11.18f),dark);
                rail.transform.eulerAngles=new Vector3(-26.565f,0,0);
                foreach(float z in new[]{-5f,5f})
                    Decor("rounded handrail return",PrimitiveType.Cylinder,new Vector3(edge,(z+5f)*.5f+.98f,z),new Vector3(.24f,.065f,.24f),dark).transform.eulerAngles=new Vector3(0,0,90);
            }
            Decor("lower comb plate",PrimitiveType.Cube,new Vector3(x,.11f,-5.25f),new Vector3(1.45f,.025f,.55f),shelfTop);
            Decor("upper comb plate",PrimitiveType.Cube,new Vector3(x,5.02f,5.25f),new Vector3(1.45f,.025f,.55f),shelfTop);
            foreach(int side in new[]{-1,1})
                Decor("comb safety yellow edge",PrimitiveType.Cube,new Vector3(x+side*.69f,.13f,-5.25f),new Vector3(.035f,.012f,.55f),yellow);
        }
        Label("2F  패션 · 고객센터",new Vector3(38,3.4f,-8),Vector3.zero,.45f);
    }

    static void BuildLighting()
    {
        void Practical(string name, Vector3 bulb, float range, float intensity, Color tint)
        {
            var light=new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(root);light.transform.position=bulb;
            light.type=LightType.Spot;light.range=range;light.intensity=intensity;light.color=tint;
            light.transform.rotation=Quaternion.Euler(90,0,0);light.spotAngle=120;light.innerSpotAngle=70;
            light.shadows=LightShadows.None;
            Decor(name+" housing",PrimitiveType.Cylinder,bulb+Vector3.up*.05f,new Vector3(.30f,.05f,.30f),shelf);
            Decor(name+" diffuser",PrimitiveType.Cylinder,bulb,new Vector3(.24f,.007f,.24f),fluorescent);
        }
        Practical("customer service closing lamp",new Vector3(-3,3.8f,-30),10f,5.2f,new Color(1f,.76f,.47f));
        Practical("night light at central aisle crossing",new Vector3(.5f,3.9f,-18.5f),11f,4.5f,new Color(.76f,.83f,.87f));
        void Accent(string name,Vector3 p,Vector3 target,Color color,float intensity){
            var lamp=new GameObject(name).AddComponent<Light>();lamp.transform.SetParent(root);lamp.transform.position=p;
            lamp.transform.LookAt(target);lamp.type=LightType.Spot;lamp.spotAngle=92;lamp.innerSpotAngle=48;
            lamp.range=11;lamp.intensity=intensity;lamp.color=color;lamp.shadows=LightShadows.Soft;
            lamp.shadowBias=.03f;lamp.shadowNormalBias=.08f;
            lamp.shadowResolution=LightShadowResolution.Medium;
            var lightData=new SerializedObject(lamp.GetUniversalAdditionalLightData());
            lightData.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=1;
            lightData.ApplyModifiedPropertiesWithoutUndo();
            Decor(name+" suspended fitting",PrimitiveType.Cylinder,p+Vector3.up*.04f,new Vector3(.26f,.065f,.26f),shelf);
            Decor(name+" warm diffuser",PrimitiveType.Cylinder,p,new Vector3(.20f,.008f,.20f),fluorescent);
        }
        Accent("entry night lamp",new Vector3(0,3.9f,-41),new Vector3(0,1,-43),new Color(1,.80f,.60f),5.2f);
        Accent("orchard track lamp",new Vector3(-37,3.7f,-13.8f),new Vector3(-37,1,-15),new Color(1,.83f,.60f),4.6f);
        Accent("pepper track lamp",new Vector3(-30,3.7f,-7.8f),new Vector3(-30,1,-8),new Color(1,.85f,.67f),3.6f);
        Accent("escalator landing lamp",new Vector3(35.4f,4.5f,-4.9f),new Vector3(34.5f,1.5f,-1),new Color(1,.78f,.49f),5.3f);
        Accent("meat counter closing lamp",new Vector3(-42,3.8f,27.5f),new Vector3(-42,.9f,28),new Color(1,.87f,.73f),3.7f);
        Accent("upper floor night lamp",new Vector3(-32,8.9f,-18),new Vector3(-33,5.5f,-17),new Color(1,.79f,.51f),5.2f);
        for(int i=0;i<4;i++)
            Practical("freezer LED spill",new Vector3(-12+i*9,2.32f,32.9f),4.7f,.72f,new Color(.70f,.83f,.95f));
        GameObject sun = new GameObject("dim ambient directional light");
        sun.transform.SetParent(root);
        sun.transform.rotation = Quaternion.Euler(55, -25, 0);
        Light ambient = sun.AddComponent<Light>();
        ambient.type = LightType.Directional;
        ambient.intensity = 0.003f;
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.044f,.052f,.065f);
        RenderSettings.ambientEquatorColor = new Color(.029f,.029f,.028f);
        RenderSettings.ambientGroundColor = new Color(.014f,.013f,.012f);
        RenderSettings.reflectionIntensity = .10f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(.004f,.006f,.008f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = .052f;
        MarketNightGrade();
        for (int level = 0; level < 2; level++)
            for (int x = -40; x <= 40; x += 16)
                for (int z = -36; z <= 36; z += 12)
                {
                    if (level == 1 && x > 30 && z > -17 && z < 17) continue;
                    if (level == 1 && Mathf.Abs(x) < 8 && Mathf.Abs(z) < 12) continue;
                    float y = level == 0 ? 4.45f : 9.65f;
                    Decor("fluorescent metal housing", PrimitiveType.Cube, new Vector3(x, y, z), new Vector3(0.42f, 0.14f, 4.2f), wornMetal);
                    bool working = ((x / 8 + z / 6) % 3 == 0);
                    if (working)
                        Decor("fluorescent glowing tube", PrimitiveType.Cube, new Vector3(x, y - 0.085f, z), new Vector3(0.25f, 0.025f, 3.8f), fluorescent);
                    GameObject lightObj = new GameObject("fluorescent light");
                    lightObj.transform.SetParent(root);
                    lightObj.transform.position = new Vector3(x, y - 0.18f, z);
                    Light lamp = lightObj.AddComponent<Light>();
                    lamp.type = LightType.Spot;
                    lamp.transform.rotation=Quaternion.Euler(90,0,0);lamp.spotAngle=122;lamp.innerSpotAngle=72;
                    lamp.range = 8.5f;
                    lamp.intensity = working ? 3.5f : 0f;
                    lamp.enabled = working;
                    lamp.color = z < -18 || level==1 && x>8 ? new Color(.96f,.80f,.62f) : new Color(.73f,.84f,.94f);
                    if (working && (x + z) % 3 == 0) lightObj.AddComponent<MarketFlicker>();
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
        UnityEngine.Rendering.Universal.UniversalAdditionalCameraData cameraData = cameraObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        cameraData.requiresColorOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off;
        cameraData.requiresDepthOption = UnityEngine.Rendering.Universal.CameraOverrideOption.Off;
        movement.viewCamera = camera;
        cameraObj.AddComponent<AudioListener>();
        // Set the optical origin back from the lens so nearby printed stock
        // remains readable without lighting the far end of an aisle.
        Transform beam=new GameObject("Flashlight beam").transform;
        beam.SetParent(cameraObj.transform,false);
        beam.localPosition=new Vector3(.06f,-.04f,-.68f);
        Light flashlight = beam.gameObject.AddComponent<Light>();
        flashlight.type = LightType.Spot;
        flashlight.range = 17;
        flashlight.spotAngle = 54;
        flashlight.innerSpotAngle = 24;
        flashlight.color = new Color(1f,.956f,.858f);
        flashlight.intensity = 4.6f;
        flashlight.shadows = LightShadows.Soft;
        flashlight.shadowBias = .02f;
        flashlight.shadowNormalBias = .04f;
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
        SculptHeldHand(bare, false);
        movement.bareHandsModel = bare.gameObject;

        Transform torch = new GameObject("Held - flashlight").transform;
        torch.SetParent(camera, false);
        SculptHeldHand(torch, true);
        Held(torch, "flashlight metal body", PrimitiveType.Cylinder, new Vector3(0.38f, -0.16f, 0.89f), new Vector3(0.11f, 0.23f, 0.11f), shelfTop, new Vector3(90, 0, 0));
        Held(torch, "flashlight front rim", PrimitiveType.Cylinder, new Vector3(0.38f, -0.16f, 1.10f), new Vector3(0.16f, 0.04f, 0.16f), dark, new Vector3(90, 0, 0));
        Held(torch, "flashlight lens", PrimitiveType.Cylinder, new Vector3(0.38f, -0.16f, 1.14f), new Vector3(0.12f, 0.012f, 0.12f), yellow, new Vector3(90, 0, 0));
        movement.flashlightModel = torch.gameObject;

        Light viewLight = new GameObject("subtle hands visibility").AddComponent<Light>();
        viewLight.transform.SetParent(camera, false);
        viewLight.transform.localPosition = new Vector3(.15f, -.12f, .09f);
        viewLight.type = LightType.Point;
        viewLight.range = 1.3f;
        viewLight.intensity = .055f;
    }

    static void BuildObjectives()
    {
        GameObject exit = Box("LOCKED FRONT EXIT - E", new Vector3(0, 1.5f, -44.7f), new Vector3(3.8f, 3, 0.2f), glass);
        exit.AddComponent<MarketInteractable>().kind = MarketInteractable.Kind.Exit;
        AddFuse("Fuse A - cold display", new Vector3(-37, 1.2f, 39));
        AddFuse("Fuse B - freezer room", new Vector3(42.5f, 1.2f, 40.5f));
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


