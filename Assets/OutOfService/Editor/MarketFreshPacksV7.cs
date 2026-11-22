using UnityEditor;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    static readonly string[] freshPackNames={"표고버섯","새송이버섯","양송이버섯","느타리버섯","팽이버섯","쌈채소","간편대파","깐감자","카레용 채소"};
    static Mesh LettuceLeaf(int variant)
    {
        return HarvestLeaf(variant);
    }
    // Retain the three potato surface samples. Skins are mapped onto separate
    // rounded pieces, and each piece can roll independently in the sleeve.
    static Mesh PeeledPotato(int pick)
    {
        Vector4[] crops={new Vector4(.27f,.25f,.32f,.29f),new Vector4(.52f,.45f,.33f,.32f),new Vector4(.68f,.75f,.34f,.27f)};
        string path="Assets/OutOfService/Generated/RetailPieceV2_7_"+pick+".asset";
        if(handmadeMeshes.TryGetValue(path,out Mesh mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh)
        {
            Vector4 crop=crops[pick];
            const int rings=5,sides=16;
            var v=new Vector3[(rings+1)*sides];var uv=new Vector2[v.Length];var indices=new int[rings*sides*6];
            for(int r=0;r<=rings;r++)for(int j=0;j<sides;j++){
                float a=j*2*Mathf.PI/sides,theta=r*Mathf.PI*.56f/rings;
                float x=Mathf.Cos(a)*Mathf.Sin(theta),z=Mathf.Sin(a)*Mathf.Sin(theta);
                int n=r*sides+j;
                v[n]=new Vector3(x*.5f,Mathf.Cos(theta)*.48f,z*.5f);
                uv[n]=new Vector2((1+crop.x+x*crop.z*.5f)/3,(1-crop.y+z*crop.w*.5f)/3);
                if(r==rings)continue;
                int k=n*6,next=r*sides+(j+1)%sides;
                indices[k]=n;indices[k+1]=next+sides;indices[k+2]=n+sides;
                indices[k+3]=n;indices[k+4]=next;indices[k+5]=next+sides;
            }
            mesh=new Mesh{name="peeled potato",vertices=v,uv=uv,triangles=indices};
            mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[path]=mesh;return mesh;
    }

    static void FillVegetablePack(int kind,int seed,float w,float d,Vector3 p,Transform pack)
    {
        var rng=new System.Random(seed*397+kind);
        float Range(float lo,float hi)=>Mathf.Lerp(lo,hi,(float)rng.NextDouble());
        if(kind==5)
        {
            int count=6+seed%3;
            for(int i=0;i<count;i++)
            {
                float yaw=(i%3-1)*22+Range(-13,13);
                Vector3 basePoint=p+new Vector3((i%3-1)*w*.16f,.012f+i*.004f,-d*(i<3?.34f:.24f)+Range(-.012f,.015f));
                var leaf=HandmadeProp("overlapping ssam leaf",LettuceLeaf((i+seed)%3),basePoint,
                    new Vector3(w*Range(.30f,.42f),.16f,d*Range(.45f,.65f)),HarvestLeafSkin(),pack);
                leaf.transform.rotation=Quaternion.Euler(Range(-8,6),yaw,Range(-15,15));
            }
            return;
        }
        if(kind==6)
        {
            var shank=RetailSurface("trimmed leek ivory",new Color(.86f,.87f,.66f),0,.26f);
            var green=RetailSurface("trimmed leek green",new Color(.46f,.64f,.25f),0,.28f);
            for(int i=0;i<6;i++)
            {
                var mid=p+new Vector3((i-2.5f)*w*.149f,.024f,Range(-.01f,.01f));
                Vector3 axis=Quaternion.Euler(0,Range(-5,5),0)*Vector3.forward;
                float half=d*Range(.32f,.40f),diameter=Range(.023f,.032f);
                RailSegment("cut scallion section",mid-axis*half,mid+axis*half,diameter,i%3==1?green:shank,pack);
                RailSegment("scallion cut heart",mid+axis*half,mid+axis*(half+.001f),diameter*.66f,i%3==1?shank:green,pack);
            }
            return;
        }
        var food=RetailSurface("peeled potato moist flesh",new Color(.87f,.79f,.55f),0,.29f);
        bool mix=kind==8;
        var pieces=SettledFruit(mix?12:5+seed%2,w*.46f,d*.46f,mix?.028f:.040f,0,seed);
        for(int i=0;i<pieces.Length;i++)
        {
            var f=pieces[i];
            var pos=p+new Vector3(f.x,.017f+.45f*(f.y-f.w),f.z);
            var dimensions=new Vector3(2*f.w,Range(.035f,.055f),2.1f*f.w);
            Mesh shape=HarvestBody(4,i%5);
            Material surface=food;
            if(mix)
            {
                shape=FreshMesh(false,i%4);
                surface=i%3==0?RetailSurface("diced carrot",new Color(.90f,.34f,.095f),0,.31f):
                    i%3==1?RetailSurface("diced onion",new Color(.91f,.89f,.76f),0,.33f):
                    RetailSurface("diced potato",new Color(.84f,.72f,.47f),0,.25f);
            }
            var prop=HandmadeProp(freshPackNames[kind],shape,pos,dimensions,surface,pack);
            prop.transform.rotation=Quaternion.Euler(Range(-8,8),Range(-180,180),Range(-7,7));
        }
    }
    static void ChilledProducePack(Vector3 p,int kind,int seed,Transform wall)
    {
        Transform pack=new GameObject(freshPackNames[kind]).transform;
        pack.SetParent(wall);pack.position=p;
        bool sleeve=kind==3||kind==4||kind>=5;
        float width=kind==4?.174f:kind==1?.249f:.245f;
        float length=kind==4?.318f:.273f;
        if(!sleeve)
        {
            var tray=kind==0?dark:kind==1?cream:ChilledWrap();
            HandmadeProp("formed produce tray",ProduceShell(false,false),p,new Vector3(width,.040f,length),tray,pack);
        }
        if(kind<5) PackMushrooms(kind,seed,width,length,p,pack);
        else FillVegetablePack(kind,seed,width,length,p,pack);
        float wrapHeight=kind==0||kind==2?.094f:.077f;
        var film=HandmadeProp("tensioned produce wrap",ProduceShell(true,sleeve),p,
            new Vector3(width+.005f,wrapHeight,length+.008f),ChilledWrap(),pack);
        film.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        if(sleeve)
        {
            Material seal=kind==4?RetailSurface("blue enoki seal",new Color(.034f,.15f,.46f),0,.36f):
                RetailSurface("frosted produce seal",new Color(.63f,.67f,.60f),0,.36f);
            float depth=kind==4?.043f:.005f;
            Decor("heat sealed bag foot",PrimitiveType.Cube,p+new Vector3(0,.012f,-length*.49f+depth*.5f),
                new Vector3(width*.97f,.003f,depth),seal,pack);
        }
        if(kind<5)
        {
            // Broad film is left clear. A narrow grower's ticket sits on the
            // corner of the tray or near the seal of the bag.
            float lx=kind==4?0:kind==0?.016f:kind==2?-.046f:.041f;
            float lz=kind==4?-length*.26f:kind==3?.048f:kind==1?.035f:.013f;
            float lw=kind==4?.122f:kind==0?.081f:.090f;
            var ticket=Decor("grower weight and harvest label",PrimitiveType.Quad,
                p+new Vector3(lx,wrapHeight+.0015f,lz),new Vector3(lw,lw*210/320,1),
                PantryInk("FarmMushroom",kind,.18f),pack);
            ticket.transform.rotation=Quaternion.Euler(0,seed%7-3,0)*Quaternion.Euler(90,0,0);
        }
        else
        {
            var label=RetailSurface("fresh product ticket "+kind,Color.white,0,.2f);
            if(!label.mainTexture)label.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Packs/FreshProduceLabel"+kind+".png");
            float labelDepth=kind==4?.069f:.055f;
            var header=Decor("printed retail closure",PrimitiveType.Quad,p+new Vector3(sleeve?0:width*.15f,wrapHeight+.001f,sleeve?length*.24f:length*.18f),
                new Vector3(sleeve?width*.81f:width*.42f,labelDepth,1),label,pack);
            header.transform.rotation=Quaternion.Euler(90,0,0);
        }

        // Lean the printed face towards the customer on the east side.
        pack.rotation=Quaternion.AngleAxis(-55+seed%5,Vector3.forward)*Quaternion.Euler(0,-90,0);
    }
}
