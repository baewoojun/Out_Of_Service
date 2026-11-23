using UnityEngine;
using UnityEditor;
using OutOfService;

public static partial class MarketPrototypeBuilderV7
{
    // All printed faces are authored with the shopper looking toward +Z.
    static void SellUnit(string title, Vector3 foot, Transform parent, float yaw, bool pickup,int family,int variant,
        System.Action<Transform> model)
    {
        Transform unit = new GameObject(title + (pickup ? " 들 수 있는 상품" : " facing")).transform;
        unit.SetParent(parent);
        unit.position = foot;
        model(unit);
        if (pickup)
        {
            PrintHeldUnit(unit,family,variant);
            var bounds = ModelBounds(unit.gameObject);
            var box = unit.gameObject.AddComponent<BoxCollider>();
            box.center = bounds.center - foot;
            box.size = bounds.size;
            unit.gameObject.AddComponent<Rigidbody>().isKinematic = true;
            unit.gameObject.AddComponent<MarketProduct>();
        }
        SetShoppedFacing(unit,foot,yaw,family,variant);
        if (pickup) return;
        for (int i = unit.childCount - 1; i >= 0; i--) unit.GetChild(i).SetParent(parent, true);
        Object.DestroyImmediate(unit.gameObject);
    }

    static Mesh LabelArc()
    {
        const string path = "Assets/OutOfService/Generated/ConsumerBottleLabel.asset";
        if (handmadeMeshes.TryGetValue(path, out var mesh)) return mesh;
        mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!mesh)
        {
            const int steps=16;
            var vertices=new Vector3[2*(steps+1)];var uv=new Vector2[vertices.Length];var triangles=new int[steps*6];
            for(int i=0;i<=steps;i++)
            {
                float u=(float)i/steps, angle=(u-.5f)*Mathf.PI*.83f;
                Vector3 ring=new Vector3(.5f*Mathf.Sin(angle),0,-.5f*Mathf.Cos(angle));
                int j=2*i;vertices[j]=ring;vertices[j+1]=ring+Vector3.up;
                uv[j]=new Vector2(u,0);uv[j+1]=new Vector2(u,1);
                if(i==steps)continue;
                int t=6*i;
                triangles[t]=j;triangles[t+1]=j+1;triangles[t+2]=j+2;
                triangles[t+3]=j+2;triangles[t+4]=j+1;triangles[t+5]=j+3;
            }
            mesh=new Mesh{name="curved bottle shoulder label",vertices=vertices,uv=uv,triangles=triangles};
            mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[path]=mesh;return mesh;
    }

    static void CartonFace(Vector3 p, Vector3 size, Material ink, Transform group,int family,int variant)
    {
        var stock=RetailSurface("folded ivory paperboard",new Color(.84f,.81f,.73f),0,.19f);
        MerchBox("folding carton with sealed flaps",p+Vector3.up*size.y*.5f,size,stock,group);
        Decor("printed carton front",PrimitiveType.Quad,p+new Vector3(0,size.y*.5f,-size.z*.5f-.0006f),
            new Vector3(size.x,size.y,1),ink,group);
        CartonInformation(p,size,family,variant,group);
        Decor("carton top glued flap seam",PrimitiveType.Cube,p+new Vector3(0,size.y+.0003f,0),new Vector3(size.x*.92f,.0006f,.0015f),shelf,group);
        foreach(int side in new[]{-1,1})
            Decor("carton tucked end crease",PrimitiveType.Cube,p+new Vector3(side*size.x*.38f,size.y+.0004f,0),new Vector3(.0009f,.0004f,size.z*.88f),stock,group);
    }

    static Material PackBackMaterial()
    {
        const string key="Korean pack ingredients panel";
        if(retailSurfaces.TryGetValue(key,out var facts))return facts;
        facts=Mat(key,Color.white,0,.18f);
        facts.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Packs/pack_information.png"));
        retailSurfaces.Add(key,facts);return facts;
    }

    static void CurvedPrint(Vector3 p,float diameter,float height,Material art,Transform parent)
    {
        HandmadeProp("printed cylindrical label",LabelArc(),p,new Vector3(diameter,height,diameter),art,parent);
    }

