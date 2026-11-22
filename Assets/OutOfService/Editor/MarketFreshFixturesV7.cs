using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class MarketPrototypeBuilderV7
{
    static readonly Dictionary<int, Material> freshMaterials = new Dictionary<int, Material>();
    static readonly Dictionary<Vector2, Material> floorMaterials = new Dictionary<Vector2, Material>();

    static Material SizedFloor(Vector3 size)
    {
        Vector2 key=new Vector2(size.x,size.z);
        if(floorMaterials.TryGetValue(key,out Material cached))return cached;
        Material material=new Material(floor){name="600 mm porcelain tiles "+key};
        foreach(string property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap"})
            material.SetTextureScale(property,key/2.4f);
        floorMaterials[key]=material;return material;
    }

    [MenuItem("Out Of Service/Run Scene Review V7")]
    public static void RunSceneReview()
    {
        if(!Application.isPlaying || OutOfService.MarketPlayer.Instance==null)return;
        OutOfService.MarketPlayer.Instance.gameObject.AddComponent<OutOfService.MarketReviewProbe>();
    }

    static void FreshStaffDoor(string name, Vector3 p, float width, Transform parent)
    {
        Transform hinge=new GameObject(name + " - E to open").transform;hinge.SetParent(parent);hinge.position=p;
        hinge.gameObject.AddComponent<OutOfService.MarketSwingDoor>().openAngle=-95f;
        Box(name + " insulated leaf",p+new Vector3(width*.5f,1.32f,0),new Vector3(width-.06f,2.64f,.10f),priceWhite,hinge);
        Decor("staff door observation window",PrimitiveType.Cylinder,p+new Vector3(width*.5f,1.88f,-.058f),
            new Vector3(.30f,.009f,.30f),glass,hinge).transform.eulerAngles=new Vector3(90,0,0);
        Decor("staff door kick plate",PrimitiveType.Cube,p+new Vector3(width*.5f,.26f,-.06f),new Vector3(width-.13f,.40f,.012f),shelfTop,hinge);
        Box("staff door push plate",p+new Vector3(width-.21f,1.15f,-.068f),new Vector3(.12f,.32f,.025f),wornMetal,hinge);
    }

    static void DepthTestText(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null || renderer.sharedMaterial == null) return;
        var text=obj.GetComponent<TextMesh>();
        // TextMesh updates _MainTex when Unity repacks the dynamic font atlas.
        // A copied URP _BaseMap becomes stale and renders unrelated glyphs.
        Material ink=new Material(Shader.Find("OutOfService/WorldText")){name="painted store lettering"};
        ink.mainTexture=renderer.sharedMaterial.mainTexture;
        ink.SetColor("_Color",Color.white);
        renderer.sharedMaterial = ink;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    static Material FreshMaterial(int kind)
    {
        if (freshMaterials.TryGetValue(kind, out Material material) && material != null) return material;
        if(kind>=4)
        {
            material=Mat(kind==4?"pale chicken flesh":"raw shrimp shell",kind==4?new Color(.80f,.66f,.55f):new Color(.63f,.58f,.54f),.02f,.38f);
            freshMaterials[kind]=material;return material;
        }
        material = Mat("fresh food surface " + kind, Color.white, kind == 3 ? .22f : 0f, kind == 3 ? .55f : .36f);
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/OutOfService/Art/Textures/fresh_food_material_atlas_v1.png"));
        material.SetTextureScale("_BaseMap", new Vector2(.49f, .49f));
        material.SetTextureOffset("_BaseMap", new Vector2(kind % 2 * .5f + .005f, kind < 2 ? .505f : .005f));
        freshMaterials[kind] = material;
        return material;
    }

    static Mesh FreshMesh(bool fish, int variant)
    {
        string id = (fish ? "whole_mackerel" : "fresh_cut_soft_v2_") + variant;
        if (handmadeMeshes.TryGetValue(id, out Mesh cached)) return cached;
        string path = "Assets/OutOfService/Generated/Handmade_" + id + ".asset";
        Mesh saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved != null) { handmadeMeshes[id] = saved; return saved; }
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        const int n = 24;
        if (fish)
        {
            float[] xs = {-.5f,-.43f,-.28f,-.08f,.13f,.31f,.43f,.5f};
            float[] widths = {.025f,.18f,.40f,.50f,.43f,.28f,.07f,.035f};
            for (int ring = 0; ring < xs.Length; ring++)
                for (int side = 0; side <= n; side++)
                {
                    float angle = side * Mathf.PI * 2 / n;
                    v.Add(new Vector3(xs[ring], Mathf.Sin(angle) * widths[ring] * .34f, Mathf.Cos(angle) * widths[ring]));
                    uv.Add(new Vector2((float)side / n, (float)ring / (xs.Length - 1)));
                }
            for (int ring=0;ring<xs.Length-1;ring++) for(int side=0;side<n;side++)
            {
                int a=ring*(n+1)+side,b=a+n+1;
                tri.Add(a);tri.Add(b);tri.Add(a+1);tri.Add(a+1);tri.Add(b);tri.Add(b+1);
            }
        }
        else
        {
            // Rounded cut perimeter, closed underside, and side UV depth: a
            // slice must not extrude the same texture row into vertical stripes.
            float[] radii={0,.86f,1,.99f,.91f,0},ys={-.5f,-.5f,-.34f,.28f,.44f,.48f};
            for(int ring=0;ring<radii.Length;ring++)for(int i=0;i<=n;i++){
                float a=i*Mathf.PI*2/n;
                float r=(.45f+Mathf.Sin(a*3+variant)*.045f+Mathf.Cos(a*2+variant*.7f)*.033f)*radii[ring];
                float y=ys[ring]+Mathf.Sin(a*2+variant)*.018f*radii[ring];
                var p=new Vector3(Mathf.Cos(a)*r+.018f*Mathf.Sin(variant)*radii[ring],y,Mathf.Sin(a)*r);
                v.Add(p);
                float t=variant*.39f,c=Mathf.Cos(t),s=Mathf.Sin(t);
                uv.Add(new Vector2(.5f+(p.x*c-p.z*s)*.82f,.42f+(p.z*c+p.x*s)*.68f+(.5f-y)*.16f));
                if(ring==radii.Length-1 || i==n)continue;
                int k=ring*(n+1)+i;tri.AddRange(new[]{k,k+n+1,k+1,k+1,k+n+1,k+n+2});
            }
        }
        Mesh mesh=new Mesh { name=id };mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();
        AssetDatabase.CreateAsset(mesh,path);handmadeMeshes[id]=mesh;return mesh;
    }

    static void FreshPack(Vector3 p, bool seafood, int type, int seed, Transform parent, bool counterSale)
    {
        Transform pack=new GameObject((seafood ? new[]{"국산 고등어","연어 필렛","흰다리 새우"} :
            new[]{"한우 등심","돼지 삼겹살","닭 정육"})[type] + " display pack").transform;
        pack.SetParent(parent);pack.position=p;
        if (!counterSale)
        {
            Material tray=seafood?dark:priceWhite;
            HandmadeProp("thermoformed meat tray",ProduceShell(false,false),p+Vector3.down*.055f,new Vector3(.61f,.058f,.43f),tray,pack);
        }
        else if(seafood)
        {
            for(int chip=0;chip<16;chip++)
            {
                Vector3 ice=p+new Vector3((chip%4-1.5f)*.14f,-.035f,(chip/4-1.5f)*.10f);
                Decor("crushed ice",PrimitiveType.Sphere,ice,new Vector3(.18f,.07f,.14f),priceWhite,pack);
            }
        }
        else Decor("butcher counter enamel platter",PrimitiveType.Cylinder,p+Vector3.down*.04f,
                new Vector3(.63f,.018f,.43f),dark,pack);
        if(seafood && type==0)
        {
            WholeFish(p,(seed%5-2)*3f,pack);
        }
        else if(seafood && type==2)
        {
            for(int shrimp=0;shrimp<4;shrimp++) for(int segment=0;segment<5;segment++)
            {
                float a=segment*.52f;
                Decor("curved shrimp segment",PrimitiveType.Sphere,p+new Vector3(-.20f+shrimp*.13f+Mathf.Sin(a)*.035f,.018f,Mathf.Cos(a)*.11f),
                    new Vector3(.065f-segment*.006f,.035f,.068f),FreshMaterial(5),pack);
            }
        }
        else
        {
            int pieces=type==1 && !seafood ? 4 : type==2 ? 3 : 2;
            for(int piece=0;piece<pieces;piece++)
            {
                Vector3 offset=type==1 && !seafood ? new Vector3(0,.008f,(-1.5f+piece)*.085f) :
                    new Vector3((piece-(pieces-1)*.5f)*.20f,.005f,0);
                Vector3 size=type==1 && !seafood ? new Vector3(.50f,.045f,.09f) : new Vector3(.26f,.075f,.31f);
                HandmadeProp("individual fresh cut",FreshMesh(false,(seed+piece)%4),p+offset,size,
                    FreshMaterial(seafood?2:type==0?0:type==2?4:1),pack);
            }
        }
        if(counterSale) return;
        if(!seafood || type!=0)
            HandmadeProp("heat stretched cling film",ProduceShell(true,false),p+Vector3.down*.040f,new Vector3(.61f,.116f,.43f),ChilledWrap(),pack);
        // The barcode sits on the wrap at the corner, clear of the visible food.
        int ticket=(seafood ? 3 : 0)+type%3;
        string key="FreshTicket"+ticket;
        if(!modelMaterials.TryGetValue(key,out Material paper))
        {
            paper=Mat(key,Color.white,0,.16f);
            paper.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Packs/"+key+".png");
            modelMaterials[key]=paper;
        }
        Decor("printed chilled weight sticker",PrimitiveType.Quad,p+new Vector3(-.18f,.077f,.12f),
            new Vector3(.18f,.114f,1),paper,pack).transform.rotation=Quaternion.Euler(90,0,0);
        pack.Rotate(0,(ProduceNoise(seed,8)-.5f)*5f,0);
    }

    [MenuItem("Out Of Service/Preview Fresh Counter V7")]
    public static void PreviewFreshCounter()
    {
        if(!Application.isPlaying || OutOfService.MarketPlayer.Instance==null)return;
        var player=OutOfService.MarketPlayer.Instance;var controller=player.GetComponent<CharacterController>();
        controller.enabled=false;player.transform.position=new Vector3(-42,.9f,25.4f);
        player.transform.rotation=Quaternion.identity;player.viewCamera.transform.LookAt(new Vector3(-42,1.45f,27.8f));
        player.enabled=false;controller.enabled=true;player.flashlight.enabled=true;
    }
}
