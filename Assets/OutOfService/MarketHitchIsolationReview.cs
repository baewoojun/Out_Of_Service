using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OutOfService
{
    public sealed class MarketHitchIsolationReview:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Background(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-hitchIsolationReview")>=0)Application.runInBackground=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(!Application.isEditor && Array.IndexOf(Environment.GetCommandLineArgs(),"-hitchIsolationReview")>=0)new GameObject("Render hitch isolation").AddComponent<MarketHitchIsolationReview>();}
        struct Sample{public string phase;public int sample,frame;public double cpu,total;}
        readonly List<Sample> data=new List<Sample>(300);readonly System.Diagnostics.Stopwatch timer=new System.Diagnostics.Stopwatch();
        Camera view;RenderTexture target;Texture2D pixel;
        void Draw(string phase,int n){timer.Restart();view.Render();double cpu=timer.Elapsed.TotalMilliseconds;RenderTexture.active=target;pixel.ReadPixels(new Rect(960,540,1,1),0,0,false);double total=timer.Elapsed.TotalMilliseconds;data.Add(new Sample{phase=phase,sample=n,frame=Time.frameCount,cpu=cpu,total=total});}
        void NewTarget(){view.targetTexture=null;RenderTexture.active=null;if(target){target.Release();Destroy(target);}target=new RenderTexture(1920,1080,24);view.targetTexture=target;}
        IEnumerator Start(){
            yield return null;while(!MarketRenderPreparation.IsReady)yield return null;
            var player=MarketPlayer.Instance;player.enabled=false;foreach(var ai in FindObjectsByType<MarketThreat>())ai.enabled=false;
            view=player.viewCamera;view.enabled=false;player.flashlight.enabled=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            pixel=new Texture2D(1,1,TextureFormat.RGB24,false);NewTarget();var groups=FindObjectsByType<MarketDetailVisibility>();
            foreach(string phase in new[]{"initial_target","same_frame_double","visibility_only","target_only","combined_same_frame","idle_then_render"}){
                if(phase=="visibility_only" || phase=="combined_same_frame")foreach(var g in groups){g.Configure(false);g.Configure(true);}
                if(phase=="target_only" || phase=="combined_same_frame")NewTarget();
                if(phase=="idle_then_render")yield return new WaitForSecondsRealtime(2);
                if(phase=="same_frame_double" || phase=="combined_same_frame")Draw(phase,-1);
                for(int n=0;n<40;n++){Draw(phase,n);yield return null;}
            }
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../HitchIsolationReview"));Directory.CreateDirectory(folder);
            using(var file=new StreamWriter(Path.Combine(folder,"frames.tsv"))){file.WriteLine("phase\tsample\tunity_frame\trender_cpu_ms\treadback_wait_ms\ttotal_sync_ms");foreach(var s in data)file.WriteLine(FormattableString.Invariant($"{s.phase}\t{s.sample}\t{s.frame}\t{s.cpu:F3}\t{s.total-s.cpu:F3}\t{s.total:F3}"));}
            File.WriteAllText(Path.Combine(folder,"result.txt"),"Recorded all 242 samples including first draws, 1920x1080 controlled render/readback. Separate target allocation, visibility toggles, same-frame duplicate rendering and idle restart. AI disabled; diagnostic only, not game FPS.");
            view.targetTexture=null;RenderTexture.active=null;target.Release();Destroy(target);Destroy(pixel);Application.Quit();
        }
    }
}
