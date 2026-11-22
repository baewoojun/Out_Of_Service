using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OutOfService
{
    // Standalone-only merchandising inspection, at normal camera height/lighting.
    public sealed class MarketMerchReview : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(!Application.isEditor && (Array.IndexOf(Environment.GetCommandLineArgs(),"-merchReview")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-vegetableBuildingReview")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-freshPrepReview")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-mallDetailReview")>=0))new GameObject("Merchandising review").AddComponent<MarketMerchReview>();}
        IEnumerator Start()
        {
            yield return null;
            while (!MarketRenderPreparation.IsReady) yield return null;
            var player=MarketPlayer.Instance;player.enabled=false;player.GetComponent<CharacterController>().enabled=false;
            foreach(var t in FindObjectsByType<MarketThreat>())t.enabled=false;
            foreach(var f in FindObjectsByType<MarketFlicker>())f.enabled=false;
            player.flashlight.enabled=true;player.bareHandsModel.SetActive(false);player.flashlightModel.SetActive(false);
            Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            var camera=player.viewCamera;camera.enabled=false;camera.fieldOfView=60;
            var target=new RenderTexture(1920,1080,24);var picture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            var pixel=new Texture2D(1,1,TextureFormat.RGB24,false);camera.targetTexture=target;
            bool prep=Array.IndexOf(Environment.GetCommandLineArgs(),"-freshPrepReview")>=0;
            bool mall=Array.IndexOf(Environment.GetCommandLineArgs(),"-mallDetailReview")>=0;
            bool vegetables=Array.IndexOf(Environment.GetCommandLineArgs(),"-vegetableBuildingReview")>=0;
            var folder=Path.GetFullPath(Path.Combine(Application.dataPath,vegetables?"../VegetableBuildingReview":mall?"../MallDetailReview":prep?"../FreshPrepReview":"../MerchReview"));Directory.CreateDirectory(folder);
            string[] names={"ramen-shelf","ramen-endcap","cup-noodles","rice-bowls","snacks","carbonated-drinks","sauces","detergent","checkout","coffee-counter","appliances","pharmacy"};
            Vector3[] eyes={new(-9.75f,1.55f,-14),new(-8,1.65f,-20.3f),new(-9.7f,1.60f,-6),new(-9.7f,1.52f,7),new(2.65f,1.52f,-14),new(11.65f,1.52f,-6),new(20.65f,1.52f,23),new(27.85f,1.6f,7),new(10,1.65f,-28.5f),new(-20.9f,-3.35f,-14),new(37.6f,1.65f,16.2f),new(17,-3.45f,-11.9f)};
            Vector3[] aims={new(-8.65f,1.3f,-14),new(-8,1.5f,-17.9f),new(-8.65f,1.25f,-6),new(-8.65f,1.19f,7),new(1.65f,1.2f,-14),new(10.65f,1.25f,-6),new(19.65f,1.2f,23),new(26.65f,1.25f,7),new(8,1.1f,-28.5f),new(-22,-3.6f,-14),new(37.7f,.65f,18),new(18,-3.48f,-10.5f)};
            if(prep){
                names=new[]{"meat-counter","fish-counter","meat-prep","fish-prep","meat-sink","fish-sink","butcher-equipment","fish-scaling","wrapping-station","cold-stock"};
                eyes=new[]{new Vector3(-42,1.6f,25.6f),new(-27,1.6f,25.6f),new(-42,1.65f,33.5f),new(-27,1.65f,33.5f),new(-36.7f,1.65f,34.3f),new(-21.7f,1.65f,34.3f),new(-45.7f,1.65f,33.4f),new(-30.7f,1.65f,33.4f),new(-46.3f,1.65f,36.3f),new(-42,1.7f,40)};
                aims=new[]{new Vector3(-42,1.1f,27.7f),new(-27,1.1f,27.7f),new(-42,1.05f,35.5f),new(-27,1.05f,35.5f),new(-37.3f,.9f,35.8f),new(-22.3f,.9f,35.8f),new(-46.9f,1.1f,35.2f),new(-31.9f,1.0f,35.2f),new(-47.2f,1.1f,37.5f),new(-46.7f,1.2f,41.6f)};
            }
            if(mall){
                names=new[]{"florist-overview","cut-flower-buckets","cut-flower-close","gift-bouquets","potted-foliage","flower-wrapping","sewing-station","laundry-garments","ironing-board","laundry-reception","cafe-counter","cafe-self-service","cafe-kitchen","restaurant-kitchen"};
                string[] shops={"잎새 화원","잎새 화원","잎새 화원","잎새 화원","잎새 화원","잎새 화원","바늘 세탁·수선","바늘 세탁·수선","바늘 세탁·수선","바늘 세탁·수선","모퉁이 커피","모퉁이 커피","모퉁이 커피","한끼 식당"};
                eyes=new[]{new Vector3(0,1.65f,1.8f),new(-4.55f,1.65f,6.5f),new(-5.0f,1.48f,4.25f),new(-1.8f,1.6f,3.6f),new(3.4f,1.6f,7),new(0,1.65f,13.15f),new(-4.2f,1.55f,5.9f),new(3.7f,1.65f,8.2f),new(-2.1f,1.6f,10.3f),new(1,1.65f,3.5f),new(0,1.65f,10.15f),new(4.9f,1.65f,11.3f),new(.6f,1.65f,14.3f),new(-2.7f,1.65f,14.1f)};
                aims=new[]{new Vector3(-3,1,7),new(-6.15f,1.2f,7.1f),new(-5.85f,1f,4),new(-3.25f,1.1f,4.2f),new(5.1f,1.1f,7.5f),new(0,1.03f,14.5f),new(-4.1f,1.13f,7),new(5.45f,1.2f,10),new(-3.5f,.95f,10.3f),new(3,1.03f,5),new(1.7f,1.4f,12),new(6.2f,1.17f,11.5f),new(-3,1.2f,15.15f),new(-3.25f,1.2f,15.1f)};
                for(int i=0;i<names.Length;i++){
                    var unit=GameObject.Find("B1 tenant - "+shops[i]).transform;eyes[i]=unit.TransformPoint(eyes[i]);aims[i]=unit.TransformPoint(aims[i]);
                }
                int first=names.Length;Array.Resize(ref names,first+5);Array.Resize(ref eyes,first+5);Array.Resize(ref aims,first+5);
                var florist=GameObject.Find("B1 tenant - 잎새 화원").transform;
                string[] detailNames={"garden-rose-close","florist-ceiling","wall-service-detail","concourse-ceiling","service-corridor-ducts"};
                Vector3[] detailEyes={new(-5.0f,1.58f,5.3f),new(0,1.65f,6),new(5.5f,1.65f,13.6f),new(2,-3.35f,-10),new(30,-3.35f,-20)};
                Vector3[] detailAims={new(-5.48f,1.13f,5.16f),new(2,3.85f,11),new(6.85f,2.3f,14.3f),new(0,-.7f,3),new(30,-.9f,-10)};
                for(int i=0;i<5;i++){names[first+i]=detailNames[i];eyes[first+i]=i<3?florist.TransformPoint(detailEyes[i]):detailEyes[i];aims[first+i]=i<3?florist.TransformPoint(detailAims[i]):detailAims[i];}
                int close=names.Length;Array.Resize(ref names,close+3);Array.Resize(ref eyes,close+3);Array.Resize(ref aims,close+3);
                var laundry=GameObject.Find("B1 tenant - 바늘 세탁·수선").transform;
                var cafe=GameObject.Find("B1 tenant - 모퉁이 커피").transform;
                names[close]="laundry-pocket-close";eyes[close]=laundry.TransformPoint(new Vector3(4.7f,1.55f,7.9f));aims[close]=laundry.TransformPoint(new Vector3(5.45f,1.47f,8.6f));
                names[close+1]="cafe-service-close";eyes[close+1]=cafe.TransformPoint(new Vector3(1.65f,1.65f,11.28f));aims[close+1]=cafe.TransformPoint(new Vector3(1.8f,1.38f,11.96f));
                names[close+2]="succulent-close";eyes[close+2]=florist.TransformPoint(new Vector3(4.5f,1.55f,5.5f));aims[close+2]=florist.TransformPoint(new Vector3(5.05f,.50f,5.8f));
            }
            if(vegetables){
                names=new[]{"tomato-punnet","cucumber-sleeve","scallion-bunch","potato-bin","pepper-rebuilt","ssam-chiller","b1-main-arcade","b1-west-service","b1-east-service","b1-stockroom","b1-florist-facade","1f-wall-finish"};
                eyes=new[]{new Vector3(-23.1f,1.6f,-17.45f),new Vector3(-23.1f,1.6f,-2.45f),new Vector3(-31.1f,1.6f,12.5f),new Vector3(-31.1f,1.6f,-2.45f),new Vector3(-32.3f,1.65f,-8),new Vector3(-43.3f,1.55f,-22),new Vector3(3,-3.35f,-23),new Vector3(-29,-3.35f,-22),new Vector3(29,-3.35f,16),new Vector3(-34,-3.35f,-16),new Vector3(7,-3.35f,2),new Vector3(-47,1.65f,-28)};
                aims=new[]{new Vector3(-23.1f,1.02f,-16),new Vector3(-23.1f,1.02f,-1),new Vector3(-31.1f,1.02f,14),new Vector3(-31.1f,1.04f,-1),new Vector3(-30.7f,1.05f,-8),new Vector3(-45.5f,1.3f,-22),new Vector3(1,-3.1f,4),new Vector3(-32,-3.1f,-16),new Vector3(32,-3.1f,24),new Vector3(-42,-3.6f,-16),new Vector3(12,-3.1f,4),new Vector3(-49.5f,2.5f,-24)};
                Array.Resize(ref names,15);Array.Resize(ref eyes,15);Array.Resize(ref aims,15);
                names[12]="tomato-close";eyes[12]=new Vector3(-23.66f,1.28f,-17.1f);aims[12]=new Vector3(-23.66f,1.03f,-16.64f);
                names[13]="cucumber-close";eyes[13]=new Vector3(-23.7f,1.25f,-2.12f);aims[13]=new Vector3(-23.7f,1.0f,-1.65f);
                names[14]="potato-close";eyes[14]=new Vector3(-31.3f,1.30f,-2.1f);aims[14]=new Vector3(-31.3f,1.13f,-1.6f);
                Array.Resize(ref names,21);Array.Resize(ref eyes,21);Array.Resize(ref aims,21);
                string[] additions={"모아 뷰티","바른 안경","픽셀 모바일","맡김 보관서비스"};
                for(int i=0;i<additions.Length;i++){
                    var unit=GameObject.Find("B1 tenant - "+additions[i]).transform;
                    names[15+i]="b1-new-service-"+i;
                    eyes[15+i]=unit.TransformPoint(new Vector3(0,1.65f,2));
                    aims[15+i]=unit.TransformPoint(new Vector3(-3.4f,1.2f,7));
                }
                var beauty=GameObject.Find("B1 tenant - 모아 뷰티").transform;
                names[19]="cosmetic-close";eyes[19]=beauty.TransformPoint(new Vector3(-2.9f,1.3f,4.8f));aims[19]=beauty.TransformPoint(new Vector3(-3.4f,1.1f,4.8f));
                names[20]="b1-south-fronts";eyes[20]=new Vector3(-4,-3.35f,-24.5f);aims[20]=new Vector3(-10,-2.9f,-28);
            }
            using(var report=new StreamWriter(Path.Combine(folder,"frames.tsv"))){
                report.WriteLine("view\trender_sync_p95_ms\tsamples");
                for(int i=0;i<names.Length;i++){
                    player.transform.position=eyes[i]-Vector3.up*.65f;camera.transform.position=eyes[i];camera.transform.LookAt(aims[i]);
                    foreach(var group in FindObjectsByType<MarketDetailVisibility>())group.Refresh();
                    yield return null;yield return null;camera.Render();RenderTexture.active=target;
                    picture.ReadPixels(new Rect(0,0,1920,1080),0,0,false);
                    File.WriteAllBytes(Path.Combine(folder,names[i]+".jpg"),picture.EncodeToJPG(94));
                    var samples=new List<double>();var watch=new System.Diagnostics.Stopwatch();
                    for(int n=0;n<60;n++){
                        watch.Restart();camera.Render();RenderTexture.active=target;pixel.ReadPixels(new Rect(0,0,1,1),0,0,false);watch.Stop();
                        if(n>=10)samples.Add(watch.Elapsed.TotalMilliseconds);yield return null;
                    }
                    samples.Sort();report.WriteLine(FormattableString.Invariant($"{names[i]}\t{samples[47]:F3}\t50"));report.Flush();
                }
            }
            camera.targetTexture=null;RenderTexture.active=null;target.Release();
            File.WriteAllText(Path.Combine(folder,"result.txt"),names.Length+" merchandising views rendered at 1920x1080 in the Windows player; scripted GPU-synchronized timings, not full-game FPS.");
            Application.Quit();
        }
    }
}
