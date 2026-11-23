using UnityEngine;
using OutOfService;

public static partial class MarketPrototypeBuilderV7
{
    // Vertically aligned service strip behind the entrance concourse, outside the
    // checkout / service queues and the existing east-west emergency route.
    static void BuildPublicRestrooms()
    {
        foreach(float floorY in new[]{0f,5f})PublicRestroomSuite(new Vector3(18,floorY,-35.8f),floorY<1?"1F":"2F");
    }

    static Mesh SanitaryBasin()
    {
        return TurnedPackageMesh("open_glazed_washbasin_v1",
            new[]{0f,.07f,.55f,1f,1.02f,.84f,.23f,.18f,.18f},
            new[]{0f,.37f,.48f,.50f,.46f,.40f,.18f,.08f,0f});
    }

    static void PublicWC(Vector3 p,bool support)
    {
        var ceramic=RetailSurface("sanitary porcelain glaze",new Color(.79f,.82f,.80f),0,.61f);
        HandmadeProp("WC tapered porcelain pedestal",TurnedPackageMesh("sanitary_pedestal_v1",new[]{0f,.06f,.18f,.62f,.90f,1f},new[]{.39f,.46f,.42f,.28f,.36f,.43f}),p,new Vector3(.44f,.35f,.58f),ceramic,root);
        HandmadeProp("WC open ceramic bowl",SanitaryBasin(),p+Vector3.up*.31f,new Vector3(.47f,.15f,.64f),ceramic,root);
        var resin=RetailSurface("sanitary seat resin",new Color(.83f,.84f,.80f),0,.38f);
        DetailRing("WC oval seat",p+Vector3.up*.474f,new Vector3(.46f,.62f,.035f),resin,null,new Vector3(90,0,0));
        MerchBox("WC flush cistern",p+new Vector3(0,.65f,.32f),new Vector3(.41f,.43f,.18f),ceramic);
        MerchBox("WC cistern lid",p+new Vector3(0,.875f,.32f),new Vector3(.44f,.035f,.205f),ceramic);
        Decor("dual flush button",PrimitiveType.Cylinder,p+new Vector3(0,.900f,.32f),new Vector3(.07f,.006f,.07f),PrepSteel());
        var body=new GameObject("WC fixture collision");body.transform.SetParent(root);body.transform.position=p+new Vector3(0,.44f,.05f);body.AddComponent<BoxCollider>().size=new Vector3(.50f,.88f,.86f);
        foreach(int side in new[]{-1,1}){
            Decor("WC floor fixing cap",PrimitiveType.Sphere,p+new Vector3(side*.17f,.055f,.02f),Vector3.one*.035f,cream);
            if(support){
                RailSegment("WC support side rail",p+new Vector3(side*.48f,.73f,-.37f),p+new Vector3(side*.48f,.73f,.70f),.035f,PrepSteel(),root);
                RailSegment("WC support rail upright",p+new Vector3(side*.48f,.73f,.70f),p+new Vector3(side*.48f,1.0f,.70f),.035f,PrepSteel(),root);
                foreach(float y in new[]{.73f,1.0f}){
                    RailSegment("WC wall rail standoff",p+new Vector3(side*.48f,y,.70f),p+new Vector3(side*.48f,y,.752f),.031f,PrepSteel(),root);
                    MerchBox("WC wall rail fixing plate",p+new Vector3(side*.48f,y,.75f),new Vector3(.12f,.12f,.014f),PrepSteel());
                }
            }
        }
        var roll=p+new Vector3(-.66f,.74f,-.05f);
        var paper=RetailSurface("white sanitary tissue",new Color(.84f,.83f,.77f),0,.03f);
        Decor("toilet paper roll",PrimitiveType.Cylinder,roll,new Vector3(.12f,.065f,.12f),paper).transform.rotation=Quaternion.Euler(0,0,90);
        RailSegment("toilet tissue holder",roll+Vector3.left*.10f,roll+Vector3.right*.09f,.015f,PrepSteel(),root);
        RailSegment("tissue holder mounting bracket",roll+Vector3.left*.10f,p+new Vector3(support?-.48f:-.935f,.74f,-.05f),.018f,PrepSteel(),root);
        MerchBox("tissue loose paper end",roll+new Vector3(0,-.09f,-.052f),new Vector3(.11f,.16f,.005f),paper);
    }

