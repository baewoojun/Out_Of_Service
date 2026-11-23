using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OutOfService
{
    // Opt-in regression checks for resting doors and decorative visibility. This
    // component does not run or allocate in ordinary play.
    public sealed class MarketRefinementReview : MonoBehaviour
    {
        string folder;readonly List<string> checks=new List<string>();bool complete;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(!Application.isEditor && Array.IndexOf(Environment.GetCommandLineArgs(),"-refinementReview")>=0)new GameObject("Refinement regression review").AddComponent<MarketRefinementReview>();}
        void Check(string label,bool pass){checks.Add((pass?"PASS ":"FAIL ")+label);if(!pass)throw new InvalidOperationException(label);}
        void OnLog(string message,string stack,LogType type){
            if(complete || (type!=LogType.Exception && type!=LogType.Error && type!=LogType.Assert))return;
            Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL: "+message);Application.Quit(2);
        }
        IEnumerator Start()
        {
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../RefinementReview"));Directory.CreateDirectory(folder);
            Application.logMessageReceived+=OnLog;Application.runInBackground=true;
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            yield return null;
            while (!MarketRenderPreparation.IsReady) yield return null;
            Check("startup preparation completed nine real-game views including 2F stock",MarketRenderPreparation.Instance && MarketRenderPreparation.Instance.Completed && MarketRenderPreparation.PreparedViews==9);
            Check("startup prepares destination before releasing input",MarketRenderPreparation.PreparedOutputFrames==4);
            Check("startup restores time and player input",Time.timeScale==1 && MarketPlayer.Instance.enabled);
            Check("startup restores main camera and render target",MarketPlayer.Instance.viewCamera.enabled && !MarketPlayer.Instance.viewCamera.targetTexture);
            Check("startup preserves initial torch and HUD state",!MarketPlayer.Instance.flashlight.enabled && !MarketGame.Instance.showHud);
            var player=MarketPlayer.Instance;player.enabled=false;
            foreach(var threat in FindObjectsByType<MarketThreat>())threat.enabled=false;
            var camera=player.viewCamera;camera.enabled=false;player.flashlight.enabled=true;
            var doors=FindObjectsByType<MarketSwingDoor>();
            Check("swing doors present",doors.Length>20);
            yield return new WaitForSecondsRealtime(1);
            foreach(var door in doors)door.transform.hasChanged=false;
            for(int n=0;n<12;n++)yield return null;
            int dirty=0;foreach(var door in doors)if(door.transform.hasChanged)dirty++;
            Check("all "+doors.Length+" resting doors leave transforms untouched",dirty==0);
            var sample=Array.Find(doors,d=>d.name.Contains("fire door"));
            Check("fire door available",sample!=null);
            var closed=sample.transform.localRotation;
            sample.Toggle();yield return new WaitForSecondsRealtime(.16f);
            float half=Quaternion.Angle(closed,sample.transform.localRotation);
            Check("door moves before reaching end stop",half>1 && half<Mathf.Abs(sample.openAngle)-1);
            sample.Toggle();yield return new WaitForSecondsRealtime(.65f);
            Check("mid-swing reversal returns to closed hinge pose",Quaternion.Angle(closed,sample.transform.localRotation)<.05f);
            sample.isOpen=true;yield return new WaitForSecondsRealtime(.75f);
            Check("direct open-state assignment is supported",Mathf.Abs(Quaternion.Angle(closed,sample.transform.localRotation)-Mathf.Abs(sample.openAngle))<.05f);
            sample.isOpen=false;yield return new WaitForSecondsRealtime(.75f);
            Check("direct close-state assignment is supported",Quaternion.Angle(closed,sample.transform.localRotation)<.05f);
            sample.transform.hasChanged=false;for(int n=0;n<12;n++)yield return null;
            Check("closed door stops writing transform again",!sample.transform.hasChanged);
            foreach(var lid in Array.FindAll(doors,d=>d.name=="top loader lid - E")){
                var pull=lid.transform.Find("washer lid recessed pull");var start=pull.position;
                var eye=lid.transform.position+new Vector3(0,.65f,-.95f);
                Physics.SyncTransforms();
                Check("standing player can target closed washer lid",Physics.Raycast(eye,(pull.position-eye).normalized,out var closedHit,3f) && closedHit.collider.GetComponentInParent<MarketSwingDoor>()==lid);
                lid.Toggle();yield return new WaitForSecondsRealtime(.75f);
                Check("washer lid opens upward on rear hinge",pull.position.y>start.y+.25f);
                Physics.SyncTransforms();
                Check("standing player can target open washer lid to close it",Physics.Raycast(eye,(pull.position-eye).normalized,out var openHit,3f) && openHit.collider.GetComponentInParent<MarketSwingDoor>()==lid);
                lid.Toggle();yield return new WaitForSecondsRealtime(.75f);
                Check("washer lid closes to exact resting position",Vector3.Distance(pull.position,start)<.001f);
            }
            var bounce=FindFirstObjectByType<MarketBounceLighting>();Check("seven source-linked bounce lights present",bounce && bounce.links.Length==7);
            var link=bounce.links[0];bool sourceOn=link.source.enabled;float intensity=link.source.intensity;
            link.source.enabled=false;yield return null;yield return null;
            Check("source blackout also extinguishes bounce",!link.bounce.enabled);
            link.source.enabled=true;link.source.intensity=intensity*.5f;yield return null;yield return null;
            Check("source dimming updates bounce",link.bounce.enabled && Mathf.Abs(link.bounce.intensity-Mathf.Min(link.maximum,intensity*.5f*link.gain))<.001f);
            link.source.enabled=sourceOn;link.source.intensity=intensity;bounce.Refresh();
            var groups=FindObjectsByType<MarketDetailVisibility>();Check("decorative visibility group present",groups.Length>0);
            foreach(var group in groups){
                bool was=group.optimized;group.Configure(false);int hidden=0;
                foreach(var r in group.details)if(r&&!r.enabled)hidden++;
                Check("unoptimized mode restores every decorative renderer",hidden==0);
                group.Configure(true);hidden=0;foreach(var r in group.details)if(r&&!r.enabled)hidden++;
                Check("optimized mode still culls remote sectors",hidden>0);
                group.Configure(was);
            }
            var cpuTrace=new double[181];var frameTrace=new int[181];var totals=new double[181];
            var target=new RenderTexture(1920,1080,24);var pixel=new Texture2D(1,1,TextureFormat.RGB24,false);camera.targetTexture=target;
            var clock=System.Diagnostics.Stopwatch.StartNew();
            camera.Render();cpuTrace[0]=clock.Elapsed.TotalMilliseconds;RenderTexture.active=target;pixel.ReadPixels(new Rect(960,540,1,1),0,0,false);
            double first=clock.Elapsed.TotalMilliseconds;
            totals[0]=first;frameTrace[0]=Time.frameCount;
            // URP synchronous readback twice on the same camera in one engine
            // frame creates a GPU stall (~40ms on the measured D3D12 player).
            // Keep the cold draw in the report, then sample one render per frame,
            // matching the game's camera schedule. Do not discard the cold draw.
            yield return null;
            var samples=new List<double>(180);var intervals=new List<double>(180);
            var loopClock=System.Diagnostics.Stopwatch.StartNew();double previous=0;
            long allocated=GC.GetAllocatedBytesForCurrentThread();
            for(int n=0;n<180;n++){
                clock.Restart();camera.Render();cpuTrace[n+1]=clock.Elapsed.TotalMilliseconds;RenderTexture.active=target;pixel.ReadPixels(new Rect(960,540,1,1),0,0,false);
                totals[n+1]=clock.Elapsed.TotalMilliseconds;frameTrace[n+1]=Time.frameCount;
                samples.Add(totals[n+1]);yield return null;
                double now=loopClock.Elapsed.TotalMilliseconds;intervals.Add(now-previous);previous=now;
            }
            long delta=GC.GetAllocatedBytesForCurrentThread()-allocated;
            using(var trace=new StreamWriter(Path.Combine(folder,"render-stages.tsv"))){
                trace.WriteLine("sample\tunity_frame\trender_cpu_ms\treadback_wait_ms\ttotal_sync_ms");
                for(int n=0;n<totals.Length;n++)trace.WriteLine(FormattableString.Invariant($"{n-1}\t{frameTrace[n]}\t{cpuTrace[n]:F3}\t{totals[n]-cpuTrace[n]:F3}\t{totals[n]:F3}"));
            }
            using(var slow=new StreamWriter(Path.Combine(folder,"hitches.tsv"))){
                slow.WriteLine("sample\trender_sync_ms\tloop_interval_ms");
                for(int n=0;n<samples.Count;n++)if(samples[n]>16.67 || intervals[n]>16.67)
                    slow.WriteLine(FormattableString.Invariant($"{n}\t{samples[n]:F3}\t{intervals[n]:F3}"));
            }
            samples.Sort();intervals.Sort();
            File.WriteAllLines(Path.Combine(folder,"checks.txt"),checks);
            File.WriteAllText(Path.Combine(folder,"render.txt"),FormattableString.Invariant($"1920x1080 controlled render/readback, AI disabled, not full-game FPS. Real startup preparation {MarketRenderPreparation.ElapsedMilliseconds:F2} ms / {MarketRenderPreparation.PreparedViews} views. First NEW test-target draw after door, light and mass culling toggles {first:F2} ms, followed by 180 renders on distinct engine frames: p95 {samples[170]:F2} ms / max {samples[179]:F2} ms. Whole scripted-loop frame intervals p95 {intervals[170]:F2} ms / max {intervals[179]:F2} ms. Main-thread allocations across sample frames including QA/render callbacks {delta} bytes. All 181 draws including the initial cold sample are retained in render-stages.tsv; no warmup spikes discarded. Legacy same-frame duplicate render/readback is isolated by -hitchIsolationReview; use -visibilityGameplayReview for normal-backbuffer transition cadence."));
            complete=true;File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: "+checks.Count+" door and culling regression checks; "+doors.Length+" resting doors verified.");
            camera.targetTexture=null;RenderTexture.active=null;target.Release();Destroy(target);Destroy(pixel);Application.Quit();
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
