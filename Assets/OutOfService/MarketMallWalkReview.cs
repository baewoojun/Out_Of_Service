using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace OutOfService
{
    // Opt-in standalone QA only. Samples real controller movement and render completion.
    public sealed class MarketMallWalkReview : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(!Application.isEditor && Array.IndexOf(Environment.GetCommandLineArgs(),"-mallFirstVisit")>=0)
                new GameObject("B1 flower shop first visit review").AddComponent<MarketMallWalkReview>();
        }
        IEnumerator Start()
        {
            yield return null;
            while (!MarketRenderPreparation.IsReady) yield return null;
            Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            var player=MarketPlayer.Instance;player.enabled=false;
            foreach(var threat in FindObjectsByType<MarketThreat>())threat.enabled=false;
            player.flashlight.enabled=true;
            var controller=player.GetComponent<CharacterController>();controller.enabled=false;controller.minMoveDistance=0;
            var unit=GameObject.Find("B1 tenant - 잎새 화원").transform;
            var camera=player.viewCamera;
            int rendered=0;void Count(ScriptableRenderContext context,Camera current){if(current==camera)rendered++;}
            RenderPipelineManager.endCameraRendering+=Count;
            yield return new WaitForSecondsRealtime(1);
            bool offscreen=rendered==0;
            RenderTexture target=null;Texture2D pixel=null;
            double startupFirstDraw=0;
            if(offscreen){
                target=new RenderTexture(1920,1080,24);pixel=new Texture2D(1,1,TextureFormat.RGB24,false);camera.enabled=false;camera.targetTexture=target;
                // Initialize this test's render target after the same startup
                // preparation that ordinary players receive. Report both costs.
                var initialization=System.Diagnostics.Stopwatch.StartNew();
                for(int i=0;i<30;i++){
                    camera.Render();RenderTexture.active=target;pixel.ReadPixels(new Rect(960,540,1,1),0,0,false);
                    if(i==0)startupFirstDraw=initialization.Elapsed.TotalMilliseconds;
                    yield return null;
                }
            }
            Vector3[] points={new(0,.92f,-2),new(0,.92f,2.2f),new(-4.55f,.92f,2.2f),new(-4.55f,.92f,10.7f),new(0,.92f,12.8f),new(2.5f,.92f,12.8f),new(2.5f,.92f,7),new(0,.92f,7),new(0,.92f,1)};
            for(int i=0;i<points.Length;i++)points[i]=unit.TransformPoint(points[i]);
            player.transform.position=points[0];controller.enabled=true;
            var times=new List<double>(12000);var clock=System.Diagnostics.Stopwatch.StartNew();
            var hitches=new List<string>();var stage=new System.Diagnostics.Stopwatch();
            double previous=clock.Elapsed.TotalMilliseconds;int slow=0,passed=0;float distance=0;int gc=GC.CollectionCount(0);rendered=0;
            for(int n=1;n<points.Length;n++){
                double deadline=clock.Elapsed.TotalSeconds+7;
                while(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(points[n].x,points[n].z))>.08f){
                    if(clock.Elapsed.TotalSeconds>deadline)throw new InvalidOperationException("Florist traversal blocked at waypoint "+n);
                    var before=player.transform.position;
                    var delta=points[n]-before;delta.y=0;
                    float step=Mathf.Min(.05f,Time.unscaledDeltaTime);
                    stage.Restart();
                    controller.Move(Vector3.ClampMagnitude(delta,step*3.5f)+Vector3.down*(step*3));
                    double moveMs=stage.Elapsed.TotalMilliseconds;
                    distance+=Vector3.Distance(new Vector3(before.x,0,before.z),new Vector3(player.transform.position.x,0,player.transform.position.z));
                    var local=unit.InverseTransformPoint(player.transform.position);
                    camera.transform.LookAt(unit.TransformPoint(new Vector3(local.x<0?-5.5f:5.1f,1.25f,Mathf.Clamp(local.z+1,4,12))));
                    stage.Restart();
                    if(offscreen){camera.Render();RenderTexture.active=target;pixel.ReadPixels(new Rect(960,540,1,1),0,0,false);}
                    double renderMs=stage.Elapsed.TotalMilliseconds;
                    yield return null;
                    double now=clock.Elapsed.TotalMilliseconds,ms=now-previous;previous=now;times.Add(ms);if(ms>16.67){slow++;hitches.Add(FormattableString.Invariant($"sample={times.Count} leg={n} elapsed={now:F1} frame={ms:F2} controller={moveMs:F2} render_sync={renderMs:F2} gc={GC.CollectionCount(0)-gc} local={local}"));}
                }
                passed++;
            }
            RenderPipelineManager.endCameraRendering-=Count;
            times.Sort();double P(float q)=>times[Mathf.CeilToInt(times.Count*q)-1];
            File.WriteAllText(Path.Combine(Application.dataPath,"..","mall-first-visit.txt"),FormattableString.Invariant($"1920x1080 | {(offscreen?"offscreen GPU sync":"normal camera")} | AI disabled | real startup preparation {MarketRenderPreparation.ElapsedMilliseconds:F2} ms / {MarketRenderPreparation.PreparedViews} views (includes B1) | test target initialization {startupFirstDraw:F2} ms | first controllable B1 visit after loading | {rendered} rendered | {passed}/8 route legs | distance {distance:F2} m | p95 {P(.95f):F2} ms | p99 {P(.99f):F2} ms | max {times[times.Count-1]:F2} ms | over 16.67ms {slow}/{times.Count} | gen0 collections {GC.CollectionCount(0)-gc}"));
            File.WriteAllLines(Path.Combine(Application.dataPath,"..","mall-walk-hitches.txt"),hitches);
            if(offscreen){camera.targetTexture=null;RenderTexture.active=null;target.Release();Destroy(target);Destroy(pixel);}
            Application.Quit();
        }
    }
}
