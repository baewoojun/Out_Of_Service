using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using OutOfService;

public static partial class MarketPrototypeBuilderV7
{
    // All coordinates are authored in metres. B1/1F/2F walking surfaces are -5/0/5.
    // The existing retail fixtures stay on 1F. Openings are actual missing slabs,
    // not invisible portals or teleports through the original solid floor.
    static void FloorRect(string name,float x0,float x1,float z0,float z1,float top)
    {
        Box(name,new Vector3((x0+x1)*.5f,top-.20f,(z0+z1)*.5f),new Vector3(x1-x0,.4f,z1-z0),floor);
    }

    static void BuildGroundSlabs()
    {
        FloorRect("1F main floor",-43,31,-45,45,0);
        FloorRect("1F west rear floor",-50,-43,-33,45,0);
        FloorRect("1F west front apron",-50,-43,-45,-42,0);
        FloorRect("1F west stair outer rim",-50,-49,-42,-33,0);
        FloorRect("1F east rear floor",31,50,5,45,0);
        FloorRect("1F east front floor",31,50,-33,-5,0);
        FloorRect("1F east front apron",31,50,-45,-42,0);
        FloorRect("1F east stair inner apron",31,43,-42,-33,0);
        FloorRect("1F east stair outer rim",49,50,-42,-33,0);
        FloorRect("1F escalator west opening edge",31,33.5f,-5,5,0);
        FloorRect("1F escalator central opening infill",35.5f,40.5f,-5,5,0);
        FloorRect("1F escalator east opening edge",42.5f,50,-5,5,0);
    }

    static void BuildThreeFloorExtension()
    {
        BuildStairTower(-46,"WEST");BuildStairTower(46,"EAST");
        BuildBasementMall();BuildBasementEscalators();
        BuildAppliances();BuildCustomerService();BuildFamilyRoom();HomewareRetailCorner();
        foreach(float y in new[]{-5f,0f,5f}){
            string level=y<0?"B1":y<1?"1F":"2F";
            for(int side=-1;side<=1;side+=2){
                var p=new Vector3(side*39,y+2.95f,-33.9f);
                Box(level+" stair direction board",p,new Vector3(4,.55f,.08f),green);
                Label(side<0?"← 비상계단  B1 · 1F · 2F":"B1 · 1F · 2F  비상계단 →",p+Vector3.back*.06f,Vector3.zero,.21f);
            }
        }
    }

    static void LocalLamp(string name,Vector3 position,Vector3 aim,Color tint,float intensity,float range,bool shadows=false)
    {
        var lamp=new GameObject(name).AddComponent<Light>();lamp.transform.SetParent(root);
        lamp.transform.position=position;
        // Recessed downlights cannot throw the edge of their cone upward through
        // a non-shadowed ceiling. Keep the entire 116-degree cone below horizontal.
        var direction=Vector3.RotateTowards(Vector3.down,(aim-position).normalized,25*Mathf.Deg2Rad,0);
        lamp.transform.rotation=Quaternion.LookRotation(direction);lamp.type=LightType.Spot;
        lamp.color=tint;lamp.range=range;lamp.intensity=intensity;lamp.spotAngle=116;lamp.innerSpotAngle=70;
        lamp.shadows=shadows?LightShadows.Soft:LightShadows.None;
        if(shadows){
            lamp.shadowBias=.03f;lamp.shadowNormalBias=.06f;
            var data=new SerializedObject(lamp.GetUniversalAdditionalLightData());
            data.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=1;data.ApplyModifiedPropertiesWithoutUndo();
        }
        Decor(name+" recessed fitting",PrimitiveType.Cylinder,position,new Vector3(.32f,.045f,.32f),shelf);
        Decor(name+" opal diffuser",PrimitiveType.Cylinder,position-Vector3.up*.03f,new Vector3(.25f,.008f,.25f),fluorescent);
    }