    static void RestroomBasinStation(Vector3 p,float yaw)
    {
        var old=root;root=new GameObject("public restroom basin and mirror").transform;root.SetParent(old);
        var ceramic=RetailSurface("sanitary porcelain glaze",new Color(.79f,.82f,.80f),0,.61f);
        MerchBox("wall mounted stone vanity",new Vector3(0,.82f,0),new Vector3(.95f,.10f,.57f),cream,root,true);
        HandmadeProp("washbasin open porcelain vessel",SanitaryBasin(),new Vector3(0,.875f,-.035f),new Vector3(.54f,.16f,.42f),ceramic,root);
        Decor("basin metal drain",PrimitiveType.Cylinder,new Vector3(0,.909f,-.035f),new Vector3(.045f,.003f,.045f),PrepSteel());
        RailSegment("basin chrome tap stem",new Vector3(0,.88f,.22f),new Vector3(0,1.16f,.22f),.024f,PrepSteel(),root);
        RailSegment("basin sensor faucet spout",new Vector3(0,1.16f,.22f),new Vector3(0,1.16f,.04f),.026f,PrepSteel(),root);
        MerchBox("basin sensor window",new Vector3(0,1.05f,.205f),new Vector3(.021f,.028f,.004f),dark);
        PrepPipe("basin exposed P trap",new[]{new Vector3(0,.79f,0),new Vector3(0,.54f,0),new Vector3(0,.48f,.08f),new Vector3(0,.48f,.18f),new Vector3(0,.63f,.24f)},.032f,PrepSteel(),root);
        MerchBox("restroom framed mirror backing",new Vector3(0,1.72f,.285f),new Vector3(.90f,1.05f,.035f),PrepSteel());
        MerchBox("restroom smoked mirror face",new Vector3(0,1.72f,.263f),new Vector3(.85f,1,.008f),cabinetGlass);
        MerchBox("wall soap dispenser",new Vector3(.62f,1.21f,.23f),new Vector3(.17f,.28f,.13f),cream);
        MerchBox("soap dispenser level window",new Vector3(.62f,1.20f,.158f),new Vector3(.055f,.15f,.009f),glass);
        MerchBox("paper towel dispenser",new Vector3(-.69f,1.48f,.18f),new Vector3(.32f,.37f,.20f),PrepSteel());
        MerchBox("dispenser paper tail",new Vector3(-.69f,1.22f,.11f),new Vector3(.23f,.17f,.008f),cream);
        Decor("mirror task light diffuser",PrimitiveType.Cube,new Vector3(0,2.28f,.19f),new Vector3(.85f,.035f,.08f),priceWhite);
        root.position=p;root.rotation=Quaternion.Euler(0,yaw,0);root=old;
    }

    static void RestroomPrivacyDoor(string name,Vector3 hinge,float width,float height)
    {
        var leaf=new GameObject(name).transform;leaf.SetParent(root);leaf.position=hinge;
        leaf.gameObject.AddComponent<MarketSwingDoor>().openAngle=-100;
        var skin=MallFinish("washable",new Color(.40f,.48f,.45f),0,.26f);
        MerchBox(name+" laminate leaf",hinge+new Vector3(width*.5f,height*.5f+.08f,0),new Vector3(width-.045f,height,.055f),skin,leaf,true);
        foreach(int side in new[]{-1,1}){
            RailSegment(name+" lever grip",hinge+new Vector3(width-.15f,1.02f,side*.08f),hinge+new Vector3(width-.32f,1.02f,side*.08f),.021f,PrepSteel(),leaf);
            Decor(name+" occupancy latch bezel",PrimitiveType.Cylinder,hinge+new Vector3(width-.15f,1.17f,side*.032f),new Vector3(.055f,.006f,.055f),PrepSteel(),leaf).transform.rotation=Quaternion.Euler(90,0,0);
        }
        foreach(float y in new[]{.25f,height-.18f})Decor(name+" barrel hinge",PrimitiveType.Cylinder,hinge+new Vector3(.015f,y,.033f),new Vector3(.028f,.042f,.028f),PrepSteel(),leaf);
        foreach(float x in new[]{-.025f,width+.025f})Box(name+" fixed jamb",hinge+new Vector3(x,height*.5f+.08f,0),new Vector3(.04f,height+.12f,.10f),PrepSteel());
    }

