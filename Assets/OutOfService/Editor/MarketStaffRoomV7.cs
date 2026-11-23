using UnityEngine;
using UnityEditor;
using OutOfService;

public static partial class MarketPrototypeBuilderV7
{
    static Material StaffPrint(string kind)
    {
        var mat=RetailSurface("staff paper "+kind,Color.white,0,.10f);
        mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Prints/staff-"+kind+".png");return mat;
    }
    static void FurnishStaffRoom()
    {
        var laminate=RetailSurface("staff oatmeal laminate",new Color(.59f,.58f,.51f),0,.26f);
        var tube=RetailSurface("staff graphite steel tube",new Color(.09f,.11f,.12f),.4f,.31f);
        var plastic=RetailSurface("staff polypropylene chair",new Color(.22f,.30f,.31f),0,.22f);
        // Two real table modules, instead of one unsupported four-metre block.
        foreach(float x in new[]{15.15f,16.85f}) {
            var p=new Vector3(x,5.05f,38);
            MerchBox("staff laminate tabletop",p+Vector3.up*.724f,new Vector3(1.66f,.028f,.90f),laminate,null,true);
            foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1}) {
                RailSegment("staff table round leg",p+new Vector3(side*.74f,.02f,end*.36f),p+new Vector3(side*.74f,.71f,end*.36f),.038f,tube,root);
                Decor("table adjustable glide",PrimitiveType.Cylinder,p+new Vector3(side*.74f,.016f,end*.36f),new Vector3(.045f,.013f,.045f),dark);
            }
            foreach(int end in new[]{-1,1})Decor("under top reinforcing tube",PrimitiveType.Cube,p+new Vector3(0,.685f,end*.34f),new Vector3(1.48f,.04f,.025f),tube);
        }
        for(int i=0;i<3;i++)foreach(int side in new[]{-1,1}) {
            float x=14.75f+i*1.23f+(side<0?.04f:0),z=38+side*(.78f+(i==1?.10f:0));
            RetailDiningChair(new Vector3(x,5.05f,z),side<0?180+i*3:-6+i*4,3,plastic,tube);
        }
        CeramicCup(new Vector3(15.14f,5.79f,37.81f));
        CeramicCup(new Vector3(17.20f,5.79f,38.18f),null,true);
        var paper=Decor("shift handover sheet on table",PrimitiveType.Quad,new Vector3(16.13f,5.790f,38.05f),new Vector3(.21f,.297f,1),StaffPrint("shift"));paper.transform.rotation=Quaternion.Euler(90,8,0);
        RailSegment("staff ballpoint pen",new Vector3(16.30f,5.796f,37.90f),new Vector3(16.35f,5.796f,38.04f),.008f,blue,root);
        SellUnit("휴게실 생수",new Vector3(17.5f,5.79f,37.82f),root,-17,true,10,1,u=>DrinkFacing(10,1,new Vector3(17.5f,5.79f,37.82f),PackMaterial(10,1),u));
        // Keep the front doorway and the centre walk to the rear lockers clear.
        for(int i=0;i<16;i++)StaffLocker(new Vector3(10.9f+i*.46f,5.05f,43.65f),i,i==3||i==11);
        StaffKitchen(new Vector3(22.4f,5.05f,43.35f));
        StaffVending(new Vector3(24.8f,5.05f,35));
        var board=new Vector3(22.4f,7.23f,44.54f);
        MerchBox("staff notice cork board",board,new Vector3(1.90f,1.16f,.055f),MallFinish("wood",new Color(.44f,.34f,.21f),0,.18f));
        foreach(float x in new[]{-.58f,0,.58f}) {
            var notice=Decor("staff roster pinned sheet",PrimitiveType.Quad,board+new Vector3(x,0,-.032f),new Vector3(.40f,.566f,1),StaffPrint("shift"));notice.transform.rotation=Quaternion.Euler(0,0,x*3);
            Decor("notice board pushpin",PrimitiveType.Sphere,board+new Vector3(x,.26f,-.039f),new Vector3(.013f,.013f,.012f),red);
        }
    }
    static void StaffLocker(Vector3 p,int number,bool interactive)
    {
        var enamel=RetailSurface("employee locker powder coat",new Color(.43f,.49f,.47f),.25f,.32f);
        const float w=.444f,h=1.82f,d=.46f;
        foreach(int side in new[]{-1,1})Box("locker folded side",p+new Vector3(side*(w*.5f-.008f),h*.5f,0),new Vector3(.016f,h,d),enamel);
        foreach(float y in new[]{.04f,h-.01f})Box("locker welded top and base",p+new Vector3(0,y,0),new Vector3(w,.02f,d),enamel);
        Box("locker back sheet",p+new Vector3(0,h*.5f,d*.5f-.008f),new Vector3(w,h,.016f),enamel);
        Transform leaf=new GameObject(interactive?"employee locker "+(number+1)+" - E":"employee locker sealed leaf").transform;leaf.SetParent(root);leaf.position=p+new Vector3(-w*.5f+.008f,.06f,-d*.5f);
        if(interactive)leaf.gameObject.AddComponent<MarketSwingDoor>().openAngle=104;
        var doorPoint=leaf.position+new Vector3((w-.022f)*.5f,(h-.08f)*.5f,0);
        MerchBox("locker recessed steel door",doorPoint,new Vector3(w-.026f,h-.08f,.022f),enamel,leaf,true);
        for(int n=0;n<5;n++)Decor("pressed locker ventilation slot",PrimitiveType.Cube,doorPoint+new Vector3(0,.62f+n*.024f,-.013f),new Vector3(.18f,.006f,.0015f),dark,leaf);
        var handle=doorPoint+new Vector3(.13f,.05f,-.033f);
        MerchBox("locker recessed pull",handle,new Vector3(.045f,.09f,.018f),dark,leaf);
        Decor("locker key cylinder",PrimitiveType.Cylinder,handle+new Vector3(0,-.075f,-.005f),new Vector3(.018f,.003f,.018f),shelfTop,leaf).transform.rotation=Quaternion.Euler(90,0,0);
        var nameplate=doorPoint+new Vector3(0,.44f,-.015f);
        Decor("locker name holder",PrimitiveType.Cube,nameplate,new Vector3(.13f,.041f,.004f),cream,leaf);
        // The operational lockers have actual interior volume and fixed contents.
        if(interactive) {
            Box("locker internal shelf",p+new Vector3(0,1.53f,0),new Vector3(w-.03f,.016f,d-.03f),enamel);
            RailSegment("locker clothes rail",p+new Vector3(-.18f,1.43f,0),p+new Vector3(.18f,1.43f,0),.018f,shelfTop,root);
            HandmadeProp("employee spare shirt",LaundryGarment(2),p+new Vector3(0,1.39f,.04f),Vector3.one*.67f,LaundryCotton(number%4),root);
            var folded=MerchBox("folded employee towel",p+new Vector3(.025f,1.59f,.015f),new Vector3(.29f,.09f,.23f),LaundryCotton(2));folded.transform.rotation=Quaternion.Euler(0,7,0);
            foreach(int side in new[]{-1,1})MerchBox("locker slippers",p+new Vector3(side*.075f,.085f,-.01f),new Vector3(.11f,.055f,.25f),dark);
        }
    }
    static void StaffKitchen(Vector3 p)
    {
        var enamel=RetailSurface("staff kitchenette ivory melamine",new Color(.63f,.62f,.55f),0,.23f);
        Box("staff kitchenette cabinets",p+new Vector3(0,.40f,0),new Vector3(3.1f,.78f,.63f),enamel);
        MerchBox("staff kitchenette worktop",p+new Vector3(0,.805f,0),new Vector3(3.16f,.04f,.67f),shelfTop,null,true);
        for(int i=0;i<5;i++) {
            float x=-1.24f+i*.62f;
            Decor("cabinet door edge",PrimitiveType.Cube,p+new Vector3(x-.30f,.40f,-.319f),new Vector3(.005f,.71f,.002f),dark);
            RailSegment("cabinet slim bar pull",p+new Vector3(x-.10f,.65f,-.349f),p+new Vector3(x+.10f,.65f,-.349f),.012f,shelfTop,root);
        }
        var oven=p+new Vector3(-.90f,.835f,-.01f);
        MerchBox("microwave folded enclosure",oven+new Vector3(0,.15f,0),new Vector3(.51f,.30f,.37f),enamel,null,true);
        MerchBox("microwave dark door frame",oven+new Vector3(-.044f,.15f,-.192f),new Vector3(.38f,.246f,.019f),dark);
        MerchBox("microwave perforated view window",oven+new Vector3(-.055f,.15f,-.204f),new Vector3(.29f,.16f,.006f),RetailSurface("microwave perforated black glass",new Color(.025f,.033f,.030f),.12f,.46f));
        foreach(float y in new[]{.092f,.218f})Decor("microwave timer dial",PrimitiveType.Cylinder,oven+new Vector3(.205f,y,-.203f),new Vector3(.044f,.009f,.044f),cream).transform.rotation=Quaternion.Euler(90,0,0);
        RailSegment("microwave vertical pull",oven+new Vector3(.105f,.075f,-.221f),oven+new Vector3(.105f,.225f,-.221f),.016f,shelfTop,root);
        for(int slot=0;slot<9;slot++)Decor("microwave ventilation slit",PrimitiveType.Cube,oven+new Vector3(.258f,.10f+slot*.013f,.07f),new Vector3(.002f,.004f,.12f),dark);
        foreach(float x in new[]{.30f,.58f})CeramicCup(p+new Vector3(x,.832f,-.10f));
        MerchBox("instant coffee sachet box",p+new Vector3(1.05f,.94f,.04f),new Vector3(.23f,.22f,.16f),cream);
        Decor("coffee sachet box print",PrimitiveType.Quad,p+new Vector3(1.05f,.94f,-.041f),new Vector3(.225f,.21f,1),PackMaterial(15,0));
        PrepPipe("microwave power cable",new[]{oven+new Vector3(-.20f,.09f,.18f),p+new Vector3(-1.30f,.84f,.26f),p+new Vector3(-1.28f,1.17f,.29f)},.007f,dark,root);
        MerchBox("staff double outlet faceplate",p+new Vector3(-1.28f,1.18f,.32f),new Vector3(.10f,.16f,.018f),cream);
    }
    static void StaffVending(Vector3 p)
    {
        var enamel=RetailSurface("parody vending burgundy enamel",new Color(.39f,.045f,.045f),.32f,.35f);
        Box("vending rear cabinet",p+new Vector3(0,.95f,.31f),new Vector3(1.05f,1.90f,.12f),enamel);
        foreach(int side in new[]{-1,1})Box("vending side frame",p+new Vector3(side*.49f,.95f,0),new Vector3(.07f,1.90f,.74f),enamel);
        Box("vending lower service cabinet",p+new Vector3(0,.30f,-.01f),new Vector3(.98f,.60f,.71f),enamel);
        Box("vending top fascia",p+new Vector3(0,1.82f,-.01f),new Vector3(.98f,.16f,.71f),enamel);
        Box("vending payment panel",p+new Vector3(.36f,1.16f,-.34f),new Vector3(.23f,1.16f,.055f),dark);
        Decor("vending black refrigerated interior",PrimitiveType.Cube,p+new Vector3(-.13f,1.16f,.22f),new Vector3(.68f,1.16f,.015f),dark);
        for(int row=0;row<4;row++) {
            float y=.66f+row*.255f;
            Decor("vending shelf lip",PrimitiveType.Cube,p+new Vector3(-.12f,y,-.19f),new Vector3(.66f,.024f,.22f),shelfTop);
            for(int col=0;col<6;col++) {
                var foot=p+new Vector3(-.395f+col*.108f,y+.014f,-.17f);
                int family=row%2==0?11:17,variant=row%2==0?5:col%5+1;
                SellUnit("vending display can",foot,root,0,false,family,variant,u=>DrinkFacing(family,variant,foot,PackMaterial(family,variant),u));
            }
        }
        Decor("vending protective pane",PrimitiveType.Cube,p+new Vector3(-.12f,1.17f,-.328f),new Vector3(.67f,1.11f,.006f),ChilledWrap());
        foreach(float y in new[]{.99f,1.20f,1.41f})MerchBox("vending selection key",p+new Vector3(.36f,y,-.38f),new Vector3(.16f,.055f,.016f),cream);
        MerchBox("vending coin slot",p+new Vector3(.36f,1.61f,-.376f),new Vector3(.046f,.008f,.004f),shelfTop);
        MerchBox("vending delivery recess",p+new Vector3(-.08f,.34f,-.374f),new Vector3(.62f,.19f,.026f),dark);
        Decor("vending collection flap",PrimitiveType.Cube,p+new Vector3(-.08f,.35f,-.392f),new Vector3(.55f,.13f,.007f),wornMetal).transform.rotation=Quaternion.Euler(-12,0,0);
        var card=Decor("out of service maintenance notice",PrimitiveType.Quad,p+new Vector3(-.10f,1.26f,-.342f),new Vector3(.21f,.297f,1),StaffPrint("maintenance"));card.transform.rotation=Quaternion.Euler(0,0,-7);
        foreach(float x in new[]{-.08f,.08f})Decor("maintenance notice tape",PrimitiveType.Cube,p+new Vector3(-.1f+x,1.40f,-.345f),new Vector3(.065f,.018f,.001f),cream);
    }
}
