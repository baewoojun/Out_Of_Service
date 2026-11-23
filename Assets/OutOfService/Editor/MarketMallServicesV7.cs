using UnityEngine;
using UnityEditor;

public static partial class MarketPrototypeBuilderV7
{
    // Original mall service shop fittings. These are an authored composite layout,
    // not a measured reproduction of a real branch. Keep the central 2.8m route free.
    static void ServiceCaption(string text,Vector3 p,float size,Color colour,Vector3 rotation=default)
    {
        Label(text,p,rotation,size);
        root.GetChild(root.childCount-1).GetComponent<TextMesh>().color=colour;
    }

    static void ServiceCounter(string title,Vector3 p,Vector3 size,Material finish)
    {
        MerchBox(title+" cabinet",p+Vector3.up*(size.y*.5f),size,finish,root,true);
        MerchBox(title+" countertop",p+Vector3.up*(size.y+.035f),new Vector3(size.x+.05f,.07f,size.z+.05f),cream,root,true);
        Decor(title+" recessed plinth",PrimitiveType.Cube,p+new Vector3(0,.06f,-size.z*.5f-.006f),new Vector3(size.x-.10f,.12f,.015f),shelf);
        for(float x=-size.x*.5f+.4f;x<size.x*.5f;x+=.8f){
            Decor(title+" cabinet seam",PrimitiveType.Cube,p+new Vector3(x,size.y*.5f,-size.z*.5f-.008f),new Vector3(.007f,size.y-.16f,.008f),shelf);
            Decor(title+" metal drawer pull",PrimitiveType.Cube,p+new Vector3(x+.18f,size.y-.12f,-size.z*.5f-.03f),new Vector3(.18f,.017f,.04f),PrepSteel());
        }
    }

    static void CosmeticProduct(Vector3 p,int type,int variant,float yaw)
    {
        if(type>=3){SpecialtyCosmetic(p,type,variant,yaw);return;}
        var old=root;root=new GameObject("parody skincare product "+type).transform;root.SetParent(old);
        var plastic=RetailSurface("cosmetic package "+variant,variant==0?new Color(.79f,.72f,.56f):variant==1?new Color(.36f,.48f,.36f):new Color(.72f,.60f,.58f),0,.35f);
        float h=type==0?.21f:type==1?.15f:.065f,r=type==2?.047f:.031f;
        HandmadeProp("rounded skincare container",RoundedSkincareContainer(type,h,r),Vector3.zero,Vector3.one,plastic,root);
        Decor("skincare closure",PrimitiveType.Cylinder,new Vector3(0,h*.96f,0),new Vector3(type==2?r*2:.037f,h*.075f,type==2?r*2:.037f),priceWhite);
        if(type==0){
            Decor("pump stem",PrimitiveType.Cylinder,new Vector3(0,h+.016f,0),new Vector3(.014f,.013f,.014f),priceWhite);
            MerchBox("pump actuator",new Vector3(0,h+.03f,-.009f),new Vector3(.029f,.012f,.052f),priceWhite);
            Decor("pump outlet",PrimitiveType.Cube,new Vector3(0,h+.026f,-.035f),new Vector3(.008f,.005f,.002f),shelf);
        }
        // Shared curved label meshes join spatial batches, avoiding 600 separate
        // dynamic TextMesh renderers and materials on tiny background packages.
        var ink=RetailSurface("original moa label "+type,Color.white,0,.23f);
        if(!ink.mainTexture)ink.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/MallDetails/moa-skincare-"+type+".png");
        var label=MallMesh("curved_cosmetic_label_v1",(v,u,t)=>{
            for(int n=0;n<=10;n++){
                float a=(n/10f-.5f)*1.8f;
                v.Add(new Vector3(Mathf.Sin(a),-.5f,-Mathf.Cos(a)));v.Add(new Vector3(Mathf.Sin(a),.5f,-Mathf.Cos(a)));
                u.Add(new Vector2(n/10f,0));u.Add(new Vector2(n/10f,1));
                if(n<10){int k=n*2;t.AddRange(new[]{k,k+1,k+2,k+2,k+1,k+3});}
            }
        });
        HandmadeProp("curved parody skincare label",label,new Vector3(0,h*.48f,0),new Vector3(r+.0008f,h*.46f,r+.0008f),ink,root);
        SetShoppedFacing(root,p,yaw,31+type,variant);root=old;
    }

