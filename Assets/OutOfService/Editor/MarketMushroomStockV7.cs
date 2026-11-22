using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    // Each pair is (position along the stalk, radius). The lip doubles back
    // under the cap; its gills are visible even when a piece lies on its side.
    static readonly Vector2[][] MushroomProfiles =
    {
        Profile(-.49f,.12f, -.42f,.16f, -.18f,.14f, .06f,.12f, .02f,.35f, -.03f,.48f, .08f,.49f, .26f,.41f, .40f,.23f, .44f,.01f),
        Profile(-.50f,.23f, -.40f,.33f, -.18f,.35f, .08f,.28f, .29f,.19f, .32f,.32f, .34f,.48f, .39f,.49f, .46f,.32f, .49f,.01f),
        Profile(-.45f,.16f, -.36f,.22f, -.13f,.17f, -.09f,.20f, -.18f,.32f, -.14f,.43f, .02f,.49f, .23f,.46f, .41f,.27f, .46f,.01f),
        Profile(-.50f,.09f, -.34f,.13f, -.07f,.14f, .14f,.13f, .23f,.18f, .30f,.31f, .39f,.48f, .43f,.47f, .44f,.27f, .40f,.01f),
        Profile(-.50f,.09f, -.45f,.12f, -.14f,.095f, .12f,.09f, .35f,.08f, .36f,.22f, .37f,.34f, .42f,.36f, .48f,.24f, .50f,.01f)
    };

    static Vector2[] Profile(params float[] values)
    {
        var points = new Vector2[values.Length / 2];
        for (int i=0; i<points.Length; i++) points[i]=new Vector2(values[2*i],values[2*i+1]);
        return points;
    }

    static Material MushroomSkin()
    {
        var skin=RetailSurface("fresh mushroom stalk gills and cap",Color.white,0,.25f);
        if (skin.mainTexture) return skin;
        const string path="Assets/OutOfService/Generated/HarvestMushroomSurface.asset";
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (!tex)
        {
            const int width=256, band=96;
            tex=new Texture2D(width,5*band,TextureFormat.RGB24,true){name="Mushroom skin by species",wrapMode=TextureWrapMode.Clamp};
            Color[] caps={new Color(.47f,.29f,.18f),new Color(.67f,.50f,.34f),new Color(.91f,.86f,.75f),new Color(.48f,.43f,.38f),new Color(.94f,.90f,.77f)};
            for(int kind=0;kind<5;kind++)
                for(int y=0;y<band;y++)for(int x=0;x<width;x++)
                {
                    float t=(float)y/(band-1),u=(float)x/(width-1);
                    float grain=Mathf.PerlinNoise(x*.19f+kind*29,y*.085f);
                    float fleck=Mathf.PerlinNoise(x*.43f,y*.45f);
                    Color color;
                    if(t<.43f)
                        color=Color.Lerp(new Color(.80f,.72f,.59f),new Color(.98f,.95f,.86f),grain*.55f+.36f*t/.43f);
                    else if(t<.65f)
                    {
                        float rib=.5f+.5f*Mathf.Cos(u*2*Mathf.PI*44+grain*.8f);
                        color=Color.Lerp(new Color(.67f,.56f,.44f),new Color(.94f,.86f,.71f),rib*.68f+.22f);
                    }
                    else
                    {
                        color=caps[kind]*(.78f+.31f*grain+.15f*fleck);
                        if(kind==0)
                        {
                            float mottling=Mathf.PerlinNoise(x*.063f+7,y*.14f);
                            float fissure=Mathf.Clamp01((.046f-Mathf.Abs(mottling-.50f))/.046f);
                            color=Color.Lerp(color,new Color(.79f,.67f,.51f),fissure*grain*.79f);
                        }
                        if(kind==3)color*=.88f+.12f*Mathf.Sin(t*Mathf.PI);
                    }
                    tex.SetPixel(x,kind*band+y,color);
                }
            tex.Apply(true,false);
            AssetDatabase.CreateAsset(tex,path);
        }
        skin.mainTexture=tex;
        return skin;
    }

    static Mesh MushroomModel(int kind,int variant)
    {
        string path="Assets/OutOfService/Generated/HarvestMushroom_"+kind+"_"+variant+".asset";
        if(handmadeMeshes.TryGetValue(path,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh)
        {
            var verts=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            var profile=MushroomProfiles[kind];
            float[] skinV={.03f,.13f,.27f,.37f,.44f,.56f,.65f,.74f,.88f,.97f};
            int count=kind==4?15:kind==3?4:1;
            int sides=kind==4?7:kind==3?12:20;
            var rng=new System.Random(397*variant+kind);
            float Random01()=>(float)rng.NextDouble();
            for(int piece=0;piece<count;piece++)
            {
                float spread=count>1?(float)piece/(count-1)-.5f:0;
                float girth=count==1?1:kind==4?.145f:.36f;
                float reach=count==1?1:.72f+.27f*Random01();
                float lean=(Random01()-.5f)*(kind==3?.26f:.08f);
                float rise=count==1?0:.16f*Random01()-.08f;
                int first=verts.Count;
                for(int r=0;r<profile.Length;r++)
                {
                    float z=profile[r].x, t=z+.5f;
                    float crown=Mathf.InverseLerp(4,7,r);
                    for(int j=0;j<=sides;j++)
                    {
                        float a=2*Mathf.PI*j/sides;
                        float radius=profile[r].y*(1+.045f*Mathf.Cos(3*a+variant)+.025f*Mathf.Sin(5*a+piece));
                        float centerX=spread*(.39f+.51f*t)+lean*t*t;
                        float centerY=rise+.035f*Mathf.Sin(t*2.6f+variant);
                        if(kind==4)centerY+=.19f*Mathf.Sin(piece*2.40f+variant)*(.55f+.45f*t);
                        if(kind==3)
                        {
                            centerX+=.13f*crown*(piece%2==0?-1:1);
                            z=profile[r].x+.050f*crown*Mathf.Cos(a*2+piece)+.032f*crown*Mathf.Cos(5*a);
                        }
                        verts.Add(new Vector3(centerX+girth*radius*Mathf.Cos(a),
                            centerY+radius*Mathf.Sin(a)*(kind==3?.45f:kind==4?.36f:1),
                            z*reach+(kind==3?.055f*Mathf.Cos(a)*t*t:0)));
                        uv.Add(new Vector2((.01f+.98f*j/sides),(kind+skinV[r])/5));
                        if(r==0||j==0)continue;
                        int d=verts.Count-1,c=d-1,b=d-(sides+1),aa=b-1;
                        indices.Add(aa);indices.Add(b);indices.Add(c);
                        indices.Add(b);indices.Add(d);indices.Add(c);
                    }
                }
                for(int j=1;j<sides-1;j++)
                {
                    indices.Add(first);indices.Add(first+j+1);indices.Add(first+j);
                    int top=first+(profile.Length-1)*(sides+1);
                    indices.Add(top);indices.Add(top+j);indices.Add(top+j+1);
                }
            }
            mesh=new Mesh{name=freshPackNames[kind]+" harvest "+variant};
            mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[path]=mesh;return mesh;
    }

    static void PackMushrooms(int kind,int seed,float w,float d,Vector3 p,Transform pack)
    {
        var rng=new System.Random(seed*397+kind);
        float Range(float lo,float hi)=>Mathf.Lerp(lo,hi,(float)rng.NextDouble());
        bool loose=kind==0||kind==2;
        int count=loose?10+rng.Next(2):kind==1?3:kind==3?3:2;
        var skin=MushroomSkin();
        for(int i=0;i<count;i++)
        {
            float x,z,h,angle,tilt;
            Vector3 size;
            if(loose)
            {
                int offset=(i+seed%9)%9;
                float pitchX=w*.293f,pitchZ=d*.273f;
                // Interlocking rows fill the footprint, leaving two recesses
                // near the middle where the packer can rest a final cap.
                x=i<9?(offset%3-1+Range(-.12f,.12f))*pitchX:((i-9)*2-1)*pitchX*.45f;
                z=i<9?(offset/3-1+Range(-.14f,.14f))*pitchZ:Range(-.28f,.28f)*pitchZ;
                h=i<9?.032f:.058f;
                angle=Range(-180,180);tilt=(i+seed)%5==0?Range(17,47):Range(-84,-42);
                float diameter=Range(.071f,.086f);
                size=new Vector3(diameter,diameter*.85f,diameter*(kind==0?.68f:.78f));
            }
            else
            {
                float pitch=w*.92f/count;
                x=(i-(count-1)*.5f)*pitch;z=Range(-.012f,.005f);h=.036f;
                angle=Range(-6,6);tilt=Range(-3,3);
                if(kind==1 && (i+seed)%3==0)angle+=180;
                size=new Vector3(pitch*Range(.86f,.99f),kind==1?.068f:.061f,
                    d*(kind==1?Range(.60f,.79f):Range(.73f,.84f)));
            }
            var item=HandmadeProp(freshPackNames[kind],MushroomModel(kind,rng.Next(3)),p+new Vector3(x,h,z),size,skin,pack);
            item.transform.rotation=Quaternion.Euler(tilt,angle,Range(-9,9));
        }
    }
}
