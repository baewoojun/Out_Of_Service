using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class MarketPrototypeBuilderV7
{
    static void BatchSmallProps()
    {
        int layer=LayerMask.NameToLayer("Market Small Props");
        if(layer<0)
        {
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers=tags.FindProperty("layers");
            for(int i=8;i<32;i++)if(string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
            { layers.GetArrayElementAtIndex(i).stringValue="Market Small Props";layer=i;tags.ApplyModifiedProperties();break; }
        }
        var groups=new Dictionary<string,List<MeshRenderer>>();
        var chillerFilm=ChilledWrap();
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            var filter=renderer.GetComponent<MeshFilter>();
            if(layer>=0 && (renderer.sharedMaterial==chillerFilm ||
                renderer.sharedMaterial && renderer.sharedMaterial.name=="clear beverage PET v1" && !renderer.GetComponentInParent<Rigidbody>()))
            {
                renderer.gameObject.layer=layer;
                continue; // Preserve per-package transparent sorting at close range.
            }
            if(filter==null || filter.sharedMesh==null || renderer.sharedMaterials.Length!=1 ||
                renderer.sharedMaterial==null || renderer.sharedMaterial.renderQueue>=3000 ||
                renderer.GetComponentInParent<Rigidbody>()!=null || renderer.GetComponentInParent<MonoBehaviour>()!=null || renderer.GetComponent<TextMesh>()!=null)continue;
            Vector3 size=renderer.bounds.size;
            bool fixture=renderer.name=="powder coated shelf upright" || renderer.name=="fixture flush end panel" ||
                renderer.name=="end panel folded edge" || renderer.name=="adjustable metal shelf" ||
                renderer.name=="recessed metal plinth" || renderer.name=="perforated shelf back" ||
                renderer.name=="shelf edge price rail" || renderer.name=="price strip retaining lip";
            if(!fixture && Mathf.Max(size.x,Mathf.Max(size.y,size.z))>1.25f)continue;
            bool moving=false;
            for(Transform t=renderer.transform;t!=null;t=t.parent)
                if(t.GetComponent<LODGroup>()!=null || t.GetComponent<OutOfService.MarketSwingDoor>()!=null || t.GetComponent<OutOfService.MarketProduct>()!=null ||
                    t.GetComponent<OutOfService.MarketPlayer>()!=null || t.GetComponent<OutOfService.MarketThreat>()!=null)
                { moving=true;break; }
            if(moving)continue;
            if(!fixture && layer>=0 && renderer.GetComponent<Collider>()==null)renderer.gameObject.layer=layer;
            // Keep photogrammetry meshes shared and instanced instead of expanding them into huge mesh assets.
            bool closeRose=filter.sharedMesh.name.StartsWith("MallDetailV3_rose_cupped_v9_");
            bool tailored=filter.sharedMesh.name.StartsWith("MallDetailV3_laundry_sewn_v9_")||filter.sharedMesh.name.StartsWith("MallDetailV3_tailoring_front_seams_v4_");
            if(filter.sharedMesh.vertexCount>(tailored?3200:closeRose?2000:1200) || filter.sharedMesh.subMeshCount!=1)continue;
            Vector3 center=renderer.bounds.center;
            string key=(fixture?"fixture_":"")+Mathf.FloorToInt(center.x/6)+"_"+Mathf.FloorToInt(center.y/5)+"_"+
                Mathf.FloorToInt(center.z/6)+"_"+renderer.sharedMaterial.name;
            // A print/trim deliberately excluded from shadow maps must retain
            // that state when merged with the same material in this sector.
            if(renderer.shadowCastingMode!=ShadowCastingMode.On || !renderer.receiveShadows)
                key+="_shadow_"+(int)renderer.shadowCastingMode+"_receive_"+renderer.receiveShadows;
            if(!groups.TryGetValue(key,out var list)){list=new List<MeshRenderer>();groups.Add(key,list);}
            list.Add(renderer);
        }
        const string folder="Assets/OutOfService/Generated/Batches";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/OutOfService/Generated","Batches");
        int index=0,merged=0;
        foreach(var entry in groups)
        {
            if(entry.Value.Count<3)continue;
            var combines=new List<CombineInstance>();
            foreach(var renderer in entry.Value)
                combines.Add(new CombineInstance {mesh=renderer.GetComponent<MeshFilter>().sharedMesh,transform=renderer.localToWorldMatrix});
            string assetName="retail_"+Hash128.Compute(entry.Key).ToString();
            Mesh mesh=new Mesh {name=assetName,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(combines.ToArray(),true,true);mesh.RecalculateBounds();
            string path=folder+"/"+assetName+".asset";
            Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null)AssetDatabase.CreateAsset(mesh,path);
            else
            {
                // Explicitly replace native mesh buffers; CopySerialized can leave stale GPU geometry.
                existing.Clear();existing.indexFormat=mesh.indexFormat;existing.name=mesh.name;
                existing.vertices=mesh.vertices;existing.normals=mesh.normals;existing.uv=mesh.uv;
                if(mesh.tangents.Length>0)existing.tangents=mesh.tangents;
                existing.triangles=mesh.triangles;existing.RecalculateBounds();existing.UploadMeshData(false);
                EditorUtility.SetDirty(existing);Object.DestroyImmediate(mesh);mesh=existing;
            }
            bool fixtureBatch=entry.Key.StartsWith("fixture_");
            GameObject batch=new GameObject((fixtureBatch?"Fixture structure sector ":"Small props sector ")+index++);batch.transform.SetParent(root);
            if(!fixtureBatch && layer>=0)batch.layer=layer;
            batch.AddComponent<MeshFilter>().sharedMesh=mesh;
            var combinedRenderer=batch.AddComponent<MeshRenderer>();
            combinedRenderer.sharedMaterial=entry.Value[0].sharedMaterial;
            combinedRenderer.shadowCastingMode=entry.Value[0].shadowCastingMode;
            combinedRenderer.receiveShadows=entry.Value[0].receiveShadows;
            foreach(var renderer in entry.Value)
            {
                GameObject source=renderer.gameObject;
                Object.DestroyImmediate(renderer);Object.DestroyImmediate(source.GetComponent<MeshFilter>());
                if(source.transform.childCount==0 && source.GetComponents<Component>().Length==1)Object.DestroyImmediate(source);
                merged++;
            }
        }
        var details=new List<Renderer>();
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>()) if(r.name.StartsWith("Small props sector "))details.Add(r);
        root.gameObject.AddComponent<OutOfService.MarketDetailVisibility>().details=details.ToArray();
        var solids=new List<Bounds>();
        foreach(string name in new[]{"1F main floor","1F west rear floor","1F east rear floor","1F east front floor","2F west wing","2F east wing","2F central front","2F central rear"})
            solids.Add(root.Find(name).GetComponent<Renderer>().bounds);
        var stock=new List<Renderer>();
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>())
        {
            if(r.GetComponentInParent<Rigidbody>() || r.GetComponentInParent<OutOfService.MarketSwingDoor>() ||
               r.GetComponentInParent<OutOfService.MarketPlayer>() || r.GetComponentInParent<OutOfService.MarketThreat>())continue;
            stock.Add(r);
        }
        var occlusion=root.gameObject.AddComponent<OutOfService.MarketWestSlabVisibility>();
        occlusion.slabs=solids.ToArray();occlusion.staticStock=stock.ToArray();
        var floorLights=new List<Light>();
        foreach(var light in root.GetComponentsInChildren<Light>())
            if(light.enabled && !light.GetComponentInParent<OutOfService.MarketPlayer>() && !light.GetComponentInParent<OutOfService.MarketThreat>())floorLights.Add(light);
        occlusion.staticLights=floorLights.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Batched "+merged+" fixed small prop renderers into "+index+" material sectors. Interactive objects remain separate.");
    }
}