    static void SafetyDoor(string name,Vector3 hingePoint,float width,float angle=100)
    {
        Transform hinge=new GameObject(name).transform;hinge.SetParent(root);hinge.position=hingePoint;
        hinge.gameObject.AddComponent<MarketSwingDoor>().openAngle=angle;
        var p=hingePoint+new Vector3(width*.5f,1.22f,0);
        Box(name+" steel leaf",p,new Vector3(width-.04f,2.44f,.085f),cream,hinge);
        foreach(int side in new[]{-1,1}){
            Decor(name+" kick plate",PrimitiveType.Cube,p+new Vector3(0,-.93f,side*.052f),new Vector3(width-.14f,.38f,.015f),shelfTop,hinge);
            Decor(name+" push bar",PrimitiveType.Cube,p+new Vector3(0,-.12f,side*.09f),new Vector3(width-.28f,.045f,.045f),shelfTop,hinge);
            Decor(name+" safety vision panel",PrimitiveType.Cube,p+new Vector3(0,.58f,side*.049f),new Vector3(.24f,.50f,.012f),glass,hinge);
        }
        foreach(float y in new[]{.3f,1.15f,2.1f})Decor(name+" hinge barrel",PrimitiveType.Cylinder,hingePoint+new Vector3(.025f,y,0),new Vector3(.045f,.06f,.045f),shelfTop,hinge);
        Box(name+" closer",hingePoint+new Vector3(width*.55f,2.36f,.075f),new Vector3(.28f,.06f,.10f),shelfTop,hinge);
    }

    static void BuildStairTower(float x,string side)
    {
        Transform previous=root;root=new GameObject(side+" emergency stairs B1-1F-2F").transform;root.SetParent(previous);
        var treadFinish=new Material(wornFloor){name="cast stair concrete"};
        treadFinish.SetTextureScale("_BaseMap",new Vector2(1.1f,.167f));
        treadFinish.SetTextureScale("_BumpMap",new Vector2(1.1f,.167f));
        treadFinish.SetFloat("_BumpScale",.18f);
        Box(side+" stair west enclosure",new Vector3(x-3,-.0f,-37.5f)+Vector3.up*2.25f,new Vector3(.18f,14.5f,9),wall);
        Box(side+" stair east enclosure",new Vector3(x+3,2.25f,-37.5f),new Vector3(.18f,14.5f,9),wall);
        Box(side+" stair back enclosure",new Vector3(x,2.25f,-42),new Vector3(6,14.5f,.18f),wall);
        foreach(float y in new[]{-5f,0f,5f}){
            string level=y<0?"B1":y<1?"1F":"2F";
            FloorRect(side+" "+level+" landing",x-2.91f,x+2.91f,-35,-33,y);
            Box(side+" "+level+" entry wall left",new Vector3(x-1.95f,y+2.25f,-33),new Vector3(2.1f,4.5f,.18f),wall);
            Box(side+" "+level+" entry wall right",new Vector3(x+1.95f,y+2.25f,-33),new Vector3(2.1f,4.5f,.18f),wall);
            Box(side+" "+level+" entry lintel",new Vector3(x,y+3.5f,-33),new Vector3(1.8f,2,.18f),wall);
            SafetyDoor(side+" "+level+" fire door",new Vector3(x-.9f,y,-33),1.8f,-100);
            Box(side+" "+level+" luminous exit board",new Vector3(x,y+2.92f,-32.85f),new Vector3(1.7f,.39f,.08f),green);
            Label(level+"  비상계단",new Vector3(x,y+2.92f,-32.79f),new Vector3(0,180,0),.22f);
            Label(level+"  EXIT",new Vector3(x,y+2.9f,-33.12f),Vector3.zero,.24f);
            LocalLamp(side+" "+level+" stair lamp",new Vector3(x,y+3.65f,-34),new Vector3(x,y,-36.5f),new Color(.72f,.87f,.78f),3.2f,9);
            Box(side+" "+level+" handrail across void",new Vector3(x,y+1,-35.02f),new Vector3(.6f,.07f,.07f),shelfTop);
            Decor(side+" floor identifier band",PrimitiveType.Cube,new Vector3(x-2.89f,y+1.18f,-38),new Vector3(.015f,.8f,3.8f),green);
            Label(level,new Vector3(x-2.87f,y+1.2f,-38),new Vector3(0,-90,0),.8f);
        }
        for(int level=0;level<2;level++){
            float baseY=-5+level*5;
            FloorRect(side+" mid landing "+level,x-2.91f,x+2.91f,-42,-40,baseY+2.5f);
            LocalLamp(side+" mid landing practical",new Vector3(x,baseY+4.65f,-41.2f),new Vector3(x,baseY+2.5f,-41.2f),new Color(.72f,.87f,.78f),1.8f,6);
            for(int flight=0;flight<2;flight++){
                float cx=x+(flight==0?-1.5f:1.5f),startZ=flight==0?-35:-40,dir=flight==0?-1:1,low=baseY+flight*2.5f;
                for(int step=0;step<15;step++){
                    float z=startZ+dir*(step+.5f)/3f,y=low+(step+1)/6f;
                    Box(side+" concrete stair tread",new Vector3(cx,y-.08334f,z),new Vector3(2.2f,.16668f,1f/3f+.002f),treadFinish);
                    Decor(side+" stair anti slip nosing",PrimitiveType.Cube,new Vector3(cx,y+.003f,z-dir*.14f),new Vector3(2.16f,.006f,.045f),dark);
                }
                foreach(int edge in new[]{-1,1}){
                    Vector3 a=new Vector3(cx+edge*1.12f,low+1,startZ),b=new Vector3(a.x,low+3.5f,startZ+dir*5);
                    RailSegment(side+" stair handrail",a,b,.045f,shelfTop,root);
                    // Side barriers follow each flight and stop above walking clearance.
                    var barrier=Box(side+" stair guard infill",(a+b)*.5f-Vector3.up*.40f,new Vector3(.055f,.80f,5.59f),shelf);
                    barrier.transform.rotation=Quaternion.Euler(dir<0?26.565f:-26.565f,0,0);
                    for(int post=0;post<=5;post++){
                        var p=Vector3.Lerp(a,b,post/5f);
                        Box(side+" guard post",p-Vector3.up*.48f,new Vector3(.05f,.96f,.05f),shelfTop);
                    }
                }
            }
        }
        // The 2F opening above the first flight needs a guard where no next flight starts.
        Box(side+" top landing guard",new Vector3(x-1.5f,6,-35.02f),new Vector3(2.2f,1.1f,.07f),shelf);
        root=previous;
    }

