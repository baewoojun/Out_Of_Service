using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OutOfService
{
    // Standalone-only fixed-camera comparison. No component in ordinary play.
    public sealed class MarketGraphicsReview:MonoBehaviour
    {
        string folder;bool complete;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Background(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-graphicsReview")>=0)Application.runInBackground=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(!Application.isEditor&&Array.IndexOf(Environment.GetCommandLineArgs(),"-graphicsReview")>=0)new GameObject("Graphics quality review").AddComponent<MarketGraphicsReview>();}
        void OnLog(string text,string trace,LogType type){if(!complete&&(type==LogType.Exception||type==LogType.Error)){complete=true;File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL: "+text+"\n"+trace);Application.Quit(2);}}
        IEnumerator Start()
        {
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../GraphicsReview"));Directory.CreateDirectory(folder);
            Application.logMessageReceived+=OnLog;
            yield return null;while(!MarketRenderPreparation.IsReady)yield return null;
            var player=MarketPlayer.Instance;player.enabled=false;player.GetComponent<CharacterController>().enabled=false;
            foreach(var t in FindObjectsByType<MarketThreat>())t.enabled=false;
            foreach(var f in FindObjectsByType<MarketFlicker>())f.enabled=false;
            player.bareHandsModel.SetActive(false);player.flashlightModel.SetActive(false);
            Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            var camera=player.viewCamera;camera.enabled=false;camera.fieldOfView=75;
            var target=new RenderTexture(1920,1080,24);var picture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            var pixel=new Texture2D(1,1,TextureFormat.RGB24,false);camera.targetTexture=target;
            string[] names={"cafe-metal","cafe-room","b1-shopfront","beauty-counter","produce-contact","fridge-glass","checkout-floor","2f-fashion","wall-grazing","cafe-seat","restaurant-seat","beauty-range","appliance-lid"};
            Vector3[] eyes={new(-20.9f,-3.35f,-14),new(-13,-3.35f,-16),new(3,-3.35f,-23),new(-27.1f,-3.65f,-32.8f),new(-32.3f,1.65f,-8),new(7,1.65f,36.5f),new(4,1.65f,-28),new(-27,6.65f,-18),new(-47,1.65f,-28),new(-12,-3.35f,-17.2f),new(-12,-3.35f,2.8f),new(-27.8f,-3.35f,-33),new(38.6f,1.65f,16.8f)};
            Vector3[] aims={new(-22,-3.6f,-14),new(-22,-3.5f,-16),new(-10,-2.8f,-28),new(-26.6f,-3.9f,-32.8f),new(-30.7f,1.05f,-8),new(7,1.4f,41),new(8,.15f,-32),new(-28,6,-13),new(-49.5f,2,-24),new(-13,-4.3f,-18.7f),new(-13,-4.3f,1.3f),new(-26.6f,-3.98f,-33),new(37.7f,.9f,18)};
            names[8]="family-wall";
            var viewNames=new List<string>(names);var viewEyes=new List<Vector3>(eyes);var viewAims=new List<Vector3>(aims);
            void Add(string name,Vector3 eye,Vector3 aim){viewNames.Add(name);viewEyes.Add(eye);viewAims.Add(aim);}
            Add("family-exterior",new(-40.5f,1.65f,-21.5f),new(-46.4f,1.7f,-23.9f));
            Add("family-interior",new(-44.6f,1.65f,-25.4f),new(-48,.8f,-28.7f));
            Add("nursing-chair",new(-47.9f,1.65f,-27),new(-48,.65f,-28.7f));
            Add("cookware-open",new(-36.6f,1.65f,-29.7f),new(-36.8f,1,-31));
            Add("storage-mugs",new(-31.6f,1.65f,-29.9f),new(-32,1,-31));
            Add("hanging-pans",new(-36.7f,1.65f,-25.8f),new(-36.8f,1.45f,-27.7f));
            Add("customer-seats",new(9.5f,6.65f,-24),new(11,5.7f,-24));
            Add("concourse-bench",new(4.4f,-3.35f,-30),new(6,-4.3f,-30));
            names=viewNames.ToArray();eyes=viewEyes.ToArray();aims=viewAims.ToArray();
            var probes=FindObjectsByType<ReflectionProbe>();
            using(var report=new StreamWriter(Path.Combine(folder,"frames.tsv"))){
                report.WriteLine("view\ttorch\tp95_render_sync_ms\tmean_srgb\tblack_fraction\tprobe_volumes_at_eye\tactive_probes");
                for(int i=0;i<names.Length;i++)for(int torch=0;torch<2;torch++){
                    player.transform.position=eyes[i]-Vector3.up*.65f;camera.transform.position=eyes[i];camera.transform.LookAt(aims[i]);player.flashlight.enabled=torch==1;
                    foreach(var g in FindObjectsByType<MarketDetailVisibility>())g.Refresh();
                    yield return null;yield return null;
                    camera.Render();RenderTexture.active=target;picture.ReadPixels(new Rect(0,0,1920,1080),0,0,false);
                    File.WriteAllBytes(Path.Combine(folder,names[i]+(torch==0?"-off":"-on")+".jpg"),picture.EncodeToJPG(94));
                    var pixels=picture.GetPixels32();double mean=0;int black=0,count=0;
                    for(int p=0;p<pixels.Length;p+=8){var c=pixels[p];double l=(c.r*.2126+c.g*.7152+c.b*.0722)/255;mean+=l;if(l<.025)black++;count++;}
                    int coverage=0;foreach(var probe in probes)if(probe.bounds.Contains(eyes[i]))coverage++;
                    yield return null; // no second synchronous camera draw in this engine frame
                    var times=new List<double>(60);var timer=new System.Diagnostics.Stopwatch();
                    for(int n=0;n<70;n++){timer.Restart();camera.Render();RenderTexture.active=target;pixel.ReadPixels(new Rect(960,540,1,1),0,0,false);timer.Stop();if(n>=10)times.Add(timer.Elapsed.TotalMilliseconds);yield return null;}
                    times.Sort();report.WriteLine(FormattableString.Invariant($"{names[i]}\t{torch}\t{times[56]:F3}\t{mean/count:F4}\t{black/(double)count:F4}\t{coverage}\t{probes.Length}"));report.Flush();
                }
            }
            // Inspect articulated lid in its actual open pose as well as the closed display.
            var lid=Array.Find(FindObjectsByType<MarketSwingDoor>(),d=>d.name=="top loader lid - E" && Mathf.Abs(d.transform.position.x-37.7f)<.1f);
            int applianceView=Array.IndexOf(names,"appliance-lid");player.transform.position=eyes[applianceView]-Vector3.up*.65f;camera.transform.position=eyes[applianceView];camera.transform.LookAt(aims[applianceView]);player.flashlight.enabled=true;
            foreach(var group in FindObjectsByType<MarketDetailVisibility>())group.Refresh();yield return null;
            if(lid){lid.Toggle();yield return new WaitForSecondsRealtime(.8f);camera.Render();RenderTexture.active=target;picture.ReadPixels(new Rect(0,0,1920,1080),0,0,false);File.WriteAllBytes(Path.Combine(folder,"appliance-lid-open.jpg"),picture.EncodeToJPG(94));lid.Toggle();}
            int valid=0;long bytes=0;foreach(var p in probes)if(p.customBakedTexture){valid++;bytes+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(p.customBakedTexture);}
            string memory=bytes>0?bytes+" bytes":"unavailable in this non-development player";
            File.WriteAllText(Path.Combine(folder,"result.txt"),$"{(valid==probes.Length?"PASS":"FAIL")}: {names.Length*2} views at 1920x1080 / FOV75, torch ON/OFF, {valid}/{probes.Length} custom reflection textures; profiler texture memory: {memory}. AI disabled, controlled render + GPU readback, not full-game FPS.");
            complete=true;camera.targetTexture=null;RenderTexture.active=null;target.Release();Destroy(target);Destroy(picture);Destroy(pixel);Application.Quit();
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
