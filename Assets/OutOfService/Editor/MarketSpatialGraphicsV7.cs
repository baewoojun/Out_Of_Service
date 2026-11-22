using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class MarketPrototypeBuilderV7
{
    // Finishing pass is part of generation, not a manual edit to the saved scene.
    static void SpatialGraphicsFinish()
    {
        var rendererData=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        if(!rendererData)throw new InvalidOperationException("Missing verified PC URP renderer");
        bool aoFound=false;
        foreach(var feature in rendererData.rendererFeatures){
            if(!feature || feature.GetType().Name!="ScreenSpaceAmbientOcclusion")continue;
            var settings=new SerializedObject(feature);
            settings.FindProperty("m_Settings.Intensity").floatValue=1.05f;
            settings.FindProperty("m_Settings.Radius").floatValue=.16f;
            settings.FindProperty("m_Settings.DirectLightingStrength").floatValue=.42f;
            settings.FindProperty("m_Settings.Falloff").floatValue=32;
            settings.ApplyModifiedPropertiesWithoutUndo();feature.SetActive(true);aoFound=true;
        }
        if(!aoFound)throw new InvalidOperationException("Expected PC contact occlusion feature not found");

        // Reserve the 2048 atlas for the 1024 torch and nearby 512 task lamps.
        // Background architectural spots use 256; otherwise URP halves every map,
        // including the torch, when a view contains fourteen shadowed lights.
        foreach(var light in root.GetComponentsInChildren<Light>())if(light.type==LightType.Spot && light.shadows!=LightShadows.None){
            var data=new SerializedObject(light.GetUniversalAdditionalLightData());
            bool produce=light.name=="pepper track lamp" || light.name=="orchard track lamp";
            data.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=produce?1:0;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        // Material variation stays shared: grain and roughness, not one material per prop.
        var materials=new HashSet<Material>();var checkedMeshes=new HashSet<Mesh>();
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true)){
            if(renderer.name.EndsWith(" countertop") || renderer.name=="service counter top")FinishCounterSurface(renderer);
            foreach(var mat in renderer.sharedMaterials){
                if(!mat || !mat.HasProperty("_BumpMap"))continue;
                if(materials.Add(mat)){
                    string n=mat.name;
                    if(n.StartsWith("B1 finish v4 ")){
                        string kind=n.Substring(13).Split(' ')[0];
                        if(kind=="wood" || kind=="enamel" || kind=="bucket" || kind=="washable" || kind=="clay"){
                            float metal=mat.GetFloat("_Metallic");
                            mat.SetTexture("_MetallicGlossMap",FinishTexture("SpatialRoughnessV1_"+kind,(u,v)=>{
                                float wipe=Mathf.PerlinNoise(u*5+21,v*15+4);
                                float grain=Mathf.PerlinNoise(u*(kind=="wood"?170:80)+9,v*(kind=="wood"?3:70));
                                return new Color(metal,0,0,Mathf.Clamp01(.92f-.28f*wipe-.13f*grain));
                            }));mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                            FineRelief(mat,"SpatialReliefV1_"+kind,(u,v)=>
                                Mathf.PerlinNoise(u*(kind=="wood"?155:110),v*(kind=="wood"?3:110))*(kind=="wood"?.13f:.035f),1);
                        }
                    }
                    if(n=="brushed stainless shop fittings"){
                        FineRelief(mat,"SpatialStainlessV1",(u,v)=>Mathf.PerlinNoise(u*5+12,v*200)*.065f,1);
                        mat.SetFloat("_Smoothness",.67f);
                    }
                    if(n=="cafe brushed sheet steel v2")mat.SetFloat("_Smoothness",.74f);
                    if(n=="clear creased chiller wrap")FineRelief(mat,"SpatialFilmV1",(u,v)=>
                        .035f*Mathf.Sin(u*36+Mathf.Sin(v*13)*2)+.04f*Mathf.PerlinNoise(u*13,v*31),1);
                }
                var filter=renderer.GetComponent<MeshFilter>();
                if(mat.GetTexture("_BumpMap") && filter && filter.sharedMesh && checkedMeshes.Add(filter.sharedMesh)){
                    var mesh=filter.sharedMesh;
                    // Cached hand-built meshes predate normal mapping. Imported Unity primitives already have tangents.
                    if(mesh.isReadable && mesh.uv.Length==mesh.vertexCount && mesh.tangents.Length!=mesh.vertexCount){
                        mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);
                    }
                }
            }
        }

        var tenants=new List<Transform>();
        foreach(var t in root.GetComponentsInChildren<Transform>())if(t.name.StartsWith("B1 tenant - "))tenants.Add(t);
        foreach(var tenant in tenants){
            if(tenant.name.Contains("커피"))PracticalStrip("cafe counter task light",tenant.TransformPoint(new Vector3(2,3.2f,11)),
                tenant.TransformPoint(new Vector3(2,1.2f,12)),new Color(1,.83f,.64f),4.6f,6,tenant.rotation);
            if(tenant.name.Contains("뷰티"))PracticalStrip("beauty counter task light",tenant.TransformPoint(new Vector3(-3,3.2f,4.7f)),
                tenant.TransformPoint(new Vector3(-3,.9f,4.7f)),new Color(.88f,.94f,1),3.6f,5,tenant.rotation);
            EntryWear(tenant.TransformPoint(new Vector3(.6f,.008f,.8f)),tenant.eulerAngles.y+7,1.7f);
        }
        foreach(var light in root.GetComponentsInChildren<Light>())if(light.name=="pepper track lamp"){
            light.shadowNormalBias=.035f;light.shadowBias=.025f;
            SetTaskShadowResolution(light);
        }
        PracticalStrip("checkout remaining work light",new Vector3(7.5f,3.8f,-31),new Vector3(7.5f,.6f,-31),new Color(1,.85f,.67f),3.5f,6,Quaternion.identity);
        foreach(float x in new[]{-12,-4,4,12})EntryWear(new Vector3(x,.008f,-30),-7,2.2f);
        AssetDatabase.SaveAssets();
    }

    static void FinishCounterSurface(MeshRenderer renderer)
    {
        var mat=RetailSurface("honed quartz service surface v1",new Color(.58f,.56f,.50f),0,.43f);
        if(!mat.mainTexture){
            mat.mainTexture=FinishTexture("SpatialQuartzColorV1",(u,v)=>{
                float grain=Mathf.PerlinNoise(u*170+9,v*170+21),cloud=Mathf.PerlinNoise(u*7,v*7);
                float fleck=Mathf.SmoothStep(.42f,.62f,grain);
                return Color.Lerp(new Color(.57f,.55f,.50f),new Color(.88f,.87f,.82f),.55f+fleck*.35f+cloud*.10f).linear;
            });
            mat.SetTexture("_MetallicGlossMap",FinishTexture("SpatialQuartzGlossV1",(u,v)=>
                new Color(0,0,0,.82f-.17f*Mathf.PerlinNoise(u*12,v*12+7))));mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            FineRelief(mat,"SpatialQuartzReliefV1",(u,v)=>Mathf.PerlinNoise(u*170+9,v*170+21)*.026f,1);
            mat.mainTextureScale=new Vector2(2,2);
        }
        renderer.sharedMaterial=mat;
        var filter=renderer.GetComponent<MeshFilter>();if(!filter || !filter.sharedMesh)return;
        // Metre-based UVs prevent a nine-metre counter from stretching one square texture.
        var dimensions=Vector3.Scale(filter.sharedMesh.bounds.size,renderer.transform.localScale);
        string key="quartz_counter_uv_v1_"+dimensions.ToString("F3").Replace(" ","");
        string path="Assets/OutOfService/Generated/"+key+".asset";
        if(!handmadeMeshes.TryGetValue(key,out var mesh)){
            mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){
                mesh=UnityEngine.Object.Instantiate(SoftShopEdge(dimensions));mesh.name=key;
                var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
                for(int i=0;i<uv.Length;i++){
                    var n=normals[i];var p=vertices[i];
                    uv[i]=Mathf.Abs(n.y)>.7f?new Vector2(p.x,p.z):Mathf.Abs(n.x)>.7f?new Vector2(p.z,p.y):new Vector2(p.x,p.y);
                }
                mesh.uv=uv;mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);
            }handmadeMeshes[key]=mesh;
        }
        if(renderer.TryGetComponent<BoxCollider>(out var box)){
            box.center=Vector3.Scale(box.center,renderer.transform.localScale);box.size=Vector3.Scale(box.size,renderer.transform.localScale);
        }
        filter.sharedMesh=mesh;renderer.transform.localScale=Vector3.one;
    }

    static void PracticalStrip(string name,Vector3 position,Vector3 aim,Color color,float intensity,float range,Quaternion rotation)
    {
        var housing=Decor(name+" housing",PrimitiveType.Cube,position,new Vector3(1.2f,.06f,.12f),shelf);
        housing.transform.rotation=rotation;
        foreach(float side in new[]{-.45f,.45f}){
            var bottom=position+rotation*Vector3.right*side+Vector3.up*.035f;
            RailSegment(name+" suspension rod",bottom,bottom+Vector3.up*.69f,.009f,PrepSteel(),root);
        }
        var diffuser=RetailSurface("task strip opal diffuser "+ColorUtility.ToHtmlStringRGB(color),color*.72f,0,.22f);
        diffuser.EnableKeyword("_EMISSION");diffuser.SetColor("_EmissionColor",color*.9f);
        var lens=Decor(name+" opal lens",PrimitiveType.Cube,position+Vector3.down*.035f,new Vector3(1.12f,.012f,.085f),diffuser);
        lens.transform.rotation=rotation;
        var light=new GameObject(name).AddComponent<Light>();light.transform.SetParent(root);
        light.transform.position=position+Vector3.down*.05f;light.transform.rotation=Quaternion.LookRotation(aim-light.transform.position);
        light.type=LightType.Spot;light.color=color;light.intensity=intensity;light.range=range;
        light.spotAngle=100;light.innerSpotAngle=62;light.shadows=LightShadows.Soft;
        light.shadowBias=.025f;light.shadowNormalBias=.035f;
        var data=light.GetUniversalAdditionalLightData();data.softShadowQuality=SoftShadowQuality.Medium;
        SetTaskShadowResolution(light);
        light.lightmapBakeType=LightmapBakeType.Realtime;
        // Three added task lights. Reuse the existing produce lamp instead of overlapping it.
    }

    static void SetTaskShadowResolution(Light light)
    {
        // URP 17 only permits the public tier setter during play mode.
        var data=new SerializedObject(light.GetUniversalAdditionalLightData());
        data.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EntryWear(Vector3 position,float yaw,float size)
    {
        const string key="local rubber sole and trolley wear";
        if(!retailSurfaces.TryGetValue(key,out var mat)){
            mat=TransparentMat(key,new Color(.10f,.085f,.068f,.24f));mat.SetFloat("_Smoothness",.10f);mat.SetFloat("_Metallic",0);
            mat.mainTexture=FinishTexture("SpatialThresholdScuffsV1",(u,v)=>{
                float a=0;
                for(int i=0;i<5;i++){
                    float x=.20f+i*.137f+.018f*Mathf.Sin(v*24+i*3),y=.22f+(i%3)*.19f;
                    float stroke=Mathf.Exp(-Mathf.Pow((u-x)*150,2))*Mathf.Exp(-Mathf.Pow((v-y)*11,4));
                    a=Mathf.Max(a,stroke);
                }
                a*=Mathf.Clamp01((Mathf.PerlinNoise(u*90+7,v*150)-.2f)*2);
                return new Color(1,1,1,a);
            });retailSurfaces.Add(key,mat);
        }
        var go=Decor("localized threshold scuff",PrimitiveType.Quad,position,new Vector3(size,size,1),mat);
        go.transform.rotation=Quaternion.Euler(90,yaw,0);go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
    }

    static void BakeRoomReflections()
    {
        // Static room cubemaps: no realtime capture cost, no stale player/monster in glass.
        var hidden=new List<(Renderer renderer,bool enabled)>();
        var lights=new List<(Light light,bool enabled)>();
        foreach(var sceneRoot in root.gameObject.scene.GetRootGameObjects()){
            if(sceneRoot==root.gameObject)continue;
            foreach(var renderer in sceneRoot.GetComponentsInChildren<Renderer>(true)){
                hidden.Add((renderer,renderer.enabled));renderer.enabled=false;
            }
            foreach(var light in sceneRoot.GetComponentsInChildren<Light>(true)){
                lights.Add((light,light.enabled));light.enabled=false;
            }
        }
        bool fog=RenderSettings.fog;
        try{
            // Fog is applied once in the final camera, not baked into the environment a second time.
            RenderSettings.fog=false;
            Vector3[] centers={new(-3,2,-29),new(-35,2,-12),new(1,2,35),new(38,2.5f,-2),new(-28,7,-20),new(24,2,18),new(24,7,15),new(3,2,3)};
            Vector3[] sizes={new(25,4.5f,20),new(27,4.5f,30),new(44,4.5f,20),new(17,7,25),new(42,4.5f,44),new(30,4.5f,27),new(40,4.5f,40),new(40,4.5f,38)};
            for(int i=0;i<centers.Length;i++)BakeRoomProbe("retail_"+i,centers[i],sizes[i],128,0);
            BakeRoomProbe("family_west",new Vector3(-46.4f,1.8f,-26.7f),new Vector3(6.23f,3.45f,5.43f),128,2);
            BakeRoomProbe("cookware_corner",new Vector3(-35.4f,1.8f,-30.5f),new Vector3(9,3.5f,8),128,1);
            for(int i=0;i<4;i++)BakeRoomProbe("B1_concourse_"+i,new Vector3(0,-3,-29+i*20),new Vector3(20,4,24),128,0);
            var rooms=new List<Transform>();
            foreach(var t in root.GetComponentsInChildren<Transform>())
                if(t.name.StartsWith("B1 tenant - ") || t.name.StartsWith("B1 support room - "))rooms.Add(t);
            for(int i=0;i<rooms.Count;i++){
                var room=rooms[i];bool quarterTurn=Mathf.Abs(room.forward.x)>.5f;
                var size=quarterTurn?new Vector3(15.8f,3.9f,13.8f):new Vector3(13.8f,3.9f,15.8f);
                BakeRoomProbe("B1_room_"+i,room.TransformPoint(new Vector3(0,2,8)),size,
                    room.name.Contains("커피") || room.name.Contains("뷰티")?256:128,2);
            }
        }finally{
            RenderSettings.fog=fog;
            foreach(var item in hidden)if(item.renderer)item.renderer.enabled=item.enabled;
            foreach(var item in lights)if(item.light)item.light.enabled=item.enabled;
        }
    }

    static void BakeRoomProbe(string key,Vector3 center,Vector3 size,int resolution,int importance)
    {
        var probe=new GameObject("baked room reflection "+key).AddComponent<ReflectionProbe>();
        probe.transform.SetParent(root);probe.transform.position=center;
        probe.mode=ReflectionProbeMode.Custom;probe.renderDynamicObjects=true;probe.resolution=resolution;probe.size=size;
        probe.boxProjection=true;probe.blendDistance=importance>0?.65f:1.5f;probe.intensity=.95f;probe.importance=importance;
        probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.009f,.011f,.015f);
        probe.nearClipPlane=.12f;probe.farClipPlane=45;probe.shadowDistance=16;
        string path="Assets/OutOfService/Generated/SpatialReflectionV1_"+key+".exr";
        if(!Lightmapping.BakeReflectionProbe(probe,path))throw new InvalidOperationException("Failed room reflection "+key);
        AssetDatabase.ImportAsset(path);probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
        if(!probe.customBakedTexture)throw new InvalidOperationException("Missing room reflection "+key);
    }
}
