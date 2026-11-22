using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static partial class MarketPrototypeBuilderV7
{
    // Original sewn silhouettes, with front/back volume and asymmetric cloth folds.
    static Mesh GarmentMesh(int style)
    {
        string path="Assets/OutOfService/Generated/GarmentOptimized_"+style+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path); if(existing!=null)return existing;
        var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        const int rows=20,cols=20;
        for(int side=0;side<2;side++)for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++)
        {
            float h=(float)y/rows,u=(float)x/cols;
            float width=h>.78f?Mathf.Lerp(.40f,.14f,(h-.78f)/.22f):(.27f+(style==1?.13f*(1-h):0));
            if(style==2 && h>.48f)width=Mathf.Lerp(.27f,.50f,Mathf.Clamp01((h-.48f)/.3f));
            float px=(u-.5f)*2*width;
            float fold=.014f*Mathf.Sin(u*31+h*3)+.008f*Mathf.Sin(u*53-h*7);
            float z=(side==0?-1:1)*(.025f+.04f*Mathf.Sin(u*Mathf.PI))+fold;
            v.Add(new Vector3(px,h*(style==1?1.05f:.76f),z));uv.Add(new Vector2(u,h));
        }
        int stride=cols+1,plane=(rows+1)*stride;
        for(int s=0;s<2;s++)for(int y=0;y<rows;y++)for(int x=0;x<cols;x++)
        {
            if(y>=18 && x>=7 && x<13)continue; // Open neckline, not a solid triangular shoulder.
            int a=s*plane+y*stride+x,b=a+1,c=a+stride,d=c+1;
            if(s==0)t.AddRange(new[]{a,c,b,b,c,d});else t.AddRange(new[]{a,b,c,b,d,c});
        }
        for(int y=0;y<rows;y++)foreach(int x in new[]{0,cols})
        {int a=y*stride+x,b=a+stride;t.AddRange(x==0?new[]{a,a+plane,b,b,a+plane,b+plane}:new[]{a,b,a+plane,b,b+plane,a+plane});}
        var mesh=new Mesh{name="Draped garment "+style};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static void RailSegment(string name,Vector3 a,Vector3 b,float radius,Material mat,Transform parent)
    {
        var o=Decor(name,PrimitiveType.Cylinder,(a+b)*.5f,new Vector3(radius,(b-a).magnitude*.5f,radius),mat,parent);
        o.transform.rotation=Quaternion.FromToRotation(Vector3.up,b-a);
    }
    static void ClothingRack(float x,float z,string category,Transform group)
    {
        float top=6.82f;
        foreach(float end in new[]{-2.05f,2.05f})
        {
            RailSegment("chrome upright",new Vector3(x,5.12f,z+end),new Vector3(x,top,z+end),.045f,shelfTop,group);
            RailSegment("low T foot",new Vector3(x-.65f,5.12f,z+end),new Vector3(x+.65f,5.12f,z+end),.055f,shelfTop,group);
            foreach(float dx in new[]{-.55f,.55f})Decor("rubber caster",PrimitiveType.Sphere,new Vector3(x+dx,5.08f,z+end),new Vector3(.10f,.10f,.07f),dark,group);
        }
        RailSegment("hanging rail",new Vector3(x,top,z-2.1f),new Vector3(x,top,z+2.1f),.04f,shelfTop,group);
        var mats=new Material[4];for(int i=0;i<4;i++)mats[i]=LaundryCotton(i);
        for(int i=0;i<18;i++)
        {
            float zz=z-1.85f+i*.215f;int style=category=="여성의류"?i/6%3:i/9*2;
            float scale=category=="아동의류"?.68f:1;
            // Shared closed-volume torso and separate hanging sleeves. The previous
            // two-panel silhouette read as a folded board at game camera height.
            var garment=HandmadeProp("sewn "+(style==1?"coat":style==2?"short sleeve shirt":"long sleeve shirt"),LaundryGarment(style),new Vector3(x,top-.14f,zz),Vector3.one*scale,mats[i/5%4],group);
            garment.transform.rotation=Quaternion.Euler(0,(i%3-1)*5,0);
            Vector3 hook=new Vector3(x,top-.10f,zz),left=hook+new Vector3(-.20f*scale,-.09f,0),right=hook+new Vector3(.20f*scale,-.09f,0);
            RailSegment("hanger shoulder",hook,left,.012f,dark,group);RailSegment("hanger shoulder",hook,right,.012f,dark,group);RailSegment("hanger crossbar",left,right,.01f,dark,group);
            RailSegment("hanger neck",hook,hook+Vector3.up*.1f,.009f,shelfTop,group);
            Decor("paper size tag",PrimitiveType.Cube,new Vector3(x+.12f*scale,top-.27f,zz-.06f),new Vector3(.045f,.065f,.003f),cream,group);
        }
        // One narrow collision envelope preserves the aisle without a solid plinth.
        var hit=group.gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(x,5.9f,z);hit.size=new Vector3(.85f,1.8f,4.2f);
    }
}
