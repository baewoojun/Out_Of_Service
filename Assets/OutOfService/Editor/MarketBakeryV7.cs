using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static partial class MarketPrototypeBuilderV7
{
    static Mesh BreadForm(bool loaf)
    {
        string path="Assets/OutOfService/Generated/"+(loaf?"SlicedPanLoaf":"ScoredBaguette")+".asset";
        if(handmadeMeshes.TryGetValue(path,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh)
        {
            const int length=48,ring=16;
            var v=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            for(int i=0;i<=length;i++)
            {
                float x=(float)i/length;
                float taper=loaf?Mathf.Min(1,.84f+4*Mathf.Min(x,1-x)):
                    Mathf.Pow(Mathf.Max(.003f,Mathf.Sin(x*Mathf.PI)),.36f);
                for(int j=0;j<=ring;j++)
                {
                    float u=(float)j/ring,a=u*2*Mathf.PI;
                    float top=Mathf.Cos(a),z=Mathf.Sin(a);
                    float split=loaf?Mathf.Pow(Mathf.Abs(Mathf.Cos(x*9*Mathf.PI)),20f)*.065f:
                        .17f*Mathf.Pow(Mathf.Max(0,Mathf.Cos((x+.037f*z)*8*Mathf.PI)),10)*Mathf.Clamp01(2*top);
                    float width=loaf?Mathf.Sign(z)*Mathf.Pow(Mathf.Abs(z),.47f):z;
                    float profile=loaf?Mathf.Sign(top)*Mathf.Pow(Mathf.Abs(top),.49f):top;
                    v.Add(new Vector3(x-.5f,Mathf.Max(0,.455f+(.52f*profile-split)*taper),width*.5f*taper));
                    uv.Add(new Vector2(x,u));
                    if(i==0||j==0)continue;
                    int d=v.Count-1,b=d-ring-1;indices.AddRange(new[]{b-1,b,d-1,b,d,d-1});
                }
            }
            for(int end=0;end<2;end++)
            {
                int start=end*length*(ring+1),center=v.Count;
                v.Add(new Vector3(end-.5f,.455f,0));uv.Add(new Vector2(end,.5f));
                for(int j=0;j<ring;j++)if(end==0)indices.AddRange(new[]{center,start+j+1,start+j});
                    else indices.AddRange(new[]{center,start+j,start+j+1});
            }
            mesh=new Mesh{name=loaf?"sliced milk loaf":"scored baguette"};mesh.SetVertices(v);mesh.SetUVs(0,uv);
            mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[path]=mesh;return mesh;
    }

    static Material BreadCrust(bool loaf)
    {
        string key=loaf?"milk loaf crust":"baguette crust";
        if(retailSurfaces.TryGetValue(key,out var known))return known;
        string path="Assets/OutOfService/Generated/"+(loaf?"MilkLoafCrust":"BaguetteCrust")+".asset";
        var map=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(!map)
        {
            const int w=256,h=96;
            map=new Texture2D(w,h,TextureFormat.RGB24,true);
            var pixels=new Color32[w*h];
            var rand=new System.Random(3517);
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float s=(float)x/w,t=(float)y/h,top=Mathf.Cos(t*2*Mathf.PI);
                float slash=loaf?Mathf.Pow(Mathf.Abs(Mathf.Cos(s*9*Mathf.PI)),20):
                    Mathf.Max(0,top)*Mathf.Pow(Mathf.Max(0,Mathf.Cos((s+.037f*Mathf.Sin(t*2*Mathf.PI))*8*Mathf.PI)),9);
                float toasted=Mathf.Clamp01(.36f+.36f*top+.32f*Mathf.PerlinNoise(s*19,t*11)-slash*.64f);
                pixels[y*w+x]=Color.Lerp(new Color(.88f,.71f,.45f),new Color(.52f,.248f,.098f),
                    Mathf.Clamp01(toasted+((float)rand.NextDouble()-.5f)*.18f));
            }
            map.SetPixels32(pixels);map.Apply(true);AssetDatabase.CreateAsset(map,path);
        }
        var material=RetailSurface(key,Color.white,0,.14f);material.mainTexture=map;return material;
    }

    static void BakeryStock()
    {
        Vector3 center=new Vector3(-35,0,-22);
        var counter=new GameObject("베이커리 진열대").transform;counter.SetParent(root);counter.position=center;
        Box("bakery counter",center+Vector3.up*.65f,new Vector3(8,1.3f,3),cream,counter);
        for(int lane=0;lane<5;lane++)for(int row=0;row<2;row++)
        {
            bool loaf=(lane+row)%3==0;
            var tray=center+new Vector3((lane-2)*1.47f,1.301f,(row-.5f)*1.20f);
            HandmadeProp("pressed bakery tray",ProduceShell(false,false),tray,new Vector3(1.39f,.044f,1.10f),wornMetal,counter);
            for(int item=0;item<4;item++)
            {
                var foot=tray+new Vector3((item%2-.5f)*.64f,.010f,(item/2-.5f)*.48f);
                var size=loaf?new Vector3(.244f,.137f,.131f):new Vector3(.48f,.088f,.098f);
                var bread=HandmadeProp(loaf?"sliced milk bread":"scored baguette",BreadForm(loaf),foot,size,BreadCrust(loaf),counter);
                bread.transform.Rotate(0,8f*(item%3-1),0);
                if(loaf)
                {
                    var film=HandmadeProp("loaf bag",ProduceShell(true,true),foot,
                        new Vector3(size.x+.04f,size.y+.019f,size.z+.028f),produceFilm,counter);
                    film.transform.rotation=bread.transform.rotation;
                }
            }
        }
        counter.Rotate(0,24,0);
    }
}
