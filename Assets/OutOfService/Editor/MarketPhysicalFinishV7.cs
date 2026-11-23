using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    const string SurveyedFinishFolder="Assets/OutOfService/Art/Textures/SurveyedPBR/";

    static void PreparePhysicalFinishImports()
    {
        var paths=new List<string>(Directory.GetFiles(SurveyedFinishFolder,"*.jpg"));
        paths.AddRange(Directory.GetFiles("Assets/OutOfService/Art/Textures","*_nor_dx.jpg"));
        foreach(string file in paths){
            string path=file.Replace('\\','/');
            AssetDatabase.ImportAsset(path);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer)throw new InvalidOperationException("Missing finish importer: "+path);
            bool normal=path.Contains("_nor_"),linear=normal||path.Contains("_arm_");
            bool flip=path.Contains("_nor_dx");
            bool changed=importer.textureType!=(normal?TextureImporterType.NormalMap:TextureImporterType.Default)
                ||importer.sRGBTexture==linear||importer.flipGreenChannel!=flip||importer.anisoLevel!=8
                ||importer.filterMode!=FilterMode.Trilinear||!importer.mipmapEnabled||importer.maxTextureSize!=2048
                ||importer.textureCompression!=TextureImporterCompression.CompressedHQ;
            if(!changed)continue;
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=!linear;importer.flipGreenChannel=flip;
            importer.anisoLevel=8;importer.filterMode=FilterMode.Trilinear;importer.mipmapEnabled=true;
            importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
    }

    static Texture2D SurveyedMasks(string id)
    {
        string path="Assets/OutOfService/Generated/SurveyedMasksV1_"+id+".asset";
        var saved=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(saved)return saved;
        // Decode the original data map in linear space. The ARM source is retained
        // unchanged; a shared, compressed 1K Unity mask has metal R, AO G, gloss A.
        var input=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        if(!ImageConversion.LoadImage(input,File.ReadAllBytes(SurveyedFinishFolder+id+"_arm_2k.jpg")))
            throw new InvalidOperationException("Cannot decode surveyed mask: "+id);
        const int size=1024;var pixels=new Color32[size*size];var source=input.GetPixels32();
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){
            int sx=x*input.width/size,sy=y*input.height/size;
            var a=source[sy*input.width+sx];var b=source[sy*input.width+Math.Min(sx+1,input.width-1)];
            var c=source[Math.Min(sy+1,input.height-1)*input.width+sx];var d=source[Math.Min(sy+1,input.height-1)*input.width+Math.Min(sx+1,input.width-1)];
            float rough=(a.g+b.g+c.g+d.g)/(4f*255);
            float gloss=id=="white_oak_veneer"?.55f+.20f*(1-rough):1-rough;
            pixels[y*size+x]=new Color32((byte)((a.b+b.b+c.b+d.b)/4),(byte)((a.r+b.r+c.r+d.r)/4),0,(byte)Mathf.RoundToInt(gloss*255));
        }
        UnityEngine.Object.DestroyImmediate(input);
        saved=new Texture2D(size,size,TextureFormat.RGBA32,true,true){name="SurveyedMasksV1_"+id,anisoLevel=8,filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Repeat};
        saved.SetPixels32(pixels);saved.Apply();
        EditorUtility.CompressTexture(saved,TextureFormat.DXT5,TextureCompressionQuality.Best);
        saved.Apply(false,true);AssetDatabase.CreateAsset(saved,path);return saved;
    }

    static void BindSurveyedMasks(Material material,string id)
    {
        var masks=SurveyedMasks(id);
        material.SetTexture("_MetallicGlossMap",masks);material.SetTexture("_OcclusionMap",masks);
        material.SetFloat("_Smoothness",1);material.SetFloat("_OcclusionStrength",.65f);
        material.EnableKeyword("_METALLICSPECGLOSSMAP");material.EnableKeyword("_OCCLUSIONMAP");
    }

    static Material OakVeneer(Color tint)
    {
        var mat=RetailSurface("surveyed oak veneer v1 "+ColorUtility.ToHtmlStringRGB(tint),Color.Lerp(Color.white,tint,.40f),0,.65f);
        mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(SurveyedFinishFolder+"white_oak_veneer_Diffuse_2k.jpg");
        mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(SurveyedFinishFolder+"white_oak_veneer_nor_gl_2k.jpg"));
        mat.SetFloat("_BumpScale",.24f);mat.EnableKeyword("_NORMALMAP");BindSurveyedMasks(mat,"white_oak_veneer");
        return mat;
    }

    static void PhysicalSurfaceScale()
    {
        var seen=new HashSet<Material>();int changed=0;
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true)){
            var mat=renderer.sharedMaterial;if(!mat||renderer.sharedMaterials.Length!=1)continue;
            bool wood=mat.name.StartsWith("surveyed oak veneer v1 ");
            bool plaster=mat==wall||mat.name.StartsWith("B1 finish v4 paint ");
            bool concrete=mat==wornFloor,metal=mat==wornMetal;
            if(!wood&&!plaster&&!concrete&&!metal)continue;
            var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh||!filter.sharedMesh.isReadable)continue;
            if(seen.Add(mat)){
                if(!wood)BindSurveyedMasks(mat,plaster?"painted_plaster_wall":concrete?"worn_concrete_floor":"metal_plate_02");
                foreach(string property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap","_OcclusionMap"})mat.SetTextureScale(property,Vector2.one);
            }
            var original=filter.sharedMesh;var scale=renderer.transform.lossyScale;
            scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
            var size=Vector3.Scale(original.bounds.size,scale);
            string identity=AssetDatabase.GetAssetPath(original)+"|"+original.name+"|"+scale.ToString("F4")+"|"+wood;
            string key="PhysicalUVV1_"+Hash128.Compute(identity);
            string path="Assets/OutOfService/Generated/"+key+".asset";
            if(!handmadeMeshes.TryGetValue(key,out var mesh)){
                mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh){
                    mesh=UnityEngine.Object.Instantiate(original);mesh.name=key;
                    var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
                    float metres=wood?.5f:2f;
                    for(int i=0;i<uv.Length;i++){
                        var p=Vector3.Scale(vertices[i],scale);var n=normals[i];Vector2 q;bool swap;
                        if(Mathf.Abs(n.y)>=Mathf.Max(Mathf.Abs(n.x),Mathf.Abs(n.z))){q=new Vector2(p.x,p.z);swap=size.x>size.z;}
                        else if(Mathf.Abs(n.x)>=Mathf.Abs(n.z)){q=new Vector2(p.z,p.y);swap=size.z>size.y;}
                        else{q=new Vector2(p.x,p.y);swap=size.x>size.y;}
                        // Oak grain runs along V. Orient it along the long cut of each board.
                        if(wood&&swap)q=new Vector2(q.y,q.x);
                        uv[i]=q/metres;
                    }
                    mesh.uv=uv;mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);
                }
                handmadeMeshes[key]=mesh;
            }
            // Only UVs change. Articulated hinges, collider dimensions and object scale stay intact.
            filter.sharedMesh=mesh;changed++;
        }
        Debug.Log("Physical surface UVs: "+changed+" renderers, "+seen.Count+" shared finishes");
    }
}
