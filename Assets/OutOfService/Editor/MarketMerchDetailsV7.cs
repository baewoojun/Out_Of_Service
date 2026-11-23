using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static partial class MarketPrototypeBuilderV7
{
    // Shared atlases and meshes: small details join the existing spatial batches.
    static Material MerchInk(bool prices=false)
    {
        string key=prices?"price_atlas":"detail_atlas";
        if(retailSurfaces.TryGetValue(key,out var material))return material;
        string path="Assets/OutOfService/Art/MerchDetails/"+key+".png";
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;
        importer.mipmapEnabled=true;importer.anisoLevel=4;importer.textureCompression=TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
        material=Mat(key,Color.white,0,.17f);
        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        retailSurfaces[key]=material;return material;
    }
    static Mesh MerchQuad(int cell,bool prices=false)
    {
        string key="MerchQuad_"+(prices?"price_":"detail_")+cell;
        string path="Assets/OutOfService/Generated/"+key+".asset";
        if(handmadeMeshes.TryGetValue(key,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){
            int columns=prices?10:4,rows=prices?19:4;
            float u=(cell%columns)/(float)columns,v=1-(cell/columns+1)/(float)rows;
            float du=1f/columns,dv=1f/rows,pad=.0008f;
            mesh=new Mesh{name=key,vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0)},
                uv=new[]{new Vector2(u+pad,v+pad),new Vector2(u+du-pad,v+pad),new Vector2(u+pad,v+dv-pad),new Vector2(u+du-pad,v+dv-pad)},triangles=new[]{0,2,1,1,2,3}};
            mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[key]=mesh;return mesh;
    }
    static GameObject MerchArt(string title,int cell,Vector3 p,Vector2 size,Vector3 rotation,Transform parent=null,bool prices=false)
    {
        var go=HandmadeProp(title,MerchQuad(cell,prices),p,new Vector3(size.x,size.y,1),MerchInk(prices),parent?parent:root);
        go.transform.rotation=Quaternion.Euler(rotation);return go;
    }
    static GameObject MerchBox(string title,Vector3 p,Vector3 size,Material material,Transform parent=null,bool solid=false)
    {
        var go=HandmadeProp(title,SoftShopEdge(size),p,Vector3.one,material,parent?parent:root);
        if(solid)go.AddComponent<BoxCollider>().size=size;
        return go;
    }
    static Mesh MerchRing()
    {
        const string key="MerchOpenRingV2";const string path="Assets/OutOfService/Generated/"+key+".asset";
        if(handmadeMeshes.TryGetValue(key,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){
            var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
            const int segments=40,sides=6;
            for(int a=0;a<=segments;a++)for(int b=0;b<=sides;b++){
                float theta=a*Mathf.PI*2/segments,phi=b*Mathf.PI*2/sides;
                var radial=new Vector3(Mathf.Cos(theta),Mathf.Sin(theta),0);
                var normal=radial*Mathf.Cos(phi)+Vector3.forward*Mathf.Sin(phi);
                v.Add(radial*.44f+normal*.06f);n.Add(normal);uv.Add(new Vector2((float)a/segments,(float)b/sides));
                if(a==segments || b==sides)continue;
                int c=a*(sides+1)+b;tr.AddRange(new[]{c,c+sides+1,c+1,c+1,c+sides+1,c+sides+2});
            }
            mesh=new Mesh{name=key};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetTriangles(tr,0);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[key]=mesh;return mesh;
    }
    static GameObject DetailRing(string title,Vector3 p,Vector3 size,Material mat,Transform parent=null,Vector3 rotation=default)
    {
        var ring=HandmadeProp(title,MerchRing(),p,size,mat,parent?parent:root);
        ring.transform.rotation=Quaternion.Euler(rotation);return ring;
    }
    static void ShelfConstruction(float x,float z,Transform group,bool pallet)
    {
        // Six standard bays, paired slotted posts and adjustable brackets.
        for(int bay=0;bay<=6;bay++){
            float along=z-3.5f+bay*(7f/6);
            foreach(int side in new[]{-1,1}){
                Decor("powder coated shelf upright",PrimitiveType.Cube,new Vector3(x+side*.045f,1.48f,along),new Vector3(.045f,2.55f,.045f),shelfTop,group);
                for(int slot=0;slot<20;slot++)
                    Decor("recessed upright adjustment slot",PrimitiveType.Quad,new Vector3(x+side*.069f,.40f+slot*.118f,along),new Vector3(.010f,.036f,1),dark,group).transform.rotation=Quaternion.Euler(0,-side*90,0);
            }
            if(bay<6){
                foreach(int side in new[]{-1,1}){
                    Decor("adjustable shelf foot",PrimitiveType.Cylinder,new Vector3(x+side*.63f,.045f,along+.1f),new Vector3(.073f,.035f,.073f),dark,group);
                    Decor("fixture base fold seam",PrimitiveType.Cube,new Vector3(x+side*(pallet?.912f:.777f),.21f,along),new Vector3(.004f,.23f,.008f),dark,group);
                }
            }
        }
    }
    static void ShelfDeckDetails(float x,float y,float z,Transform group,bool pallet)
    {
        foreach(int side in new[]{-1,1}){
            float edge=x+side*(pallet?.895f:.787f);
            // Continuous dark holder lip below removable paper labels.
            Decor("price strip retaining lip",PrimitiveType.Cube,new Vector3(edge,y+.004f,z),new Vector3(.018f,.010f,7.03f),shelf,group);
            for(int bay=0;bay<6;bay++){
                float along=z-3.5f+bay*(7f/6);
                Decor("shelf modular deck joint",PrimitiveType.Cube,new Vector3(x+side*.41f,y+.0255f,along),new Vector3(.69f,.001f,.003f),shelf,group);
                Decor("cantilever shelf bracket",PrimitiveType.Cube,new Vector3(x+side*.32f,y-.05f,along),new Vector3(.63f,.056f,.018f),shelfTop,group);
            }
        }
    }
    static void ProductTicket(int category,int variant,Vector3 p,int side,Transform parent)
    {
        MerchArt("SKU matched paper price card",ShelfArtIndex(category)*6+variant,p,new Vector2(.185f,.058f),new Vector3(0,-side*90,0),parent,true);
    }
    static void FeaturedEndcap(float x,float z,int category,Transform parent)
    {
        // Front edge stays within z=-18.25; the main aisle starts at -19.2.
        var display=new GameObject("stocked front endcap "+category).transform;display.SetParent(parent);
        float front=z-3.87f;
        Box("end display recessed base",new Vector3(x,.18f,front),new Vector3(1.54f,.36f,.68f),shelf,display);
        int levels=category==10?4:5;float step=category==10?.48f:.39f;
        for(int level=0;level<levels;level++){
            float y=.40f+level*step;
            MerchBox("end display folded shelf",new Vector3(x,y,front),new Vector3(1.58f,.035f,.68f),shelfTop,display,true);
            MerchBox("end display price holder",new Vector3(x,y+.035f,front-.352f),new Vector3(1.57f,.064f,.025f),priceWhite,display);
            int lanes=category==10?12:6;int variant=category==0?(level%2==0?0:2):category==5?level%6:category==10?level%3*2:level%6;
            for(int lane=0;lane<lanes;lane++)for(int depth=0;depth<3;depth++){
                if(level==2 && lane==lanes-1 && depth<2)continue;
                var p=new Vector3(x+(lane-(lanes-1)*.5f)*(1.40f/lanes),y+.019f,front-.21f+depth*.19f);
                var facing=new GameObject("front facing reserve stock").transform;facing.SetParent(display);facing.position=p;
                RetailItem("행사 상품",category,variant,p,1,facing);
                facing.RotateAround(p,Vector3.up,90);
            }
            MerchArt("endcap offer price",ShelfArtIndex(category)*6+variant,new Vector3(x,y+.038f,front-.367f),new Vector2(.25f,.078f),Vector3.zero,display,true);
        }
        MerchBox("end display header",new Vector3(x,2.67f,z-3.59f),new Vector3(1.58f,.48f,.08f),red,display);
        Label("이번 주 알뜰 장보기",new Vector3(x,2.67f,z-3.636f),Vector3.zero,.146f);
    }
    static void RetailTerminal(Vector3 p,float yaw,Transform parent)
    {
        var unit=new GameObject("POS terminal").transform;unit.SetParent(parent);unit.position=p;
        MerchBox("terminal weighted foot",p,new Vector3(.31f,.025f,.23f),dark,unit);
        MerchBox("terminal tilting stem",p+Vector3.up*.16f,new Vector3(.065f,.30f,.055f),shelfTop,unit);
        MerchBox("terminal rounded housing",p+new Vector3(0,.34f,0),new Vector3(.42f,.30f,.045f),dark,unit);
        MerchArt("register closed status",6,p+new Vector3(0,.34f,-.024f),new Vector2(.384f,.257f),Vector3.zero,unit);
        for(int slit=0;slit<7;slit++)Decor("monitor ventilation slot",PrimitiveType.Cube,p+new Vector3(-.12f+slit*.04f,.34f,.023f),new Vector3(.014f,.10f,.001f),shelf,unit);
        unit.rotation=Quaternion.Euler(0,yaw,0);
    }
    static void CheckoutFittings(float x)
    {
        var p=new Vector3(x,1.035f,-28.65f);
        MerchBox("scanner scale steel surround",p,new Vector3(.63f,.018f,.55f),wornMetal,null,true);
        MerchBox("scanner black optical recess",p+Vector3.up*.013f,new Vector3(.30f,.008f,.38f),dark);
        MerchBox("scanner smoked scan window",p+Vector3.up*.019f,new Vector3(.25f,.004f,.31f),glass);
        MerchBox("vertical barcode scanner",p+new Vector3(-.38f,.16f,0),new Vector3(.11f,.32f,.38f),shelf);
        MerchBox("vertical scanner lens",p+new Vector3(-.32f,.17f,0),new Vector3(.008f,.20f,.27f),glass);
        RetailTerminal(p+new Vector3(-.42f,.025f,.50f),90,root);
        RailSegment("customer display pole",p+new Vector3(.43f,.02f,.72f),p+new Vector3(.43f,.56f,.72f),.018f,shelfTop,root);
        var customerDisplay=MerchBox("customer facing status display",p+new Vector3(.43f,.60f,.72f),new Vector3(.29f,.14f,.036f),dark);
        customerDisplay.transform.rotation=Quaternion.Euler(0,-90,0);
        MerchArt("customer facing closed status",6,p+new Vector3(.449f,.60f,.72f),new Vector2(.26f,.12f),new Vector3(0,-90,0));
        var pad=new GameObject("customer card terminal").transform;pad.SetParent(root);pad.position=p+new Vector3(.50f,.12f,.42f);
        MerchBox("card terminal moulded case",pad.position,new Vector3(.15f,.22f,.07f),dark,pad);
        MerchArt("terminal display and card marking",12,pad.position+Vector3.back*.036f,new Vector2(.131f,.19f),Vector3.zero,pad);
        for(int row=0;row<4;row++)for(int col=0;col<3;col++)MerchBox("raised keypad button",pad.position+new Vector3((col-1)*.035f,-.025f-row*.019f,-.040f),new Vector3(.026f,.014f,.010f),row==3?(col==2?green:col==0?red:cream):shelfTop,pad);
        pad.rotation=Quaternion.Euler(-23,-90,0);
        var receipt=p+new Vector3(-.43f,.05f,-.48f);
        MerchBox("thermal receipt printer",receipt,new Vector3(.19f,.13f,.24f),dark);
        MerchBox("receipt paper exit slot",receipt+new Vector3(0,.066f,-.06f),new Vector3(.09f,.003f,.012f),shelf);
        MerchArt("printed receipt",7,receipt+new Vector3(0,.072f,-.12f),new Vector2(.079f,.17f),new Vector3(90,0,0));
        for(int side=-1;side<=1;side+=2){
            var a=new Vector3(x+side*.57f,1.065f,-32.0f);
            RailSegment("conveyor rounded bumper",a,a+Vector3.forward*4.65f,.025f,shelfTop,root);
        }
        for(int line=0;line<3;line++)Decor("conveyor rubber joint",PrimitiveType.Cube,new Vector3(x,1.021f,-31.85f+line*.90f),new Vector3(1.04f,.001f,.0025f),shelf);
        // One divider and a partially stocked bag holder, clear of the scanner.
        MerchBox("next customer belt divider",new Vector3(x,1.05f,-31.25f),new Vector3(.79f,.058f,.045f),cream);
        var rack=new Vector3(x+.37f,1.075f,-27.65f);
        foreach(float offset in new[]{-.14f,.14f}){
            RailSegment("bag dispenser upright",rack+Vector3.right*offset,rack+new Vector3(offset,.39f,0),.013f,wornMetal,root);
            RailSegment("bag dispenser arm",rack+new Vector3(offset,.39f,0),rack+new Vector3(offset,.39f,-.17f),.013f,wornMetal,root);
        }
        MerchBox("folded paper carrier bags",rack+new Vector3(0,.18f,-.03f),new Vector3(.26f,.29f,.09f),cream);
        foreach(float offset in new[]{-.085f,.085f})DetailRing("paper carrier handle",rack+new Vector3(offset,.365f,-.074f),new Vector3(.095f,.12f,.04f),cream);
    }
    static void CeramicCup(Vector3 p,Transform parent=null,bool paper=false)
    {
        var mat=CafeCeramic(paper);
        HandmadeProp("cup with inner wall and rolled lip",CafeHollowCup(),p,Vector3.one,mat,parent?parent:root);
        if(!paper)DetailRing("ceramic cup handle",p+new Vector3(.054f,.055f,0),new Vector3(.064f,.067f,.055f),mat,parent);
    }
    static void PlacedCafeCup(Vector3 p,float yaw,bool inverted)
    {
        var holder=new GameObject(inverted?"inverted warming cup":"service cup").transform;
        holder.SetParent(root,false);holder.position=p;
        CeramicCup(p,holder);
        holder.rotation=Quaternion.Euler(0,yaw,0)*Quaternion.Euler(inverted?180:0,0,0);
        if(inverted)holder.position+=Vector3.up*.096f;
    }
    static void CoffeeMachine(Vector3 p)
    {
        var body=RetailSurface("espresso enamel graphite",new Color(.12f,.135f,.14f),.35f,.45f);
        MerchBox("espresso machine boiler enclosure",p+new Vector3(0,.37f,.12f),new Vector3(.86f,.43f,.33f),body,null,true);
        foreach(int side in new[]{-1,1})MerchBox("espresso curved side panel",p+new Vector3(side*.43f,.27f,0),new Vector3(.048f,.54f,.57f),wornMetal);
        MerchBox("espresso drip tray surround",p+new Vector3(0,.046f,-.09f),new Vector3(.83f,.075f,.40f),wornMetal);
        MerchBox("espresso tray inset",p+new Vector3(0,.087f,-.09f),new Vector3(.75f,.008f,.32f),dark);
        for(int n=0;n<21;n++)Decor("drip tray steel grate",PrimitiveType.Cube,p+new Vector3(-.35f+n*.035f,.093f,-.09f),new Vector3(.010f,.004f,.31f),wornMetal);
        foreach(int side in new[]{-1,1}){
            Decor("espresso group head",PrimitiveType.Cylinder,p+new Vector3(side*.21f,.31f,-.115f),new Vector3(.14f,.035f,.14f),wornMetal);
            RailSegment("portafilter black handle",p+new Vector3(side*.21f,.28f,-.14f),p+new Vector3(side*.21f,.28f,-.32f),.028f,dark,root);
            foreach(float dx in new[]{-.027f,.027f})RailSegment("twin espresso spout",p+new Vector3(side*.21f+dx,.275f,-.105f),p+new Vector3(side*.21f+dx,.235f,-.105f),.011f,wornMetal,root);
            PlacedCafeCup(p+new Vector3(side*.21f,.097f,-.11f),side<0?-24:31,false);
            for(int button=0;button<4;button++)Decor("espresso dose button",PrimitiveType.Cylinder,p+new Vector3(side*.21f+(button-1.5f)*.035f,.45f,-.052f),new Vector3(.019f,.004f,.019f),priceWhite).transform.rotation=Quaternion.Euler(90,0,0);
            RailSegment("steam wand elbow",p+new Vector3(side*.36f,.31f,-.10f),p+new Vector3(side*.39f,.20f,-.23f),.009f,wornMetal,root);
            RailSegment("steam wand tip",p+new Vector3(side*.39f,.20f,-.23f),p+new Vector3(side*.39f,.12f,-.20f),.009f,wornMetal,root);
        }
        MerchBox("cup warming tray",p+new Vector3(0,.595f,.10f),new Vector3(.82f,.017f,.32f),wornMetal);
        Vector3[] cups={new(-.28f,.605f,.097f),new(-.094f,.605f,.126f),new(.071f,.605f,.109f),new(.274f,.605f,.088f)};
        float[] cupYaw={-20,17,42,-8};
        for(int i=0;i<cups.Length;i++)PlacedCafeCup(p+cups[i],cupYaw[i],true);
        var grinder=p+new Vector3(.80f,0,.05f);
        MerchBox("coffee grinder motor",grinder+Vector3.up*.19f,new Vector3(.23f,.37f,.24f),body);
        HandmadeProp("grinder tapered hopper",TurnedPackageMesh("coffee_hopper",new[]{0f,.15f,.85f,1f},new[]{.24f,.43f,.49f,.49f}),grinder+Vector3.up*.38f,new Vector3(.25f,.24f,.25f),glass,root);
        Decor("coffee hopper lid",PrimitiveType.Cylinder,grinder+Vector3.up*.63f,new Vector3(.26f,.014f,.26f),dark);
        for(int i=0;i<5;i++)CeramicCup(p+new Vector3(-.9f,0,.12f+i*.004f)+Vector3.up*(i*.027f),null,true);
        CafeServiceDetails(p);
    }
    static void HeatedDisplay(Vector3 p)
    {
        MerchBox("heated display base",p+Vector3.up*.05f,new Vector3(1.4f,.10f,.63f),shelfTop,null,true);
        MerchBox("heated display glass front",p+new Vector3(0,.35f,-.31f),new Vector3(1.36f,.58f,.012f),cabinetGlass);
        foreach(int side in new[]{-1,1}){
            MerchBox("heated display corner",p+new Vector3(side*.67f,.36f,-.30f),new Vector3(.025f,.61f,.025f),wornMetal);
            MerchBox("heated display side",p+new Vector3(side*.69f,.36f,0),new Vector3(.012f,.58f,.61f),cabinetGlass);
        }
        MerchBox("heated display canopy",p+Vector3.up*.68f,new Vector3(1.42f,.07f,.65f),shelfTop);
        for(int tray=0;tray<3;tray++){
            var tp=p+new Vector3((tray-1)*.43f,.12f,0);
            MerchBox("food service tray",tp,new Vector3(.39f,.025f,.51f),wornMetal);
            for(int a=0;a<2;a++)for(int b=0;b<3;b++)Decor("display bread roll",PrimitiveType.Sphere,tp+new Vector3((a-.5f)*.16f,.045f,(b-1)*.14f),new Vector3(.13f,.08f,.12f),cream);
        }
    }
    static void PharmacyStock(Vector3 p,int variant,int side,Transform parent)
    {
        SellUnit("하루봄 생활 건강 상품",p,parent,-side*90,false,30,variant,unit=>{
            if(variant<2){
                var size=variant==0?new Vector3(.12f,.10f,.032f):new Vector3(.088f,.17f,.06f);
                MerchBox("health paperboard carton",p+Vector3.up*size.y*.5f,size,priceWhite,unit);
                MerchArt("health carton product print",variant,p+new Vector3(0,size.y*.5f,-size.z*.5f-.0007f),new Vector2(size.x,size.y),Vector3.zero,unit);
                MerchArt("health carton back print",variant,p+new Vector3(0,size.y*.5f,size.z*.5f+.0007f),new Vector2(size.x,size.y),new Vector3(0,180,0),unit);
            }else{
                float w=variant==2?.066f:variant==3?.052f:.038f,h=variant==2?.104f:variant==3?.15f:.11f;
                HandmadeProp("health bottle",RetailBottleProfile(variant==2?6:4),p,new Vector3(w,h,w),variant==2?RetailSurface("vitamin amber",new Color(.19f,.08f,.026f),0,.66f):priceWhite,unit);
                CurvedPrint(p+Vector3.up*h*.17f,w*1.018f,h*.60f,MerchInk(),unit);
                // Use the correct atlas section on a curved label rather than the entire atlas.
                var print=unit.GetChild(unit.childCount-1).GetComponent<MeshFilter>();
                string key="HealthArc"+variant;string path="Assets/OutOfService/Generated/"+key+".asset";
                if(!handmadeMeshes.TryGetValue(key,out var mesh)){
                    mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(!mesh){mesh=Object.Instantiate(LabelArc());mesh.name=key;var uv=mesh.uv;for(int i=0;i<uv.Length;i++)uv[i]=new Vector2((variant%4+uv[i].x)/4f,(3-variant/4+uv[i].y)/4f);mesh.uv=uv;AssetDatabase.CreateAsset(mesh,path);}
                    handmadeMeshes[key]=mesh;
                }
                print.sharedMesh=mesh;
                Decor("health bottle cap",PrimitiveType.Cylinder,p+Vector3.up*(h+.009f),new Vector3(w*(variant==2?.86f:.45f),.012f,w*(variant==2?.86f:.45f)),variant==2?cream:priceWhite,unit);
                if(variant>=4){MerchBox("spray actuator",p+new Vector3(0,h+.034f,-.008f),new Vector3(.020f,.026f,.035f),priceWhite,unit);MerchBox("spray nozzle",p+new Vector3(0,h+.039f,-.027f),new Vector3(.007f,.007f,.003f),shelf,unit);}
            }
        });
    }
}