    static void PublicRestroomSuite(Vector3 position,string level)
    {
        var old=root;root=new GameObject(level+" public restroom suite").transform;root.SetParent(old);
        const float half=6.3f,depth=8.6f,height=3.7f;
        foreach(int side in new[]{-1,1}){
            Box("restroom perimeter side",new Vector3(side*half,height*.5f,depth*.5f),new Vector3(.18f,height,depth),wall);
            Box("restroom entrance wing",new Vector3(side*4.15f,height*.5f,0),new Vector3(4.3f,height,.18f),wall);
            FinishPanel("restroom outer glazed dado",new Vector3(side*4.15f,.12f,-.108f),4.24f,1.5f,0,DepartmentTile(3));
        }
        Box("restroom rear service wall",new Vector3(0,height*.5f,depth),new Vector3(12.6f,height,.18f),wall);
        Box("restroom entrance lintel",new Vector3(0,3.18f,0),new Vector3(4,.99f,.18f),wall);
        Box("restroom acoustic ceiling",new Vector3(0,height+.055f,depth*.5f),new Vector3(12.6f,.11f,depth),cream);
        Box("restroom flush terrazzo floor",new Vector3(0,.012f,depth*.5f),new Vector3(12.45f,.024f,depth-.12f),floor);
        Label(level+"  화장실  /  RESTROOM",new Vector3(0,3.13f,-.105f),Vector3.zero,.25f);
        foreach(float x in new[]{-2.1f,2.1f})Box("restroom gender division",new Vector3(x,height*.5f,5.45f),new Vector3(.14f,height,6.3f),wall);
        for(int room=0;room<3;room++){
            float x=-4.2f+room*4.2f,w=room==2?1.4f:1.15f;
            string label=room==0?"여자 화장실":room==1?"남자 화장실":"가족 화장실";
            foreach(int side in new[]{-1,1}){
                float wing=(4.2f-w)*.5f;
                Box("restroom privacy entrance wing",new Vector3(x+side*(w*.5f+wing*.5f),height*.5f,2.3f),new Vector3(wing,height,.14f),wall);
            }
            Box("restroom privacy entrance lintel",new Vector3(x,3.03f,2.3f),new Vector3(w,1.34f,.14f),wall);
            RestroomPrivacyDoor(level+" public washroom entrance "+room,new Vector3(x-w*.5f,0,2.3f),w,2.25f);
            Label(label,new Vector3(x,2.75f,2.21f),Vector3.zero,.18f);
            FinishPanel("restroom tile rear wall",new Vector3(x,.08f,8.49f),4.02f,3.3f,0,DepartmentTile(3));
            foreach(int side in new[]{-1,1})FinishPanel("restroom tile side wall",new Vector3(x+side*1.98f,.08f,5.45f),6.1f,3.3f,side<0?-90:90,DepartmentTile(3));
            LocalLamp(level+" restroom practical "+room,new Vector3(x,3.50f,5),new Vector3(x,.7f,5.1f),new Color(.72f,.83f,.80f),1.75f,6);
            CeilingVent(new Vector3(x,3.62f,7.1f));
            if(room<2){
                RestroomBasinStation(new Vector3(x-1.70f,0,4.15f),-90);
                int stalls=room==0?2:1;
                for(int stall=0;stall<stalls;stall++){
                    float cx=x-1+stall*2;
                    foreach(int side in new[]{-1,1})Box("washroom cubicle partition",new Vector3(cx+side*.97f,1.16f,7.32f),new Vector3(.055f,2.05f,2.32f),cream);
                    foreach(int side in new[]{-1,1})Box("washroom cubicle front return",new Vector3(cx+side*.71f,1.16f,6.15f),new Vector3(.52f,2.05f,.055f),cream);
                    RestroomPrivacyDoor(level+" public washroom cubicle "+room+"-"+stall,new Vector3(cx-.45f,.06f,6.15f),.90f,1.98f);
                    PublicWC(new Vector3(cx,0,7.78f),false);
                }
                if(room==1){
                    foreach(float z in new[]{5.5f,7.2f}){
                        var q=new Vector3(x+1.72f,.53f,z);
                        var urinal=HandmadeProp("wall mounted urinal open bowl",SanitaryBasin(),q,new Vector3(.42f,.29f,.49f),priceWhite,root);urinal.transform.rotation=Quaternion.Euler(0,90,0);
                        MerchBox("urinal ceramic back",q+new Vector3(.11f,.32f,0),new Vector3(.20f,.63f,.39f),priceWhite);
                        RailSegment("urinal flush pipe",q+new Vector3(.14f,.52f,0),q+new Vector3(.14f,.79f,0),.021f,PrepSteel(),root);
                    }
                    Box("urinal privacy fin",new Vector3(x+1.59f,1.10f,6.35f),new Vector3(.78f,1.2f,.06f),cream);
                }
            }else{
                PublicWC(new Vector3(x,0,7.75f),true);
                RestroomBasinStation(new Vector3(x+1.70f,0,4.65f),90);
                MerchBox("folded family changing table",new Vector3(x-1.96f,1.02f,4.65f),new Vector3(.12f,.72f,.87f),cream);
                RailSegment("family assistance cord",new Vector3(x+.65f,3.63f,8.33f),new Vector3(x+.65f,.32f,8.33f),.008f,red,root);
                MerchBox("assistance cord ceiling fitting",new Vector3(x+.65f,3.65f,8.33f),new Vector3(.09f,.07f,.09f),cream);
                DetailRing("family assistance pull",new Vector3(x+.65f,.28f,8.33f),new Vector3(.09f,.09f,.025f),red);
            }
            Decor("restroom floor drain",PrimitiveType.Cube,new Vector3(x,.026f,5.15f),new Vector3(.14f,.004f,.14f),PrepSteel());
            for(int slot=0;slot<5;slot++)Decor("drain grate slot",PrimitiveType.Cube,new Vector3(x-.047f+slot*.024f,.029f,5.15f),new Vector3(.009f,.002f,.11f),dark);
        }
        LocalLamp(level+" restroom vestibule guide light",new Vector3(0,3.48f,1),new Vector3(0,.3f,1),new Color(.72f,.83f,.80f),1.9f,7);
        root.position=position;root.rotation=Quaternion.Euler(0,180,0);root=old;
        var sign=new Vector3(18,position.y+2.8f,-34.9f);
        Box(level+" public restroom projecting sign",sign,new Vector3(3.7f,.45f,.07f),green);
        Label("화장실  /  RESTROOM",sign+Vector3.forward*.045f,new Vector3(0,180,0),.19f);
    }
}