    static void BuildBasementEscalators()
    {
        // Reuse the measured 1F-2F pair geometry at the next storey, including its colliders.
        Transform group=new GameObject("B1 public escalators to 1F").transform;group.SetParent(root);
        var originals=new List<Transform>();
        foreach(Transform child in root){
            string n=child.name;
            if(n.StartsWith("escalator ")||n=="step anti slip groove"||n=="rounded handrail return"||
                n=="lower comb plate"||n=="upper comb plate"||n=="comb safety yellow edge")originals.Add(child);
        }
        foreach(var source in originals){var copy=Object.Instantiate(source.gameObject,group);copy.name="B1 "+source.name;copy.transform.position=source.position+Vector3.down*5;}
        Label("1F  식품 · 생활 · 가전",new Vector3(38,-1.6f,-8),Vector3.zero,.43f);
        Label("B1  카페 · 식당 · 편의시설",new Vector3(38,2.6f,8),new Vector3(0,180,0),.40f);
        LocalLamp("B1 escalator landing lamp",new Vector3(38,-1.25f,-7.2f),new Vector3(38,-4.2f,-3),new Color(.96f,.79f,.53f),4.8f,12,true);
    }

    static void BuildBasementMall()
    {
        FloorRect("B1 mall floor",-50,50,-45,45,-5);
        Box("B1 west enclosure",new Vector3(-50,-2.5f,0),new Vector3(.5f,5,90),wall);
        Box("B1 east enclosure",new Vector3(50,-2.5f,0),new Vector3(.5f,5,90),wall);
        Box("B1 rear enclosure",new Vector3(0,-2.5f,45),new Vector3(100,5,.5f),wall);
        Box("B1 front enclosure",new Vector3(0,-2.5f,-45),new Vector3(100,5,.5f),wall);
        for(int x=-40;x<=40;x+=20)for(int z=-30;z<=30;z+=20){
            Box("B1 column",new Vector3(x,-2.7f,z),new Vector3(.8f,4.6f,.8f),wall);
            Box("B1 column bumper",new Vector3(x,-4.45f,z),new Vector3(.85f,1.1f,.85f),cream);
        }
        MallUnit("모퉁이 커피",new Vector3(-10,-5,-16),-90,0);
        MallUnit("한끼 식당",new Vector3(-10,-5,4),-90,1);
        MallUnit("화장실",new Vector3(-10,-5,24),-90,2);
        MallUnit("온유 약국",new Vector3(10,-5,-16),90,3);
        MallUnit("잎새 화원",new Vector3(10,-5,4),90,4);
        MallUnit("바늘 세탁·수선",new Vector3(10,-5,24),90,5);
        BasementConcourseFinish();
        BasementOccupancy();
        for(int z=-30;z<=36;z+=11){
            LocalLamp("B1 concourse light",new Vector3(0,-.85f,z),new Vector3(0,-5,z),new Color(.96f,.81f,.60f),4.6f,12);
            if(z%2==0)MallBench(new Vector3(6,-5,z),90);
        }
        foreach(int side in new[]{-1,1})for(int z=-29;z<=35;z+=16)
            LocalLamp("B1 secondary route light",new Vector3(side*36,-.85f,z),new Vector3(side*36,-5,z),new Color(.72f,.83f,.90f),3.1f,11);
        Box("B1 overhead directory backing",new Vector3(0,-1.65f,-27),new Vector3(10,.95f,.12f),green);
        Label("B1  몰 · 편의시설",new Vector3(0,-1.5f,-27.08f),Vector3.zero,.52f);
        Label("카페 · 식당 · 약국 · 화원 · 세탁 · 화장실",new Vector3(0,-1.91f,-27.08f),Vector3.zero,.22f);
        Box("B1 emergency supply cabinet",new Vector3(-9,-4.8f,-25),new Vector3(.8f,.4f,.65f),shelf);
        AddBattery(new Vector3(-9,-4.24f,-25));
    }

