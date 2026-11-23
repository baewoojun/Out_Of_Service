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
        float[] spacing={0,.18f,.37f,.56f,.76f,.97f,1.15f,1.34f,1.58f,1.84f,2.02f,2.22f,2.43f,2.61f,2.80f,3.11f,3.39f,3.68f};
        for(int i=0;i<18;i++)
        {
            float zz=z-1.85f+spacing[i];int style=category=="여성의류"?i/6%3:i/9*2;
            float scale=category=="아동의류"?.68f:1;
            // Shared sewn torso with attached sleeves and a thin cloth section.
            var neck=new Vector3(x,top-.14f,zz);
            var rotation=Quaternion.Euler(0,(i%5-2)*2+(i%7==0?2:0),0);
            var garment=HandmadeProp("sewn "+(style==1?"coat":style==2?"crew neck tee":"long sleeve shirt"),LaundryGarment(style),neck,Vector3.one*scale,GarmentCloth(style,i/5%4),group);
            garment.transform.rotation=rotation;
            GarmentDetails(neck,rotation,scale,style,group);
            Vector3 hook=new Vector3(x,top-.10f,zz),left=hook+new Vector3(-.20f*scale,-.09f,0),right=hook+new Vector3(.20f*scale,-.09f,0);
            left=hook+rotation*(left-hook);right=hook+rotation*(right-hook);
            RailSegment("hanger shoulder",hook,left,.009f,dark,group);RailSegment("hanger shoulder",hook,right,.009f,dark,group);RailSegment("hanger crossbar",left,right,.007f,dark,group);
            var hookPoints=new[]{new Vector3(0,0,0),new Vector3(0,.065f,0),new Vector3(-.022f,.074f,0),new Vector3(-.033f,.095f,0),new Vector3(-.018f,.119f,0),new Vector3(.012f,.124f,0),new Vector3(.029f,.107f,0),new Vector3(.025f,.083f,0)};
            for(int n=0;n<hookPoints.Length;n++)hookPoints[n]=hook+rotation*hookPoints[n];
            PrepPipe("curved hanger hook over rail",hookPoints,.004f,shelfTop,group);
            var tagPoint=neck+rotation*(new Vector3(.13f,-.18f,-.054f)*scale);
            var tag=Decor("SOOIM size and price hangtag",PrimitiveType.Cube,tagPoint,new Vector3(.045f,.065f,.001f)*scale,cream,group);tag.transform.rotation=rotation;
            var print=Decor("readable original garment tag",PrimitiveType.Quad,tagPoint+rotation*Vector3.back*.0007f,new Vector3(.044f,.064f,1)*scale,GarmentTagInk(),group);print.transform.rotation=rotation;
            RailSegment("hangtag cotton loop",neck+rotation*new Vector3(.10f,-.12f,LaundryFrontDepth(style,.10f,-.12f)-.003f)*scale,tagPoint+Vector3.up*.029f*scale,.0007f,dark,group);
        }
        // One narrow collision envelope preserves the aisle without a solid plinth.
        var hit=group.gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(x,5.9f,z);hit.size=new Vector3(.85f,1.8f,4.2f);
    }
}
