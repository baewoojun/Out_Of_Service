using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    static Material PrepSteel()
    {
        var mat=RetailSurface("food prep satin stainless",new Color(.55f,.58f,.59f),.65f,.33f);
        if(!mat.GetTexture("_BumpMap")){
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap",FinishTexture("PrepBrushedSteelNormal",(u,v)=>{
                float grain=(Mathf.PerlinNoise(u*5,v*190)-.5f)*.11f;
                return new Color(.5f,.5f+grain,1,1);
            }));mat.SetFloat("_BumpScale",.17f);
        }
        return mat;
    }
    static Material PrepFloor()
    {
        var mat=RetailSurface("fine quarry tile prep floor",Color.white,0,.19f);
        if(mat.mainTexture)return mat;
        mat.mainTexture=FinishTexture("PrepQuarryTiles",(u,v)=>{
            float a=Mathf.Abs(Mathf.Repeat(u*4+.5f,1)-.5f),b=Mathf.Abs(Mathf.Repeat(v*4+.5f,1)-.5f);
            float noise=Mathf.PerlinNoise(u*210+3,v*210+8)*.028f;
            return Mathf.Min(a,b)<.014f?new Color(.115f,.132f,.126f):new Color(.23f+noise,.26f+noise,.25f+noise);
        });
        mat.mainTextureScale=new Vector2(13.8f/1.2f,11.7f/1.2f);return mat;
    }
    static Material PrepWall()
    {
        var mat=RetailSurface("washable prep ceramic",Color.white,0,.28f);
        if(mat.mainTexture)return mat;
        mat.mainTexture=FinishTexture("PrepCeramicFourTiles",(u,v)=>{
            float a=Mathf.Abs(Mathf.Repeat(u*4+.5f,1)-.5f),b=Mathf.Abs(Mathf.Repeat(v*4+.5f,1)-.5f);
            float edge=Mathf.Min(a,b);
            return edge<.012f?new Color(.40f,.43f,.40f):new Color(.73f,.75f,.71f);
        });
        mat.mainTextureScale=new Vector2(6.8f/1.2f,1.8f/1.2f);return mat;
    }
    static Material BoardPlastic(bool fish)
    {
        var mat=RetailSurface(fish?"blue food grade cutting board":"ivory food grade cutting board",fish?new Color(.25f,.46f,.49f):new Color(.73f,.71f,.56f),0,.22f);
        if(mat.HasProperty("_BumpMap") && !mat.GetTexture("_BumpMap")){
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap",FinishTexture("UsedHDPECuttingSurface",(u,v)=>{
                float line=Mathf.Sin((u*95+v*57)*Mathf.PI*2)*Mathf.Pow(Mathf.PerlinNoise(u*16,v*18),5)*.22f;
                return new Color(.5f+line,.5f-line*.55f,1,1);
            }));mat.SetFloat("_BumpScale",.28f);
        }
        return mat;
    }
    static void PrepPipe(string title,Vector3[] points,float radius,Material material,Transform parent)
    {
        for(int i=1;i<points.Length;i++)RailSegment(title,points[i-1],points[i],radius,material,parent);
    }
    static Mesh PrepBasinMesh()
    {
        const string key="HollowPrepBasinV1";string path="Assets/OutOfService/Generated/"+key+".asset";
        if(handmadeMeshes.TryGetValue(key,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){
            float[] radii={0,.43f,.50f,.455f,.39f,0},heights={-.95f,-.95f,0,0,-.82f,-.82f};
            var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();const int n=40;
            for(int r=0;r<radii.Length;r++)for(int k=0;k<=n;k++){
                float angle=k*Mathf.PI*2/n,c=Mathf.Cos(angle),s=Mathf.Sin(angle);
                v.Add(new Vector3(Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),.40f)*radii[r],heights[r],Mathf.Sign(s)*Mathf.Pow(Mathf.Abs(s),.40f)*radii[r]));
                uv.Add(new Vector2((float)k/n,r/(float)(radii.Length-1)));
                if(r==radii.Length-1 || k==n)continue;
                int a=r*(n+1)+k;tri.AddRange(new[]{a,a+n+1,a+1,a+1,a+n+1,a+n+2});
            }
            mesh=new Mesh{name=key};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[key]=mesh;return mesh;
    }
    static void PrepSink(Vector3 p,Transform parent)
    {
        var steel=PrepSteel();
        HandmadeProp("pressed open stainless wash basin",PrepBasinMesh(),p+Vector3.up*.97f,new Vector3(.87f,.27f,.61f),steel,parent);
        foreach(int side in new[]{-1,1}){
            MerchBox("sink left right draining rim",p+new Vector3(side*.56f,.955f,0),new Vector3(.28f,.032f,.85f),steel,parent,true);
            MerchBox("sink front rear folded rim",p+new Vector3(0,.955f,side*.365f),new Vector3(.86f,.032f,.12f),steel,parent,true);
            foreach(int end in new[]{-1,1}){
                var leg=p+new Vector3(side*.61f,.46f,end*.33f);
                MerchBox("sink tubular support",leg,new Vector3(.04f,.90f,.04f),steel,parent);
                Decor("sink adjustable rubber foot",PrimitiveType.Cylinder,leg+Vector3.down*.435f,new Vector3(.065f,.025f,.065f),dark,parent);
            }
        }
        MerchBox("wash basin splash back",p+new Vector3(0,1.13f,.42f),new Vector3(1.4f,.36f,.022f),steel,parent,true);
        Decor("basin drain flange",PrimitiveType.Cylinder,p+new Vector3(0,.751f,.06f),new Vector3(.085f,.002f,.085f),shelfTop,parent);
        for(int k=-2;k<=2;k++)MerchBox("drain strainer slit",p+new Vector3(k*.012f,.754f,.06f),new Vector3(.004f,.001f,.055f),dark,parent);
        PrepPipe("sink waste trap",new[]{p+new Vector3(0,.74f,.06f),p+new Vector3(0,.43f,.06f),p+new Vector3(0,.36f,.12f),p+new Vector3(0,.36f,.23f),p+new Vector3(0,.48f,.30f),p+new Vector3(0,.49f,.42f)},.025f,steel,parent);
        PrepPipe("gooseneck wash faucet",new[]{p+new Vector3(.16f,.98f,.31f),p+new Vector3(.16f,1.24f,.31f),p+new Vector3(.16f,1.31f,.26f),p+new Vector3(.16f,1.32f,.13f),p+new Vector3(.16f,1.27f,.06f)},.018f,steel,parent);
        RailSegment("faucet lever",p+new Vector3(.19f,1.04f,.31f),p+new Vector3(.31f,1.07f,.31f),.012f,steel,parent);
        MerchBox("handwash soap reservoir",p+new Vector3(-.55f,1.12f,.27f),new Vector3(.13f,.25f,.12f),priceWhite,parent);
        MerchBox("soap dispenser pump",p+new Vector3(-.55f,1.27f,.24f),new Vector3(.13f,.035f,.12f),shelf,parent);
        MerchBox("paper towel dispenser",p+new Vector3(.52f,1.14f,.27f),new Vector3(.22f,.27f,.15f),priceWhite,parent);
        MerchBox("paper towel slot",p+new Vector3(.52f,1.005f,.22f),new Vector3(.16f,.015f,.05f),dark,parent);
        MerchBox("folded towel end",p+new Vector3(.52f,.96f,.21f),new Vector3(.14f,.09f,.002f),cream,parent);
    }
    static Mesh PrepKnifeBlade()
    {
        const string key="BoningKnifeBladeV1";string path="Assets/OutOfService/Generated/"+key+".asset";
        if(handmadeMeshes.TryGetValue(key,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){
            Vector2[] outline={new(-.23f,.005f),new(-.17f,-.031f),new(.006f,-.033f),new(.018f,.027f),new(-.12f,.025f)};
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            foreach(float y in new[]{-.0012f,.0012f})foreach(var q in outline){v.Add(new Vector3(q.x,y,q.y));uv.Add(q*3+Vector2.one*.5f);}
            for(int i=1;i<4;i++){t.AddRange(new[]{0,i,i+1,5,5+i+1,5+i});}
            for(int i=0;i<5;i++){int j=(i+1)%5;t.AddRange(new[]{i,5+i,j,j,5+i,5+j});}
            mesh=new Mesh{name=key};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[key]=mesh;return mesh;
    }
    static void PrepKnife(Vector3 p,Vector3 rotation,Transform parent)
    {
        var knife=new GameObject("sanitary prep knife").transform;knife.SetParent(parent);knife.position=p;
        HandmadeProp("tapered sharpened knife blade",PrepKnifeBlade(),p,Vector3.one,PrepSteel(),knife);
        MerchBox("moulded knife grip",p+new Vector3(.079f,0,0),new Vector3(.135f,.020f,.036f),dark,knife);
        foreach(float x in new[]{.044f,.115f})Decor("knife grip rivet",PrimitiveType.Cylinder,p+new Vector3(x,.0105f,0),new Vector3(.006f,.001f,.006f),shelfTop,knife);
        knife.rotation=Quaternion.Euler(rotation);
    }
    static void PrepCrate(Vector3 p,Vector3 size,Material material,Transform parent)
    {
        var c=new GameObject("ventilated washable stock crate").transform;c.SetParent(parent);c.position=p;
        var collision=c.gameObject.AddComponent<BoxCollider>();collision.center=Vector3.up*size.y*.5f;collision.size=size;
        MerchBox("crate bottom",p+Vector3.up*.022f,new Vector3(size.x,.035f,size.z),material,c);
        foreach(int side in new[]{-1,1}){
            for(int rail=0;rail<3;rail++){
                float h=.07f+rail*(size.y-.08f)*.5f;
                MerchBox("crate slotted side rail",p+new Vector3(side*(size.x*.5f-.013f),h,0),new Vector3(.026f,.038f,size.z),material,c);
                MerchBox("crate slotted end rail",p+new Vector3(0,h,side*(size.z*.5f-.013f)),new Vector3(size.x,.038f,.026f),material,c);
            }
            for(int rib=0;rib<=6;rib++)
                MerchBox("crate ventilated wall rib",p+new Vector3((rib/6f-.5f)*(size.x-.025f),size.y*.5f,side*(size.z*.5f-.013f)),new Vector3(.024f,size.y,.026f),material,c);
            foreach(int end in new[]{-1,1})MerchBox("crate corner",p+new Vector3(side*(size.x*.5f-.02f),size.y*.5f,end*(size.z*.5f-.02f)),new Vector3(.043f,size.y,.043f),material,c);
        }
    }
    static void PrepScale(Vector3 p,Transform parent)
    {
        MerchBox("price computing scale base",p+Vector3.up*.07f,new Vector3(.52f,.13f,.46f),priceWhite,parent);
        MerchBox("scale removable steel pan",p+new Vector3(-.035f,.155f,.03f),new Vector3(.43f,.022f,.34f),PrepSteel(),parent);
        MerchArt("scale zero weight display",13,p+new Vector3(-.11f,.075f,-.232f),new Vector2(.19f,.075f),Vector3.zero,parent);
        for(int row=0;row<3;row++)for(int col=0;col<4;col++)MerchBox("scale membrane keypad",p+new Vector3(.065f+col*.037f,.036f+row*.026f,-.233f),new Vector3(.027f,.018f,.002f),row==0&&col==3?green:shelf,parent);
        MerchBox("label printer slot",p+new Vector3(.12f,.11f,-.232f),new Vector3(.13f,.012f,.008f),dark,parent);
        MerchArt("scale printed weight receipt",7,p+new Vector3(.12f,.04f,-.24f),new Vector2(.083f,.12f),Vector3.zero,parent);
        foreach(int a in new[]{-1,1})foreach(int b in new[]{-1,1})Decor("scale levelling foot",PrimitiveType.Cylinder,p+new Vector3(a*.21f,.008f,b*.17f),new Vector3(.038f,.009f,.038f),dark,parent);
    }
    static void PrepBench(Vector3 p,Vector2 size,Transform parent)
    {
        var steel=PrepSteel();
        MerchBox("prep bench folded top",p+Vector3.up*.95f,new Vector3(size.x,.055f,size.y),steel,parent,true);
        MerchBox("prep bench lower storage shelf",p+Vector3.up*.30f,new Vector3(size.x-.10f,.025f,size.y-.10f),steel,parent);
        foreach(int a in new[]{-1,1})foreach(int b in new[]{-1,1}){
            MerchBox("prep bench square tube leg",p+new Vector3(a*(size.x*.5f-.07f),.47f,b*(size.y*.5f-.07f)),new Vector3(.043f,.94f,.043f),steel,parent);
            Decor("prep bench adjustable foot",PrimitiveType.Cylinder,p+new Vector3(a*(size.x*.5f-.07f),.027f,b*(size.y*.5f-.07f)),new Vector3(.063f,.025f,.063f),dark,parent);
        }
    }
    static void FilmWrappingStation(Vector3 p,Transform parent)
    {
        var steel=PrepSteel();
        MerchBox("tray wrapping machine base",p+Vector3.up*.05f,new Vector3(.66f,.095f,.59f),steel,parent);
        foreach(int side in new[]{-1,1})MerchBox("film roll spindle bracket",p+new Vector3(side*.29f,.17f,.16f),new Vector3(.034f,.27f,.18f),steel,parent);
        Decor("food wrap translucent roll",PrimitiveType.Cylinder,p+new Vector3(0,.23f,.16f),new Vector3(.17f,.27f,.17f),cream,parent).transform.rotation=Quaternion.Euler(0,0,90);
        foreach(int side in new[]{-1,1})Decor("film cardboard core",PrimitiveType.Cylinder,p+new Vector3(side*.273f,.23f,.16f),new Vector3(.048f,.004f,.048f),shelf,parent).transform.rotation=Quaternion.Euler(0,0,90);
        MerchBox("wrapping warm sealing plate",p+new Vector3(0,.105f,-.14f),new Vector3(.49f,.025f,.25f),dark,parent);
        MerchBox("film cutting rail",p+new Vector3(0,.13f,-.29f),new Vector3(.53f,.018f,.025f),steel,parent);
        Decor("film pulled over platform",PrimitiveType.Quad,p+new Vector3(0,.145f,-.03f),new Vector3(.49f,.33f,1),ChilledWrap(),parent).transform.rotation=Quaternion.Euler(74,0,0);
        Decor("wrapper heat selector knob",PrimitiveType.Cylinder,p+new Vector3(.245f,.047f,-.30f),new Vector3(.032f,.009f,.032f),dark,parent).transform.rotation=Quaternion.Euler(90,0,0);
        MerchBox("wrapper power rocker",p+new Vector3(-.24f,.045f,-.30f),new Vector3(.025f,.035f,.012f),red,parent);
        for(int i=0;i<4;i++)HandmadeProp("stacked empty food tray",ProduceShell(false,false),p+new Vector3(.65f,.015f+i*.018f,0),new Vector3(.43f,.045f,.31f),priceWhite,parent);
    }
    static void ButcherSaw(Vector3 p,Transform parent)
    {
        var steel=PrepSteel();
        MerchBox("bandsaw enclosed lower wheel cabinet",p+new Vector3(-.10f,.40f,.07f),new Vector3(.51f,.78f,.53f),steel,parent,true);
        MerchBox("bandsaw washable working table",p+Vector3.up*.84f,new Vector3(.83f,.055f,.72f),steel,parent,true);
        MerchBox("bandsaw vertical column",p+new Vector3(-.29f,1.20f,.17f),new Vector3(.22f,.76f,.28f),priceWhite,parent,true);
        MerchBox("bandsaw enclosed upper wheel guard",p+new Vector3(-.04f,1.55f,.17f),new Vector3(.70f,.29f,.31f),priceWhite,parent,true);
        MerchBox("bandsaw narrow stopped blade",p+new Vector3(.23f,1.15f,.13f),new Vector3(.004f,.61f,.022f),steel,parent);
        MerchBox("bandsaw blade guide",p+new Vector3(.23f,1.35f,.13f),new Vector3(.065f,.13f,.073f),shelf,parent);
        MerchBox("bandsaw sliding fence",p+new Vector3(-.05f,.91f,.10f),new Vector3(.23f,.09f,.42f),steel,parent);
        Decor("bandsaw guard latch",PrimitiveType.Cylinder,p+new Vector3(-.04f,1.55f,.004f),new Vector3(.055f,.012f,.055f),dark,parent).transform.rotation=Quaternion.Euler(90,0,0);
        MerchBox("bandsaw stop button backing",p+new Vector3(-.23f,.65f,-.202f),new Vector3(.075f,.075f,.008f),yellow,parent);
        Decor("bandsaw emergency stop cap",PrimitiveType.Cylinder,p+new Vector3(-.23f,.65f,-.215f),new Vector3(.045f,.012f,.045f),red,parent).transform.rotation=Quaternion.Euler(90,0,0);
        foreach(int a in new[]{-1,1})foreach(int b in new[]{-1,1})Decor("bandsaw adjustable base",PrimitiveType.Cylinder,p+new Vector3(-.10f+a*.19f,.035f,.07f+b*.20f),new Vector3(.085f,.035f,.085f),dark,parent);
    }
    static Material FishSkin()
    {
        var mat=RetailSurface("silver mackerel skin",Color.white,.18f,.44f);
        if(mat.mainTexture)return mat;
        mat.mainTexture=FinishTexture("MackerelSilverSkin",(u,v)=>{
            float dorsal=Mathf.Clamp01(Mathf.Sin(u*Mathf.PI*2)*1.4f-.08f);
            float stripe=Mathf.SmoothStep(.78f,.96f,Mathf.Sin(v*105+Mathf.Sin(u*31)*2)*.5f+.5f)*dorsal;
            var color=Color.Lerp(new Color(.68f,.72f,.68f),new Color(.10f,.25f,.27f),dorsal);
            return color*(1-.48f*stripe)*( .93f+.07f*Mathf.PerlinNoise(u*130,v*100));
        });return mat;
    }
    static Mesh FishFin(bool fork)
    {
        string key=fork?"MackerelForkFin":"MackerelSideFin",path="Assets/OutOfService/Generated/"+key+".asset";
        if(handmadeMeshes.TryGetValue(key,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){
            Vector3[] shape=fork?new[]{new Vector3(0,0,-.15f),new(1,0,-.65f),new(.65f,0,0),new(1,0,.65f),new(0,0,.15f)}:
                new[]{new Vector3(0,0,0),new(.9f,0,-.5f),new(.68f,0,.12f),new(.18f,0,.23f)};
            var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
            foreach(float y in new[]{-.012f,.012f})foreach(var a in shape){v.Add(a+Vector3.up*y);uv.Add(new Vector2(a.x,a.z+.5f));}
            int n=shape.Length;for(int i=1;i<n-1;i++)tr.AddRange(new[]{0,i,i+1,n,n+i+1,n+i});
            for(int i=0;i<n;i++){int j=(i+1)%n;tr.AddRange(new[]{i,i+n,j,j,i+n,j+n});}
            mesh=new Mesh{name=key};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[key]=mesh;return mesh;
    }
    static void WholeFish(Vector3 p,float yaw,Transform parent)
    {
        var fish=new GameObject("whole mackerel with forked tail").transform;fish.SetParent(parent);fish.position=p;
        HandmadeProp("mackerel silver body",FreshMesh(true,0),p,new Vector3(.52f,.32f,.14f),FishSkin(),fish);
        var fin=RetailSurface("mackerel translucent fin tone",new Color(.25f,.35f,.35f),.10f,.30f);
        HandmadeProp("mackerel forked caudal fin",FishFin(true),p+new Vector3(.23f,0,0),new Vector3(.10f,.10f,.10f),fin,fish);
        foreach(int side in new[]{-1,1}){
            Decor("fish silver eye socket",PrimitiveType.Sphere,p+new Vector3(-.20f,.012f,side*.034f),new Vector3(.023f,.019f,.020f),priceWhite,fish);
            Decor("fish dark pupil",PrimitiveType.Sphere,p+new Vector3(-.20f,.014f,side*.042f),new Vector3(.014f,.013f,.010f),dark,fish);
            HandmadeProp("mackerel pectoral fin",FishFin(false),p+new Vector3(-.11f,.020f,side*.061f),new Vector3(.068f,.07f,.07f),fin,fish).transform.rotation=Quaternion.Euler(side*23,side*32,0);
            PrepPipe("mackerel gill seam",new[]{p+new Vector3(-.139f,.036f,side*.042f),p+new Vector3(-.124f,.020f,side*.059f),p+new Vector3(-.132f,.002f,side*.062f)},.0014f,fin,fish);
        }
        HandmadeProp("mackerel dorsal fin",FishFin(false),p+new Vector3(-.03f,.048f,0),new Vector3(.10f,.07f,.08f),fin,fish).transform.rotation=Quaternion.Euler(80,0,0);
        fish.rotation=Quaternion.Euler(0,yaw,0);
    }
    static void BuildPrepDetails(float x,bool fish,Transform area)
    {
        var steel=PrepSteel();
        foreach(int side in new[]{-1,1}){
            Decor("washable lower prep wall tile",PrimitiveType.Quad,new Vector3(x+side*6.875f,1.05f,35.6f),new Vector3(6.8f,1.8f,1),PrepWall(),area).transform.rotation=Quaternion.Euler(0,side*90,0);
            MerchBox("prep wall hygienic skirting",new Vector3(x+side*6.87f,.17f,35.6f),new Vector3(.025f,.15f,6.8f),steel,area);
        }
        var bench=new Vector3(x,.07f,35.5f);PrepBench(bench,new Vector2(4.5f,1.8f),area);
        var board=bench+new Vector3(-.7f,1.003f,-.12f);
        MerchBox("used food grade cutting board",board,new Vector3(.94f,.045f,.60f),BoardPlastic(fish),area);
        PrepKnife(board+new Vector3(.64f,.025f,-.07f),new Vector3(0,33,0),area);
        if(fish)WholeFish(board+new Vector3(-.03f,.068f,.02f),-16,area);
        else{
            HandmadeProp("meat cut being portioned",FreshMesh(false,3),board+new Vector3(-.10f,.069f,.02f),new Vector3(.38f,.09f,.27f),FreshMaterial(0),area);
            for(int cut=0;cut<3;cut++)HandmadeProp("portioned meat slice",FreshMesh(false,cut),board+new Vector3(.23f,.039f,-.13f+cut*.094f),new Vector3(.15f,.035f,.085f),FreshMaterial(0),area);
        }
        PrepScale(bench+new Vector3(1.48f,.982f,-.1f),area);
        for(int i=0;i<3;i++)HandmadeProp("stacked prep tray",ProduceShell(false,false),bench+new Vector3(.68f,.985f+i*.025f,.50f),new Vector3(.46f,.045f,.31f),priceWhite,area);
        MerchBox("disposable glove dispenser carton",bench+new Vector3(-1.65f,1.045f,.50f),new Vector3(.24f,.12f,.13f),cream,area);
        MerchBox("glove dispensing aperture",bench+new Vector3(-1.65f,1.108f,.50f),new Vector3(.14f,.002f,.055f),dark,area);
        PrepSink(new Vector3(x+4.7f,.07f,35.8f),area);
        MerchBox("trench drain recess",new Vector3(x+.3f,.075f,37.0f),new Vector3(2.8f,.008f,.18f),dark,area);
        for(int i=0;i<36;i++)MerchBox("trench drain grate bar",new Vector3(x-1.04f+i*.077f,.083f,37.0f),new Vector3(.022f,.010f,.18f),steel,area);
        var wrapping=new Vector3(x-5.2f,.07f,37.6f);PrepBench(wrapping,new Vector2(2.15f,.92f),area);FilmWrappingStation(wrapping+new Vector3(-.20f,.98f,0),area);
        if(fish){
            var station=new Vector3(x-4.9f,.07f,35.2f);PrepBench(station,new Vector2(2.0f,1.05f),area);
            MerchBox("fish trimming blue board",station+Vector3.up*1.00f,new Vector3(1.12f,.04f,.66f),BoardPlastic(true),area);
            WholeFish(station+new Vector3(-.10f,1.06f,0),19,area);PrepKnife(station+new Vector3(.63f,1.024f,0),new Vector3(0,-65,0),area);
        }else ButcherSaw(new Vector3(x-4.9f,.07f,35.2f),area);
        for(int i=0;i<2;i++){
            var crate=new Vector3(x-3.15f+i*1.15f,.08f,38.15f);
            PrepCrate(crate,new Vector3(.84f,.40f,.61f),fish?blue:cream,area);
            if(fish)for(int n=0;n<3;n++)WholeFish(crate+new Vector3(0,.20f,(n-1)*.15f),n*7-8,area);
            else for(int n=0;n<2;n++)HandmadeProp("covered meat in prep crate",FreshMesh(false,n),crate+new Vector3((n-.5f)*.29f,.22f,0),new Vector3(.28f,.15f,.34f),FreshMaterial(0),area);
        }
        var rail=new Vector3(x-4.3f,1.70f,39.065f);
        MerchBox("wall mounted knife magnetic bar",rail,new Vector3(1.3f,.065f,.025f),steel,area);
        for(int i=0;i<3;i++)PrepKnife(rail+new Vector3(-.4f+i*.37f,-.05f,-.025f),Quaternion.LookRotation(Vector3.left,Vector3.back).eulerAngles,area);
        foreach(int side in new[]{-1,1})for(int t=0;t<2;t++){
            var rack=new Vector3(x+side*4.7f,.7f+t*.95f,41.6f);
            if(t==0)foreach(int a in new[]{-1,1})foreach(int b in new[]{-1,1})MerchBox("walk-in rack upright",new Vector3(rack.x+a*1.25f,1.05f,rack.z+b*1.54f),new Vector3(.04f,2.1f,.04f),steel,area);
            for(int n=-1;n<=1;n++){
                var c=rack+new Vector3(0,.04f,n*.92f);
                PrepCrate(c,new Vector3(.98f,.33f,.65f),fish?priceWhite:cream,area);
                var tag=MerchArt("cold stock handling label",15,c+new Vector3(0,.17f,-.327f),new Vector2(.23f,.15f),Vector3.zero,area);
                for(int lid=0;lid<2;lid++)MerchBox("cold stock sealed insulated inner carton",c+new Vector3((lid-.5f)*.42f,.16f,0),new Vector3(.38f,.25f,.49f),priceWhite,area);
            }
        }
    }
}