    static void MallBench(Vector3 p,float yaw)
    {
        ServiceSeating(p,yaw,root.name.Contains("customer service") || root.name.Contains("맡김"));
    }

    static void DiningSet(Vector3 p)
    {
        RetailDiningSet(p,root.name.Contains("식당"));
    }

    static void MallUnit(string title,Vector3 location,float yaw,int kind)
    {
        var old=root;root=new GameObject("B1 tenant - "+title).transform;root.SetParent(old);
        Box("tenant west partition",new Vector3(-7,2,8),new Vector3(.20f,4,16),wall);
        Box("tenant east partition",new Vector3(7,2,8),new Vector3(.20f,4,16),wall);
        Box("tenant back partition",new Vector3(0,2,16),new Vector3(14,4,.20f),wall);
        Box("tenant sign fascia",new Vector3(0,3.45f,0),new Vector3(14,.9f,.18f),kind==3?green:kind==4?cream:shelf);
        Label(title,new Vector3(0,3.44f,-.12f),Vector3.zero,.64f);
        foreach(int side in new[]{-1,1}){
            Box("storefront opaque knee wall",new Vector3(side*4.5f,.33f,0),new Vector3(5,.66f,.12f),cream);
            foreach(float y in new[]{.65f,3.06f})Box("storefront horizontal glazing frame",new Vector3(side*4.5f,y,0),new Vector3(5,.055f,.08f),shelfTop);
            foreach(float x in new[]{side*2.03f,side*6.97f})Box("storefront vertical glazing frame",new Vector3(x,1.85f,0),new Vector3(.055f,2.45f,.08f),shelfTop);
            Box("storefront glass",new Vector3(side*4.5f,1.85f,0),new Vector3(4.86f,2.36f,.02f),kind==2?glass:cabinetGlass);
        }
        // A four-metre public entrance; no mandatory cooperative switches.
        foreach(int side in new[]{-1,1})Box("store entrance jamb",new Vector3(side*1.98f,1.5f,0),new Vector3(.07f,3,.15f),shelfTop);
        for(int i=0;i<2;i++)LocalLamp(title+" practical",new Vector3(i==0?-3:3,3.75f,8),new Vector3(0,.8f,7),kind==3?new Color(.80f,.90f,1):new Color(1,.78f,.49f),3.9f,11,i==0);
        if(kind==0 || kind==1){
            for(int row=0;row<3;row++)foreach(float x in new[]{-5.4f,-2.7f,2.7f,5.4f})DiningSet(new Vector3(x,0,3+row*3));
            Box("service counter plinth",new Vector3(0,.52f,12),new Vector3(9,1.04f,1.1f),cream);
            Box("service counter top",new Vector3(0,1.09f,12),new Vector3(9.15f,.12f,1.18f),shelfTop);
            Box("kitchen access partition left",new Vector3(-2,2,13.5f),new Vector3(10,4,.12f),wall);
            Box("kitchen access partition right",new Vector3(6.1f,2,13.5f),new Vector3(1.8f,4,.12f),wall);
            Box("kitchen doorway lintel",new Vector3(4.3f,3.25f,13.5f),new Vector3(1.8f,1.5f,.12f),wall);
            SafetyDoor("staff kitchen door - "+title,new Vector3(3.4f,0,13.5f),1.8f);


            for(int n=0;n<5;n++)CeramicCup(new Vector3(-3.2f+n*.15f,1.151f,12));
            if(kind==0)CoffeeMachine(new Vector3(2,1.15f,12));
            else HeatedDisplay(new Vector3(2,1.15f,12));
            RetailTerminal(new Vector3(-.8f,1.165f,11.9f),0,root);
            Box("tenant menu",new Vector3(-1,2.7f,13.38f),new Vector3(5.2f,1.05f,.04f),dark);
            MerchArt("tenant printed menu",kind==0?8:14,new Vector3(-1,2.7f,13.35f),new Vector2(2.1f,.95f),Vector3.zero);
            BuildCafeDetails(kind);
        }else if(kind==2)BuildMallToilets();
        else if(kind==3){
            Box("pharmacy counter",new Vector3(0,.52f,10),new Vector3(8,1.04f,.9f),priceWhite);
            Box("pharmacy dispensing partition",new Vector3(0,2,12.8f),new Vector3(9,4,.12f),wall);
            Label("조제실  /  처방 접수",new Vector3(0,2.7f,12.70f),Vector3.zero,.40f);
            for(int side=-1;side<=1;side+=2)for(int tier=0;tier<6;tier++){
                Box("pharmacy glass shelf",new Vector3(side*5.5f,.6f+tier*.33f,8),new Vector3(1.05f,.035f,8),shelfTop);
                Box("pharmacy shelf price strip",new Vector3(side*4.97f,.63f+tier*.33f,8),new Vector3(.025f,.055f,8),priceWhite);
                for(int n=0;n<48;n++){
                    int variant=(n/8+tier)%6;
                    if(n==45 && tier==2)continue;
                    for(int depth=0;depth<3;depth++){
                        var p=new Vector3(side*(5.10f+depth*.17f),.619f+tier*.33f,4.25f+n*.156f);
                        PharmacyStock(p,variant,-side,root);
                    }
                }
            }
        }else if(kind==4)BuildFloristDetails();
        else if(kind>=6)BuildMallServiceShop(kind);
        else BuildLaundryDetails();
        TenantFinish(kind);
        MallFacadeDetail(kind);
        root.position=location;root.rotation=Quaternion.Euler(0,yaw,0);root=old;
    }