    static void BuildBeautyShop()
    {
        var cabinet=MallFinish("washable",new Color(.48f,.42f,.35f),0,.26f);
        foreach(int side in new[]{-1,1}){
            Box("beauty wall display backing",new Vector3(side*6.3f,1.43f,8),new Vector3(.18f,2.75f,10.2f),cabinet);
            for(int bay=0;bay<3;bay++)for(int tier=0;tier<4;tier++){
                float z=4.6f+bay*3.15f,y=.48f+tier*.49f;
                Box("beauty adjustable shelf",new Vector3(side*6,y,z),new Vector3(.70f,.04f,2.75f),priceWhite);
                int product=bay*2+(tier>=2?1:0);
                for(int facing=0;facing<18;facing++)for(int depth=0;depth<2;depth++){
                    if(depth==0&&((bay*13+tier*7+(side+1)*3)%11==0)&&facing>=13&&facing<=14)continue;
                    CosmeticProduct(new Vector3(side*(5.78f+depth*.18f),y+.021f,z-1.19f+facing*.14f),product,(facing/6+tier)%3,side*90);
                }
            }
            foreach(float z in new[]{4.8f,9.8f}){
                ServiceCounter("beauty tester island",new Vector3(side*3.4f,0,z),new Vector3(1.1f,.92f,2.3f),cabinet);
                BeautyTesterAccessories(side*3.4f,z,side);
                for(int n=0;n<6;n++)CosmeticProduct(new Vector3(side*3.4f+(n%2==0?-.032f:.038f),1.002f,z-.8f+n*.32f+(n==3?.025f:0)),n,n%3,side*90+(n%2==0?-5:7));
                DetailRing("tester tabletop mirror frame",new Vector3(side*3.4f,1.30f,z+.98f),new Vector3(.31f,.38f,.035f),PrepSteel(),root,new Vector3(0,side*90,0));
                Decor("tester tinted mirror",PrimitiveType.Sphere,new Vector3(side*3.4f,1.30f,z+.98f),new Vector3(.008f,.34f,.27f),cabinetGlass);
                RailSegment("table mirror supporting stem",new Vector3(side*3.4f,1.0f,z+.98f),new Vector3(side*3.4f,1.14f,z+.98f),.012f,PrepSteel(),root);
                Decor("weighted tabletop mirror base",PrimitiveType.Cylinder,new Vector3(side*3.4f,.999f,z+.98f),new Vector3(.18f,.009f,.18f),PrepSteel());
            }
        }
        ServiceCounter("beauty checkout",new Vector3(0,0,13.8f),new Vector3(6.4f,1.02f,.85f),cabinet);
        RetailTerminal(new Vector3(-1.3f,1.10f,13.8f),0,root);
        ServiceCaption("모아 뷰티  /  SKIN & CARE",new Vector3(0,2.8f,15.75f),.40f,Color.white);
    }

    static void Spectacles(Vector3 p,int variant,float yaw)
    {
        var old=root;root=new GameObject("optical frame pair").transform;root.SetParent(old);
        var finish=variant%2==0?PrepSteel():RetailSurface("acetate spectacle frame",new Color(.12f,.065f,.035f),0,.47f);
        for(int side=-1;side<=1;side+=2){
            DetailRing("spectacle rim",new Vector3(side*.032f,.024f,0),new Vector3(.054f,variant%3==0?.045f:.035f,.020f),finish);
            RailSegment("spectacle temple",new Vector3(side*.060f,.030f,0),new Vector3(side*.055f,.027f,.105f),.0027f,finish,root);
            RailSegment("curved temple end",new Vector3(side*.055f,.027f,.105f),new Vector3(side*.052f,.008f,.124f),.003f,finish,root);
        }
        RailSegment("spectacle bridge",new Vector3(-.009f,.030f,0),new Vector3(.009f,.030f,0),.0025f,finish,root);
        root.position=p;root.rotation=Quaternion.Euler(0,yaw,0);root=old;
    }

