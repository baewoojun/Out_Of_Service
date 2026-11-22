using UnityEngine;
using UnityEditor;

public static partial class MarketPrototypeBuilderV7
{
    static Material HomewareInk()
    {
        var m=RetailSurface("damum original homeware packaging",Color.white,0,.18f);
        m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/MallDetails/damum-homewares.png");
        if(!m.mainTexture)throw new System.InvalidOperationException("Missing original DAMUM package art");return m;
    }
    static GameObject HomewareLabel(int item,Vector3 p,Vector2 size,Quaternion rotation)
    {
        var mesh=MallMesh("damum_label_v1_"+item,(v,u,t)=>{
            float x=item%3/3f,y=1-(item/3+1)/2f;
            v.AddRange(new[]{new Vector3(-.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0),new Vector3(.5f,-.5f,0)});
            u.AddRange(new[]{new Vector2(x+.001f,y+.001f),new Vector2(x+.001f,y+.499f),new Vector2(x+1f/3-.001f,y+.499f),new Vector2(x+1f/3-.001f,y+.001f)});
            t.AddRange(new[]{0,1,2,0,2,3});
        });
        var go=HandmadeProp("original DAMUM product card "+item,mesh,p,new Vector3(size.x,size.y,1),HomewareInk(),root);go.transform.rotation=rotation;return go;
    }
    static Mesh CookwareBowl(int kind)
    {
        return MallMesh("open_cookware_v1_"+kind,(v,u,t)=>{
            float r=kind==0?.125f:kind==1?.09f:kind==2?.14f:.15f,h=kind==0?.13f:kind==1?.105f:kind==2?.045f:.095f;
            bool wok=kind==3;
            float[] ys={0,.003f,.012f,h*.55f,h-.004f,h,h,h-.004f,h*.55f,.013f,.008f,.008f};
            float[] rs={0,r*(wok?.44f:.78f),r*(wok?.57f:.93f),r*(wok?.79f:.99f),r,r*1.015f,r-.0035f,r-.005f,r*(wok?.79f:.99f)-.004f,r*(wok?.57f:.93f)-.004f,r*(wok?.44f:.78f),0};
            const int sides=40;
            for(int row=0;row<ys.Length;row++)for(int n=0;n<=sides;n++){
                float a=n*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*rs[row],ys[row],Mathf.Sin(a)*rs[row]));u.Add(new Vector2(n/(float)sides,ys[row]*4));
                if(row<ys.Length-1 && n<sides){int k=row*(sides+1)+n;t.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});}
            }
        });
    }
    static void CookwareProduct(Vector3 p,int kind,Quaternion rotation,bool lid)
    {
        var previous=root;root=new GameObject("DAMUM cookware form "+kind).transform;root.SetParent(previous);
        var steel=PrepSteel();var coating=RetailSurface("satin nonstick cookware",new Color(.035f,.040f,.037f),.22f,.28f);
        var grip=RetailSurface("cookware heat resistant grip",new Color(.038f,.039f,.036f),0,.25f);
        float r=kind==0?.125f:kind==1?.09f:kind==2?.14f:.15f,h=kind==0?.13f:kind==1?.105f:kind==2?.045f:.095f;
        HandmadeProp("open shaped pan with inner wall",CookwareBowl(kind),Vector3.zero,Vector3.one,kind<2?steel:coating,root);
        if(kind==0)foreach(int side in new[]{-1,1}){
            PrepPipe("pot hollow side handle",new[]{new Vector3(side*r,h*.70f,-.033f),new Vector3(side*(r+.055f),h*.85f,-.033f),new Vector3(side*(r+.061f),h*.85f,.033f),new Vector3(side*r,h*.70f,.033f)},.009f,steel,root);
        }else{
            MerchBox("pan handle metal neck",new Vector3(r+.025f,h*.80f,0),new Vector3(.07f,.017f,.024f),steel);
            // Open hanging eye remains real geometry; no painted hole on a block.
            foreach(int z in new[]{-1,1})RailSegment("pan handle long edge",new Vector3(r+.03f,h*.83f,z*.013f),new Vector3(r+.19f,h+.018f,z*.011f),.014f,kind==1?steel:grip,root);
            RailSegment("handle closed end bridge",new Vector3(r+.19f,h+.018f,-.011f),new Vector3(r+.19f,h+.018f,.011f),.014f,kind==1?steel:grip,root);
            foreach(float z in new[]{-.024f,.024f})Decor("pan attachment rivet",PrimitiveType.Sphere,new Vector3(r-.005f,h*.64f,z),new Vector3(.007f,.009f,.009f),steel);
        }
        if(lid && kind<2){
            HandmadeProp("separate domed cookware lid",TurnedPackageMesh("CookwareLidV1_"+kind,new[]{0f,0f,.008f,.016f,.022f},new[]{0f,r*1.02f,r*.98f,r*.7f,0f}),new Vector3(0,h+.002f,0),Vector3.one,kind==0?cabinetGlass:steel,root);
            HandmadeProp("lid metal rolled edge",TurnedPackageMesh("CookwareLidRimV1_"+kind,new[]{0f,.006f,.008f,.006f,0f},new[]{r*.98f,r*1.025f,r*1.01f,r*.97f,r*.98f}),new Vector3(0,h,0),Vector3.one,steel,root);
            PrepPipe("lid raised loop grip",new[]{new Vector3(-.036f,h+.018f,0),new Vector3(-.03f,h+.055f,0),new Vector3(.03f,h+.055f,0),new Vector3(.036f,h+.018f,0)},.008f,steel,root);
        }else {
            bool hanging=Vector3.Dot(rotation*Vector3.up,Vector3.up)<.5f;
            HomewareLabel(kind,new Vector3(0,.013f,0),new Vector2(r*1.1f,r*.55f),
                hanging?Quaternion.LookRotation(Vector3.down,Vector3.right):Quaternion.Euler(90,180,0));
        }
        root.position=p;root.rotation=rotation;root=previous;
    }
    static void StorageAndMug(Vector3 p,int kind,int variant)
    {
        var previous=root;root=new GameObject(kind==4?"rectangular clipped food container":"ceramic handled mug").transform;root.SetParent(previous);
        if(kind==4){
            float size=variant==0?1:.78f;
            const string key="moulded semi clear homeware polymer";
            if(!retailSurfaces.TryGetValue(key,out var clear)){
                clear=TransparentMat(key,new Color(.68f,.76f,.74f,.28f));clear.SetFloat("_Smoothness",.47f);retailSurfaces[key]=clear;
            }
            HandmadeProp("clear rounded food container",ProduceShell(false,false),Vector3.zero,new Vector3(.23f*size,.10f,.16f*size),clear,root);
            var polymer=RetailSurface("food container ivory lid",new Color(.70f,.75f,.70f),0,.46f);
            MerchBox("container gasket lid",new Vector3(0,.105f,0),new Vector3(.238f*size,.014f,.168f*size),polymer);
            foreach(int side in new[]{-1,1})MerchBox("container locking wing",new Vector3(side*.123f*size,.095f,0),new Vector3(.017f,.027f,.083f*size),polymer);
            HomewareLabel(4,new Vector3(0,.113f,0),new Vector2(.13f*size,.065f*size),Quaternion.Euler(90,180,0));
        }else{
            var glaze=RetailSurface("stoneware glaze "+variant,variant==0?new Color(.59f,.64f,.58f):new Color(.72f,.66f,.53f),0,.67f);
            HandmadeProp("mug open ceramic walls",TurnedPackageMesh("RetailMugV1",new[]{0f,.005f,.02f,.10f,.105f,.105f,.10f,.02f,.009f,.009f},new[]{0f,.030f,.036f,.043f,.043f,.039f,.039f,.031f,.026f,0f}),Vector3.zero,Vector3.one,glaze,root);
            // The existing ring is a true opening in the YZ plane.
            HandmadeProp("mug open loop handle",HandleRing(),new Vector3(.044f,.059f,0),new Vector3(.055f,.075f,.057f),glaze,root).transform.rotation=Quaternion.Euler(0,90,0);
        }
        root.position=p;root=previous;
    }
    static void HomewareRetailCorner()
    {
        var previous=root;root=new GameObject("1F cookware and storage corner").transform;root.SetParent(previous);
        var powder=RetailSurface("homeware fixture ivory steel",new Color(.48f,.49f,.44f),.18f,.28f);
        var wood=MallFinish("wood",new Color(.34f,.25f,.16f),0,.26f);
        var center=new Vector3(-36.8f,0,-31.0f);
        foreach(float x in new[]{-1.70f,1.70f})foreach(float z in new[]{-.54f,.54f})Box("cookware island tubular leg",center+new Vector3(x,.48f,z),new Vector3(.045f,.96f,.045f),powder);
        foreach(float y in new[]{.25f,.92f})MerchBox("cookware open display tier",center+Vector3.up*y,new Vector3(3.60f,.04f,1.20f),wood,null,true);
        for(int kind=0;kind<4;kind++){
            float x=-1.32f+kind*.88f;
            foreach(float z in new[]{-.31f,.31f})CookwareProduct(center+new Vector3(x,.942f,z),kind,Quaternion.identity,kind<2);
            for(int depth=0;depth<3;depth++){
                var p=center+new Vector3(x,.395f,-.39f+depth*.37f);
                MerchBox("boxed cookware stock",p,new Vector3(.52f,.25f,.34f),cream);
                HomewareLabel(kind,p+Vector3.back*.171f,new Vector2(.38f,.19f),Quaternion.identity);
                HomewareLabel(kind,p+Vector3.forward*.171f,new Vector2(.38f,.19f),Quaternion.Euler(0,180,0));
            }
            HomewareLabel(kind,center+new Vector3(x,.89f,-.623f),new Vector2(.24f,.12f),Quaternion.identity);
            HomewareLabel(kind,center+new Vector3(x,.89f,.623f),new Vector2(.24f,.12f),Quaternion.Euler(0,180,0));
        }
        var rack=new Vector3(-32,0,-31);
        foreach(float x in new[]{-.72f,.72f})foreach(float z in new[]{-.44f,.44f})Box("houseware tall rack upright",rack+new Vector3(x,.80f,z),new Vector3(.035f,1.6f,.035f),shelf);
        foreach(float y in new[]{.27f,.76f,1.25f}){
            MerchBox("storage display shallow shelf",rack+Vector3.up*y,new Vector3(1.50f,.03f,1),powder,null,true);
            for(int x=0;x<5;x++)for(int z=0;z<3;z++)StorageAndMug(rack+new Vector3(-.56f+x*.28f,y+.016f,-.31f+z*.29f),y<1?4:5,(x/2)%2);
            HomewareLabel(y<1?4:5,rack+new Vector3(0,y-.02f,-.515f),new Vector2(.24f,.12f),Quaternion.identity);
            HomewareLabel(y<1?4:5,rack+new Vector3(0,y-.02f,.515f),new Vector2(.24f,.12f),Quaternion.Euler(0,180,0));
        }
        var hanger=new Vector3(-36.8f,0,-27.7f);
        foreach(float x in new[]{-1.7f,1.7f}){
            RailSegment("pan rack upright",hanger+new Vector3(x,.04f,0),hanger+new Vector3(x,1.92f,0),.045f,shelfTop,root);
            Box("pan rack stabilising foot",hanger+new Vector3(x,.035f,0),new Vector3(.10f,.07f,.60f),shelfTop);
        }
        RailSegment("pan hanging rail",hanger+new Vector3(-1.7f,1.68f,0),hanger+new Vector3(1.7f,1.68f,0),.035f,shelfTop,root);
        for(int n=0;n<6;n++){
            var p=hanger+new Vector3(-1.40f+n*.56f,1.30f,.04f);
            CookwareProduct(p,2+n%2,Quaternion.LookRotation(Vector3.right,Vector3.forward),false);
            PrepPipe("pan retail hook",new[]{p+new Vector3(0,.39f,-.04f),p+new Vector3(0,.39f,.05f),p+new Vector3(0,.31f,.05f)},.005f,shelfTop,root);
        }
        Box("cookware low category fascia",hanger+Vector3.up*2.08f,new Vector3(3.5f,.32f,.08f),green);
        Label("주방 · 보관  /  냄비 · 팬 · 밀폐용기",hanger+new Vector3(0,2.08f,.055f),new Vector3(0,180,0),.16f);
        LocalLamp("cookware closing display lamp",new Vector3(-35.4f,3.9f,-30.6f),new Vector3(-36,0,-30.6f),new Color(.93f,.88f,.74f),2.2f,7,true);
        root=previous;
    }
}