    static void BuildMallToilets()
    {
        Box("washroom privacy return",new Vector3(0,1.8f,3),new Vector3(7,3.6f,.16f),wall);
        Box("washroom central privacy divider",new Vector3(0,1.8f,9.5f),new Vector3(.16f,3.6f,13),wall);
        for(int side=-1;side<=1;side+=2){
            Label(side<0?"여자 화장실":"남자 화장실",new Vector3(side*4.8f,2.8f,2.9f),Vector3.zero,.28f);
            for(int cubicle=0;cubicle<3;cubicle++){
                float z=6+cubicle*3;
                Box("washroom cubicle partition",new Vector3(side*4.9f,1.25f,z+1.4f),new Vector3(3.7f,2.5f,.08f),cream);
                foreach(int end in new[]{-1,1})Box("washroom cubicle front",new Vector3(side*3.15f,1.25f,z+end*.98f),new Vector3(.08f,2.5f,.86f),cream);
                var hinge=new GameObject("washroom stall door").transform;hinge.SetParent(root);hinge.position=new Vector3(side*3.15f,0,z-.50f);
                hinge.gameObject.AddComponent<MarketSwingDoor>().openAngle=side*95;
                Box("washroom door leaf",hinge.position+new Vector3(0,1.2f,.5f),new Vector3(.06f,2.2f,.98f),cream,hinge);
                Box("washroom door latch",hinge.position+new Vector3(-side*.055f,1.1f,.87f),new Vector3(.05f,.04f,.11f),shelfTop,hinge);
                Decor("toilet pedestal",PrimitiveType.Cylinder,new Vector3(side*5.8f,.20f,z),new Vector3(.34f,.20f,.46f),priceWhite);
                Decor("toilet ceramic bowl",PrimitiveType.Sphere,new Vector3(side*5.8f,.44f,z),new Vector3(.50f,.30f,.68f),priceWhite);
                Decor("toilet seat recess",PrimitiveType.Sphere,new Vector3(side*5.8f,.56f,z),new Vector3(.29f,.04f,.42f),dark);
                Box("flush tank",new Vector3(side*6.1f,.75f,z+.25f),new Vector3(.48f,.55f,.20f),priceWhite);
            }
            Box("washroom vanity",new Vector3(side*1.8f,.81f,10),new Vector3(1.2f,.17f,5),shelfTop);
            Box("washroom vanity cabinet",new Vector3(side*1.8f,.38f,10),new Vector3(1.03f,.76f,4.85f),cream);
            Box("washroom mirror backing",new Vector3(side*.10f,1.65f,10),new Vector3(.035f,1.2f,4.8f),shelfTop);
            for(int bowl=0;bowl<3;bowl++){
                var p=new Vector3(side*1.8f,.94f,8.4f+bowl*1.6f);
                Decor("washbasin bowl",PrimitiveType.Sphere,p,new Vector3(.68f,.15f,.56f),priceWhite);
                RailSegment("washbasin tap",p+new Vector3(side*.23f,0,0),p+new Vector3(side*.23f,.24f,0),.035f,shelfTop,root);
            }
        }
        Label("화장실",new Vector3(0,3.05f,2.9f),Vector3.zero,.28f);
    }

