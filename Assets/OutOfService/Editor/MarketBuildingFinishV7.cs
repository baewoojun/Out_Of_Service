using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    static void MallFacadeDetail(int kind)
    {
        FinishOpenMallFrontage(kind);
    }

    static void BasementSupportBay(string title,Vector3 location,float yaw,int kind)
    {
        var old=root;root=new GameObject("B1 support room - "+title).transform;root.SetParent(old);
        var lower=MallFinish("washable",new Color(.34f,.38f,.34f),0,.20f);
        foreach(int side in new[]{-1,1}){
            Box("service room side wall",new Vector3(side*7,2.1f,8),new Vector3(.18f,4.2f,16),wall);
            Box("service room front wing",new Vector3(side*4.2f,2.1f,0),new Vector3(5.6f,4.2f,.18f),wall);
            Decor("service wall lower protection",PrimitiveType.Cube,new Vector3(side*4.2f,.66f,-.104f),new Vector3(5.6f,1.32f,.025f),lower);
            Decor("service wall skirting",PrimitiveType.Cube,new Vector3(side*4.2f,.07f,-.13f),new Vector3(5.6f,.14f,.04f),shelf);
            for(int seam=0;seam<4;seam++)Decor("service concrete form seam",PrimitiveType.Cube,
                new Vector3(side*(1.6f+seam*1.7f),2.8f,-.094f),new Vector3(.009f,2.5f,.008f),shelf);
        }
        Box("service room back wall",new Vector3(0,2.1f,16),new Vector3(14,4.2f,.18f),wall);
        Box("service room door lintel",new Vector3(0,3.35f,0),new Vector3(2.8f,1.7f,.18f),wall);
        if(kind<3)StorageSlidingDoor("B1 storage sliding door - "+title,new Vector3(0,0,-.23f),2.72f,false);
        else SafetyDoor("B1 support doorway - "+title,new Vector3(-1.36f,0,0),2.72f,-100);
        Label(title,new Vector3(0,2.88f,-.11f),Vector3.zero,.29f);
        Label("직원 전용",new Vector3(0,3.40f,-.11f),Vector3.zero,.18f);
        SuspendedCeiling(kind==1?"support_open":"support_closed",13.8f,15.6f,4.12f,.2f,kind==1);
        foreach(float z in new[]{4,11})CeilingVent(new Vector3(0,4.08f,z));
        LocalLamp("B1 service room worklight",new Vector3(0,3.9f,7),new Vector3(0,0,7),new Color(.72f,.84f,.79f),1.9f,10);
        LocalLamp("B1 support entry light",new Vector3(0,3.66f,-.3f),new Vector3(0,0,-1),new Color(.70f,.82f,.72f),1.1f,5);
        if(kind<3){
            foreach(int side in new[]{-1,1})foreach(float lane in new[]{2.1f,5.6f})for(int row=0;row<3;row++){
                float x=side*lane,z=3.3f+row*4.0f;
                foreach(float dx in new[]{-.55f,.55f})foreach(float dz in new[]{-1.32f,1.32f})
                    Box("support storage rack upright",new Vector3(x+dx,1.35f,z+dz),new Vector3(.055f,2.7f,.055f),shelfTop);
                for(int tier=0;tier<4;tier++){
                    float y=.12f+tier*.66f;Box("support storage shelf",new Vector3(x,y,z),new Vector3(1.2f,.045f,2.75f),shelf);
                    for(int box=0;box<4;box++){
                        float h=.30f+.045f*((box+tier+row)%3);
                        var p=new Vector3(x,y+.026f+h*.5f,z+(box-1.5f)*.62f);
                        WarehouseCarton(p-Vector3.up*h*.5f,new Vector3(.51f,h,.82f),(box+tier)%2,side*90);
                    }
                }
            }
            // Logistics stock belongs on shelving; remove the oversized empty table.
            StockroomOpenRack(new Vector3(0,0,14.7f),2.4f,false);
            for(int tier=0;tier<3;tier++)for(int n=0;n<3;n++){
                Vector3 p=new Vector3((n-1)*.69f,.19f+tier*.78f,14.7f);
                WarehouseCarton(p,new Vector3(.61f,.46f,.70f),(n+tier)%2,(n-1)*2);
            }
        }else{
            for(int row=0;row<2;row++){
                Box("maintenance desk top",new Vector3(-3,.78f,5+row*4),new Vector3(1.6f,.06f,.78f),cream);
                foreach(float x in new[]{-3.65f,-2.35f})Box("maintenance desk base",new Vector3(x,.36f,5+row*4),new Vector3(.08f,.72f,.67f),shelf);
                RetailTerminal(new Vector3(-3,.84f,5+row*4),0,root);
                Box("office chair upholstered seat",new Vector3(-3,.47f,3.9f+row*4),new Vector3(.48f,.10f,.48f),fabric);
                Box("office chair upholstered back",new Vector3(-3,.80f,3.68f+row*4),new Vector3(.48f,.58f,.07f),fabric);
                Box("office chair pedestal",new Vector3(-3,.23f,3.9f+row*4),new Vector3(.06f,.46f,.06f),PrepSteel());
                Decor("office chair base",PrimitiveType.Cylinder,new Vector3(-3,.045f,3.9f+row*4),new Vector3(.53f,.025f,.53f),shelf);
            }
            foreach(float x in new[]{-5,-3,3,5}){
                Box("maintenance document cabinet",new Vector3(x,.65f,14.6f),new Vector3(1.35f,1.3f,.65f),cream);
                for(int drawer=0;drawer<4;drawer++){
                    Decor("document cabinet drawer seam",PrimitiveType.Cube,new Vector3(x,.18f+drawer*.31f,14.267f),new Vector3(1.22f,.009f,.006f),shelf);
                    Decor("document cabinet recessed pull",PrimitiveType.Cube,new Vector3(x,.30f+drawer*.31f,14.25f),new Vector3(.18f,.035f,.03f),PrepSteel());
                }
            }
            for(int locker=0;locker<6;locker++){
                var p=new Vector3(5.5f,1.0f,3+locker*1.4f);
                Box("staff metal locker",p,new Vector3(.7f,2,1.3f),lower);
                Decor("locker handle",PrimitiveType.Cube,p+new Vector3(-.38f,0,.35f),new Vector3(.06f,.15f,.025f),PrepSteel());
                for(int vent=0;vent<5;vent++)Decor("locker ventilation slit",PrimitiveType.Cube,p+new Vector3(-.354f,.65f+vent*.025f,0),new Vector3(.006f,.008f,.55f),dark);
            }
        }
        root.position=location;root.rotation=Quaternion.Euler(0,yaw,0);root=old;
    }

    static void BasementOccupancy()
    {
        // Fill the unused side bands while retaining the southern -27m cross
        // corridor, northern return and both public escalator landing approaches.
        BasementSupportBay("몰 공동창고",new Vector3(-32,-5,-16),-90,0);
        BasementSupportBay("식음료 입고실",new Vector3(-32,-5,4),-90,1);
        BasementSupportBay("매장 비품실",new Vector3(-32,-5,24),-90,2);
        BasementSupportBay("시설 관리실",new Vector3(32,-5,-16),90,3);
        BasementSupportBay("직원 준비실",new Vector3(32,-5,24),90,4);
        MallUnit("모아 뷰티",new Vector3(-30,-5,-28),180,6);
        MallUnit("바른 안경",new Vector3(-10,-5,-28),180,7);
        MallUnit("픽셀 모바일",new Vector3(10,-5,-28),180,8);
        MallUnit("맡김 보관서비스",new Vector3(30,-5,-28),180,9);
        var old=root;root=new GameObject("B1 concourse furniture and storefront returns").transform;root.SetParent(old);
        var finish=MallFinish("washable",new Color(.40f,.38f,.31f),0,.22f);
        foreach(float z in new[]{-6,14,34})foreach(int side in new[]{-1,1}){
            var p=new Vector3(side*8,-5,z);
            Box("concourse planter base",p+Vector3.up*.36f,new Vector3(.8f,.72f,1.4f),finish);
            Decor("planter recessed soil",PrimitiveType.Cube,p+Vector3.up*.728f,new Vector3(.70f,.016f,1.30f),dark);
            for(int plant=0;plant<3;plant++){
                var basePoint=p+new Vector3(0,.74f,(plant-1)*.39f);
                for(int blade=0;blade<7;blade++){
                    var leaf=HandmadeProp("mall planter arching foliage",BotanicalLeaf(1,blade%3),basePoint,new Vector3(.34f,.61f,.63f),FloristGreen(blade%2),root);
                    leaf.transform.rotation=Quaternion.Euler(-35,blade*137.5f+plant*17,0);
                }
            }
        }
        // Fire and cleaning stations give the long return corridor visible purpose.
        foreach(int side in new[]{-1,1})for(int n=0;n<3;n++){
            float x=side*27.2f,z=-16+n*20;
            Box("B1 recessed fire equipment case",new Vector3(x,-3.85f,z),new Vector3(.24f,1.65f,1.15f),red);
            Decor("fire case dark vision inset",PrimitiveType.Cube,new Vector3(x+side*.13f,-3.7f,z),new Vector3(.025f,.65f,.80f),glass);
            Label("소화전",new Vector3(x+side*.16f,-3.15f,z),new Vector3(0,-side*90,0),.20f);
        }
        root=old;
    }

    static void BuildingArchitecturalFinish()
    {
        var old=root;root=new GameObject("Three storey architectural finish").transform;root.SetParent(old);
        var panel=MallFinish("washable",new Color(.43f,.44f,.39f),0,.23f);
        var trim=PrepSteel();
        foreach(float floorY in new[]{-5f,0f,5f}){
            foreach(int side in new[]{-1,1}){
                // Perimeter details remain against the existing walls, away from stairs.
                Decor("perimeter impact skirting",PrimitiveType.Cube,new Vector3(side*49.70f,floorY+.095f,0),new Vector3(.075f,.19f,89.4f),shelf);
                Decor("perimeter wall dado",PrimitiveType.Cube,new Vector3(side*49.71f,floorY+.76f,0),new Vector3(.03f,1.10f,89.4f),panel);
                for(int z=-43;z<=43;z+=4){
                    Decor("wall panel expansion joint",PrimitiveType.Cube,new Vector3(side*49.68f,floorY+1.8f,z),new Vector3(.035f,3.3f,.012f),shelf);
                    Decor("perimeter ceiling angle",PrimitiveType.Cube,new Vector3(side*49.65f,floorY+4.52f,z),new Vector3(.09f,.07f,3.98f),trim);
                }
            }
            foreach(int side in new[]{-1,1})for(int x=-40;x<=20;x+=20){
                bool besideStairs=side<0 && x==-40;
                var p=new Vector3(x+(besideStairs?3.5f:0),floorY+4.18f,side*40);
                Decor("overhead service cable tray",PrimitiveType.Cube,p,new Vector3(besideStairs?12.8f:19.8f,.12f,.42f),shelf);
                int clamps=besideStairs?3:5;
                for(int clamp=0;clamp<clamps;clamp++){
                    var h=p+new Vector3((clamp-(clamps-1)*.5f)*4,.16f,0);
                    RailSegment("cable tray suspension",h,h+Vector3.up*.30f,.012f,trim,root);
                    Decor("cable tray folded splice",PrimitiveType.Cube,h-Vector3.up*.16f,new Vector3(.035f,.15f,.46f),trim);
                }
            }
        }
        root=old;
    }
}
