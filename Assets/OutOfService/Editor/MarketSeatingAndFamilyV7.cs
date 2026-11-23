using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    static Mesh UpholsteredPad(Vector3 size)
    {
        return MallMesh("upholstery_rounded_v1_"+size.ToString("F3"),(v,u,t)=>{
            Vector3 half=size*.5f;float radius=Mathf.Min(.030f,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.75f);
            foreach(var n in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back}){
                var right=Vector3.Cross(n,Mathf.Abs(n.y)>.5f?Vector3.forward:Vector3.up);var up=Vector3.Cross(n,right);
                float w=Vector3.Scale(right,half).magnitude,h=Vector3.Scale(up,half).magnitude;
                float[] xs={-w,-w+radius*.293f,-w+radius,0,w-radius,w-radius*.293f,w};
                float[] ys={-h,-h+radius*.293f,-h+radius,0,h-radius,h-radius*.293f,h};int first=v.Count;
                for(int y=0;y<7;y++)for(int x=0;x<7;x++){
                    var p=Vector3.Scale(n,half)+right*xs[x]+up*ys[y];
                    var core=new Vector3(Mathf.Clamp(p.x,-half.x+radius,half.x-radius),Mathf.Clamp(p.y,-half.y+radius,half.y-radius),Mathf.Clamp(p.z,-half.z+radius,half.z-radius));
                    v.Add(core+(p-core).normalized*radius);u.Add(new Vector2((xs[x]+w)/(2*w),(ys[y]+h)/(2*h)));
                    if(x>0 && y>0){int k=first+y*7+x;t.AddRange(new[]{k-8,k-7,k-1,k-7,k,k-1});}
                }
            }
        });
    }
    static GameObject SeatPad(string name,Vector3 p,Vector3 size,Material cloth)
    {
        return HandmadeProp(name,UpholsteredPad(size),p,Vector3.one,cloth,root);
    }
    static void ServiceSeating(Vector3 position,float yaw,bool individual)
    {
        var previous=root;root=new GameObject(individual?"linked customer waiting seats":"slatted concourse bench").transform;root.SetParent(previous);
        var metal=RetailSurface("waiting seat charcoal frame",new Color(.075f,.08f,.077f),.55f,.34f);
        var wood=MallFinish("wood",new Color(.40f,.29f,.18f),0,.27f);
        foreach(float x in new[]{-.70f,.70f}){
            RailSegment("seat pedestal",new Vector3(x,.05f,0),new Vector3(x,.41f,0),.055f,metal,root);
            RailSegment("seat T foot",new Vector3(x,.04f,-.22f),new Vector3(x,.04f,.22f),.045f,metal,root);
        }
        MerchBox("seat shared structural beam",new Vector3(0,.37f,0),new Vector3(1.70f,.065f,.065f),metal);
        if(individual){
            var fabricSeat=LaundryCotton(1);
            foreach(float x in new[]{-.61f,0,.61f}){
                SeatPad("individual upholstered waiting cushion",new Vector3(x,.46f,-.015f),new Vector3(.52f,.09f,.49f),fabricSeat);
                var back=SeatPad("individual tilted waiting back",new Vector3(x,.76f,.23f),new Vector3(.52f,.46f,.075f),fabricSeat);back.transform.rotation=Quaternion.Euler(-9,0,0);
                RailSegment("waiting back upright behind upholstery",new Vector3(x,.37f,.29f),new Vector3(x,.87f,.29f),.035f,metal,root);
            }
            foreach(float x in new[]{-.91f,-.305f,.305f,.91f}){
                RailSegment("seat arm support",new Vector3(x,.39f,.16f),new Vector3(x,.66f,.16f),.025f,metal,root);
                MerchBox("separated waiting armrest",new Vector3(x,.67f,-.025f),new Vector3(.045f,.035f,.35f),metal);
            }
        }else{
            for(int n=0;n<7;n++)MerchBox("bench individual seat slat",new Vector3(0,.47f,-.23f+n*.076f),new Vector3(1.89f,.037f,.063f),wood);
            for(int n=0;n<5;n++)MerchBox("bench individual back slat",new Vector3(0,.64f+n*.077f,.23f+n*.012f),new Vector3(1.89f,.062f,.033f),wood);
            foreach(float x in new[]{-.78f,.78f})RailSegment("bench back steel support",new Vector3(x,.36f,.19f),new Vector3(x,.97f,.29f),.032f,metal,root);
            foreach(float x in new[]{-.93f,.93f}){
                RailSegment("bench end arm upright",new Vector3(x,.39f,-.13f),new Vector3(x,.66f,-.13f),.025f,metal,root);
                MerchBox("bench end arm grip",new Vector3(x,.67f,.015f),new Vector3(.045f,.035f,.40f),metal);
            }
        }
        var seat=root.gameObject.AddComponent<BoxCollider>();seat.center=new Vector3(0,.24f,0);seat.size=new Vector3(1.9f,.48f,.55f);
        var backCollision=root.gameObject.AddComponent<BoxCollider>();backCollision.center=new Vector3(0,.76f,.25f);backCollision.size=new Vector3(1.9f,.5f,.10f);
        root.position=position;root.rotation=Quaternion.Euler(0,yaw,0);root=previous;
    }

    static Mesh BentArmFrame()
    {
        return MallMesh("nursing_bent_arm_v1",(v,u,t)=>{
            Vector2[] points={new(.33f,.04f),new(-.27f,.04f),new(-.37f,.14f),new(-.35f,.51f),new(-.22f,.63f),new(.31f,.68f)};
            const int steps=40;
            Vector2 At(float f){float q=Mathf.Clamp(f,0,.99999f)*(points.Length-1);int k=Mathf.FloorToInt(q);q-=k;
                var a=points[Mathf.Max(0,k-1)];var b=points[k];var c=points[k+1];var d=points[Mathf.Min(points.Length-1,k+2)];
                return .5f*((2*b)+(-a+c)*q+(2*a-5*b+4*c-d)*q*q+(-a+3*b-3*c+d)*q*q*q);}
            for(int n=0;n<=steps;n++){
                float f=n/(float)steps;var p=At(f);var tangent=(At(f+.001f)-At(f-.001f)).normalized;var normal=new Vector2(-tangent.y,tangent.x)*.017f;
                foreach(var corner in new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(-1,1)}){
                    var yz=p+normal*corner.y;v.Add(new Vector3(corner.x*.025f,yz.y,yz.x));u.Add(new Vector2((corner.x+1)*.5f,f*3));
                }
                if(n<steps)for(int j=0;j<4;j++){int a=n*4+j,b=n*4+(j+1)%4;t.AddRange(new[]{a,b,a+4,b,b+4,a+4});}
            }
            t.AddRange(new[]{0,2,1,0,3,2,steps*4,steps*4+1,steps*4+2,steps*4,steps*4+2,steps*4+3});
        });
    }
    static void NursingArmchair(Vector3 p)
    {
        var previous=root;root=new GameObject("upholstered nursing armchair").transform;root.SetParent(previous);
        var wood=MallFinish("wood",new Color(.49f,.38f,.24f),0,.30f);var cloth=LaundryCotton(2);
        foreach(float x in new[]{-.32f,.32f})HandmadeProp("bent laminated arm and base",BentArmFrame(),new Vector3(x,0,0),Vector3.one,wood,root);
        foreach(float z in new[]{-.23f,.20f})MerchBox("chair seat wood crossbar",new Vector3(0,.37f,z),new Vector3(.64f,.045f,.045f),wood);
        var seat=SeatPad("nursing seat pad",new Vector3(0,.435f,-.02f),new Vector3(.56f,.105f,.51f),cloth);seat.transform.rotation=Quaternion.Euler(-5,0,0);
        var back=SeatPad("nursing padded reclined back",new Vector3(0,.76f,.25f),new Vector3(.56f,.63f,.09f),cloth);back.transform.rotation=Quaternion.Euler(-14,0,0);
        var head=SeatPad("nursing head cushion",new Vector3(0,1.00f,.218f),new Vector3(.43f,.15f,.09f),cloth);head.transform.rotation=back.transform.rotation;
        var collision=root.gameObject.AddComponent<BoxCollider>();collision.center=new Vector3(0,.53f,0);collision.size=new Vector3(.70f,1.06f,.80f);
        root.position=p;root.rotation=Quaternion.Euler(0,180,0);root=previous;
    }

    static void FamilyRoomByWall()
    {
        var previous=root;root=new GameObject("1F nursing and family room").transform;root.SetParent(previous);
        // Authored 6.4 x 5.6 m room, against the west exterior. The 3.4 m
        // corridor behind it keeps the WEST fire-door approach unobstructed.
        Box("family room back wall",new Vector3(0,1.8f,-5.6f),new Vector3(6.4f,3.6f,.16f),wall);
        foreach(float x in new[]{-3.2f,3.2f})Box("family room side wall",new Vector3(x,1.8f,-2.8f),new Vector3(.16f,3.6f,5.6f),wall);
        Box("family room privacy front",new Vector3(-1.15f,1.8f,0),new Vector3(4.1f,3.6f,.16f),wall);
        Box("family room entrance return",new Vector3(2.95f,1.8f,0),new Vector3(.5f,3.6f,.16f),wall);
        Box("family room entrance lintel",new Vector3(1.8f,3.05f,0),new Vector3(1.8f,1.1f,.16f),wall);
        Box("family room acoustic ceiling",new Vector3(0,3.66f,-2.8f),new Vector3(6.4f,.12f,5.6f),cream);
        SafetyDoor("family room privacy door",new Vector3(.9f,0,0),1.8f,-100);
        Box("nursing privacy screen",new Vector3(1.65f,1.05f,-2.65f),new Vector3(2.9f,2.1f,.08f),cream);
        Box("nursing bay divider",new Vector3(0,1.05f,-4.7f),new Vector3(.08f,2.1f,1.65f),cream);
        foreach(float x in new[]{-1.6f,1.6f}){
            NursingArmchair(new Vector3(x,0,-4.8f));
            MerchBox("nursing side table",new Vector3(x+.58f,.29f,-4.9f),new Vector3(.39f,.58f,.39f),cream,null,true);
            CeramicCup(new Vector3(x+.58f,.586f,-4.9f));
        }
        ServiceCounter("family changing station",new Vector3(-2.65f,0,-1.30f),new Vector3(.80f,.90f,1.70f),cream);
        MerchBox("changing pad raised foam",new Vector3(-2.65f,.992f,-1.12f),new Vector3(.70f,.085f,1.02f),fabric);
        foreach(float x in new[]{-2.94f,-2.36f})MerchBox("changing mat safety bolster",new Vector3(x,1.045f,-1.12f),new Vector3(.10f,.11f,1.02f),fabric);
        Decor("closed diaper disposal bin",PrimitiveType.Cylinder,new Vector3(-1.8f,.25f,-.65f),new Vector3(.30f,.25f,.30f),priceWhite);
        Decor("diaper bin lid",PrimitiveType.Cylinder,new Vector3(-1.8f,.514f,-.65f),new Vector3(.32f,.015f,.32f),cream);
        var sink=new Vector3(-2.65f,0,-2.60f);
        foreach(int side in new[]{-1,1}){
            MerchBox("family sink cabinet side",sink+new Vector3(side*.38f,.44f,0),new Vector3(.035f,.88f,.70f),cream,null,true);
            MerchBox("family sink cabinet face",sink+new Vector3(0,.44f,side*.33f),new Vector3(.75f,.88f,.035f),cream,null,true);
            MerchBox("family basin side rim",sink+new Vector3(side*.36f,.94f,0),new Vector3(.10f,.035f,.70f),priceWhite);
            MerchBox("family basin front rear rim",sink+new Vector3(0,.94f,side*.29f),new Vector3(.64f,.035f,.12f),priceWhite);
        }
        HandmadeProp("family inset ceramic basin",PrepBasinMesh(),sink+Vector3.up*.955f,new Vector3(.65f,.20f,.50f),CafeCeramic(false),root);
        PrepPipe("family mixer faucet",new[]{sink+new Vector3(0,.96f,-.28f),sink+new Vector3(0,1.17f,-.28f),sink+new Vector3(0,1.20f,-.10f),sink+new Vector3(0,1.17f,-.08f)},.015f,PrepSteel(),root);
        Label("수유실 · 가족쉼터",new Vector3(-.5f,2.95f,.11f),new Vector3(0,180,0),.32f);
        LocalLamp("family room lamp",new Vector3(-.5f,3.35f,-2.2f),new Vector3(-.5f,0,-2.2f),new Color(1,.81f,.62f),2.5f,6,true);
        root.position=new Vector3(-46.4f,0,-23.9f);root=previous;
    }
}