    static void DrinkFacing(int index,int variant,Vector3 p,Material ink,Transform group)
    {
        bool canned=index==11 && variant==5 || index==12 && variant==5 || index==17 && variant!=0;
        if(canned || index==4 && variant!=1)
        {
            float w=index==4?(variant==0?.086f:.084f):.070f;
            float h=index==4?(variant==0?.041f:variant==3?.114f:.132f):index==17?(variant%2==0?.130f:.104f):.149f;
            var aluminium=RetailSurface("rolled aluminium packaging",new Color(.72f,.74f,.75f),.72f,.37f);
            HandmadeProp("drawn aluminium container",HandmadeCanMesh(),p,new Vector3(w,h,w),aluminium,group);
            Decor("sealed satin can lid",PrimitiveType.Cylinder,p+Vector3.up*(h*.986f),new Vector3(w*.86f,.00025f,w*.86f),
                RetailSurface("satin aluminium lid",new Color(.70f,.72f,.73f),.24f,.25f),group);
            CurvedPrint(p+Vector3.up*h*.055f,w*1.016f,h*.89f,ink,group);
            DetailRing("sealed can pull tab",p+new Vector3(0,h+.0005f,-.004f),new Vector3(w*.21f,w*.32f,.0025f),aluminium,group,new Vector3(90,0,0)).GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Decor("can tab rivet",PrimitiveType.Cylinder,p+new Vector3(0,h+.0006f,.003f),new Vector3(.004f,.0006f,.004f),aluminium,group).GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return;
        }
        if(index==17 && variant==0)
        {
            const float w=.080f,h=.118f;
            HandmadeProp("chilled latte cup",HandmadeCupMesh(),p,new Vector3(w,h,w),cream,group);
            HandmadeProp("tapered latte sleeve",PrintedCollar(false),p+Vector3.up*.011f,new Vector3(w*1.015f,h*.85f,w*1.015f),ink,group);
            Decor("snap lid",PrimitiveType.Cylinder,p+Vector3.up*(h+.003f),new Vector3(w*1.04f,.004f,w*1.04f),dark,group);
            return;
        }
        if(index==13 && variant==4)
        {
            CartonFace(p,new Vector3(.061f,.105f,.044f),ink,group,index,variant);
            Decor("folded drink brick top",PrimitiveType.Cube,p+Vector3.up*.106f,new Vector3(.060f,.002f,.043f),cream,group);
            return;
        }
        if(index==13 && variant==1)
        {
            float h=variant%2==0?.218f:.157f;
            HandmadeProp("gable top juice paperboard",HandmadeMilkCartonMesh(),p,new Vector3(.089f,h,.082f),cream,group);
            Decor("juice heat sealed ridge",PrimitiveType.Cube,p+Vector3.up*(h+.004f),new Vector3(.089f,.009f,.0035f),cream,group);
            Decor("juice carton print",PrimitiveType.Quad,p+new Vector3(0,h*.398f,-.042f),
                new Vector3(.088f,h*.79f,1),ink,group);
            return;
        }
        if(index>=10 && index<=14)
        {
            FinishedBeverage(index,variant,p,ink,group);return;
        }
        bool preserve=index==18;
        float width=preserve?(variant==5?.069f:.097f):index==10 && variant%2==0?.103f:index==19?.075f:index==13 && variant==3?.098f:.076f;
        float height=preserve?(variant==5?.217f:.131f):index==10 && variant%2==0?.31f:index==19?(variant==2?.162f:.237f):index==17?.122f:index==13 && variant==3?.181f:.226f;
        Material tint=index==10?WaterPET():index==11 && (variant==0||variant==2)?
            RetailSurface("cola through PET",new Color(.092f,.042f,.021f),0,.75f):
            index==11 && variant==4?cream:index==11 && variant==3?RetailSurface("orange soda PET",new Color(.95f,.40f,.04f),0,.65f):
            index==14 || preserve || index==19?RetailSurface("amber beverage "+index+" "+variant,
                Color.Lerp(new Color(.23f,.10f,.035f),new Color(.68f,.48f,.18f),(variant%3)*.43f),0,.68f):
            index==13?(variant==0 || variant==3?RetailSurface("citrus juice PET",new Color(.93f,.48f,.052f),0,.65f):
                RetailSurface("pressed fruit juice PET",new Color(.71f,.52f,.22f),0,.64f)):
            index==12?(variant==0?WaterPET():variant==1?blue:variant==2?RetailSurface("grape drink PET",new Color(.31f,.12f,.41f),0,.67f):variant==3?cream:yellow):
            RetailSurface("clear green soda PET",new Color(.13f,.40f,.20f),0,.67f);
        int form=preserve && variant==0?6:index==10?0:index==11?1:index==12?2:index==14?3:4;
        Mesh profile=RetailBottleProfile(form);
        if(index==10 && variant%3!=0)
            profile=TurnedPackageMesh("water_stacked_grip",new[]{0f,.03f,.10f,.12f,.15f,.19f,.22f,.25f,.48f,.65f,.68f,.72f,.82f,.92f,1f},new[]{.35f,.47f,.49f,.44f,.49f,.49f,.44f,.49f,.48f,.48f,.43f,.47f,.40f,.16f,.16f});
        if(index==12 && variant%2==1 || index==13 && variant==3)
            profile=TurnedPackageMesh("wide_shouldered_drink",new[]{0f,.035f,.09f,.33f,.39f,.48f,.52f,.73f,.82f,.87f,1f},new[]{.4f,.49f,.49f,.48f,.40f,.4f,.49f,.49f,.42f,.22f,.22f});
        if(index==14){
            if(variant==1 || variant==4){
                width*=1.10f;height*=.90f;
                profile=TurnedPackageMesh("barley_grip_rings",new[]{0f,.03f,.10f,.13f,.17f,.21f,.25f,.30f,.64f,.72f,.82f,.88f,1f},new[]{.37f,.48f,.49f,.42f,.49f,.42f,.49f,.46f,.46f,.49f,.32f,.18f,.18f});
            }
            if(variant==2 || variant==5){
                width*=.90f;height*=1.06f;
                profile=RetailBottleProfile(4);
            }
        }
        HandmadeProp("blow moulded bottle",profile,p,new Vector3(width,height,width),tint,group);
        float cap=width*(form==6?.89f:index==12?.50f:.36f);
        Decor("closure and tamper band",PrimitiveType.Cylinder,p+Vector3.up*(height+.004f),new Vector3(cap,.006f,cap),
            preserve?wornMetal:index==10?blue:priceWhite,group);
        CurvedPrint(p+Vector3.up*height*.22f,width*1.025f,height*(preserve?.59f:.40f),ink,group);
    }

