using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class MarketPrototypeBuilderV7
{
    static Texture2D FinishTexture(string name,Func<float,float,Color> pixel)
    {
        string path="Assets/OutOfService/Generated/"+name+".asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(texture)return texture;
        const int size=512;
        texture=new Texture2D(size,size,TextureFormat.RGBA32,true,true){name=name,wrapMode=TextureWrapMode.Repeat,anisoLevel=4};
        var colors=new Color32[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)colors[y*size+x]=pixel((float)x/size,(float)y/size);
        texture.SetPixels32(colors);texture.Apply();
        AssetDatabase.CreateAsset(texture,path);return texture;
    }

    static float GroutRelief(float u,float v)
    {
        float x=Mathf.Abs(Mathf.Repeat(u*4+.5f,1)-.5f);
        float y=Mathf.Abs(Mathf.Repeat(v*4+.5f,1)-.5f);
        return -Mathf.Exp(-Mathf.Pow(Mathf.Min(x,y)*95,2));
    }

    static void FloorFinish()
    {
        floor.EnableKeyword("_NORMALMAP");
        floor.SetTexture("_BumpMap",FinishTexture("PorcelainGroutNormal",(u,v)=>{
            const float delta=1f/512;
            var n=new Vector3((GroutRelief(u-delta,v)-GroutRelief(u+delta,v))*.38f,
                (GroutRelief(u,v-delta)-GroutRelief(u,v+delta))*.38f,1).normalized;
            return new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
        }));
        floor.SetFloat("_BumpScale",.55f);
        floor.EnableKeyword("_METALLICSPECGLOSSMAP");
        floor.SetTexture("_MetallicGlossMap",FinishTexture("PorcelainWearGloss",(u,v)=>{
            float wear=Mathf.PerlinNoise(u*22+17,v*22+3);
            float brushed=Mathf.PerlinNoise(u*2+25,v*85);
            return new Color(0,0,0,Mathf.Clamp01(.79f-.16f*wear-.11f*brushed+GroutRelief(u,v)*.55f));
        }));
        floor.SetFloat("_Smoothness",.87f);
        // Set before SizedFloor clones the master material for each slab.
        floor.SetTexture("_OcclusionMap",FinishTexture("SpatialPorcelainGroutAO_V1",(u,v)=>{
            float value=1+GroutRelief(u,v)*.50f;return new Color(value,value,value,1);
        }));floor.SetFloat("_OcclusionStrength",.7f);floor.EnableKeyword("_OCCLUSIONMAP");
    }

    static void ShopSurfaceFinish()
    {
        var steel=Mat("brushed stainless shop fittings",new Color(.61f,.64f,.65f),.85f,.55f);
        steel.EnableKeyword("_METALLICSPECGLOSSMAP");
        steel.SetTexture("_MetallicGlossMap",FinishTexture("BrushedSteelGloss",(u,v)=>{
            float brush=Mathf.PerlinNoise(u*4+29,v*240);
            return new Color(.88f,0,0,.54f+.32f*brush);
        }));
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>()){
            if(renderer.sharedMaterial==wornMetal && !renderer.name.Contains("locker") && !renderer.name.Contains("pallet"))
                renderer.sharedMaterial=steel;
            if(renderer.name=="automatic door aluminium stile" || renderer.name.StartsWith("moving ") ||
                renderer.name.EndsWith("stainless cutting table") || renderer.name=="register counter top"){
                var filter=renderer.GetComponent<MeshFilter>();
                if(!filter || !filter.sharedMesh || filter.sharedMesh.name!="Cube")continue;
                var dimensions=renderer.transform.localScale;
                filter.sharedMesh=SoftShopEdge(dimensions);
                if(renderer.TryGetComponent<BoxCollider>(out var collider))collider.size=Vector3.Scale(collider.size,dimensions);
                renderer.transform.localScale=Vector3.one;
            }
        }
    }

    static Mesh SoftShopEdge(Vector3 dimensions)
    {
        string path="Assets/OutOfService/Generated/ShopEdge_"+dimensions.ToString("F3").Replace(" ","")+".asset";
        if(handmadeMeshes.TryGetValue(path,out var mesh))return mesh;
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){
            var verts=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            var half=dimensions*.5f;
            float radius=Mathf.Min(.004f,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.22f);
            foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back}){
                var right=Vector3.Cross(normal,Mathf.Abs(normal.y)>.5f?Vector3.forward:Vector3.up);
                var up=Vector3.Cross(normal,right);
                float w=Vector3.Scale(right,half).magnitude,h=Vector3.Scale(up,half).magnitude;
                float[] xs={-w,-w+radius,w-radius,w},ys={-h,-h+radius,h-radius,h};
                int first=verts.Count;
                for(int y=0;y<4;y++)for(int x=0;x<4;x++){
                    var p=Vector3.Scale(normal,half)+right*xs[x]+up*ys[y];
                    var core=new Vector3(Mathf.Clamp(p.x,-half.x+radius,half.x-radius),
                        Mathf.Clamp(p.y,-half.y+radius,half.y-radius),Mathf.Clamp(p.z,-half.z+radius,half.z-radius));
                    var n=(p-core).normalized;
                    verts.Add(core+n*radius);normals.Add(n);uv.Add(new Vector2((xs[x]+w)/(2*w),(ys[y]+h)/(2*h)));
                    if(x==0 || y==0)continue;
                    int d=first+y*4+x;
                    tris.AddRange(new[]{d-5,d-4,d-1,d-4,d,d-1});
                }
            }
            mesh=new Mesh();mesh.SetVertices(verts);mesh.SetNormals(normals);mesh.SetUVs(0,uv);
            mesh.SetTriangles(tris,0);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        if(mesh.tangents.Length!=mesh.vertexCount){mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);}
        handmadeMeshes[path]=mesh;return mesh;
    }

    static void BakeShopReflections()
    {
        BakeRoomReflections();
    }
}
