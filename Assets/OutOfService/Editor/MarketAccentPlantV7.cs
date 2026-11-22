using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    const string AccentFolder="Assets/OutOfService/Art/Models/potted_plant_04/";
    static Mesh accentPlant;static Material accentPlantMaterial;
    static void PrepareAccentPlant()
    {
        if(accentPlant && accentPlantMaterial)return;
        string fbx=AccentFolder+"potted_plant_04.fbx";
        AssetDatabase.ImportAsset(fbx);
        var model=(ModelImporter)AssetImporter.GetAtPath(fbx);
        if(model.materialImportMode!=ModelImporterMaterialImportMode.None || !model.isReadable){
            model.materialImportMode=ModelImporterMaterialImportMode.None;model.isReadable=true;
            model.importAnimation=false;model.SaveAndReimport();
        }
        const string meshPath="Assets/OutOfService/Generated/AccentSucculentV1.asset";
        accentPlant=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(!accentPlant){
            var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
            try{
                instance.transform.position=Vector3.zero;
                var combine=new List<CombineInstance>();
                foreach(var filter in instance.GetComponentsInChildren<MeshFilter>()){
                    var input=filter.sharedMesh;
                    if(input.uv.Length!=input.vertexCount)throw new InvalidOperationException("Accent plant UVs missing");
                    foreach(var uv in input.uv)if(float.IsNaN(uv.x)||float.IsNaN(uv.y))throw new InvalidOperationException("Accent plant invalid UV");
                    for(int s=0;s<input.subMeshCount;s++)combine.Add(new CombineInstance{mesh=input,subMeshIndex=s,transform=filter.transform.localToWorldMatrix});
                }
                accentPlant=new Mesh{name="CC0 shared succulent 30cm"};accentPlant.CombineMeshes(combine.ToArray(),true,true);accentPlant.RecalculateBounds();
                if(accentPlant.vertexCount>25000 || accentPlant.triangles.Length/3>12000)throw new InvalidOperationException("Accent plant exceeds adopted mesh budget");
                var bounds=accentPlant.bounds;
                if(bounds.size.y<.0001f)throw new InvalidOperationException("Invalid accent plant dimensions");
                var vertices=accentPlant.vertices;
                for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z))*(.30f/bounds.size.y);
                accentPlant.vertices=vertices;accentPlant.RecalculateBounds();accentPlant.RecalculateTangents();
                AssetDatabase.CreateAsset(accentPlant,meshPath);
                Directory.CreateDirectory("Review");
                File.WriteAllText("Review/accent-plant-import-2026-11-21.txt",$"Source: {fbx}\nVertices: {accentPlant.vertexCount}\nTriangles: {accentPlant.triangles.Length/3}\nSource bounds: {bounds.size}\nAdopted bounds: {accentPlant.bounds.size}\nUV count: {accentPlant.uv.Length}\nOpaque URP Lit; shared mesh and 1K maps; no per-instance colliders or scripts.\n");
            }finally{UnityEngine.Object.DestroyImmediate(instance);}
        }
        Texture2D Import(string file,bool normal,bool srgb,bool readable=false){
            string path=AccentFolder+file;AssetDatabase.ImportAsset(path);
            var ti=(TextureImporter)AssetImporter.GetAtPath(path);
            var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            if(ti.textureType!=type || ti.maxTextureSize!=1024 || ti.sRGBTexture!=srgb || ti.isReadable!=readable || ti.textureCompression!=TextureImporterCompression.CompressedHQ){
                ti.textureType=type;ti.maxTextureSize=1024;ti.sRGBTexture=srgb;ti.isReadable=readable;
                ti.mipmapEnabled=true;ti.anisoLevel=4;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        var colour=Import("potted_plant_04_diff_1k.jpg",false,true);
        var normal=Import("potted_plant_04_nor_gl_1k.png",true,false);
        const string packed="potted_plant_04_urp_mask.png";
        if(!File.Exists(AccentFolder+packed)){
            var arm=Import("potted_plant_04_arm_1k.png",false,false,true);
            var pixels=arm.GetPixels32();
            for(int i=0;i<pixels.Length;i++){var p=pixels[i];pixels[i]=new Color32(p.b,p.r,0,(byte)(255-p.g));}
            var output=new Texture2D(arm.width,arm.height,TextureFormat.RGBA32,false,true);output.SetPixels32(pixels);output.Apply();
            File.WriteAllBytes(AccentFolder+packed,output.EncodeToPNG());UnityEngine.Object.DestroyImmediate(output);
        }
        // The source channel map is not needed as a readable runtime texture.
        Import("potted_plant_04_arm_1k.png",false,false);
        var mask=Import(packed,false,false);
        accentPlantMaterial=RetailSurface("CC0 succulent ceramic gravel foliage",Color.white,1,1);
        accentPlantMaterial.mainTexture=colour;
        accentPlantMaterial.SetTexture("_BumpMap",normal);accentPlantMaterial.SetFloat("_BumpScale",.8f);accentPlantMaterial.EnableKeyword("_NORMALMAP");
        accentPlantMaterial.SetTexture("_MetallicGlossMap",mask);accentPlantMaterial.EnableKeyword("_METALLICSPECGLOSSMAP");
        accentPlantMaterial.SetTexture("_OcclusionMap",mask);accentPlantMaterial.SetFloat("_OcclusionStrength",.8f);accentPlantMaterial.EnableKeyword("_OCCLUSIONMAP");
        accentPlantMaterial.enableInstancing=true;
    }
    static void AccentSucculent(Vector3 p,float angle,float scale=1)
    {
        PrepareAccentPlant();
        HandmadeProp("small nursery succulent - Poly Haven CC0",accentPlant,p,Vector3.one*scale,accentPlantMaterial,root).transform.rotation=Quaternion.Euler(0,angle,0);
    }
}
