using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    static Material InformationInk(int family,int variant,bool compact=false)
    {
        var ink=RetailSurface($"Consumer information {(compact?"compact ":"")}{family}-{variant}",Color.white,0,.18f);
        if(!ink.mainTexture){
            string path=$"Assets/OutOfService/Art/Packs/{(compact?"CompactInformation":"RetailInformation")}{family}.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer.textureCompression!=TextureImporterCompression.CompressedHQ || importer.anisoLevel!=4 || importer.npotScale!=TextureImporterNPOTScale.None){
                importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.compressionQuality=100;
                importer.npotScale=TextureImporterNPOTScale.None;
                importer.anisoLevel=4;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();
            }
            ink.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            ink.mainTextureScale=new Vector2(.25f,.5f);
            ink.mainTextureOffset=new Vector2(variant%4*.25f,variant<4?.5f:0);
        }
        return ink;
    }

    // Bags have two separate film panels. Keep the existing face print; unwrap
    // the reverse panel and the narrow edge returns independently for inspection.
    static void PrintHeldPacket(MeshFilter filter,Material info,int family,int variant)
    {
        var source=filter.sharedMesh;
        bool flat=filter.transform.localScale.x>2*filter.transform.localScale.y;
        string path="Assets/OutOfService/Generated/Held_"+source.name+(flat?"_compact2":"")+".asset";
        if(!handmadeMeshes.TryGetValue(path,out var mesh)){
            mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){
                var p=source.vertices;var uv=source.uv;int half=p.Length/2;
                var verts=new List<Vector3>();var coords=new List<Vector2>();
                var front=new List<int>();var reverse=new List<int>();
                var tri=source.triangles;
                for(int t=0;t<tri.Length;t+=3){
                    bool a=tri[t]<half,b=tri[t+1]<half,c=tri[t+2]<half;
                    bool face=a && b && c,back=!a && !b && !c;
                    var target=face?front:reverse;
                    for(int j=0;j<3;j++){
                        int k=tri[t+j];target.Add(verts.Count);verts.Add(p[k]);
                        if(face)coords.Add(uv[k]);
                        else if(back)coords.Add(flat?new Vector2(.005f+.99f*uv[k].x,.302f+.693f*uv[k].y):new Vector2(.004f+.726f*uv[k].x,uv[k].y));
                        else{
                            float u=(k>=half)==(p[k].x>0)?1:0;
                            coords.Add(flat?new Vector2(.005f+.99f*u,.005f+.240f*uv[k].y):
                                new Vector2(.756f+.239f*u,uv[k].y));
                        }
                    }
                }
                mesh=new Mesh{name="two sided printed packet"};mesh.SetVertices(verts);mesh.SetUVs(0,coords);
                mesh.subMeshCount=2;mesh.SetTriangles(front,0);mesh.SetTriangles(reverse,1);
                // Split UV islands must retain the smooth shading from the film mold.
                var normals=source.normals;var copy=new Vector3[tri.Length];
                for(int i=0;i<tri.Length;i++)copy[i]=normals[tri[i]];
                mesh.normals=copy;mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            handmadeMeshes[path]=mesh;
        }
        filter.sharedMesh=mesh;var renderer=filter.GetComponent<MeshRenderer>();
        renderer.sharedMaterials=new[]{renderer.sharedMaterial,flat?InformationInk(family,variant,true):info};
    }

    static void PrintHeldUnit(Transform unit,int family,int variant)
    {
        if(family==2){
            var bottomPanel=MallMesh("rice_bottom_information_v1",(v,u,t)=>{
                v.AddRange(new[]{new Vector3(-.0245f,0,-.032f),new Vector3(.0245f,0,-.032f),new Vector3(.0245f,0,.032f),new Vector3(-.0245f,0,.032f)});
                u.AddRange(new[]{new Vector2(.004f,.995f),new Vector2(.730f,.995f),new Vector2(.730f,.005f),new Vector2(.004f,.005f)});
                t.AddRange(new[]{0,1,2,0,2,3});
            });
            HandmadeProp("rice underside product information",bottomPanel,unit.position+Vector3.down*.00025f,Vector3.one,InformationInk(family,variant),unit);
            return;
        }
        if(HasFrozenFilm(family,variant))return;
        if(unit.Find("fitted full wrap consumer information"))return;
        if(unit.Find("carton printed reverse"))return; // All three faces already have fitted print.
        var ink=InformationInk(family,variant);
        var generic=unit.Find("ingredients and scan panel");
        if(generic)Object.DestroyImmediate(generic.gameObject);
        var filters=unit.GetComponentsInChildren<MeshFilter>();
        foreach(var f in filters)
            if(f.sharedMesh && (AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/OutOfService/Generated/NoodleWrapper_",System.StringComparison.Ordinal)||
                AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/OutOfService/Generated/RetailFilmV",System.StringComparison.Ordinal))){
                PrintHeldPacket(f,ink,family,variant);return;
            }
        // Sample the actual outside of the assembled bottle, tray, carton or
        // multipack. This accounts for taper, folded sleeves and curved shoulders.
        var bounds=ModelBounds(unit.gameObject);
        string path=$"Assets/OutOfService/Generated/HeldPanels_{family}_{variant}.asset";
        if(!handmadeMeshes.TryGetValue(path,out var panel)){
            panel=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            {
                var verts=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();var compactTris=new List<int>();
                foreach(var normal in new[]{Vector3.forward,Vector3.right,Vector3.left})
                    InformationPanel(unit,filters,bounds,normal,family,verts,uv,tris,compactTris);
                if(tris.Count+compactTris.Count==0)throw new System.InvalidOperationException("Empty retail information surface "+unit.name);
                bool create=!panel;
                if(create)panel=new Mesh();else panel.Clear();
                panel.SetVertices(verts);panel.SetUVs(0,uv);panel.subMeshCount=2;
                panel.SetTriangles(tris,0);panel.SetTriangles(compactTris,1);panel.RecalculateNormals();panel.RecalculateBounds();
                if(create)AssetDatabase.CreateAsset(panel,path);else EditorUtility.SetDirty(panel);
            }
            handmadeMeshes[path]=panel;
        }
        var label=new GameObject("rear and gusset printing");label.transform.SetParent(unit,false);
        label.AddComponent<MeshFilter>().sharedMesh=panel;
        label.AddComponent<MeshRenderer>().sharedMaterials=new[]{ink,panel.GetIndexCount(1)>0?InformationInk(family,variant,true):ink};
    }

    static void CartonInformation(Vector3 p,Vector3 size,int family,int variant,Transform parent)
    {
        foreach(int face in new[]{0,1,2}){
            bool back=face==0;
            string path="Assets/OutOfService/Generated/CartonInfoFace_"+(back?"rear":"side")+".asset";
            if(!handmadeMeshes.TryGetValue(path,out var mesh)){
                mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh){
                    float left=back?.004f:.755f,right=back?.731f:.995f;
                    mesh=new Mesh{vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0)},
                        uv=new[]{new Vector2(left,.005f),new Vector2(right,.005f),new Vector2(left,.995f),new Vector2(right,.995f)},triangles=new[]{0,2,1,1,2,3}};
                    mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
                }
                handmadeMeshes[path]=mesh;
            }
            int sign=face==1?1:-1;
            var at=p+new Vector3(back?0:sign*(size.x*.5f+.0007f),size.y*.5f,back?size.z*.5f+.0007f:0);
            var panel=HandmadeProp(back?"carton printed reverse":"carton printed side",mesh,at,new Vector3(back?size.x:size.z,size.y,1),InformationInk(family,variant),parent);
            panel.transform.rotation=Quaternion.Euler(0,back?180:-sign*90,0);
        }
    }

    static void InformationPanel(Transform unit,MeshFilter[] filters,Bounds bounds,Vector3 normal,int family,
        List<Vector3> vertices,List<Vector2> uv,List<int> triangles,List<int> compactTriangles)
    {
        // The information patch follows the faceted mold. Coarse samples cut
        // through tapered cups between samples and made the print look torn.
        const int nx=32,ny=24;
        var right=Vector3.Cross(normal,Vector3.up);
        bool back=normal==Vector3.forward;
        float width=(back?bounds.size.x:bounds.size.z)*(back?.86f:.57f);
        bool bottle=family>=10 && family<=14 || family==19 || family==27;
        float height=bounds.size.y*(bottle?.40f:.66f);
        bool compact=width/height>(back?1.38f:.78f);
        var faceTriangles=compact?compactTriangles:triangles;
        float aspect=compact?(back?1.75f:4.75f):(back?.735f:.248f);
        width=Mathf.Min(width,height*aspect);
        height=Mathf.Min(height,width/aspect);
        Vector3 center=bounds.center-Vector3.up*bounds.size.y*(bottle?.08f:.085f);
        var depth=new float[(nx+1)*(ny+1)];System.Array.Fill(depth,float.NegativeInfinity);
        foreach(var filter in filters){
            var mesh=filter.sharedMesh;if(!mesh)continue;
            var p=mesh.vertices;
            for(int i=0;i<p.Length;i++){
                Vector3 v=filter.transform.TransformPoint(p[i])-center;
                p[i]=new Vector3(Vector3.Dot(v,right)/width+.5f,v.y/height+.5f,Vector3.Dot(v,normal));
            }
            var t=mesh.triangles;
            for(int i=0;i<t.Length;i+=3){
                var a=p[t[i]];var b=p[t[i+1]]-a;var c=p[t[i+2]]-a;
                float det=b.x*c.y-b.y*c.x;
                if(Mathf.Abs(det)<.00001f)continue;
                int x0=Mathf.Max(0,Mathf.CeilToInt(nx*(a.x+Mathf.Min(0,b.x,c.x))));
                int x1=Mathf.Min(nx,Mathf.FloorToInt(nx*(a.x+Mathf.Max(0,b.x,c.x))));
                int y0=Mathf.Max(0,Mathf.CeilToInt(ny*(a.y+Mathf.Min(0,b.y,c.y))));
                int y1=Mathf.Min(ny,Mathf.FloorToInt(ny*(a.y+Mathf.Max(0,b.y,c.y))));
                for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){
                    float dx=(float)x/nx-a.x,dy=(float)y/ny-a.y;
                    float u=(dx*c.y-dy*c.x)/det,v=(b.x*dy-b.y*dx)/det;
                    if(u<-.0001f || v<-.0001f || u+v>1.0001f)continue;
                    int j=y*(nx+1)+x;depth[j]=Mathf.Max(depth[j],a.z+b.z*u+c.z*v);
                }
            }
        }
        int first=vertices.Count;
        for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++){
            int j=y*(nx+1)+x;float u=(float)x/nx,v=(float)y/ny;
            bool hit=!float.IsNegativeInfinity(depth[j]);
            vertices.Add(unit.InverseTransformPoint(center+right*((u-.5f)*width)+Vector3.up*((v-.5f)*height)+normal*(hit?depth[j]+.0013f:0)));
            if(compact)uv.Add(new Vector2(.005f+u*.99f,Mathf.Lerp(back?.302f:.005f,back?.995f:.245f,v)));
            else uv.Add(new Vector2(back?.004f+u*.727f:.755f+u*.24f,Mathf.Lerp(.005f,.995f,v)));
            if(!hit || x==0 || y==0)continue;
            int b=j-nx-1;
            if(float.IsNegativeInfinity(depth[b-1]) || float.IsNegativeInfinity(depth[b]) || float.IsNegativeInfinity(depth[j-1]))continue;
            faceTriangles.AddRange(new[]{first+b-1,first+j-1,first+b,first+b,first+j-1,first+j});
        }
    }
}
