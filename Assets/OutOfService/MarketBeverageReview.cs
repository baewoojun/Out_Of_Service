using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
using UnityEngine.Rendering.Universal;

namespace OutOfService
{
    // Opt-in standalone inspection; never instantiated in ordinary play.
    public sealed class MarketBeverageReview:MonoBehaviour
    {
        static readonly string[] Arguments=Environment.GetCommandLineArgs();
        static readonly bool ShadowStudy=Array.IndexOf(Arguments,"-beverageShadowStudy")>=0;
        static readonly bool Study=ShadowStudy||Array.IndexOf(Arguments,"-beverageMaterialStudy")>=0;
        static readonly bool Laundry=Array.IndexOf(Arguments,"-laundryWalk")>=0;
        static readonly bool Dairy=Array.IndexOf(Arguments,"-dairyWalk")>=0;
        static readonly bool Produce=Array.IndexOf(Arguments,"-produceWalk")>=0;
        static readonly bool Upper=Array.IndexOf(Arguments,"-upperRetailWalk")>=0;
        static readonly bool Fashion=Array.IndexOf(Arguments,"-fashionWalk")>=0;
        static readonly bool Walk=Laundry||Dairy||Produce||Fashion||Upper||Array.IndexOf(Arguments,"-beverageWalk")>=0;
        string output;bool complete;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Background(){if(Study||Walk)Application.runInBackground=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(!Application.isEditor&&(Study||Walk))new GameObject("Beverage standalone review").AddComponent<MarketBeverageReview>();}
        void OnLog(string message,string trace,LogType kind){if(!complete&&(kind==LogType.Exception||kind==LogType.Error)){complete=true;File.WriteAllText(Path.Combine(output,"failure.txt"),message+"\n"+trace);Application.Quit(2);}}
        IEnumerator Start()
        {
            output=Path.GetFullPath(Path.Combine(Application.dataPath,ShadowStudy?"../BeverageShadowStudy":Study?"../BeverageMaterialStudy":Laundry?"../LaundryWalk":Dairy?"../DairyWalk":Produce?"../ProduceWalk":Upper?"../UpperRetailWalk":Fashion?"../FashionWalk":"../BeverageWalk"));Directory.CreateDirectory(output);
            File.Delete(Path.Combine(output,"failure.txt"));File.Delete(Path.Combine(output,"result.txt"));
            Application.logMessageReceived+=OnLog;
            bool instanced=Array.IndexOf(Environment.GetCommandLineArgs(),"-beverageInstancing")>=0;
            if(instanced){
                var candidate=Resources.Load<Material>("MarketPETInstancing");if(!candidate)throw new InvalidOperationException("Missing instancing comparison material");
                foreach(var renderer in FindObjectsByType<MeshRenderer>())if(renderer.sharedMaterial&&renderer.sharedMaterial.name=="clear beverage PET v1")renderer.sharedMaterial=candidate;
            }
            yield return null;while(!MarketRenderPreparation.IsReady)yield return null;
            var player=MarketPlayer.Instance;player.enabled=false;foreach(var ai in FindObjectsByType<MarketThreat>())ai.enabled=false;
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;player.flashlight.enabled=true;
            if(Study)yield return Inspect(player);else yield return Traverse(player);
            complete=true;Application.Quit();
        }
        IEnumerator Inspect(MarketPlayer player)
        {
            var camera=player.viewCamera;camera.enabled=false;player.flashlightModel.SetActive(false);player.bareHandsModel.SetActive(false);
            var target=new RenderTexture(1920,1080,24);var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);camera.targetTexture=target;camera.nearClipPlane=.02f;
            Material pet=null;foreach(var r in FindObjectsByType<MeshRenderer>())if(r.sharedMaterial&&(r.sharedMaterial.name=="clear beverage PET v1"||r.sharedMaterial.name=="candidate instanced clear PET")){pet=r.sharedMaterial;break;}
            if(!pet)throw new InvalidOperationException("PET study material missing");
            float refraction=pet.GetFloat("_RefractionPixels"),edge=pet.GetFloat("_EdgeOpacity");
            var lightData=player.flashlight.GetUniversalAdditionalLightData();
            bool pipelineBias=lightData.usePipelineSettings;
            float depthBias=player.flashlight.shadowBias,normalBias=player.flashlight.shadowNormalBias;
            string[] lines=File.ReadAllLines(Path.Combine(Application.dataPath,"../catalog-views.tsv"));
            for(int mode=0;mode<3;mode++){
                if(ShadowStudy){
                    lightData.usePipelineSettings=mode==0;
                    player.flashlight.shadowBias=mode==2?.04f:.02f;
                    player.flashlight.shadowNormalBias=mode==2?.12f:.04f;
                }else{
                    pet.SetFloat("_RefractionPixels",mode==1?0:refraction);
                    pet.SetFloat("_EdgeOpacity",mode==2?0:edge);
                }
                foreach(int id in new[]{61,66,67,85}){
                    var c=lines[id].Split('\t');float Read(int k)=>float.Parse(c[k],System.Globalization.CultureInfo.InvariantCulture);
                    Vector3 foot=new(Read(1),Read(2),Read(3)),outward=new(Read(4),0,Read(5));
                    var aim=foot+Vector3.up*.12f;player.transform.position=foot+outward;
                    camera.transform.position=aim+outward*.68f+Vector3.up*.08f;camera.transform.LookAt(aim);camera.fieldOfView=34;
                    foreach(var g in FindObjectsByType<MarketDetailVisibility>())g.Refresh();yield return null;yield return null;
                    camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0,false);
                    File.WriteAllBytes(Path.Combine(output,$"mode{mode}-{id/6:D2}_{id%6}.jpg"),pixels.EncodeToJPG(94));
                }
            }
            pet.SetFloat("_RefractionPixels",refraction);pet.SetFloat("_EdgeOpacity",edge);
            lightData.usePipelineSettings=pipelineBias;player.flashlight.shadowBias=depthBias;player.flashlight.shadowNormalBias=normalBias;
            camera.targetTexture=null;RenderTexture.active=null;target.Release();Destroy(target);Destroy(pixels);
            File.WriteAllText(Path.Combine(output,"result.txt"),ShadowStudy?
                "12 actual player renders. Mode 0 pipeline bias; mode 1 local depth .02/normal .04; mode 2 local .04/.12. No permanent edits. Not FPS measurement.":
                "12 actual player renders. Mode 0 saved Thin PET; mode 1 refraction disabled; mode 2 edge coverage disabled. Inspection candidates only; no permanent material edits. Not FPS measurement.");
        }
        IEnumerator Traverse(MarketPlayer player)
        {
            var camera=player.viewCamera;camera.fieldOfView=75;
            var controller=player.GetComponent<CharacterController>();controller.enabled=false;controller.minMoveDistance=0;
            Vector3[] points={new(7,.92f,-18.4f),new(7,.92f,27.2f),new(13,.92f,27.2f),new(13,.92f,-18.4f)};
            if(Laundry)points=new[]{new Vector3(23,.92f,-18.4f),new Vector3(23,.92f,27.2f),new Vector3(29,.92f,27.2f),new Vector3(29,.92f,-18.4f)};
            if(Dairy)points=new[]{new Vector3(-19,.92f,38.3f),new Vector3(15,.92f,38.3f),new Vector3(15,.92f,39.5f),new Vector3(-19,.92f,39.5f)};
            if(Produce)points=new[]{new Vector3(-42,.92f,-19),new Vector3(-42,.92f,19),new Vector3(-34,.92f,19),new Vector3(-34,.92f,-19)};
            if(Fashion)points=new[]{new Vector3(-46.7f,5.92f,-28),new Vector3(-46.7f,5.92f,27),new Vector3(-40.7f,5.92f,27),new Vector3(-40.7f,5.92f,-28)};
            if(Upper)points=new[]{new Vector3(-35.2f,5.92f,-32),new Vector3(-35.2f,5.92f,29.4f),new Vector3(-19,5.92f,29.4f),new Vector3(-19,5.92f,-32)};
            player.transform.position=points[0];controller.enabled=true;
            int frames=0;void EndCamera(ScriptableRenderContext context,Camera c){if(c==camera)frames++;}
            RenderPipelineManager.endCameraRendering+=EndCamera;
            var samples=new double[32768];var legs=new byte[samples.Length];var collections=new int[samples.Length];var positions=new Vector3[samples.Length];var draws=new long[samples.Length];var batches=new long[samples.Length];var switches=new int[samples.Length];int count=0,passed=0,slow=0,gc=GC.CollectionCount(0);float distance=0;
            bool counters=Array.IndexOf(Environment.GetCommandLineArgs(),"-beverageCounters")>=0;
            using var drawCounter=counters?ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1):default;
            using var batchCounter=counters?ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1):default;
            var slabs=FindFirstObjectByType<MarketWestSlabVisibility>();
            var clock=System.Diagnostics.Stopwatch.StartNew();double previous=clock.Elapsed.TotalMilliseconds;
            for(int leg=1;leg<points.Length;leg++){
                double deadline=clock.Elapsed.TotalSeconds+25;
                while(Vector2.Distance(new(player.transform.position.x,player.transform.position.z),new(points[leg].x,points[leg].z))>.08f){
                    if(clock.Elapsed.TotalSeconds>deadline){
                        var near=Physics.OverlapSphere(player.transform.position,.65f);
                        string hits="";foreach(var hit in near)if(hit!=controller)hits+=" / "+hit.name+" "+hit.bounds;
                        throw new InvalidOperationException("Beverage aisle blocked at leg "+leg+" at "+player.transform.position+hits);
                    }
                    Vector3 before=player.transform.position,delta=points[leg]-before;delta.y=0;float step=Mathf.Min(.05f,Time.unscaledDeltaTime);
                    controller.Move(Vector3.ClampMagnitude(delta,step*3.5f)+Vector3.down*(3*step));
                    distance+=Vector2.Distance(new(before.x,before.z),new(player.transform.position.x,player.transform.position.z));
                    camera.transform.LookAt(Upper?new Vector3(leg==3?-22.5f:-32.5f,6.2f,player.transform.position.z+(leg==3?-2:2)):Fashion?new Vector3(leg==3?-43:-48.7f,6.5f,player.transform.position.z+(leg==3?-1.5f:1.5f)):Dairy?new Vector3(player.transform.position.x+(leg==3?-1.5f:1.5f),1.5f,41.7f):new Vector3(Produce?-38:Laundry?26:10,1.3f,Mathf.Clamp(player.transform.position.z+(leg==3?-1.5f:1.5f),Produce?-17:-14,Produce?17:23)));
                    yield return null;double now=clock.Elapsed.TotalMilliseconds,ms=now-previous;previous=now;
                    if(count>=samples.Length)throw new InvalidOperationException("Benchmark sample capacity exceeded");samples[count]=ms;legs[count]=(byte)leg;collections[count]=GC.CollectionCount(0)-gc;positions[count]=player.transform.position;
                    draws[count]=drawCounter.Valid?drawCounter.LastValue:-1;batches[count]=batchCounter.Valid?batchCounter.LastValue:-1;switches[count]=slabs?slabs.LastRendererChanges:0;count++;if(ms>16.67)slow++;
                }
                passed++;
            }
            RenderPipelineManager.endCameraRendering-=EndCamera;
            using(var file=new StreamWriter(Path.Combine(output,"frames.tsv"))){file.WriteLine("sample\tleg\tframe_ms\tgc0\tx\tz\tdraw_calls\tbatches\tslab_changes");for(int i=0;i<count;i++)file.WriteLine(FormattableString.Invariant($"{i}\t{legs[i]}\t{samples[i]:F4}\t{collections[i]}\t{positions[i].x:F3}\t{positions[i].z:F3}\t{draws[i]}\t{batches[i]}\t{switches[i]}"));}
            Array.Sort(samples,0,count);double P(float q)=>samples[Mathf.CeilToInt(count*q)-1];
            File.WriteAllText(Path.Combine(output,"result.txt"),FormattableString.Invariant($"{(passed==3&&frames>0?"PASS":"FAIL")}: {Screen.width}x{Screen.height} normal camera / original backbuffer. {passed}/3 legs, {distance:F2} m, {frames} rendered, {count} samples, p95 {P(.95f):F3} ms, p99 {P(.99f):F3} ms, max {samples[count-1]:F3} ms, over 16.67ms {slow}, Gen0 {GC.CollectionCount(0)-gc}. Loading {MarketRenderPreparation.ElapsedMilliseconds:F2} ms. All traversal samples retained; no Camera.Render/readback/test render target. Scripted controller movement, AI disabled, not combat or multiplayer."));
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