    static void BuildOpticalShop()
    {
        var wood=MallFinish("wood",new Color(.38f,.29f,.21f),0,.22f);
        foreach(int side in new[]{-1,1}){
            Box("optician illuminated display backing",new Vector3(side*6.55f,1.5f,8),new Vector3(.08f,2.7f,10.5f),cream);
            for(int row=0;row<4;row++)for(int bay=0;bay<3;bay++){
                float y=.67f+row*.43f,z=4.5f+bay*3.15f;
                Box("optical narrow shelf",new Vector3(side*6.32f,y,z),new Vector3(.48f,.026f,2.8f),wood);
                for(int item=0;item<12;item++)Spectacles(new Vector3(side*6.23f,y+.014f,z-1.22f+item*.22f),item+row,side*90);
            }
            foreach(float z in new[]{4.5f,9.5f}){
                ServiceCounter("optical glass display base",new Vector3(side*3.5f,0,z),new Vector3(1.5f,.78f,2.4f),wood);
                Decor("optical display glass top",PrimitiveType.Cube,new Vector3(side*3.5f,1.1f,z),new Vector3(1.52f,.012f,2.42f),cabinetGlass);
                foreach(float dz in new[]{-1.19f,1.19f})Decor("glass case end",PrimitiveType.Cube,new Vector3(side*3.5f,.97f,z+dz),new Vector3(1.5f,.25f,.014f),cabinetGlass);
                foreach(float dx in new[]{-.75f,.75f}){
                    Decor("optical display front glazing",PrimitiveType.Cube,new Vector3(side*3.5f+dx,.97f,z),new Vector3(.014f,.25f,2.4f),cabinetGlass);
                    foreach(float dz in new[]{-1.19f,1.19f})RailSegment("showcase corner support",new Vector3(side*3.5f+dx,.85f,z+dz),new Vector3(side*3.5f+dx,1.11f,z+dz),.009f,PrepSteel(),root);
                }
                for(int n=0;n<8;n++){
                    var p=new Vector3(side*3.5f,.855f,z-.91f+n*.26f);
                    MerchBox("optical presentation pad",p,new Vector3(.19f,.008f,.19f),cream);
                    Spectacles(p+Vector3.up*.005f,n,side*90);
                }
            }
        }
        ServiceCounter("optician fitting desk",new Vector3(0,0,13.7f),new Vector3(6.2f,.86f,1.1f),wood);
        RetailTerminal(new Vector3(-1.9f,.94f,13.7f),0,root);
        ServiceCaption("바른 안경  /  OPTICAL",new Vector3(0,2.8f,15.75f),.43f,Color.white);
        Box("vision chart board",new Vector3(4.7f,2,15.73f),new Vector3(1.05f,1.3f,.03f),priceWhite);
        ServiceCaption("C  E\nE  C  E\nC E C E\nE C E C E",new Vector3(4.7f,2,15.70f),.11f,Color.black);
    }

    static void DisplayPhone(Vector3 p,int variant,Quaternion rotation)
    {
        var old=root;root=new GameObject("display handset").transform;root.SetParent(old);
        var metal=RetailSurface("phone housing "+variant%3,variant%3==0?new Color(.12f,.14f,.16f):variant%3==1?new Color(.55f,.58f,.60f):new Color(.38f,.45f,.38f),.65f,.45f);
        MerchBox("rounded handset chassis",Vector3.zero,new Vector3(.075f,.156f,.008f),metal);
        MerchBox("inactive black glass screen",new Vector3(0,0,-.0045f),new Vector3(.068f,.146f,.001f),cabinetGlass);
        Decor("screen earpiece",PrimitiveType.Cube,new Vector3(0,.064f,-.0055f),new Vector3(.015f,.0015f,.001f),dark);
        Decor("handset side key",PrimitiveType.Cube,new Vector3(.038f,.023f,0),new Vector3(.0015f,.022f,.003f),metal);
        root.position=p;root.rotation=rotation;root=old;
    }