    static Material WaterPET()
    {
        const string key="clear water bottle PET";
        if(!retailSurfaces.TryGetValue(key,out var pet)){
            pet=TransparentMat(key,new Color(.65f,.82f,.88f,.33f));
            retailSurfaces.Add(key,pet);
        }
        return pet;
    }

    static void AisleFacing(string name,int category,int variant,Vector3 p,int side,Transform group,bool pickup)
    {
        SellUnit(name,p,group,-side*90,pickup,ShelfArtIndex(category),variant,unit=>{
            var art=PackMaterial(ShelfArtIndex(category),variant);
            if(category==21){PaperGoodsFacing(p,variant,unit);return;}
            if(category==22){LaundryFacing(p,variant,unit);return;}
            // Legacy household molds have their printed face on +X.
            if(category==19 && (variant==1||variant==3))
            {
                Transform holder=new GameObject("mold orientation").transform;
                holder.SetParent(unit);holder.position=p;
                SpecialtyRetailItem(name,category,variant,p,1,holder,art);
                holder.rotation=Quaternion.Euler(0,90,0);
                for(int i=holder.childCount-1;i>=0;i--)holder.GetChild(i).SetParent(unit,true);
                Object.DestroyImmediate(holder.gameObject);return;
            }
            if(category==4 && variant!=1 || IsLiquid(category) || category==18 && (variant==0||variant==5))
            {DrinkFacing(category,variant,p,art,unit);return;}
            if(category==24 && variant==4)
            {
                const float w=.078f,h=.041f;
                var metal=RetailSurface("satin aluminium lid",new Color(.70f,.72f,.73f),.24f,.25f);
                HandmadeProp("wet cat food sealed can",HandmadeCanMesh(),p,new Vector3(w,h,w),metal,unit);
                Decor("flat recessed cat food lid",PrimitiveType.Cylinder,p+Vector3.up*(h*.984f),new Vector3(w*.83f,.00025f,w*.83f),metal,unit);
                CurvedPrint(p+Vector3.up*.003f,w*1.015f,h-.006f,PantryInk("PetCanSide",0,.27f),unit);
                DetailRing("cat food recessed pull ring",p+new Vector3(0,h+.0005f,-.006f),new Vector3(.017f,.023f,.0025f),metal,unit,new Vector3(90,0,0)).GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                Decor("cat food lid tab rivet",PrimitiveType.Cylinder,p+new Vector3(0,h+.0006f,.002f),new Vector3(.004f,.0006f,.004f),metal,unit).GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                return;
            }
            if(category==3 || category==5 || category==9 || category==15 || category==24 || category==8 && variant>=2 ||
                category==6 && (variant==1 || variant==5) || category==18 && variant==3)
            {
                bool smallPet=category==24 && (variant==2 || variant==5);
                float w=category==24?(smallPet?.145f:.263f):category==9?(variant==0?.098f:.149f):category==15?.14f:.189f;
                float h=category==24?(smallPet?.191f:.37f):category==9?(variant==0?.133f:.20f):category==15?.24f:.258f;
                if(category==5){w*=1f+.038f*(variant%3-1);h*=1f-.055f*(variant%3-1);}
                if(category==6){w=variant==1?.153f:.167f;h=variant==1?.195f:.132f;}
                if(category==3 || category==9 || category==15){
                    w*=1f+.08f*(variant%3-1);
                    h*=1f+.10f*((variant+1)%3-1);
                }
                int family=ShelfArtIndex(category);
                float depth=category==24&&!smallPet?.126f:category==5?.105f:category==3?.061f:.077f;
                FilmSurface(art,family,variant);
                HandmadeProp("content shaped retail film",RetailFilmMold(family,variant,!pickup),p,new Vector3(w,h,depth),art,unit);
                if(category==15)
                    Decor("coffee valve",PrimitiveType.Cylinder,p+new Vector3(w*.29f,h*.68f,-.036f),new Vector3(.014f,.0015f,.014f),wornMetal,unit).transform.rotation=Quaternion.Euler(90,0,0);
                return;
            }
            if(category==4)
            {
                HandmadeProp("rounded ham tin",ProduceShell(false,false),p,new Vector3(.116f,.084f,.077f),wornMetal,unit);
                Decor("closed tin lid",PrimitiveType.Cube,p+Vector3.up*.083f,new Vector3(.112f,.004f,.074f),wornMetal,unit);
                Decor("ham tin label",PrimitiveType.Quad,p+new Vector3(0,.041f,-.039f),new Vector3(.111f,.072f,1),art,unit);
                return;
            }
            if(category==7 && variant==2)
            {
                HandmadeProp("chocolate cube screw jar",RetailBottleProfile(6),p,new Vector3(.083f,.09f,.083f),dark,unit);
                CurvedPrint(p+Vector3.up*.012f,.083f,.066f,art,unit);
                Decor("screw top",PrimitiveType.Cylinder,p+Vector3.up*.094f,new Vector3(.079f,.006f,.079f),shelfTop,unit);
                return;
            }
            if(category==7)
            {
                bool bar=variant==1 || variant==3 || variant==5;
                HandmadeProp(bar?"end crimped chocolate bar film":"folded chocolate tablet foil",bar?ChocolateBarFilm(variant):NoodlePacket(false,!pickup),p,bar?new Vector3(.134f,.046f,variant==5?.027f:.025f):new Vector3(.088f,.171f,.015f),art,unit);return;
            }
            float width=category==8?.19f:category==16?.174f:category==7?.103f:.153f;
            float high=category==8?.286f:category==16?.211f:category==7?.115f:.176f;
            if(category==6 || category==16 || category==18){
                width*=variant%3==0?1.16f:variant%3==1?.85f:1f;
                high*=variant%3==0?.70f:variant%3==1?1.18f:.93f;
            }
            CartonFace(p,new Vector3(width,high,category==8?.070f:.066f),art,unit,ShelfArtIndex(category),variant);
        });
    }