    static void Washer(Vector3 p)
    {
        MerchBox("display washing machine cabinet",p+Vector3.up*.46f,new Vector3(.69f,.92f,.69f),priceWhite,null,true);
        MerchBox("washing machine top lid seam",p+new Vector3(0,.899f,0),new Vector3(.694f,.007f,.694f),cream);
        var front=p+new Vector3(0,.43f,-.353f);
        Decor("washer drum dark recess",PrimitiveType.Cylinder,front,new Vector3(.45f,.006f,.45f),dark).transform.rotation=Quaternion.Euler(90,0,0);
        DetailRing("washer deep rubber gasket",front+Vector3.back*.019f,new Vector3(.445f,.445f,.29f),shelf);
        DetailRing("washer open chrome door rim",front+Vector3.back*.044f,new Vector3(.49f,.49f,.18f),wornMetal);
        Decor("washer convex smoked glass",PrimitiveType.Sphere,front+Vector3.back*.036f,new Vector3(.37f,.37f,.062f),glass);
        MerchBox("washer recessed door grip",front+new Vector3(.209f,0,-.057f),new Vector3(.028f,.12f,.021f),dark);
        MerchBox("washer control strip",p+new Vector3(0,.80f,-.352f),new Vector3(.58f,.13f,.018f),shelf);
        Decor("washer programme knob",PrimitiveType.Cylinder,p+new Vector3(-.15f,.80f,-.373f),new Vector3(.08f,.015f,.08f),shelfTop).transform.rotation=Quaternion.Euler(90,0,0);
        MerchArt("washer printed controls",10,p+new Vector3(.10f,.80f,-.365f),new Vector2(.25f,.11f),Vector3.zero);
        MerchBox("detergent drawer seam",p+new Vector3(-.21f,.67f,-.348f),new Vector3(.17f,.085f,.009f),cream);
        MerchBox("detergent drawer finger recess",p+new Vector3(-.21f,.651f,-.355f),new Vector3(.13f,.012f,.009f),shelf);
        MerchBox("washer lower service flap",p+new Vector3(.22f,.14f,-.349f),new Vector3(.14f,.12f,.01f),cream);
        Box("washer feet plinth",p+Vector3.up*.04f,new Vector3(.62f,.08f,.61f),dark);
    }

