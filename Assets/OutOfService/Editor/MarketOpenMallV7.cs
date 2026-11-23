using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    // Original composite fit-out, in metres. Wide entrances expose the shop stock;
    // privacy rooms retain their screen and enclosed glazing. No invisible front wall.
    static float MallOpening(int kind) => kind switch {
        0=>10.8f,1=>10.8f,2=>4f,3=>6f,4=>12f,5=>7.2f,6=>11.2f,7=>8.4f,8=>10f,_=>7.2f };

    static void BuildOpenMallFrontage(int kind)
    {
        float opening=MallOpening(kind),wing=(14-opening)*.5f;
        foreach(int side in new[]{-1,1}){
            float x=side*(opening*.5f+wing*.5f);
            Box("storefront opaque knee wall",new Vector3(x,.30f,0),new Vector3(wing,.60f,.12f),kind==4?OakVeneer(new Color(.42f,.34f,.22f)):cream);
            foreach(float y in new[]{.60f,2.97f})Box("storefront horizontal glazing frame",new Vector3(x,y,0),new Vector3(wing,.045f,.08f),shelfTop);
            Box("storefront glass sidelight",new Vector3(x,1.78f,0),new Vector3(wing-.10f,2.29f,.02f),kind==2?glass:cabinetGlass);
            foreach(float edge in new[]{opening*.5f,6.97f})Box("store entrance jamb",new Vector3(side*edge,1.5f,0),new Vector3(.06f,3,.15f),shelfTop);
            Decor("shop window caution stripe",PrimitiveType.Cube,new Vector3(x,1.22f,-.020f),new Vector3(wing-.13f,.035f,.005f),cream);
            Decor("shopfront metal kickplate",PrimitiveType.Cube,new Vector3(x,.17f,-.073f),new Vector3(wing-.13f,.28f,.018f),PrepSteel());
        }
    }

    static void FinishOpenMallFrontage(int kind)
    {
        float opening=MallOpening(kind);
        var trim=MallFinish("enamel",new Color(.23f,.25f,.23f),.18f,.27f);
        var panels=MallFinish(kind==0||kind==4?"wood":"washable",kind==4?new Color(.35f,.39f,.26f):new Color(.47f,.43f,.35f),0,.20f);
        foreach(int side in new[]{-1,1}){
            Box("shopfront lined pilaster",new Vector3(side*6.88f,1.6f,-.14f),new Vector3(.24f,3.2f,.30f),panels);
            Decor("shopfront corner cap",PrimitiveType.Cube,new Vector3(side*6.73f,1.6f,-.305f),new Vector3(.035f,3.18f,.025f),trim);
            Decor("raised shutter side guide",PrimitiveType.Cube,new Vector3(side*(opening*.5f+.07f),1.47f,.10f),new Vector3(.06f,2.94f,.08f),trim);
        }
        Decor("raised storefront shutter pelmet",PrimitiveType.Cube,new Vector3(0,3.13f,-.08f),new Vector3(opening+.2f,.27f,.35f),trim);
        for(int slat=0;slat<3;slat++)Decor("raised shutter lower slat",PrimitiveType.Cube,new Vector3(0,3.04f+slat*.047f,-.267f),new Vector3(opening,.031f,.02f),PrepSteel());
        Decor("storefront soffit",PrimitiveType.Cube,new Vector3(0,3.99f,-.34f),new Vector3(14,.12f,.82f),panels);
        Decor("storefront fascia bottom reveal",PrimitiveType.Cube,new Vector3(0,2.99f,-.15f),new Vector3(13.7f,.025f,.06f),PrepSteel());
        Decor("flush entrance sill",PrimitiveType.Cube,new Vector3(0,.007f,.02f),new Vector3(opening-.12f,.014f,.22f),PrepSteel());
        LocalLamp("B1 tenant doorway practical",new Vector3(0,2.94f,-.24f),new Vector3(0,.7f,.8f),new Color(.92f,.79f,.58f),1.15f,5);
    }

    static void OpenRetailTable(string name,Vector3 p,Vector2 footprint,float height,Material finish)
    {
        MerchBox(name+" rounded worktop",p+Vector3.up*(height-.035f),new Vector3(footprint.x,.07f,footprint.y),finish,root,true);
        foreach(int side in new[]{-1,1}){
            Box(name+" trestle foot",p+new Vector3(side*(footprint.x*.5f-.13f),.065f,0),new Vector3(.08f,.13f,footprint.y-.18f),shelf);
            Box(name+" trestle upright",p+new Vector3(side*(footprint.x*.5f-.13f),height*.5f,0),new Vector3(.06f,height-.10f,.06f),shelf);
        }
        Box(name+" stock shelf",p+Vector3.up*.22f,new Vector3(footprint.x-.18f,.035f,footprint.y-.13f),finish);
        RailSegment(name+" lower brace",p+new Vector3(-footprint.x*.5f+.13f,.20f,0),p+new Vector3(footprint.x*.5f-.13f,.20f,0),.025f,shelf,root);
    }

    static void FloristIsland(Vector3 p,bool tall)
    {
        var timber=OakVeneer(new Color(.42f,.34f,.22f));
        OpenRetailTable("florist low botanical table",p,new Vector2(1.8f,1.15f),tall?.66f:.43f,timber);
        float h=tall?.66f:.43f;
        for(int n=0;n<5;n++){
            var foot=p+new Vector3((n%3-1)*.49f,h,(n/3-.5f)*.49f);
            PottedFoliage(foot,n%2,.34f+(n%3)*.06f);
        }
        foreach(float x in new[]{-.48f,.48f}){
            var foot=p+new Vector3(x,.239f,0);
            Decor("nested terracotta sales pots",PrimitiveType.Cylinder,foot+Vector3.up*.09f,new Vector3(.22f,.09f,.22f),RetailSurface("unglazed terracotta clay",new Color(.45f,.24f,.14f),0,.10f));
            DetailRing("terracotta pot rolled rim",foot+Vector3.up*.18f,new Vector3(.23f,.23f,.045f),timber,null,new Vector3(90,0,0));
        }
    }

    static void TenantFrontMerchandising(int kind)
    {
        if(kind==2)return;
        var timber=OakVeneer(new Color(.50f,.40f,.27f));
        // Local z >= .5 keeps the entire mall corridor free. The middle 5m stays open.
        var p=new Vector3(-4.8f,0,1.1f);
        if(kind==0){
            OpenRetailTable("coffee bakery pickup",p,new Vector2(2.1f,1),.89f,timber);
            foreach(int side in new[]{-1,1}){
                var tray=p+new Vector3(side*.48f,.895f,0);
                HandmadeProp("bakery pickup pressed tray",ProduceShell(false,false),tray,new Vector3(.87f,.035f,.76f),wornMetal,root);
                for(int n=0;n<4;n++){
                    var q=tray+new Vector3((n%2-.5f)*.38f,.012f,(n/2-.5f)*.32f);
                    HandmadeProp("cafe scored bread",BreadForm(side<0),q,side<0?new Vector3(.22f,.13f,.13f):new Vector3(.33f,.085f,.095f),BreadCrust(side<0),root).transform.Rotate(0,n%2==0?-7:4,0);
                }
            }
            MerchBox("bakery sneeze screen",p+new Vector3(0,1.17f,-.46f),new Vector3(2.08f,.55f,.012f),cabinetGlass);
            foreach(int side in new[]{-1,1})RailSegment("bakery guard upright",p+new Vector3(side*1.02f,.89f,-.46f),p+new Vector3(side*1.02f,1.45f,-.46f),.018f,PrepSteel(),root);
            foreach(float x in new[]{-.70f,-.24f,.24f,.70f})MerchBox("coffee takeaway bag",p+new Vector3(x,.38f,0),new Vector3(.23f,.28f,.25f),cream);
        }else if(kind==1){
            ServiceCounter("restaurant cutlery and pickup station",p,new Vector3(1.9f,.88f,.9f),timber);
            for(int n=0;n<4;n++)HandmadeProp("clean serving tray",ProduceShell(false,false),p+new Vector3(-.45f,.96f+n*.026f,0),new Vector3(.49f,.035f,.34f),dark,root);
            foreach(float x in new[]{.19f,.48f,.77f})CeramicCup(p+new Vector3(x,.95f,0));
            MerchBox("cutlery napkin dispenser",p+new Vector3(.48f,1.08f,.25f),new Vector3(.27f,.25f,.15f),shelfTop);
        }else if(kind==4){
            FloristIsland(new Vector3(-4.6f,0,1.15f),false);
            FloristIsland(new Vector3(4.6f,0,1.15f),true);
            FloristIsland(new Vector3(-2.7f,0,8.3f),true);
            foreach(float z in new[]{6.6f,9.9f})CutFlowerBucket(new Vector3(-2.7f,0,z),z<8?1:3,1,(int)(z*13));
            foreach(float x in new[]{-4.6f,4.6f})LocalLamp("florist frontage plant pool",new Vector3(x,3.25f,.35f),new Vector3(x,.9f,1.15f),new Color(.86f,.92f,.72f),1.2f,4.6f);
        }else if(kind==5){
            OpenRetailTable("laundry collection sorting bench",p,new Vector2(1.9f,1),.87f,timber);
            for(int col=0;col<4;col++)FoldedClothStack(p+new Vector3((col-1.5f)*.41f,.875f,0),col%3,col%3,root);
            foreach(float x in new[]{-.46f,.46f})PrepCrate(p+new Vector3(x,.239f,0),new Vector3(.68f,.36f,.73f),cream,root);
        }else if(kind==6){
            OpenRetailTable("beauty open entrance tester table",p,new Vector2(2,1),.88f,timber);
            for(int col=0;col<10;col++)for(int row=0;row<2;row++)CosmeticProduct(p+new Vector3((col-4.5f)*.17f,.885f,(row-.5f)*.36f),col%6,col/4,0);
        }else if(kind==7){
            OpenRetailTable("optical entrance frames",p,new Vector2(1.8f,.95f),.94f,timber);
            for(int col=0;col<6;col++)for(int row=0;row<2;row++){
                var q=p+new Vector3((col-2.5f)*.26f,.947f,(row-.5f)*.35f);
                MerchBox("frame display felt",q,new Vector3(.23f,.009f,.25f),fabric);Spectacles(q+Vector3.up*.005f,col+row,0);
            }
        }else if(kind==8){
            ServiceCounter("mobile entrance repair pickup",p,new Vector3(1.8f,.88f,.9f),shelf);
            for(int n=0;n<5;n++){
                var q=p+new Vector3((n-2)*.29f,1.04f,0);
                MerchBox("front phone security pedestal",q-Vector3.up*.085f,new Vector3(.13f,.03f,.19f),cream);
                DisplayPhone(q,n,Quaternion.Euler(24,0,0));
            }
        }else if(kind==3){
            OpenRetailTable("pharmacy low health display",p,new Vector2(1.7f,.9f),.83f,priceWhite);
            for(int shelf=0;shelf<2;shelf++)for(int n=0;n<9;n++)for(int depth=0;depth<2;depth++)
                PharmacyStock(p+new Vector3((n-4)*.16f,shelf==0?.239f:.835f,(depth-.5f)*.25f),n/3,1,root);
        }else{
            OpenRetailTable("parcel packing station",p,new Vector2(1.8f,1),.89f,timber);
            foreach(float x in new[]{-.48f,.35f})MerchBox("service paper parcel",p+new Vector3(x,1.08f,0),new Vector3(.48f,.38f,.56f),cream);
            for(int n=0;n<4;n++)MerchBox("flat folded shipping cartons",p+new Vector3(0,.26f+n*.035f,0),new Vector3(1.28f,.025f,.72f),timber);
        }
        if(kind!=4)LocalLamp("tenant front merchandise pool",new Vector3(-4.8f,3.35f,.45f),p+Vector3.up*.8f,new Color(.94f,.84f,.65f),1.3f,4.8f);
    }

    static void UpperFoldIsland(Vector3 p,int style,bool nesting)
    {
        var previous=root;root=new GameObject(nesting?"2F nesting apparel display":"2F two sided folded apparel gondola").transform;root.SetParent(previous);
        var timber=OakVeneer(new Color(.59f,.51f,.39f));
        if(nesting){
            OpenRetailTable("large knit nesting table",new Vector3(-.3f,0,.18f),new Vector2(2.6f,1.25f),.88f,timber);
            OpenRetailTable("low knit nesting table",new Vector3(.4f,0,-1.05f),new Vector2(1.65f,.85f),.56f,timber);
            for(int row=0;row<2;row++)for(int col=0;col<5;col++)FoldedClothStack(new Vector3(-1.24f+col*.47f,.885f,.18f+(row-.5f)*.55f),style,col%3,root);
            for(int col=0;col<4;col++)FoldedClothStack(new Vector3(-.2f+col*.40f,.565f,-1.05f),style,col%3,root);
            for(int col=0;col<4;col++)FoldedClothStack(new Vector3(-1+col*.48f,.239f,.18f),style,(col+1)%3,root);
        }else{
            Box("fold gondola recessed plinth",new Vector3(0,.09f,0),new Vector3(2.75f,.18f,1.15f),shelf);
            foreach(float x in new[]{-1.45f,0,1.45f})Box("open knit bay divider",new Vector3(x,.68f,0),new Vector3(.045f,1.18f,1.32f),timber);
            Box("gondola center stock divider",new Vector3(0,.64f,0),new Vector3(2.88f,1.05f,.035f),timber);
            foreach(float y in new[]{.20f,.59f,1.18f}){
                MerchBox("fold stock rounded shelf",new Vector3(0,y,0),new Vector3(2.96f,.04f,1.36f),timber,root,true);
                for(int side=-1;side<=1;side+=2)for(int n=0;n<6;n++){
                    if(y==.59f&&n==4&&side==1)continue; // one intentional low-stock bay
                    FoldedClothStack(new Vector3((n-2.5f)*.45f,y+.023f,side*.36f),style,(n/2)%3,root);
                }
            }
            Box("fold display header post",new Vector3(0,1.55f,0),new Vector3(.025f,.7f,.025f),shelf);
            Box("fold display small category card",new Vector3(0,1.88f,0),new Vector3(1.30f,.28f,.025f),cream);
            Label(style==0?"SOOIM  /  니트":style==1?"KIDS  /  데일리":"BASIC  /  코튼",new Vector3(0,1.88f,-.016f),Vector3.zero,.085f);
            Label(style==0?"SOOIM  /  니트":style==1?"KIDS  /  데일리":"BASIC  /  코튼",new Vector3(0,1.88f,.016f),new Vector3(0,180,0),.085f);
        }
        LocalLamp("fold island limited downlight",new Vector3(0,3.35f,-.7f),new Vector3(0,.8f,0),new Color(.96f,.86f,.70f),1.45f,4.5f);
        root.position=p;root=previous;
    }

    static void OpenMallAndUpperInfill()
    {
        // Fill gaps between the existing garment rows, not their picking aisles.
        foreach(float x in new[]{-32.5f,-22.5f,-12.5f})foreach(float z in new[]{-23.5f,9.5f})UpperFoldIsland(new Vector3(x,5,z),x==-12.5f?2:z<0?0:1,false);
        foreach(float x in new[]{-32.5f,-22.5f})foreach(float z in new[]{-1.5f,20.5f})UpperFoldIsland(new Vector3(x,5,z),z<0?0:1,true);
        // East arrival area becomes a travel-accessory department; escalator landings remain open.
        foreach(var p in new[]{new Vector3(38,5,-22),new Vector3(44,5,-14)}){
            var old=root;root=new GameObject("2F travel arrival display").transform;root.SetParent(old);
            var timber=OakVeneer(new Color(.45f,.37f,.28f));
            OpenRetailTable("luggage low presentation platform",Vector3.zero,new Vector2(3.3f,1.8f),.31f,timber);
            for(int n=0;n<5;n++)TravelAccessory(new Vector3((n-2)*.59f,.315f,.32f),n%4,true,root);
            OpenRetailTable("small leather accessories console",new Vector3(0,0,-1.8f),new Vector2(2.6f,.76f),.85f,timber);
            for(int n=0;n<6;n++)TravelAccessory(new Vector3((n-2.5f)*.40f,.855f,-1.8f),n%4,false,root);
            LocalLamp("travel arrival practical",new Vector3(0,3.5f,-.5f),new Vector3(0,.8f,-.5f),new Color(.87f,.88f,.81f),1.8f,5.2f);
            root.position=p;root=old;
        }
        // A small florist pop-up and a seated resting pocket animate the wide concourse.
        var previous=root;root=new GameObject("B1 florist concourse pop up").transform;root.SetParent(previous);
        FloristIsland(Vector3.zero,false);CutFlowerBucket(new Vector3(1.25f,0,.15f),2,1,118);
        LocalLamp("concourse botanical display pool",new Vector3(0,3.7f,0),new Vector3(0,.7f,0),new Color(.85f,.91f,.72f),1.1f,4.6f);
        root.position=new Vector3(-4.8f,-5,14);root=previous;
        MallBench(new Vector3(-4.8f,-5,-6),90);
        PottedFoliage(new Vector3(-4.8f,-5,-8),1,.65f);
    }
}