    static void ColdFacing(int index,int variant,Vector3 p,Transform group,bool pickup,string title)
    {
        SellUnit(title,p,group,0,pickup,index,variant,unit=>{
            if(HasFrozenFilm(index,variant)){FrozenFacing(index,variant,p,unit);return;}
            Material ink=PackMaterial(index,variant);
            if(index==25 && variant==3){StringCheesePack(p,unit);return;}
            bool tub=index==21 && (variant==2 || variant==3) || index==25 && variant==5;
            if(tub || index==21 && variant==1)
            {
                bool cone=index==21 && variant==1;
                float w=cone?.076f:variant==3?.145f:.101f;
                float h=cone?.161f:variant==3?.102f:.067f;
                var mold=TurnedPackageMesh(cone?"icecream_cone":"icecream_tub",
                    new[]{0f,.04f,.88f,.94f,1f},new[]{cone?.035f:.36f,cone?.048f:.38f,.48f,.5f,.48f});
                HandmadeProp(cone?"printed cone jacket":"printed ice cream tub",PrintedMold(mold),p,new Vector3(w,h,w),ink,unit);
                HandmadeProp("printed dairy seal",PeelLid(),p+Vector3.up*(h+.001f),new Vector3(w,1,w),ink,unit);
                return;
            }
            if(index==25 && variant==4)
            {
                var cheese=RetailSurface("gouda wedge cut",new Color(.92f,.70f,.38f),0,.22f);
                HandmadeProp("gouda wedge",CheeseWedge(),p,new Vector3(.095f,.128f,.060f),cheese,unit);
                Decor("wedge paper sash",PrimitiveType.Quad,p+new Vector3(0,.042f,-.0307f),new Vector3(.072f,.066f,1),ink,unit);
                return;
            }
            if(index==25 && variant==0)
            {
                const float w=.112f,d=.108f,h=.034f;
                var edge=RetailSurface("warm edge of sliced cheese",new Color(.95f,.67f,.28f),0,.26f);
                for(int slice=0;slice<5;slice++)
                    Decor("stacked wrapped cheese slices",PrimitiveType.Cube,p+Vector3.up*(.003f+slice*.0062f),
                        new Vector3(w-.004f*(slice%2),.0054f,d),edge,unit);
                HandmadeProp("clear stack overwrap",ProduceShell(true,false),p,new Vector3(w+.003f,h,d+.002f),produceFilm,unit);
                Decor("cheese top label",PrimitiveType.Quad,p+Vector3.up*(h+.0006f),new Vector3(w*.92f,d*.91f,1),ink,unit)
                    .transform.rotation=Quaternion.Euler(90,0,0);
                return;
            }
            if(index==24 && (variant==0 || variant==5))
            {
                float w=.157f,h=.058f,d=.105f;
                int count=variant==0?2:1;
                if(count==2){w=.094f;d=.108f;}
                for(int k=0;k<count;k++)
                {
                    var foot=p+Vector3.right*(k-(count-1)*.5f)*(w+.002f);
                    HandmadeProp("sealed deli punnet",ProduceShell(false,false),foot,new Vector3(w,h,d),priceWhite,unit);
                    Decor("heat sealed peel top",PrimitiveType.Quad,foot+Vector3.up*(h+.001f),new Vector3(w*.96f,d*.96f,1),ink,unit)
                        .transform.rotation=Quaternion.Euler(90,0,0);
                }
                if(count==2)Decor("perforated join between tofu portions",PrimitiveType.Cube,p+Vector3.up*(h-.001f),
                    new Vector3(.005f,.002f,d*.93f),priceWhite,unit);
                return;
            }
            if(index==22 && variant==1 || index==25 && variant==2)
            {
                float w=index==25?.14f:.245f;
                CartonFace(p,new Vector3(w,index==25?.075f:.23f,index==25?.063f:.043f),ink,unit,index,variant);
                return;
            }
            bool bar=index==21;float width=bar?.173f:index==24?.158f:index==25?.129f:.214f;
            float height=bar?.062f:index==25?.194f:.252f;
            if(!bar){width*=1f+.065f*((variant+index)%3-1);height*=1f+.10f*((variant+1)%3-1);}
            HandmadeProp("sealed freezer film",NoodlePacket(false,!pickup),p,new Vector3(width,height,bar?.032f:.085f),ink,unit);
        });
    }

    static Mesh CheeseWedge()
    {
        const string path="Assets/OutOfService/Generated/GoudaWedge.asset";
        if(handmadeMeshes.TryGetValue(path,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh)
        {
            mesh=new Mesh{name="cut gouda wedge",vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(-.25f,1,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(-.25f,1,.5f)},triangles=new[]{0,2,1,3,4,5,0,1,4,0,4,3,1,2,5,1,5,4,2,0,3,2,3,5}};
            mesh.RecalculateBounds();mesh.RecalculateNormals();
            AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[path]=mesh;return mesh;
    }

    static Mesh PrintedMold(Mesh mold)
    {
        string path="Assets/OutOfService/Generated/Fitted_"+mold.name.Replace(' ','_')+".asset";
        if(handmadeMeshes.TryGetValue(path,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){
            mesh=Object.Instantiate(mold);
            var uv=mesh.uv;
            for(int i=0;i<uv.Length;i++)uv[i].x=2*uv[i].x-1f;
            mesh.uv=uv;AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[path]=mesh;return mesh;
    }
}
