using UnityEngine;
using UnityEditor;

public static partial class MarketPrototypeBuilderV7
{
    static void TimberIsland(float x,float z,Transform stand)
    {
        Material oak=RetailSurface("produce fixture honey oak",new Color(.49f,.29f,.13f),0,.24f);
        oak.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Packs/ProduceOak.png");
        if(x==-38 && z==-15)
        {
            Vector3 board=new Vector3(x,3.28f,z);
            var ink=RetailSurface("orchard placard",Color.white,0,.12f);
            ink.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Packs/OrchardBoard.png");
            Decor("suspended orchard board",PrimitiveType.Cube,board,new Vector3(2.38f,.99f,.056f),dark,stand);
            Decor("orchard offers",PrimitiveType.Quad,board+Vector3.back*.030f,new Vector3(2.36f,.98f,1),ink,stand);
            foreach(float end in new[]{-.84f,.84f})
                RailSegment("sign suspension",board+new Vector3(end,.49f,0),new Vector3(x+end,4.82f,z),.008f,dark,stand);
        }
        for(int side=-1;side<=1;side+=2)
        for(int plank=0;plank<3;plank++)
        {
            float y=.29f+plank*.207f;
            Decor("honey oak fascia",PrimitiveType.Cube,new Vector3(x,y,z+side*2.04f),new Vector3(4.1f,.195f,.075f),oak,stand);
            Decor("honey oak end fascia",PrimitiveType.Cube,new Vector3(x+side*2.04f,y,z),new Vector3(.075f,.195f,4.01f),oak,stand);
        }
    }