    static void BuildMobileShop()
    {
        var trim=MallFinish("enamel",new Color(.19f,.28f,.33f),.18f,.32f);
        foreach(int side in new[]{-1,1}){
            foreach(float z in new[]{4.5f,9.3f}){
                ServiceCounter("mobile demo island",new Vector3(side*3.4f,0,z),new Vector3(1.2f,.91f,2.7f),trim);
                for(int n=0;n<5;n++){
                    var p=new Vector3(side*3.4f,1.06f,z-1.04f+n*.50f);
                    MerchBox("handset security cradle",p-Vector3.up*.09f,new Vector3(.10f,.02f,.18f),priceWhite);
                    DisplayPhone(p,n,Quaternion.Euler(24,side*90,0));
                    RailSegment("security tether",p+Vector3.up*.02f,p+new Vector3(side*.12f,-.075f,0),.003f,shelf,root);
                }
            }
            Box("phone accessory wall",new Vector3(side*6.62f,1.5f,8),new Vector3(.08f,2.8f,10.5f),trim);
            for(int row=0;row<5;row++)for(int col=0;col<22;col++){
                var p=new Vector3(side*6.5f,.52f+row*.42f,3+col*.45f);
                Decor("accessory hanging card",PrimitiveType.Cube,p,new Vector3(.02f,.30f,.20f),col%3==0?cream:priceWhite);
                MerchBox("packaged phone case",p+new Vector3(-side*.025f,-.02f,0),new Vector3(.015f,.21f,.13f),col%3==0?trim:col%3==1?shelf:cream);
                RailSegment("accessory peg",p+Vector3.up*.14f,p+new Vector3(-side*.055f,.14f,0),.004f,PrepSteel(),root);
            }
        }
        ServiceCounter("mobile consultation desk",new Vector3(0,0,13.4f),new Vector3(6.4f,.80f,1.1f),trim);
        foreach(float x in new[]{-1.8f,1.8f})RetailTerminal(new Vector3(x,.88f,13.4f),0,root);
        ServiceCaption("픽셀 모바일  /  상담 · 수리 접수",new Vector3(0,2.8f,15.75f),.38f,Color.white);
    }

    static void BuildStorageShop()
    {
        var enamel=MallFinish("enamel",new Color(.43f,.48f,.42f),.18f,.25f);
        foreach(int side in new[]{-1,1})for(int bay=0;bay<8;bay++){
            float z=3.0f+bay*1.4f;
            Box("parcel locker carcass",new Vector3(side*5.9f,1.35f,z),new Vector3(1.2f,2.7f,1.32f),shelf);
            int cells=bay%3==0?2:bay%3==1?3:4;
            for(int row=0;row<cells;row++){
                float height=2.55f/cells,y=.12f+(row+.5f)*height;
                Decor("parcel locker door",PrimitiveType.Cube,new Vector3(side*5.285f,y,z),new Vector3(.035f,height-.025f,1.26f),enamel);
                Decor("locker latch recess",PrimitiveType.Cube,new Vector3(side*5.262f,y,z+.42f),new Vector3(.012f,.10f,.075f),dark);
                ServiceCaption((side<0?"A":"B")+(bay+1)+"-"+(row+1),new Vector3(side*5.26f,y+.15f,z),.085f,Color.white,new Vector3(0,side*90,0));
            }
        }
        ServiceCounter("parcel service desk",new Vector3(0,0,13.5f),new Vector3(6.2f,1.02f,1.05f),enamel);
        RetailTerminal(new Vector3(-1.5f,1.1f,13.5f),0,root);
        MerchBox("parcel platform scale",new Vector3(1.5f,1.14f,13.5f),new Vector3(.55f,.10f,.43f),PrepSteel());
        ServiceCaption("맡김  /  보관 · 택배 접수",new Vector3(0,2.8f,15.75f),.40f,Color.white);
        foreach(float x in new[]{-2.2f,2.2f}){
            Decor("collection parcel",PrimitiveType.Cube,new Vector3(x,.26f,15.15f),new Vector3(.85f,.52f,.64f),cream);
            Decor("parcel packing tape",PrimitiveType.Cube,new Vector3(x,.523f,15.15f),new Vector3(.08f,.005f,.64f),shelfTop);
        }
        MallBench(new Vector3(-3.3f,0,7),90);
    }

    static void BuildMallServiceShop(int kind)
    {
        if(kind==6)BuildBeautyShop();
        else if(kind==7)BuildOpticalShop();
        else if(kind==8)BuildMobileShop();
        else BuildStorageShop();
    }
}
