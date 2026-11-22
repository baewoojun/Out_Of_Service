using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public static partial class MarketPrototypeBuilderV7
{
    static readonly Dictionary<string,Mesh> retailLodMeshes=new Dictionary<string,Mesh>();
    static Mesh ClusterRetailMesh(Mesh source,string key,int resolution)
    {
        string path="Assets/OutOfService/Generated/LODv2_"+key+"_"+resolution+".asset";
        if(retailLodMeshes.TryGetValue(path,out var cached))return cached;
        var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved!=null){retailLodMeshes[path]=saved;return saved;}
        if(!source.isReadable || source.subMeshCount!=1)return source;
        var vertices=source.vertices;var normals=source.normals;var uvs=source.uv;
        var ids=new Dictionary<(int,int,int,int,int),int>();var remap=new int[vertices.Length];
        var sums=new List<Vector3>();var ns=new List<Vector3>();var tex=new List<Vector2>();var count=new List<int>();
        Bounds bounds=source.bounds;float cell=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z))/resolution;
        if(cell<=0)return source;
        for(int i=0;i<vertices.Length;i++)
        {
            Vector3 p=(vertices[i]-bounds.min)/cell;Vector2 uv=uvs.Length==vertices.Length?uvs[i]:Vector2.zero;
            // Keep UV island boundaries apart to avoid smearing the scan texture over seams.
            var k=(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y),Mathf.RoundToInt(p.z),Mathf.FloorToInt(uv.x*8),Mathf.FloorToInt(uv.y*8));
            if(!ids.TryGetValue(k,out int j)){j=sums.Count;ids.Add(k,j);sums.Add(Vector3.zero);ns.Add(Vector3.zero);tex.Add(Vector2.zero);count.Add(0);}
            remap[i]=j;sums[j]+=vertices[i];ns[j]+=normals.Length==vertices.Length?normals[i]:Vector3.up;tex[j]+=uv;count[j]++;
        }
        for(int i=0;i<sums.Count;i++){sums[i]/=count[i];ns[i]=ns[i].normalized;tex[i]/=count[i];}
        var tris=new List<int>();var original=source.triangles;var unique=new HashSet<(int,int,int)>();
        for(int i=0;i<original.Length;i+=3)
        {
            int a=remap[original[i]],b=remap[original[i+1]],c=remap[original[i+2]];
            if(a==b||b==c||c==a)continue;
            if(Vector3.Cross(sums[b]-sums[a],sums[c]-sums[a]).sqrMagnitude<cell*cell*cell*cell*1e-8f)continue;
            if(!unique.Add((a,b,c)))continue;tris.Add(a);tris.Add(b);tris.Add(c);
        }
        if(tris.Count < 36) { Debug.LogWarning("LOD rejected for " + key + ": too few valid triangles; keeping source."); return source; }
        var mesh=new Mesh{name=System.IO.Path.GetFileNameWithoutExtension(path),indexFormat=IndexFormat.UInt32};
        mesh.SetVertices(sums);mesh.SetNormals(ns);mesh.SetUVs(0,tex);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();mesh.RecalculateTangents();
        AssetDatabase.CreateAsset(mesh,path);retailLodMeshes[path]=mesh;
        Debug.Log("Retail LOD "+key+" grid "+resolution+": "+original.Length/3+" -> "+tris.Count/3+" triangles");return mesh;
    }
    static void AddRetailModelLOD(GameObject instance,string id)
    {
        if(id.Contains("shelves")||id.Contains("cardboard"))return;
        int index=0;
        foreach(var filter in instance.GetComponentsInChildren<MeshFilter>())
        {
            Mesh original=filter.sharedMesh;var renderer=filter.GetComponent<MeshRenderer>();
            if(original==null||renderer==null||original.vertexCount<3000||original.subMeshCount!=1)continue;
            var group=filter.gameObject.AddComponent<LODGroup>();
            var levels=new LOD[3];levels[0]=new LOD(.16f,new Renderer[]{renderer});
            for(int level=1;level<=2;level++)
            {
                var child=new GameObject("retail scan LOD "+level);child.transform.SetParent(filter.transform,false);
                child.AddComponent<MeshFilter>().sharedMesh=ClusterRetailMesh(original,id+"_"+index,level==1?24:10);
                var r=child.AddComponent<MeshRenderer>();r.sharedMaterials=renderer.sharedMaterials;
                r.shadowCastingMode=level==2?ShadowCastingMode.Off:renderer.shadowCastingMode;
                levels[level]=new LOD(level==1?.035f:.004f,new Renderer[]{r});
            }
            group.SetLODs(levels);group.RecalculateBounds();index++;
        }
    }
}