    static void BuildAppliances()
    {
        var old=root;root=new GameObject("1F appliance showroom").transform;root.SetParent(old);
        for(int i=0;i<4;i++){
            if(i%2==0)Washer(new Vector3(35+i*2.7f,0,18));
            else TopLoadWasher(new Vector3(35+i*2.7f,0,18));
            var p=new Vector3(35+i*2.7f,0,25);
            foreach(int side in new[]{-1,1})Box("refrigerator cabinet side",p+new Vector3(side*.38f,.95f,0),new Vector3(.04f,1.9f,.79f),i%2==0?shelfTop:priceWhite);
            Box("refrigerator cabinet back",p+new Vector3(0,.95f,.38f),new Vector3(.73f,1.9f,.04f),priceWhite);
            foreach(float h in new[]{.03f,.66f,1.88f})Box("refrigerator compartment floor",p+Vector3.up*h,new Vector3(.74f,.04f,.74f),priceWhite);
            for(int t=0;t<3;t++)Decor("refrigerator interior shelf",PrimitiveType.Cube,p+new Vector3(0,.88f+t*.31f,0),new Vector3(.70f,.015f,.66f),shelfTop);
            foreach(float h in new[]{.34f,1.27f}){
                var hinge=new GameObject("appliance fridge door - E").transform;hinge.SetParent(root);hinge.position=p+new Vector3(-.4f,h<1?0:.67f,-.41f);
                hinge.gameObject.AddComponent<MarketSwingDoor>().openAngle=105;
                Box("showroom hinged door",hinge.position+new Vector3(.4f,h<1?.32f:.61f,0),new Vector3(.79f,h<1?.63f:1.20f,.055f),priceWhite,hinge);
                Box("fridge inset handle",hinge.position+new Vector3(.70f,h<1?.39f:.60f,-.06f),new Vector3(.035f,.33f,.045f),shelfTop,hinge);
            }
            var stand=new Vector3(35+i*2.7f+.47f,.95f,17.7f);
            MerchBox("appliance price stand",stand,new Vector3(.24f,.17f,.014f),priceWhite);
            if(i%2==0)MerchArt("appliance price specification",11,stand+Vector3.back*.008f,new Vector2(.23f,.16f),Vector3.zero);
            else{
                var ink=RetailSurface("top load original specification",Color.white,0,.17f);
                ink.mainTexture=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/MallDetails/top-load-price.png");
                Decor("top load price specification",PrimitiveType.Quad,stand+Vector3.back*.008f,new Vector3(.23f,.16f,1),ink);
            }
            RailSegment("appliance label stand stem",stand-Vector3.up*.085f,new Vector3(stand.x,.03f,stand.z),.012f,shelfTop,root);
        }
        for(int i=0;i<3;i++){
            var p=new Vector3(35+i*4,0,11);
            Box("TV display table",p+Vector3.up*.55f,new Vector3(2.7f,.16f,1),cream);
            TableLegs(p,2.4f,.74f,.47f);
            float width=1.22f+i*.36f,height=width*9/16;
            if(i==1){
                Box("TV central pedestal",p+Vector3.up*.654f,new Vector3(.70f,.045f,.37f),shelfTop);
                Box("TV stand neck",p+Vector3.up*.74f,new Vector3(.10f,.17f,.065f),shelfTop);
            }else foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})
                RailSegment("TV paired angled foot",p+new Vector3(side*width*.30f,.80f,0),p+new Vector3(side*width*.36f,.65f,end*.17f),.025f,shelf,root);
            var center=p+Vector3.up*(.79f+height*.5f);
            MerchBox("TV thin rounded bezel",center,new Vector3(width,height,.055f),shelf);
            Decor("TV black screen",PrimitiveType.Quad,center+Vector3.back*.031f,new Vector3(width-.035f,height-.035f,1),glass);
        }
        Box("appliance department fascia",new Vector3(39.5f,3.2f,9.5f),new Vector3(10,.7f,.10f),shelf);
        Label("가전  /  냉장고 · 세탁기 · TV",new Vector3(39.5f,3.2f,9.43f),Vector3.zero,.37f);
        LocalLamp("appliance night display",new Vector3(39.5f,3.8f,19),new Vector3(39.5f,1.1f,20),new Color(.76f,.86f,1),4.3f,12,true);
        root=old;
    }

    static void TableLegs(Vector3 p,float width,float depth,float height)
    {
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
            Box("furniture steel leg",p+new Vector3(x*width*.5f,height*.5f,z*depth*.5f),new Vector3(.055f,height,.055f),shelfTop);
    }

    static void BuildCustomerService()
    {
        var old=root;root=new GameObject("2F customer service").transform;root.SetParent(old);
        Box("customer service counter",new Vector3(18,5.55f,-23),new Vector3(8,1.1f,1.15f),cream);
        Box("customer service worktop",new Vector3(18,6.14f,-23),new Vector3(8.1f,.12f,1.2f),shelfTop);
        for(int i=0;i<3;i++){
            var p=new Vector3(15.5f+i*2.5f,6.25f,-23);
            Box("service monitor stem",p+Vector3.up*.15f,new Vector3(.065f,.3f,.07f),shelfTop);
            Box("service terminal",p+Vector3.up*.37f,new Vector3(.5f,.32f,.045f),dark);
            Box("service keyboard",p+new Vector3(0,-.025f,.32f),new Vector3(.4f,.025f,.13f),dark);
            Box("document tray",p+new Vector3(.75f,-.015f,0),new Vector3(.31f,.035f,.40f),priceWhite);
        }
        DepartmentSign("service","고객센터","교환 · 환불 · 안내",new Vector3(18,8.45f,-21.5f),7);
        MallBench(new Vector3(11,5,-24),90);
        LocalLamp("2F customer service closing lamp",new Vector3(18,8.8f,-23),new Vector3(18,5.5f,-23),new Color(1,.82f,.60f),4.6f,10,true);
        root=old;
    }

    static void BuildFamilyRoom()
    {
        FamilyRoomByWall();
    }

    static void ShoppingCart(Vector3 p)
    {
        var old=root;root=new GameObject("parked shopping cart").transform;root.SetParent(old);
        var collision=root.gameObject.AddComponent<BoxCollider>();collision.center=p+new Vector3(0,.54f,0);collision.size=new Vector3(.61f,1.08f,1.0f);
        for(int side=-1;side<=1;side+=2){
            RailSegment("cart chassis",p+new Vector3(side*.27f,.22f,-.40f),p+new Vector3(side*.27f,.22f,.4f),.025f,shelfTop,root);
            RailSegment("cart upright",p+new Vector3(side*.27f,.2f,-.4f),p+new Vector3(side*.27f,1.02f,-.40f),.025f,shelfTop,root);
            RailSegment("cart basket rim",p+new Vector3(side*.29f,.93f,-.35f),p+new Vector3(side*.29f,.93f,.43f),.022f,shelfTop,root);
            for(int wire=0;wire<10;wire++){
                float z=-.33f+wire*.082f;
                RailSegment("cart basket vertical wire",p+new Vector3(side*.23f,.56f,z),p+new Vector3(side*.29f,.93f,z),.006f,shelfTop,root);
            }
            foreach(float z in new[]{-.4f,.4f})Decor("rubber cart caster",PrimitiveType.Cylinder,p+new Vector3(side*.27f,.10f,z),new Vector3(.14f,.045f,.14f),dark).transform.rotation=Quaternion.Euler(0,0,90);
        }
        for(int wire=0;wire<10;wire++){
            float z=-.33f+wire*.082f;
            RailSegment("cart basket floor wire",p+new Vector3(-.23f,.56f,z),p+new Vector3(.23f,.56f,z),.006f,shelfTop,root);
        }
        for(int wire=0;wire<8;wire++){
            float x=-.27f+wire*.077f;
            RailSegment("cart front basket wire",p+new Vector3(x*.8f,.56f,.42f),p+new Vector3(x,.93f,.43f),.006f,shelfTop,root);
        }
        RailSegment("red cart handle",p+new Vector3(-.29f,1.02f,-.40f),p+new Vector3(.29f,1.02f,-.4f),.040f,red,root);
        root=old;
    }
}
