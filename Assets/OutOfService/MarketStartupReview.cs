using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace OutOfService
{
    // Test owns its output BEFORE startup, so no render-target allocation is
    // mistaken for a first playable frame. A second phase measures that separately.
    public sealed class MarketStartupReview:MonoBehaviour
    {
        Camera view;RenderTexture target;Texture2D pixel;string folder;int coveredFrames;bool finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Background(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-startupReview")>=0)Application.runInBackground=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(!Application.isEditor && Array.IndexOf(Environment.GetCommandLineArgs(),"-startupReview")>=0)new GameObject("Startup frame audit").AddComponent<MarketStartupReview>();}
        void Awake(){
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../StartupReview"));Directory.CreateDirectory(folder);
            view=Camera.main;target=new RenderTexture(1920,1080,24);target.Create();pixel=new Texture2D(1,1,TextureFormat.RGB24,false);
            view.targetTexture=target;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            RenderPipelineManager.endCameraRendering+=Rendered;Application.logMessageReceived+=Logged;
        }
        void Rendered(ScriptableRenderContext context,Camera camera){if(camera==view && camera.targetTexture==target && !MarketRenderPreparation.IsReady)coveredFrames++;}
        void Logged(string message,string stack,LogType type){if(!finished && (type==LogType.Exception||type==LogType.Error)){finished=true;File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL: "+message);Application.Quit(2);}}
        IEnumerator Start(){
            yield return null;while(!MarketRenderPreparation.IsReady)yield return null;
            var player=MarketPlayer.Instance;player.enabled=false;
            foreach(var t in FindObjectsByType<MarketThreat>())t.enabled=false;
            view.enabled=false;var timer=new System.Diagnostics.Stopwatch();
            using(var file=new StreamWriter(Path.Combine(folder,"frames.tsv"))){
                file.WriteLine("phase\tsample\ttorch\trender_cpu_ms\treadback_wait_ms\ttotal_sync_ms");
                for(int phase=0;phase<2;phase++){
                    if(phase==1){view.targetTexture=null;target.Release();Destroy(target);target=new RenderTexture(1920,1080,24);target.Create();view.targetTexture=target;}
                    for(int n=0;n<60;n++){
                        player.flashlight.enabled=n>=15;
                        timer.Restart();view.Render();double render=timer.Elapsed.TotalMilliseconds;
                        RenderTexture.active=target;pixel.ReadPixels(new Rect(960,540,1,1),0,0,false);double total=timer.Elapsed.TotalMilliseconds;
                        file.WriteLine(FormattableString.Invariant($"{(phase==0?"first_control":"new_target")}\t{n}\t{(player.flashlight.enabled?1:0)}\t{render:F3}\t{total-render:F3}\t{total:F3}"));
                        yield return null;
                    }
                }
            }
            File.WriteAllText(Path.Combine(folder,"result.txt"),$"PASS: 120 frames including all cold samples, 1920x1080. Output allocated before startup; {coveredFrames} output frames under loading cover. Startup {MarketRenderPreparation.ElapsedMilliseconds:F2} ms. Then a separate newly allocated target phase. AI disabled; controlled Camera.Render and synchronous GPU readback, not normal gameplay FPS.");
            finished=true;view.targetTexture=null;RenderTexture.active=null;target.Release();Destroy(target);Destroy(pixel);Application.Quit();
        }
        void OnDestroy(){RenderPipelineManager.endCameraRendering-=Rendered;Application.logMessageReceived-=Logged;}
    }
}
