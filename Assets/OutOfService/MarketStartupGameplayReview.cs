using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace OutOfService
{
    // Normal camera/backbuffer and game simulation stay active. This complements
    // the controlled GPU-readback test; callbacks measure frame cadence, not GPU time.
    public sealed class MarketStartupGameplayReview:MonoBehaviour
    {
        const int Count=240;
        static readonly bool VisibilityMode=Array.IndexOf(Environment.GetCommandLineArgs(),"-visibilityGameplayReview")>=0;
        MarketDetailVisibility[] visibility;int transitions;
        readonly double[] times=new double[Count];
        string folder;Camera view;double readyAt,previous;int samples,threats;bool recording,finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Background(){if(VisibilityMode || Array.IndexOf(Environment.GetCommandLineArgs(),"-startupGameplayReview")>=0)Application.runInBackground=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(!Application.isEditor && (VisibilityMode || Array.IndexOf(Environment.GetCommandLineArgs(),"-startupGameplayReview")>=0))new GameObject("Normal startup frame audit").AddComponent<MarketStartupGameplayReview>();}
        void Awake(){
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,VisibilityMode?"../VisibilityGameplayReview":"../StartupGameplayReview"));Directory.CreateDirectory(folder);
            view=Camera.main;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            RenderPipelineManager.endCameraRendering+=Rendered;Application.logMessageReceived+=Logged;
        }
        void Logged(string message,string trace,LogType type){if(!finished && (type==LogType.Error || type==LogType.Exception)){finished=true;File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL: "+message);Application.Quit(2);}}
        void Rendered(ScriptableRenderContext context,Camera camera){
            if(!recording || camera!=view || samples>=Count)return;
            double now=Time.realtimeSinceStartupAsDouble;
            times[samples++]=(now-previous)*1000;previous=now;
        }
        void Update(){
            if(!VisibilityMode || !recording || transitions>=2 || samples<(transitions+1)*80)return;
            foreach(var group in visibility){group.Configure(false);group.Configure(true);}
            transitions++;
        }
        IEnumerator Start(){
            yield return null;while(!MarketRenderPreparation.IsReady)yield return null;
            if(!view.enabled || view.targetTexture || !MarketPlayer.Instance.enabled)throw new InvalidOperationException("Normal camera/player was not restored");
            foreach(var threat in FindObjectsByType<MarketThreat>())if(threat.isActiveAndEnabled)threats++;
            visibility=FindObjectsByType<MarketDetailVisibility>();
            readyAt=previous=Time.realtimeSinceStartupAsDouble;recording=true;
            while(samples<Count && Time.realtimeSinceStartupAsDouble-readyAt<30)yield return null;
            recording=false;
            if(samples<Count)throw new InvalidOperationException("Only "+samples+" normal camera frames; no offscreen substitute used");
            using(var file=new StreamWriter(Path.Combine(folder,"frames.tsv"))){
                file.WriteLine("sample\tcamera_end_interval_ms\tnote");
                for(int n=0;n<Count;n++)file.WriteLine(FormattableString.Invariant($"{n}\t{times[n]:F3}\t{(n==0?"ready_to_first_camera_end":"consecutive_camera_end")}"));
            }
            var intervals=new double[Count-1];Array.Copy(times,1,intervals,0,Count-1);Array.Sort(intervals);
            int hitches=0;foreach(var time in intervals)if(time>16.67)hitches++;
            File.WriteAllText(Path.Combine(folder,"result.txt"),FormattableString.Invariant($"PASS: {Screen.width}x{Screen.height}, original backbuffer, normal camera, player enabled, {threats} active AI; {Count} camera frames. Loading {MarketRenderPreparation.ElapsedMilliseconds:F2} ms. Ready-to-first camera end {times[0]:F3} ms (partial frame). Subsequent frame cadence p95 {intervals[(int)(intervals.Length*.95)]:F3} ms / max {intervals[intervals.Length-1]:F3} ms / over 16.67ms {hitches}/{intervals.Length}. No manual Camera.Render, GPU readback, target allocation, warmup-frame deletion or simulation disabling after loading. Uncapped static starting viewpoint, not a combat/network benchmark or isolated GPU time."));
            if(VisibilityMode)File.AppendAllText(Path.Combine(folder,"result.txt"),$" Mass visibility off/on transitions: {transitions}, immediately before samples 80 and 160. All transition samples retained.");
            finished=true;Application.Quit();
        }
        void OnDestroy(){RenderPipelineManager.endCameraRendering-=Rendered;Application.logMessageReceived-=Logged;}
    }
}
