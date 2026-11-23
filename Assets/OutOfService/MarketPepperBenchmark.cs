using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace OutOfService
{
    public sealed class MarketPepperBenchmark : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfRequested()
        {
            var args=Environment.GetCommandLineArgs();
            if (!Application.isEditor && (Array.IndexOf(args,"-environmentReview")>=0 || Array.IndexOf(args,"-freezerBenchmark")>=0 || Array.IndexOf(args,"-heldReview")>=0 || Array.IndexOf(args,"-catalogReview")>=0 || Array.IndexOf(args,"-pepperBenchmark")>=0 || Array.IndexOf(args,"-chillerBenchmark")>=0 || Array.IndexOf(args,"-pepperFirstVisit")>=0 || Array.IndexOf(args,"-pantryReview")>=0 || Array.IndexOf(args,"-storeArtReview")>=0))
            {
                Application.runInBackground=true;
                new GameObject("Pepper benchmark").AddComponent<MarketPepperBenchmark>();
            }
        }
        IEnumerator Start()
        {
            yield return null;
            while (!MarketRenderPreparation.IsReady) yield return null;
            var player=MarketPlayer.Instance;
            player.enabled=false;
            player.GetComponent<CharacterController>().enabled=false;
            foreach(var t in FindObjectsByType<MarketThreat>()) t.enabled=false;
            player.bareHandsModel.SetActive(false);
            player.flashlightModel.SetActive(true);
            player.flashlight.enabled=true;
            QualitySettings.vSyncCount=0;
            Application.targetFrameRate=-1;
            bool baseline=Array.IndexOf(Environment.GetCommandLineArgs(),"-pepperWithoutSlab")>=0;
            var slab=FindAnyObjectByType<MarketWestSlabVisibility>();
            if(slab)slab.enabled=true;
            foreach(var group in FindObjectsByType<MarketDetailVisibility>())group.Configure(!baseline);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-environmentReview")>=0){
                yield return ReviewEnvironment(player);Application.Quit();yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-heldReview")>=0){
                yield return ReviewHeld(player);Application.Quit();yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-catalogReview")>=0)
            {
                yield return ReviewCatalog(player);
                Application.Quit();
                yield break;
            }
            bool storeReview=Array.IndexOf(Environment.GetCommandLineArgs(),"-storeArtReview")>=0;
            if(storeReview || Array.IndexOf(Environment.GetCommandLineArgs(),"-pantryReview")>=0)
            {
                yield return ReviewPantry(player,storeReview);
                Application.Quit();
                yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pepperFirstVisit")>=0)
            {
                yield return WalkToPepper(player);
                Application.Quit();
                yield break;
            }
            player.transform.rotation=Quaternion.identity;
            var target=new RenderTexture(Screen.width,Screen.height,24);
            var pixel=new Texture2D(1,1,TextureFormat.RGB24,false);
            var camera=player.viewCamera;
            camera.enabled=false;
            camera.targetTexture=target;
            bool chiller=Array.IndexOf(Environment.GetCommandLineArgs(),"-chillerBenchmark")>=0;
            bool freezer=Array.IndexOf(Environment.GetCommandLineArgs(),"-freezerBenchmark")>=0;
            void MoveCamera(int i)
            {
                float phase=i*2*Mathf.PI/180;
                if(freezer){
                    float x=Mathf.Lerp(-14.8f,17.6f,(1-Mathf.Cos(phase*.5f))*.5f);
                    player.transform.position=new Vector3(x,.9f,33.72f);
                    camera.transform.LookAt(new Vector3(x+.2f,1.16f,31.95f));
                }else if(chiller)
                {
                    // Return down the same aisle, looking into the case throughout.
                    float z=Mathf.Lerp(-16,4,(1-Mathf.Cos(phase*.5f))*.5f);
                    player.transform.position=new Vector3(-43.75f,.9f,z);
                    camera.transform.LookAt(new Vector3(-45.12f,1.53f,z+.28f));
                }
                else
                {
                    player.transform.position=new Vector3(-30+.85f*Mathf.Sin(phase),.9f,-10.6f-.65f*Mathf.Cos(phase));
                    camera.transform.LookAt(new Vector3(-30+.35f*Mathf.Sin(phase),1.09f,-8));
                }
            }
            void RenderAndWait()
            {
                camera.Render();
                RenderTexture.active=target;
                pixel.ReadPixels(new Rect(target.width/2,target.height/2,1,1),0,0,false);
            }
            MoveCamera(0);
            for(int i=0;i<60;i++){RenderAndWait();yield return null;}
            var clock=new System.Diagnostics.Stopwatch();
            var whole=new System.Diagnostics.Stopwatch();
            var times=new double[360];
            var fullFrames=new double[times.Length];
            for(int i=0;i<times.Length;i++)
            {
                MoveCamera(i);
                whole.Restart();
                clock.Restart();RenderAndWait();clock.Stop();
                times[i]=clock.Elapsed.TotalMilliseconds;
                yield return null;
                fullFrames[i]=whole.Elapsed.TotalMilliseconds;
            }
            Array.Sort(times);Array.Sort(fullFrames);
            string folder=Path.Combine(Application.dataPath,"..");
            File.WriteAllText(Path.Combine(folder,freezer?"freezer-result.txt":chiller?"chiller-result.txt":"pepper-result.txt"),$"{SystemInfo.graphicsDeviceName} | {SystemInfo.graphicsDeviceType} | {target.width}x{target.height} | {QualitySettings.names[QualitySettings.GetQualityLevel()]} | detail optimization {!baseline} | render+sync p50 {times[180]:F2} ms | p95 {times[341]:F2} ms | moving frame p95 {fullFrames[341]:F2} ms | p99 {fullFrames[356]:F2} ms");
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureStock")>=0)
            {
                var frame=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                frame.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                File.WriteAllBytes(Path.Combine(folder,"pepper-player.png"),frame.EncodeToPNG());
                string[] names={"apple","shiitake","king_oyster","button","oyster","enoki","vegetables"};
                float[] zs={-16f,-15.15f,-12.9f,-11f,-8.9f,-7.1f,0f};
                for(int i=0;!baseline && i<names.Length;i++)
                {
                    player.transform.position=i==0 ? new Vector3(-35.4f,.9f,-16.7f) : new Vector3(-43.87f,.9f,zs[i]);
                    camera.transform.LookAt(i==0?new Vector3(-37.3f,1.16f,-16):new Vector3(-45.12f,1.56f,zs[i]));
                    foreach(var visibility in FindObjectsByType<MarketDetailVisibility>())visibility.Refresh();
                    yield return null;
                    RenderAndWait();
                    frame.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                    File.WriteAllBytes(Path.Combine(folder,names[i]+"-player.png"),frame.EncodeToPNG());
                }
                Destroy(frame);
            }
            RenderTexture.active=null;camera.targetTexture=null;
            target.Release();Destroy(target);Destroy(pixel);
            Application.Quit();
        }

        static IEnumerator ReviewEnvironment(MarketPlayer player)
        {
            var camera=player.viewCamera;
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../EnvironmentReview"));
            Directory.CreateDirectory(folder);
            string[] names={"entry","aisles","produce","frozen","escalator","meat","upstairs","service","b1-concourse","b1-florist","west-stairs"};
            Vector3[] eyes={new(0,1.65f,-38),new(-2,1.65f,-22),new(-34,1.65f,-18),
                new(-16,1.65f,35.3f),new(34.5f,1.65f,-8),new(-42,1.65f,25.4f),
                new(-34,6.65f,-25),new(-3,1.65f,-24),new(0,-3.35f,-20),new(14,-3.35f,4),new(-46,1.65f,-34)};
            Vector3[] aims={new(0,1.9f,-43),new(-8,1.65f,0),new(-40,1.25f,-7),
                new(4,1.55f,32),new(34.5f,3.3f,2),new(-42,1.15f,28.5f),
                new(-32,6.65f,-5),new(-3,1.45f,-30),new(0,-3.35f,20),new(18,-3.8f,9.5f),new(-46,2.8f,-40)};
            var target=new RenderTexture(1920,1080,24);
            var frame=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            var pixel=new Texture2D(1,1,TextureFormat.RGB24,false);
            using var report=new StreamWriter(Path.Combine(folder,"frames.tsv"));
            report.WriteLine("view\ttorch\trender_sync_p95_ms\tmoving_frame_p95_ms");
            camera.enabled=false;camera.targetTexture=target;
            player.flashlightModel.SetActive(false);
            for(int i=0;i<names.Length;i++){
                player.transform.position=eyes[i]-Vector3.up*.65f;
                camera.transform.position=eyes[i];camera.transform.LookAt(aims[i]);
                foreach(var group in FindObjectsByType<MarketDetailVisibility>())group.Refresh();
                for(int on=0;on<2;on++){
                    player.flashlight.enabled=on==1;
                    for(int j=0;j<3;j++)yield return null;
                    camera.Render();RenderTexture.active=target;
                    frame.ReadPixels(new Rect(0,0,1920,1080),0,0,false);
                    File.WriteAllBytes(Path.Combine(folder,names[i]+(on==1?"-torch":"-ambient")+".jpg"),frame.EncodeToJPG(93));
                    var drawTimes=new double[75];var frameTimes=new double[75];
                    var timer=new System.Diagnostics.Stopwatch();
                    for(int sample=0;sample<90;sample++){
                        timer.Restart();camera.Render();RenderTexture.active=target;
                        pixel.ReadPixels(new Rect(960,540,1,1),0,0,false);
                        double draw=timer.Elapsed.TotalMilliseconds;
                        yield return null;
                        if(sample<15)continue;
                        drawTimes[sample-15]=draw;frameTimes[sample-15]=timer.Elapsed.TotalMilliseconds;
                    }
                    Array.Sort(drawTimes);Array.Sort(frameTimes);
                    report.WriteLine(FormattableString.Invariant($"{names[i]}\t{on}\t{drawTimes[71]:F2}\t{frameTimes[71]:F2}"));
                }
            }
            camera.targetTexture=null;RenderTexture.active=null;
            target.Release();Destroy(target);Destroy(frame);Destroy(pixel);
            Debug.Log("Environment review: "+names.Length+" viewpoints, torch off/on, 1920x1080");
        }

        static IEnumerator ReviewHeld(MarketPlayer player)
        {
            var camera=player.viewCamera;
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../HeldReview"));
            Directory.CreateDirectory(folder);
            player.flashlightModel.SetActive(false);
            player.transform.position=new Vector3(0,1,-29);
            camera.transform.rotation=Quaternion.identity;
            var products=FindObjectsByType<MarketProduct>();
            Array.Sort(products,(a,b)=>string.CompareOrdinal(a.name,b.name)!=0?string.CompareOrdinal(a.name,b.name):
                a.transform.position.ToString("F3").CompareTo(b.transform.position.ToString("F3")));
            var target=new RenderTexture(1920,1080,24);camera.enabled=false;camera.targetTexture=target;
            var tile=RenderTexture.GetTemporary(480,480,0);
            var sheet=new Texture2D(1440,480,TextureFormat.RGB24,false);
            var underside=new Texture2D(480,480,TextureFormat.RGB24,false);
            using(var report=new StreamWriter(Path.Combine(folder,"items.tsv"))){
                for(int i=0;i<products.Length;i++){
                    var item=products[i];item.PickUp(camera);
                    var collider=item.GetComponent<Collider>();
                    if(collider.enabled || item.transform.parent!=camera.transform)throw new InvalidOperationException(item.name+" failed pickup");
                    float standardFov=camera.fieldOfView;
                    item.SetInspecting(camera,true);
                    item.RotateHeld(-20,0);
                    var renderers=item.GetComponentsInChildren<MeshRenderer>();
                    for(int face=0;face<4;face++){
                        item.RotateHeld(90,face==3?90:0);
                        yield return null;
                        var low=Vector2.one;var high=Vector2.zero;
                        foreach(var renderer in renderers){
                            var b=renderer.localBounds;
                            for(int k=0;k<8;k++){
                                Vector3 v=b.center+Vector3.Scale(b.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1));
                                Vector2 uv=camera.WorldToViewportPoint(renderer.transform.TransformPoint(v));
                                low=Vector2.Min(uv,low);high=Vector2.Max(uv,high);
                            }
                        }
                        var mid=(low+high)*.5f;
                        float h=Mathf.Max(high.y-low.y,(high.x-low.x)*target.width/target.height)*1.17f;
                        if(low.x<0 || low.y<0 || high.x>1 || high.y>1)throw new InvalidOperationException("Inspection clipped "+item.name);
                        var crop=new Vector2(h*target.height/target.width,h);
                        camera.Render();Graphics.Blit(target,tile,crop,mid-crop*.5f);RenderTexture.active=tile;
                        if(face<3)sheet.ReadPixels(new Rect(0,0,480,480),480*face,0,false);
                        else {
                            underside.ReadPixels(new Rect(0,0,480,480),0,0,false);
                            File.WriteAllBytes(Path.Combine(folder,$"bottom-{i:D3}.jpg"),underside.EncodeToJPG(95));
                        }
                    }
                    File.WriteAllBytes(Path.Combine(folder,$"{i:D3}.jpg"),sheet.EncodeToJPG(95));
                    report.WriteLine($"{i:D3}\t{item.name}");
                    item.Drop(camera);
                    if(!collider.enabled || item.transform.parent || item.GetComponent<Rigidbody>().isKinematic || camera.fieldOfView!=standardFov)throw new InvalidOperationException(item.name+" failed drop");
                    // Remove only the audited scene instance from the next review frame.
                    item.gameObject.SetActive(false);
                }
            }
            Debug.Log($"Held rotation audit: {products.Length} items, {products.Length*3} reverse/side views + {products.Length} underside views, 1920x1080");
            camera.ResetProjectionMatrix();camera.targetTexture=null;RenderTexture.active=null;
            RenderTexture.ReleaseTemporary(tile);target.Release();Destroy(target);Destroy(sheet);Destroy(underside);
        }

        static IEnumerator ReviewCatalog(MarketPlayer player)
        {
            var folder=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            string output=Path.Combine(folder,"CatalogCloseups");
            Directory.CreateDirectory(output);
            string profiles=Path.Combine(folder,"FrozenProfiles");Directory.CreateDirectory(profiles);
            var camera=player.viewCamera;
            camera.enabled=false;
            player.flashlightModel.SetActive(false);
            var target=new RenderTexture(1920,1080,24);
            camera.targetTexture=target;
            camera.nearClipPlane=.013f;
            var tile=new Texture2D(880,880,TextureFormat.RGB24,false);
            var groups=FindObjectsByType<MarketDetailVisibility>();
            float[] widths={.24f,.19f,.19f,.2f,.12f,.23f,.19f,.16f,.23f,.17f,.13f,.09f,.095f,.10f,.095f,.17f,.20f,.09f,.16f,.13f,.25f,.19f,.26f,.25f,.22f,.15f,.15f,.38f,.53f,.32f,.22f};
            float[] heights={.22f,.11f,.10f,.25f,.10f,.25f,.18f,.10f,.24f,.18f,.27f,.20f,.20f,.20f,.22f,.25f,.21f,.12f,.17f,.21f,.25f,.10f,.24f,.25f,.19f,.13f,.19f,.44f,.30f,.25f,.18f};
            foreach(string line in File.ReadAllLines(Path.Combine(folder,"catalog-views.tsv"))){
                var cols=line.Split('\t');
                int id=int.Parse(cols[0]), family=id/6,v=id%6;
                float Read(int i)=>float.Parse(cols[i],System.Globalization.CultureInfo.InvariantCulture);
                Vector3 foot=new Vector3(Read(1),Read(2),Read(3)),outward=new Vector3(Read(4),0,Read(5));
                float h=heights[family],w=widths[family];
                if(family==24 && (v==0 || v==5) || family==25 && (v==0 || v==4))h=.055f;
                else if(family==24)h=.265f;
                else if(family==25)h=v==2 || v==5?.085f:.208f;
                if(family==25 && v==4)h=.13f;
                if(family==28 && v==3){h=.08f;w=.20f;}
                if(family==26){h=v<2?.244f:v==3?.131f:v==5?.070f:.115f;w=v<2?.078f:v==3?.055f:.078f;}
                if(family==19 && (v==1 || v==3))h=.10f;
                if(family==18 && v==3){h=.26f;w=.21f;}
                if(family==21 && v==1){w=.10f;h=.165f;}
                if(family==29 && v==4){w=.085f;h=.044f;}
                if(family==29 && v==2 || family==29 && v==5){w=.155f;h=.20f;}
                if(family==4 && v==3)h=.116f;
                if(family==10 && v%2==0)h=.32f;
                if(family==27){h=v==0?.322f:v==3?.335f:v==1?.301f:v==4?.288f:v==2?.225f:.218f;w=v==0||v==3?.20f:v==1||v==4?.12f:.083f;}
                if(family==28){h=v==2?.235f:v==1?.17f:v==3?.058f:.115f;w=v==2?.22f:v==1?.24f:v==3?.11f:.35f;}
                if(family==7){h=v==0||v==4?.171f:v==2?.103f:.046f;w=v==0||v==4?.088f:v==2?.083f:.134f;}
                float elevation=family==1||family==2||h<.07f?35f:10f;
                if(family==7 || family==19 && (v==1 || v==3))elevation=27;
                var aim=foot+Vector3.up*(h*.51f);
                var eye=aim+(outward+Vector3.up*Mathf.Tan(elevation*Mathf.Deg2Rad)).normalized*.68f;
                player.transform.position=foot+outward*.65f;
                camera.transform.position=eye; camera.transform.LookAt(aim);
                camera.fieldOfView=2*Mathf.Rad2Deg*Mathf.Atan(Mathf.Max(w,h)*.83f/.68f);
                foreach(var group in groups)group.Refresh();
                yield return null;
                camera.Render();RenderTexture.active=target;
                tile.ReadPixels(new Rect(520,100,880,880),0,0,false);tile.Apply(false);
                File.WriteAllBytes(Path.Combine(output,$"{family:D2}_{v}.jpg"),tile.EncodeToJPG(94));
                if(family==20 || family==23 || family==27 || family==7 || family==22 && v!=1){
                    var right=Vector3.Cross(Vector3.up,outward);
                    camera.transform.position=aim+outward*.58f+right*.34f+Vector3.up*.182f;
                    camera.transform.LookAt(aim);
                    camera.fieldOfView=2*Mathf.Rad2Deg*Mathf.Atan(Mathf.Max(w,h)*.82f/.70f);
                    camera.Render();RenderTexture.active=target;
                    tile.ReadPixels(new Rect(520,100,880,880),0,0,false);tile.Apply(false);
                    File.WriteAllBytes(Path.Combine(profiles,$"{family:D2}_{v}.jpg"),tile.EncodeToJPG(94));
                }
            }
            camera.targetTexture=null;RenderTexture.active=null;
            target.Release();Destroy(target);Destroy(tile);
            Debug.Log("Catalog inspection rendered 186 shelf views at 1920x1080");
        }

        static IEnumerator ReviewPantry(MarketPlayer player,bool storeReview)
        {
            var camera=player.viewCamera;
            var target=new RenderTexture(Screen.width,Screen.height,24);
            var tile=RenderTexture.GetTemporary(440,248,0);
            var sheet=new Texture2D(880,744,TextureFormat.RGB24,false);
            camera.enabled=false;camera.targetTexture=target;
            // Face both sides of the noodle gondola, then the lids and produce case.
            Vector3[] eye={new Vector3(-6.35f,1.65f,-12),new Vector3(-9.65f,1.65f,-12),
                new Vector3(-6.43f,1.70f,-3),new Vector3(-6.52f,1.65f,6),
                new Vector3(-43.98f,1.66f,-15.1f),new Vector3(-43.98f,1.66f,-12.9f)};
            Vector3[] aim={new Vector3(-7.34f,1.20f,-12),new Vector3(-8.66f,1.20f,-12),
                new Vector3(-7.34f,1.23f,-3),new Vector3(-7.34f,1.20f,6),
                new Vector3(-45.12f,1.57f,-15.1f),new Vector3(-45.12f,1.57f,-12.9f)};
            if(storeReview)
            {
                eye=new[]{new Vector3(-9,1.62f,40.48f),new Vector3(-11.3f,1.62f,33.52f),
                    new Vector3(2.48f,1.47f,-11.6f),new Vector3(8.5f,1.49f,-3.2f),
                    new Vector3(13.35f,6.8f,-2.1f),new Vector3(1.4f,1.63f,-30.5f)};
                aim=new[]{new Vector3(-9,1.14f,41.33f),new Vector3(-11.3f,1.21f,31.95f),
                    new Vector3(1.63f,1.23f,-11.6f),new Vector3(9.41f,1.20f,-3.2f),
                    new Vector3(14.47f,6.6f,-2.1f),new Vector3(-3,2.4f,-6)};
            }
            for(int i=0;i<eye.Length;i++)
            {
                player.flashlight.enabled=!storeReview || i!=5;
                player.transform.position=eye[i]-Vector3.up*.65f;
                camera.transform.LookAt(aim[i]);
                foreach(var group in FindObjectsByType<MarketDetailVisibility>())group.Refresh();
                yield return null;
                camera.Render();Graphics.Blit(target,tile);
                RenderTexture.active=tile;
                sheet.ReadPixels(new Rect(0,0,440,248),(i%2)*440,(2-i/2)*248,false);
            }
            var goods=FindObjectsByType<MarketProduct>();
            int checkedUnits=0;
            foreach(var unit in goods)
            {
                if(!storeReview && !unit.name.StartsWith("봉지라면") && !unit.name.StartsWith("컵라면") && !unit.name.StartsWith("즉석밥"))continue;
                var collider=unit.GetComponent<BoxCollider>();
                var body=unit.GetComponent<Rigidbody>();
                if(storeReview && !collider)continue; // Existing loose produce uses primitive colliders.
                if(!collider||!body||unit.GetComponentsInChildren<MeshRenderer>().Length==0)
                    throw new InvalidOperationException("Invalid pantry product "+unit.name);
                unit.PickUp(camera);
                if(collider.enabled||unit.transform.parent!=camera.transform)throw new InvalidOperationException("Pickup failed");
                unit.Drop(camera);
                if(!collider.enabled||body.isKinematic)throw new InvalidOperationException("Drop failed");
                checkedUnits++;
            }
            Debug.Log($"Retail review (store={storeReview}): {target.width}x{target.height}, {checkedUnits} pickup/drop checks");
            Debug.Log("PANTRY_VIEW "+Convert.ToBase64String(sheet.EncodeToJPG(87)));
            camera.targetTexture=null;RenderTexture.active=null;
            RenderTexture.ReleaseTemporary(tile);target.Release();Destroy(target);Destroy(sheet);
        }

        static IEnumerator WalkToPepper(MarketPlayer player)
        {
            // A minimized player can skip its back buffer. Check for real
            // camera frames at spawn before choosing the measurement path.
            var controller=player.GetComponent<CharacterController>();
            var eye=player.viewCamera;
            var start=new Vector3(-24,.9f,-10.6f);
            var goal=new Vector3(-30,.9f,-10.6f);
            var times=new System.Collections.Generic.List<double>(2500);
            int rendered=0,slow=0;
            void CountFrame(ScriptableRenderContext _,Camera cam){if(cam==eye)rendered++;}
            RenderPipelineManager.endCameraRendering+=CountFrame;
            yield return new WaitForSecondsRealtime(1);
            bool offscreen=rendered==0;
            RenderTexture target=null;
            Texture2D pixel=null;
            void DrawOffscreen()
            {
                eye.Render();
                RenderTexture.active=target;
                pixel.ReadPixels(new Rect(target.width/2,target.height/2,1,1),0,0,false);
            }
            if(offscreen)
            {
                target=new RenderTexture(Screen.width,Screen.height,24);
                pixel=new Texture2D(1,1,TextureFormat.RGB24,false);
                eye.enabled=false;eye.targetTexture=target;
                for(int i=0;i<30;i++){DrawOffscreen();yield return null;}
            }
            rendered=0;
            player.transform.position=start;
            controller.enabled=true;controller.minMoveDistance=0;
            var clock=System.Diagnostics.Stopwatch.StartNew();
            double previous=clock.Elapsed.TotalMilliseconds;
            do
            {
                float step=Mathf.Min(.05f,Time.unscaledDeltaTime);
                var advance=Vector3.left*Mathf.Min(Mathf.Max(0,player.transform.position.x-goal.x),step*2.5f);
                controller.Move(advance+Vector3.down*(step*3));
                eye.transform.LookAt(new Vector3(-30,1.09f,-8));
                if(offscreen)DrawOffscreen();
                yield return null;
                double now=clock.Elapsed.TotalMilliseconds;
                double ms=now-previous;times.Add(ms);if(ms>16.67)slow++;
                previous=now;
            }while(clock.Elapsed.TotalSeconds<3.5);
            RenderPipelineManager.endCameraRendering-=CountFrame;
            if(offscreen)
            {
                eye.targetTexture=null;RenderTexture.active=null;target.Release();Destroy(target);Destroy(pixel);
            }
            times.Sort();
            double Percentile(float q)=>times[Mathf.CeilToInt(times.Count*q)-1];
            float walked=start.x-player.transform.position.x;
            File.WriteAllText(Path.Combine(Application.dataPath,"..","pepper-first-visit.txt"),
                $"{Screen.width}x{Screen.height} | real startup preparation {MarketRenderPreparation.ElapsedMilliseconds:F2} ms / {MarketRenderPreparation.PreparedViews} views | {(offscreen?"offscreen GPU sync":"normal camera")} {rendered} rendered | distance {walked:F2} m | p95 {Percentile(.95f):F2} ms | p99 {Percentile(.99f):F2} ms | max {times[times.Count-1]:F2} ms | frames over 16.67ms {slow}/{times.Count}");
        }
    }
}
