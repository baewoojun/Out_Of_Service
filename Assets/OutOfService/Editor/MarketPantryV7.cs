using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using OutOfService;

public static partial class MarketPrototypeBuilderV7
{
    static Material PantryInk(string series,int variant,float gloss)
    {
        string key=series+variant;
        var ink=RetailSurface(key,Color.white,0,gloss);
        if(!ink.mainTexture)
        {
            ink.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Packs/"+key+".png");
            if(!ink.mainTexture)throw new System.IO.FileNotFoundException("Missing shelf artwork: "+key);
        }
        return ink;
    }

    // One folded packet with a plump middle, indented gussets and crimped jaws.
    // Front and back have independent reading directions.
    static Mesh NoodlePacket(bool bundle,bool shelfStock=false)
    {
        string path="Assets/OutOfService/Generated/NoodleWrapper_"+(bundle?"family":"single")+(shelfStock?"_shelf":"")+".asset";
        if(handmadeMeshes.TryGetValue(path,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh)
        {
            float[] ys={0,.032f,.065f,.16f,.34f,.51f,.72f,.87f,.935f,.968f,1};
            float[] ds={.035f,.048f,.088f,.36f,.47f,.50f,.46f,.32f,.089f,.043f,.029f};
            if(shelfStock)
            {
                ys=new[]{0f,.055f,.21f,.51f,.82f,.946f,1f};
                ds=new[]{.034f,.068f,.38f,.50f,.34f,.064f,.03f};
            }
            int cols=shelfStock?6:16;
            var verts=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int face=0;face<2;face++)
                for(int r=0;r<ys.Length;r++)for(int c=0;c<=cols;c++)
                {
                    float u=(float)c/cols,x=u-.5f,t=ys[r],edge=Mathf.Abs(2*x);
                    bool seam=r<2||r>=ys.Length-2;
                    float crease=bundle?.025f*Mathf.Cos(u*8*Mathf.PI):.034f*Mathf.Sin(u*10+t*12);
                    float depth=ds[r]*(1-.72f*Mathf.Pow(edge,5))+(seam?.009f*(c%2):crease*(1-edge));
                    float y=t+(r==0||r==ys.Length-1?.008f*(c%2):0);
                    verts.Add(new Vector3(x*(seam?.98f:1),y,(face==0?-1:1)*depth));
                    uv.Add(new Vector2(face==0?u:1-u,t));
                    if(r==0||c==0)continue;
                    int d=verts.Count-1,b=d-cols-1;
                    if(face==0)tris.AddRange(new[]{b-1,d-1,b,b,d-1,d});
                    else tris.AddRange(new[]{b-1,b,d-1,b,d,d-1});
                }
            int plane=(cols+1)*ys.Length;
            void Join(int a,int b){tris.AddRange(new[]{a,b+plane,b,a,a+plane,b+plane});}
            for(int r=0;r<ys.Length-1;r++){
                int a=r*(cols+1),b=a+cols+1;Join(a,b);Join(b+cols,a+cols);
            }
            for(int c=0;c<cols;c++){Join(c+1,c);Join(plane-cols-1+c,plane-cols+c);}
            mesh=new Mesh{name="gusseted noodle wrapper"};mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[path]=mesh;return mesh;
    }

    // The foil and its peel tab share one unbroken print. Positive local Z
    // points away from the shopper, so print on a lid reads from the aisle.
    static Mesh PeelLid()
    {
        const string path="Assets/OutOfService/Generated/PantryPeelFoilV2.asset";
        if(handmadeMeshes.TryGetValue(path,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh)
        {
            const int n=40;
            var v=new Vector3[1+n];var uv=new Vector2[v.Length];var tri=new int[n*3];
            uv[0]=new Vector2(.5f,.5f);
            for(int i=0;i<n;i++){
                float a=i*2*Mathf.PI/n;
                float tab=Mathf.Clamp01(1-Mathf.Abs(Mathf.DeltaAngle(310,i*360f/n))/18f);
                float x=Mathf.Cos(a)*(.5f+.065f*tab),z=Mathf.Sin(a)*(.5f+.065f*tab);
                v[i+1]=new Vector3(x,.0014f*tab,z);uv[i+1]=new Vector2(.5f+x*.94f,.5f+z*.94f);
                tri[3*i]=0;tri[3*i+1]=1+(i+1)%n;tri[3*i+2]=i+1;
            }
            mesh=new Mesh{name="die cut peel film",vertices=v,uv=uv,triangles=tri};mesh.RecalculateNormals();
            mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[path]=mesh;return mesh;
    }

    static Mesh PrintedCollar(bool rice)
    {
        string path="Assets/OutOfService/Generated/PantryPrintedCollar_"+(rice?"dish":"cup")+".asset";
        if(handmadeMeshes.TryGetValue(path,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh)
        {
            const int n=32;
            var v=new Vector3[2*(n+1)];var uv=new Vector2[v.Length];var tri=new int[6*n];
            for(int row=0;row<2;row++)for(int i=0;i<=n;i++){
                float u=(float)i/n,a=(u-.5f)*Mathf.PI*2;
                float radius=rice?Mathf.Lerp(.405f,.502f,row):Mathf.Lerp(.384f,.489f,row);
                int k=row*(n+1)+i;
                v[k]=new Vector3(Mathf.Sin(a)*radius,row,-Mathf.Cos(a)*radius);
                // Print repeats twice over the circumference with a complete title at the aisle.
                uv[k]=new Vector2(u*2-.5f,row);
                if(row!=0||i==n)continue;
                tri[6*i]=i;tri[6*i+1]=i+n+1;tri[6*i+2]=i+1;
                tri[6*i+3]=i+1;tri[6*i+4]=i+n+1;tri[6*i+5]=i+n+2;
            }
            mesh=new Mesh{name="printed pantry sleeve",vertices=v,uv=uv,triangles=tri};
            mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[path]=mesh;return mesh;
    }

    static Mesh FoldedRiceBand()
    {
        const string path="Assets/OutOfService/Generated/FoldedRiceBandRetailPanelsV2.asset";
        if(handmadeMeshes.TryGetValue(path,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh)
        {
            const int columns=15;
            var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
            // Follow the elliptical rim across the width of the paper. Each
            // fold has separate normals, like a scored paperboard sleeve.
            for(int face=0;face<4;face++)
            for(int row=0;row<2;row++)
            for(int j=0;j<=columns;j++)
            {
                float u=(float)j/columns,x=Mathf.Lerp(-.41f,-.15f,u);
                float reach=Mathf.Sqrt(.511f*.511f-x*x);
                int corner=(face+row)%4;
                v.Add(new Vector3(x,corner==1||corner==2?1:0,corner<2?-reach:reach));
                float printY=face==0 || face==1?row:1-row;
                uv.Add(new Vector2(face==2?1-u:u,(face+printY)*.25f));
                if(row==0||j==0)continue;
                int d=v.Count-1,b=d-columns-1;
                tri.AddRange(new[]{b-1,d-1,b,b,d-1,d});
            }
            mesh=new Mesh{name="three bowl paper belly band"};
            mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();
            mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[path]=mesh;return mesh;
    }

    static void PantryFacing(string name,int category,int variant,Vector3 p,int side,Transform parent,bool pickup)
    {
        var unit=new GameObject(name+(pickup?" 들 수 있는 상품":" 진열 상품")).transform;
        unit.SetParent(parent);unit.position=p;
        if(category==0)
        {
            bool bulk=variant%3!=1;
            HandmadeProp(bulk?"five noodle bricks in film":"single noodle brick",NoodlePacket(bulk),p,
                bulk?new Vector3(.231f,.224f,.143f):new Vector3(.179f,.201f,.057f),
                PackMaterial(category,variant),unit);
        }
        else
        {
            bool rice=category==2, wide=variant==0||variant==3||variant==4;
            float width=rice?.159f:wide?.178f:.111f;
            float depth=rice?.143f:width,height=rice?.051f:wide?.084f:.118f;
            bool bundled=rice && variant%2==0;
            int count=bundled?3:rice && !pickup?2:1;
            var body=rice?TurnedPackageMesh("rice_closed_rim_v2",
                new[]{0f,0f,.025f,.12f,.18f,.24f,.31f,.38f,.72f,.85f,.89f,.94f,1f},
                new[]{0f,.28f,.34f,.391f,.395f,.384f,.413f,.414f,.455f,.465f,.50f,.50f,.484f}):
                TurnedPackageMesh("noodle_paper_bowl",new[]{0f,.025f,.09f,.13f,.85f,.93f,.955f,1f},
                    new[]{.29f,.335f,.345f,.362f,.468f,.48f,.50f,.49f});
            var resin=RetailSurface(rice?"rice ivory polypropylene v2":"noodle paper stock",
                rice?new Color(.79f,.78f,.74f):new Color(.95f,.91f,.82f),0,rice?.31f:.17f);
            for(int j=0;j<count;j++)
            {
                var basePos=p+Vector3.up*j*(height+.001f);
                HandmadeProp(rice?"microwave rice dish":"rolled noodle cup",body,basePos,new Vector3(width,height,depth),resin,unit);
                // Every bowl is sealed, including those inside the three-pack.
                HandmadeProp("printed peel lid",PeelLid(),basePos+Vector3.up*(height+.0005f),
                    new Vector3(width,width,depth),PackMaterial(category,variant),unit);
                if(!rice && !bundled)
                    HandmadeProp("printed retail sleeve",PrintedCollar(rice),basePos+Vector3.up*(height*.13f),
                        new Vector3(width,height*.80f,depth),PantryInk(rice?"RiceSingle":"NoodleBand",variant,.21f),unit);
            }
            if(bundled)
                HandmadeProp("folded three pack sleeve",FoldedRiceBand(),p+Vector3.up*.0005f,
                    new Vector3(width,count*(height+.001f),depth),PantryInk("RiceBundle",variant,.21f),unit);
        }
        if(pickup)
        {
            PrintHeldUnit(unit,category,variant);
            Bounds bounds=ModelBounds(unit.gameObject);
            var collider=unit.gameObject.AddComponent<BoxCollider>();
            collider.center=bounds.center-p;collider.size=bounds.size;
            unit.gameObject.AddComponent<Rigidbody>().isKinematic=true;
            unit.gameObject.AddComponent<MarketProduct>();
        }
        SetShoppedFacing(unit,p,-side*90,category,variant);
        if(!pickup)
        {
            for(int i=unit.childCount-1;i>=0;i--)unit.GetChild(i).SetParent(parent,true);
            Object.DestroyImmediate(unit.gameObject);
        }
    }
}
