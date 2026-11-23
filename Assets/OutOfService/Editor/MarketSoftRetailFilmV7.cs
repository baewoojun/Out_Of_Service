using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static partial class MarketPrototypeBuilderV7
{
    // Shared film molds. Shelf stock stays under 300 vertices; held packs use
    // finer curves. Different contents determine the load and corner folds.
    static Mesh RetailFilmMold(int family,int variant,bool shelfStock)
    {
        string key=$"RetailFilmV3_{family}_{variant}_{(shelfStock?"stock":"held")}";
        string path="Assets/OutOfService/Generated/"+key+".asset";
        if(handmadeMeshes.TryGetValue(path,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh) {
            bool snack=family==5||family==6,coffee=family==15,zip=family==9||family==29;
            int columns=shelfStock?8:16;
            float[] rows={0,.018f,.047f,.088f,.17f,.32f,.51f,.70f,.84f,.91f,.943f,.973f,1};
            var p=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
            float phase=variant*.91f+family*.17f;
            for(int face=0;face<2;face++)for(int r=0;r<rows.Length;r++)for(int c=0;c<=columns;c++) {
                float u=c/(float)columns,x=u*2-1,y=rows[r],edge=Mathf.Abs(x);
                float centre=Mathf.Sin(Mathf.PI*Mathf.Clamp01((y-.035f)/.93f));
                float body=Mathf.Pow(Mathf.Max(0,centre),snack?.48f:.76f);
                float radius=.025f+.47f*body;
                if(coffee)radius=y<.088f?Mathf.Lerp(.32f,.48f,y/.088f):y<.84f?.48f:Mathf.Lerp(.48f,.025f,Mathf.Clamp01((y-.84f)/.103f));
                else if(zip)radius=y<.17f?Mathf.Lerp(.30f,.48f,y/.17f):Mathf.Lerp(.48f,.025f,Mathf.Pow(Mathf.Clamp01((y-.17f)/.74f),1.5f));
                else if(!snack)radius=y<.17f?Mathf.Lerp(.20f,.42f,y/.17f):Mathf.Lerp(.42f,.025f,Mathf.Pow(Mathf.Clamp01((y-.17f)/.78f),1.8f));
                bool seal=y<=.047f||y>=.943f;
                if(seal)radius=y<=.047f&&(coffee||zip||family==3)
                    ?Mathf.Lerp(coffee?.36f:.22f,coffee?.48f:.42f,y/.047f)
                    :.019f+.007f*(r%2);
                if(zip&&r==9)radius=.06f;
                float tuck=Mathf.Exp(-Mathf.Pow((edge-.20f-y*1.24f)*9,2))*Mathf.Clamp01(1-y*2.5f);
                float upper=Mathf.Exp(-Mathf.Pow((edge-1.28f+y*.72f)*10,2))*Mathf.Clamp01((y-.66f)*3);
                float crease=.017f*Mathf.Sin(x*(snack?7:12)+phase+y*9)*body;
                float tension=(snack?.085f:.045f)*(tuck+upper);
                float z=radius*(1-(coffee?.53f:.71f)*Mathf.Pow(edge,4))+crease*(1-edge)-tension;
                float width=seal?.95f:coffee?1f:1-.052f*Mathf.Abs(y-.48f)*2;
                float bend=.012f*Mathf.Sin(phase+y*4)*body;
                float yEdge=(r==0||r==rows.Length-1)?.0018f*(c%2):0;
                p.Add(new Vector3(x*.5f*width+bend,y+yEdge,(face==0?-1:1)*Mathf.Max(.012f,z)));
                uv.Add(new Vector2(face==0?u:1-u,y));
                if(r==0||c==0)continue;
                int d=p.Count-1,b=d-columns-1;
                if(face==0)tri.AddRange(new[]{b-1,d-1,b,b,d-1,d});else tri.AddRange(new[]{b-1,b,d-1,b,d,d-1});
            }
            int half=p.Count/2;
            void Join(int a,int b){tri.AddRange(new[]{a,b+half,b,a,a+half,b+half});}
            for(int r=0;r<rows.Length-1;r++){int a=r*(columns+1),b=a+columns+1;Join(a,b);Join(b+columns,a+columns);}
            for(int c=0;c<columns;c++){Join(c+1,c);Join(half-columns-1+c,half-columns+c);}
            mesh=new Mesh{name=key};mesh.SetVertices(p);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[path]=mesh;return mesh;
    }
    static void FilmSurface(Material mat,int family,int variant)
    {
        string key="RetailFilmReliefV3_"+family+"_"+(variant%3);
        if(mat.GetTexture("_BumpMap") && mat.GetTexture("_BumpMap").name==key)return;
        // Fine pressed jaws and small foil creases sit below the readable print.
        // Main silhouettes are in the mesh; this is a shared material detail.
        mat.SetTexture("_BumpMap",null);
        FineRelief(mat,key,(u,v)=>{
            bool gusset=family==15||family==9||family==29||family==3;
            float jaw=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.91f,.97f,v))+(gusset?0:1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,.075f,v)));
            float seal=jaw*.015f*Mathf.Sin(u*96*Mathf.PI);
            float corner=Mathf.Pow(Mathf.Abs(u*2-1),5)*Mathf.Sin(v*51+u*17+variant);
            return seal+corner*.008f+.003f*Mathf.Sin(u*33+v*16+variant)*Mathf.Sin(v*Mathf.PI);
        },.48f);
        mat.SetFloat("_Smoothness",family==15?.25f:family==3?.38f:.43f);
    }
}
