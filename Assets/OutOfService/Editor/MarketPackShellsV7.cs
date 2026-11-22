using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    // Normalized package footprint; height is supplied per SKU. Each outline
    // shares the same corner samples so the tucked rim and draft stay continuous.
    static Mesh ProduceShell(bool film,bool sleeve)
    {
        string path="Assets/OutOfService/Generated/ChillerShell_"+(film?"wrap":"tray")+(sleeve?"Soft":"Formed")+".asset";
        if(handmadeMeshes.TryGetValue(path,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh)
        {
            var verts=new List<Vector3>();var faces=new List<int>();var uv=new List<Vector2>();
            const int corners=5,perimeter=corners*4;
            void Ring(float w,float d,float y,float radius,float crease=0)
            {
                int first=verts.Count;
                for(int k=0;k<4;k++) for(int j=0;j<corners;j++)
                {
                    float a=(k+(float)j/(corners-1))*Mathf.PI*.5f;
                    float x=(k==0||k==3?1:-1)*(w-radius)+radius*Mathf.Cos(a);
                    float z=(k<2?1:-1)*(d-radius)+radius*Mathf.Sin(a);
                    verts.Add(new Vector3(x,y+crease*Mathf.Sin(3*a+.5f),z));
                    uv.Add(new Vector2(x+.5f,z+.5f));
                    if(first==0)continue;
                    int i=k*corners+j,n=(i+1)%perimeter;
                    int lo=first-perimeter;
                    faces.Add(lo+i);faces.Add(first+i);faces.Add(lo+n);
                    faces.Add(lo+n);faces.Add(first+i);faces.Add(first+n);
                }
            }
            if(!film)
            {
                Ring(.445f,.447f,0,.096f);Ring(.50f,.50f,.88f,.081f);
                Ring(.496f,.497f,1,.080f);Ring(.472f,.473f,.98f,.068f);
                Ring(.421f,.423f,.17f,.079f);
            }
            else if(sleeve)
            {
                Ring(.493f,.50f,.12f,.026f);Ring(.49f,.45f,.32f,.046f,.08f);
                Ring(.445f,.39f,.78f,.082f,.09f);Ring(.33f,.30f,.98f,.09f,.02f);
            }
            else
            {
                Ring(.496f,.496f,.31f,.08f);Ring(.50f,.50f,.56f,.08f);
                Ring(.435f,.432f,.90f,.076f,.025f);Ring(.32f,.32f,.99f,.083f);
            }
            int ring=verts.Count-perimeter,center=verts.Count;
            verts.Add(new Vector3(0,film?1:.17f,0));uv.Add(new Vector2(.5f,.5f));
            for(int i=0;i<perimeter;i++)
            { faces.Add(center);faces.Add(ring+(i+1)%perimeter);faces.Add(ring+i); }
            mesh=new Mesh{name=film?"tensioned food film":"thermoformed produce punnet"};
            mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.SetTriangles(faces,0);mesh.RecalculateNormals();
            mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[path]=mesh;return mesh;
    }

    static Material ChilledWrap()
    {
        const string key="clear creased chiller wrap";
        if(retailSurfaces.TryGetValue(key,out var mat))return mat;
        mat=TransparentMat(key,new Color(.82f,.89f,.93f,.073f));
        mat.SetFloat("_Smoothness",.65f);
        retailSurfaces.Add(key,mat);
        return mat;
    }
}