    static Mesh PepperCalyx()
    {
        const string path="Assets/OutOfService/Generated/PepperCalyxV2.asset";
        if(handmadeMeshes.TryGetValue(path,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh)
        {
            const int sides=10,rings=5;
            var v=new Vector3[rings*sides+1];var uv=new Vector2[v.Length];var indices=new int[(rings-1)*sides*6+sides*3];
            float[] radii={.14f,.073f,.037f,.029f,.026f};
            Vector3[] centers={Vector3.zero,Vector3.up*.033f,Vector3.up*.054f,
                new Vector3(.017f,.132f,0),new Vector3(.097f,.176f,.021f)};
            for(int r=0;r<rings;r++) for(int j=0;j<sides;j++)
            {
                int i=r*sides+j,next=r*sides+(j+1)%sides;
                float a=j*2*Mathf.PI/sides,rad=radii[r]*(r==0 && j%2==0?1.40f:1);
                v[i]=centers[r]+new Vector3(Mathf.Cos(a)*rad,r==0?-.006f+.018f*(j%2):0,Mathf.Sin(a)*rad);
                uv[i]=new Vector2(j/(float)sides,r/(float)(rings-1)*.92f);
                if(r==rings-1)continue;
                int k=i*6;
                indices[k]=i;indices[k+1]=i+sides;indices[k+2]=next;
                indices[k+3]=next;indices[k+4]=i+sides;indices[k+5]=next+sides;
            }
            int top=v.Length-1;
            v[top]=centers[rings-1];uv[top]=new Vector2(.5f,1);
            for(int j=0;j<sides;j++)
            {
                int k=(rings-1)*sides*6+j*3;
                indices[k]=top;indices[k+1]=(rings-1)*sides+(j+1)%sides;indices[k+2]=(rings-1)*sides+j;
            }
            mesh=new Mesh{name="paprika calyx",vertices=v,uv=uv,triangles=indices};
            mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[path]=mesh;return mesh;
    }

    static void PepperPromotion(Vector3 origin)
    {
        Transform stand=new GameObject("파프리카 산지 행사 매대").transform;
        stand.SetParent(root);
        Box("promotion red plinth",origin+Vector3.up*.41f,new Vector3(2.85f,.82f,1.48f),red,stand);
        var promotion=RetailSurface("paprika original promotion print v1",Color.white,0,.13f);
        promotion.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Packs/PaprikaPromotionV1.png");
        foreach(float x in new[]{-.70f,.70f}){
            var card=Decor("paprika price paper on red fascia",PrimitiveType.Quad,origin+new Vector3(x,.44f,-.742f),new Vector3(.38f,.532f,1),promotion,stand);
            card.transform.rotation=Quaternion.Euler(0,0,x<0?-1.3f:.6f);
        }
        foreach(int end in new[]{-1,1}){
            var card=Decor("paprika end fascia price paper",PrimitiveType.Quad,origin+new Vector3(end*1.427f,.44f,0),new Vector3(.38f,.532f,1),promotion,stand);
            card.transform.rotation=Quaternion.Euler(0,-end*90,end<0?-.8f:1.1f);
        }
        for(int bin=0;bin<2;bin++)
        {
            Vector3 binCenter=origin+new Vector3((bin-.5f)*1.4f,.88f,0);
            var crate=RetailSurface("charcoal produce crate",new Color(.12f,.14f,.105f),0,.29f);
            var deck=Decor("tilted pepper crate",PrimitiveType.Cube,binCenter+Vector3.up*.12f,new Vector3(1.37f,.12f,1.42f),crate,stand);
            deck.transform.rotation=Quaternion.Euler(-12,0,0);
            for(int end=-1;end<=1;end+=2)
            {
                float dz=end*.696f;
                float baseY=.18f+dz*Mathf.Tan(12*Mathf.Deg2Rad);
                for(int rib=0;rib<=7;rib++)
                    Decor("crate vent web",PrimitiveType.Cube,binCenter+new Vector3((rib-3.5f)*.184f,baseY+.051f,dz),
                        new Vector3(.043f,.102f,.023f),crate,stand);
                Decor("crate rolled rim",PrimitiveType.Cube,binCenter+new Vector3(0,baseY+.102f,dz),
                    new Vector3(1.37f,.024f,.031f),crate,stand);
                var rim=Decor("side crate lip",PrimitiveType.Cube,binCenter+new Vector3(end*.672f,.28f,0),
                    new Vector3(.023f,.051f,1.44f),crate,stand);
                rim.transform.rotation=deck.transform.rotation;
            }
            var pile=SettledFruit(105,.675f,.70f,.075f,Mathf.Tan(12*Mathf.Deg2Rad),771+bin);
            for(int i=0;i<pile.Length;i++)
            {
                int seed=i*79+bin*11;
                var f=pile[i];
                float size=2*f.w;
                Vector3 p=binCenter+new Vector3(f.x,.18f+f.y,f.z);
                var pepper=HandmadeProp("four-lobed sweet pepper",RetailPepperBody(seed%5),p,new Vector3(size,size*1.10f,size),RetailPepperSkin(bin,seed%3),stand);
                pepper.transform.rotation=Quaternion.Euler(seed%105-52,seed*37,seed%83-41);
                Vector3 crown=p+pepper.transform.up*(size*1.10f*.34f);
                var calyx=HandmadeProp("pepper calyx and bent stalk",PepperCalyx(),crown,Vector3.one*size,RetailPepperStem(),stand);
                calyx.transform.rotation=pepper.transform.rotation;
            }
        }
        var track=new GameObject("produce track spot");track.transform.SetParent(stand);
        track.transform.position=origin+new Vector3(-.5f,3.8f,-.36f);
        track.transform.LookAt(origin+Vector3.up);
        var lamp=track.AddComponent<Light>();lamp.type=LightType.Spot;lamp.spotAngle=70;lamp.range=5;lamp.intensity=2.4f;lamp.color=new Color(1,.88f,.72f);
        Box("pepper price sign frame",origin+new Vector3(0,1.64f,.48f),new Vector3(1.18f,.36f,.045f),dark,stand);
        Label("파프리카  |  1개  1,690원",origin+new Vector3(0,1.64f,.45f),Vector3.zero,.13f);
    }

    static void SaladFacing(Vector3 p,int seed,Transform stand)
    {
        Transform tray=new GameObject("간편채소 투명 용기").transform;
        tray.SetParent(stand);tray.position=p;
        HandmadeProp("moulded salad punnet",ProduceShell(false,false),p,new Vector3(.301f,.056f,.351f),dark,tray);
        int recipe=seed/6%3;
        Material extra=recipe==1?RetailSurface("shredded red cabbage",new Color(.38f,.09f,.26f),0,.29f):RetailSurface("carrot ribbons",new Color(.91f,.32f,.07f),0,.26f);
        var rng=new System.Random(seed);
        for(int i=0;i<11;i++)
        {
            float a=(i*137.51f+seed%360)*Mathf.Deg2Rad;
            float radius=.024f+.054f*Mathf.Sqrt(i/10f);
            Vector3 leaf=p+new Vector3(Mathf.Sin(a)*radius,.019f+(i%4)*.008f,Mathf.Cos(a)*radius);
            var blade=HandmadeProp("cut salad leaf",LettuceLeaf((seed+i)%3),leaf,
                new Vector3(.102f,.10f,.095f+.028f*(float)rng.NextDouble()),HarvestLeafSkin(),tray);
            if(recipe!=0 && i%2==0)
                Decor("salad garnish ribbon",PrimitiveType.Cube,leaf+Vector3.up*.023f,new Vector3(.072f,.005f,.013f),extra,tray).transform.rotation=Quaternion.Euler(0,seed+i*47,8);
            blade.transform.rotation=Quaternion.Euler(-8,Mathf.Atan2(leaf.x-p.x,leaf.z-p.z)*Mathf.Rad2Deg+145,12);
        }
        var lid=HandmadeProp("clear salad clamshell lid",ProduceShell(true,false),p,new Vector3(.304f,.087f,.355f),ChilledWrap(),tray);
        lid.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        string key="SaladSleeve"+recipe;
        Material print=RetailSurface(key,Color.white,0,.23f);
        if(!print.mainTexture) print.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Packs/"+key+".png");
        var label=Decor("printed salad retail label",PrimitiveType.Quad,p+new Vector3(.047f,.088f,0),
            new Vector3(.218f,.091f,1),print,tray);
        label.transform.rotation=Quaternion.LookRotation(Vector3.down,Vector3.left);
        tray.Rotate(0,0,-59);
    }

    static void CleaningEndcap(float x,float z,Transform parent)
    {
        Transform rack=new GameObject("생활세제 행사 엔드매대").transform;
        rack.SetParent(parent);
        for(int level=0;level<4;level++)
        {
            float y=.36f+level*.63f;
            Box("promotion shelf",new Vector3(x,y,z),new Vector3(1.59f,.06f,.69f),priceWhite,rack);
            int lanes=level==0?3:4;
            for(int lane=0;lane<lanes;lane++)
            {
                Vector3 p=new Vector3(x+(lane-(lanes-1)*.5f)*(1.5f/lanes),y+.033f,z);
                Transform item=new GameObject("행사 세제").transform;item.SetParent(rack);item.position=p;
                RetailItem("행사 세제",22,level==0?0:level==1?1:level==2?2:5,p,1,item);
                item.RotateAround(p,Vector3.up,90);
            }
        }
        Box("promotion headboard",new Vector3(x,2.96f,z),new Vector3(1.61f,.31f,.09f),red,rack);
        Label("이번 주 생활 특가",new Vector3(x,2.96f,z-.055f),Vector3.zero,.155f);
    }

    static void SodaPack(Vector3 p,int variant,int side,Transform group)
    {
        SellUnit("캔음료 6입",p,group,-side*90,false,11,variant,unit=>{
            for(int row=0;row<2;row++)for(int col=0;col<3;col++){
                var can=p+new Vector3((col-1)*.068f,0,(row-.5f)*.068f);
                var aluminium=RetailSurface("rolled aluminium packaging",new Color(.72f,.74f,.75f),.72f,.37f);
                HandmadeProp("250 mL multipack can",HandmadeCanMesh(),can,new Vector3(.066f,.123f,.066f),aluminium,unit);
                Decor("sealed satin can lid",PrimitiveType.Cylinder,can+Vector3.up*.1213f,new Vector3(.057f,.00025f,.057f),
                    RetailSurface("satin aluminium lid",new Color(.70f,.72f,.73f),.24f,.25f),unit);
                CurvedPrint(can+Vector3.up*.008f,.0663f,.104f,PackMaterial(11,variant),unit);
                DetailRing("sealed aluminium can pull tab",can+Vector3.up*.122f,new Vector3(.012f,.020f,.007f),aluminium,unit,new Vector3(90,0,0));
            }
            var stock=RetailSurface("soda recycled carrier board",new Color(.72f,.63f,.46f),0,.16f);
            foreach(int end in new[]{-1,1}){
                MerchBox("carrier folded end",p+new Vector3(end*.102f,.045f,0),new Vector3(.002f,.088f,.137f),stock,unit);
                MerchBox("carrier side panel",p+new Vector3(0,.045f,end*.070f),new Vector3(.207f,.088f,.002f),stock,unit);
            }
            string key="can carrier "+variant;
            var art=RetailSurface(key,Color.white,0,.17f);
            if(!art.mainTexture)art.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/MerchDetails/carrier_"+variant+".png");
            foreach(int sidePanel in new[]{-1,1})
                Decor("original six pack sleeve print",PrimitiveType.Quad,p+new Vector3(0,.045f,sidePanel*.0712f),new Vector3(.205f,.086f,1),art,unit).transform.rotation=Quaternion.Euler(0,sidePanel<0?0:180,0);
        });
    }
}
