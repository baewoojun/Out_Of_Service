using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OutOfService
{
    // Opt-in standalone validation. No runtime component is added in normal play.
    public sealed class MarketFloorReview : MonoBehaviour
    {
        string folder;
        MarketPlayer player;
        CharacterController controller;
        Camera camera;
        RenderTexture target;
        Texture2D image;
        StreamWriter report;
        int checks;
        float velocity;
        bool finished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(!Application.isEditor && Array.IndexOf(Environment.GetCommandLineArgs(),"-floorReview")>=0)
                new GameObject("Three floor review").AddComponent<MarketFloorReview>();
        }

        IEnumerator Start()
        {
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../FloorReview"));Directory.CreateDirectory(folder);
            Application.logMessageReceived+=OnLog;
            report=new StreamWriter(Path.Combine(folder,"route-results.tsv")){AutoFlush=true};
            report.WriteLine("check\tresult\tx\ty\tz");
            yield return null;
            while (!MarketRenderPreparation.IsReady) yield return null;
            player=MarketPlayer.Instance;player.enabled=false;
            foreach(var threat in FindObjectsByType<MarketThreat>())threat.enabled=false;
            controller=player.GetComponent<CharacterController>();camera=player.viewCamera;
            player.flashlight.enabled=true;player.bareHandsModel.SetActive(false);player.flashlightModel.SetActive(false);
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;Application.runInBackground=true;
            target=new RenderTexture(1920,1080,24);image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            camera.enabled=true;camera.targetTexture=target;
            foreach(var door in FindObjectsByType<MarketSwingDoor>()){
                if(door.name.Contains("fire door"))door.Toggle();
            }
            yield return new WaitForSecondsRealtime(.8f);
            foreach(var door in FindObjectsByType<MarketSwingDoor>())if(door.name.Contains("fire door")){
                Check(door.name+" open",Quaternion.Angle(door.transform.localRotation,Quaternion.identity)>85);
            }
            foreach(float x in new[]{-46f,46f}){
                Warp(new Vector3(x,-4.08f,-31.8f));
                for(int level=0;level<2;level++){
                    float y=-5+level*5;
                    yield return Walk(new Vector3(x,y+.92f,-34),"stair entry");
                    yield return Walk(new Vector3(x-1.5f,y+.92f,-34),"lower landing");
                    yield return Walk(new Vector3(x-1.5f,y+3.42f,-40.8f),"first flight up");
                    yield return Walk(new Vector3(x+1.5f,y+3.42f,-40.8f),"mid landing");
                    yield return Walk(new Vector3(x+1.5f,y+5.92f,-34),"second flight up");
                    yield return Walk(new Vector3(x,y+5.92f,-34),"upper landing");
                    yield return Walk(new Vector3(x,y+5.92f,-31.8f),"stair exit");
                }
                for(int level=1;level>=0;level--){
                    float y=-5+level*5;
                    yield return Walk(new Vector3(x,y+5.92f,-34),"descending entry");
                    yield return Walk(new Vector3(x+1.5f,y+5.92f,-34),"descending upper landing");
                    yield return Walk(new Vector3(x+1.5f,y+3.42f,-40.8f),"second flight down");
                    yield return Walk(new Vector3(x-1.5f,y+3.42f,-40.8f),"descending mid landing");
                    yield return Walk(new Vector3(x-1.5f,y+.92f,-34),"first flight down");
                    yield return Walk(new Vector3(x,y+.92f,-34),"descending lower landing");
                    yield return Walk(new Vector3(x,y+.92f,-31.8f),"descending exit");
                }
            }
            foreach(float x in new[]{34.5f,41.5f}){
                Warp(new Vector3(x,-4.08f,-7));
                yield return Walk(new Vector3(x,.92f,7),"B1 escalator ascent");
                yield return Walk(new Vector3(x,-4.08f,-7),"B1 escalator descent");
                Warp(new Vector3(x,.92f,-7));
                yield return Walk(new Vector3(x,5.92f,7),"2F escalator ascent");
                yield return Walk(new Vector3(x,.92f,-7),"2F escalator descent");
            }
            Warp(new Vector3(-46,-4.08f,-31.8f));
            yield return Walk(new Vector3(-46,-4.08f,-27),"B1 west circulation");
            yield return Walk(new Vector3(46,-4.08f,-27),"B1 cross concourse");
            Warp(new Vector3(3,-4.08f,-27));
            yield return Walk(new Vector3(3,-4.08f,36),"B1 central concourse");
            Warp(new Vector3(-15,.92f,-21));
            foreach(var p in new[]{new Vector3(29,.92f,-21),new Vector3(29,.92f,28.7f),new Vector3(-15,.92f,28.7f),new Vector3(-15,.92f,-21)})
                yield return Walk(p,"1F complete retail loop");
            yield return Walk(new Vector3(-15,.92f,.5f),"1F cross aisle approach");
            yield return Walk(new Vector3(29,.92f,.5f),"1F cross aisle");
            Warp(new Vector3(-40,5.92f,-34.5f));
            foreach(var p in new[]{new Vector3(29.5f,5.92f,-34.5f),new Vector3(29.5f,5.92f,29.4f),new Vector3(-44.5f,5.92f,29.4f),new Vector3(-44.5f,5.92f,-31.5f),new Vector3(-40,5.92f,-31.5f),new Vector3(-40,5.92f,-34.5f)})
                yield return Walk(p,"2F complete fashion loop");
            foreach(Transform unit in GameObject.Find("Market Prototype V7 - textured horror environment").transform){
                if(!unit.name.StartsWith("B1 tenant - "))continue;
                if(!unit.name.Contains("화장실")){
                    float sideEntry=unit.name.Contains("약국")?2.25f:3.05f;
                    foreach(int side in new[]{-1,1}){
                        Warp(unit.TransformPoint(new Vector3(side*sideEntry,.92f,-1.3f)));
                        yield return Walk(unit.TransformPoint(new Vector3(side*sideEntry,.92f,1.8f)),unit.name+" widened side entrance");
                        yield return Walk(unit.TransformPoint(new Vector3(0,.92f,1.8f)),unit.name+" open storefront lateral circulation");
                        yield return Walk(unit.TransformPoint(new Vector3(0,.92f,-1.3f)),unit.name+" open storefront exit");
                    }
                }
                Warp(unit.TransformPoint(new Vector3(0,.92f,-2)));
                if(unit.name.Contains("화장실")){
                    yield return Walk(unit.TransformPoint(new Vector3(0,.92f,1.4f)),"washroom entrance");
                    yield return Walk(unit.TransformPoint(new Vector3(4.9f,.92f,1.4f)),"washroom privacy turn");
                    yield return Walk(unit.TransformPoint(new Vector3(4.9f,.92f,4.0f)),"washroom access");
                    yield return Walk(unit.TransformPoint(new Vector3(2.7f,.92f,4.0f)),"washroom stall approach");
                }else {
                    yield return Walk(unit.TransformPoint(new Vector3(0,.92f,6)),unit.name+" public access");
                    if(unit.name.Contains("화원")||unit.name.Contains("세탁")){
                        foreach(var p in new[]{new Vector3(0,.92f,12.8f),new Vector3(2.5f,.92f,12.8f),new Vector3(2.5f,.92f,7),new Vector3(0,.92f,7),new Vector3(0,.92f,1)})
                            yield return Walk(unit.TransformPoint(p),unit.name+" detailed shop circulation");
                    }
                    if(unit.name.Contains("모아")||unit.name.Contains("바른")||unit.name.Contains("픽셀")||unit.name.Contains("맡김")){
                        foreach(var p in new[]{new Vector3(0,.92f,11.8f),new Vector3(1.9f,.92f,11.8f),new Vector3(1.9f,.92f,2),new Vector3(0,.92f,2),new Vector3(0,.92f,-2)})
                            yield return Walk(unit.TransformPoint(p),unit.name+" customer route return");
                    }
                }
            }
            Warp(new Vector3(-35.2f,5.92f,-32));
            foreach(var p in new[]{new Vector3(-35.2f,5.92f,29.4f),new Vector3(-19,5.92f,29.4f),new Vector3(-19,5.92f,-32),new Vector3(-35.2f,5.92f,-32)})
                yield return Walk(p,"2F infill merchandise picking loop");
            Warp(new Vector3(33,5.92f,-29));
            foreach(var p in new[]{new Vector3(46.7f,5.92f,-29),new Vector3(46.7f,5.92f,-11.6f),new Vector3(33,5.92f,-11.6f),new Vector3(33,5.92f,-29)})
                yield return Walk(p,"2F travel department arrival loop");
            var newDoors=new List<MarketSwingDoor>();
            foreach(var door in FindObjectsByType<MarketSwingDoor>())
                if(door.name.StartsWith("appliance fridge door")||door.name.StartsWith("family room privacy door")||door.name.StartsWith("staff kitchen door")||door.name.StartsWith("washroom stall door")){newDoors.Add(door);door.Toggle();}
            yield return new WaitForSecondsRealtime(.8f);
            foreach(var door in newDoors)Check(door.name+" opens",Quaternion.Angle(door.transform.localRotation,Quaternion.identity)>85);
            foreach(Transform unit in GameObject.Find("Market Prototype V7 - textured horror environment").transform){
                if(unit.name!="B1 tenant - 모퉁이 커피" && unit.name!="B1 tenant - 한끼 식당")continue;
                Warp(unit.TransformPoint(new Vector3(0,.92f,10.5f)));
                foreach(var p in new[]{new Vector3(5.1f,.92f,10.5f),new Vector3(5.1f,.92f,13.08f),new Vector3(4.3f,.92f,13.08f),new Vector3(4.3f,.92f,14.1f),new Vector3(0,.92f,14.1f),new Vector3(-4,.92f,14.1f),new Vector3(4.3f,.92f,14.1f),new Vector3(4.3f,.92f,13.08f),new Vector3(5.1f,.92f,13.08f),new Vector3(5.1f,.92f,10.5f),new Vector3(0,.92f,10.5f)})
                    yield return Walk(unit.TransformPoint(p),unit.name+" staff kitchen round trip");
            }
            var family=GameObject.Find("1F nursing and family room").transform;
            Warp(family.TransformPoint(new Vector3(1.8f,.92f,2.1f)));
            foreach(var p in new[]{new Vector3(1.8f,.92f,-1.5f),new Vector3(-.8f,.92f,-1.5f),new Vector3(-.8f,.92f,-3.4f),new Vector3(-1.6f,.92f,-3.8f),new Vector3(-.8f,.92f,-3.4f),new Vector3(1.6f,.92f,-3.4f),new Vector3(1.6f,.92f,-3.8f),new Vector3(-.8f,.92f,-3.4f),new Vector3(-.8f,.92f,-1.5f),new Vector3(1.8f,.92f,-1.5f),new Vector3(1.8f,.92f,2.1f)})
                yield return Walk(family.TransformPoint(p),"family room entrance / both nursing bays / return");
            Warp(new Vector3(-46,.92f,-31.6f));
            foreach(var p in new[]{new Vector3(-40.6f,.92f,-31.6f),new Vector3(-40.6f,.92f,-21.4f),new Vector3(-44.6f,.92f,-21.4f),new Vector3(-40.6f,.92f,-21.4f),new Vector3(-40.6f,.92f,-31.6f),new Vector3(-46,.92f,-31.6f)})
                yield return Walk(p,"WEST fire-door approach remains clear after family room relocation");
            Warp(new Vector3(-40.6f,.92f,-34.3f));
            foreach(var p in new[]{new Vector3(-29.7f,.92f,-34.3f),new Vector3(-29.7f,.92f,-26.1f),new Vector3(-40.6f,.92f,-26.1f),new Vector3(-40.6f,.92f,-29.1f),new Vector3(-29.7f,.92f,-29.1f),new Vector3(-29.7f,.92f,-34.3f),new Vector3(-40.6f,.92f,-34.3f)})
                yield return Walk(p,"cookware corner perimeter and cross aisle");
            var prepDoors=new List<MarketSwingDoor>();
            foreach(var door in FindObjectsByType<MarketSwingDoor>())
                if(door.name.Contains("정육 staff door")||door.name.Contains("수산 staff door")){prepDoors.Add(door);door.Toggle();}
            yield return new WaitForSecondsRealtime(.8f);
            Check("two fresh prep doors exist",prepDoors.Count==2);
            foreach(var door in prepDoors)Check(door.name+" opens",Quaternion.Angle(door.transform.localRotation,Quaternion.identity)>85);
            foreach(float x in new[]{-42f,-27f}){
                Warp(new Vector3(x+3,.92f,30.5f));
                foreach(var p in new[]{new Vector3(x+3,.99f,33.5f),new Vector3(x+3,.99f,37.5f),new Vector3(x,.99f,37.5f),new Vector3(x,.99f,40.5f),new Vector3(x,.99f,42.5f),new Vector3(x,.99f,37.5f),new Vector3(x+3,.99f,37.5f),new Vector3(x+3,.99f,33.5f),new Vector3(x+3,.92f,30.5f)})
                    yield return Walk(p,(x==-42?"meat":"fish")+" staff room and walk-in access");
            }
            foreach(var door in prepDoors)door.Toggle();
            yield return new WaitForSecondsRealtime(.8f);
            foreach(var door in prepDoors)Check(door.name+" closes",Quaternion.Angle(door.transform.localRotation,Quaternion.identity)<1);
            int supportRooms=0;
            foreach(Transform unit in GameObject.Find("Market Prototype V7 - textured horror environment").transform){
                if(!unit.name.StartsWith("B1 support room - "))continue;
                supportRooms++;
                var door=unit.GetComponentInChildren<MarketSwingDoor>();var sliding=unit.GetComponentInChildren<MarketSlidingDoor>();
                if(door)door.Toggle();else sliding.Toggle();
                yield return new WaitForSecondsRealtime(1.5f);
                Check(unit.name+" door opens",door?Mathf.Abs(Quaternion.Angle(door.transform.localRotation,Quaternion.identity)-100)<1:sliding.OpenFraction>.99f);
                Warp(unit.TransformPoint(new Vector3(0,.92f,-1.8f)));
                foreach(float z in new[]{1.5f,6f,12f,1.5f,-1.8f})yield return Walk(unit.TransformPoint(new Vector3(0,.92f,z)),unit.name+" entry and return");
                if(door)door.Toggle();else sliding.Toggle();yield return new WaitForSecondsRealtime(1.5f);
                Check(unit.name+" door closes",door?Quaternion.Angle(door.transform.localRotation,Quaternion.identity)<1:sliding.OpenFraction<.01f);
            }
            Check("five occupied B1 support rooms",supportRooms==5);
            foreach(string level in new[]{"1F","2F"}){
                var unit=GameObject.Find(level+" public restroom suite").transform;
                Check(level+" complete public restroom suite exists",unit.GetComponentsInChildren<MarketSwingDoor>().Length==6);
                Warp(unit.TransformPoint(new Vector3(0,.94f,-1.6f)));
                yield return Walk(unit.TransformPoint(new Vector3(0,.94f,1.0f)),level+" restroom main entrance");
                for(int room=0;room<3;room++){
                    float x=-4.2f+room*4.2f;
                    var door=Array.Find(unit.GetComponentsInChildren<MarketSwingDoor>(),d=>d.name==level+" public washroom entrance "+room);
                    yield return Walk(unit.TransformPoint(new Vector3(x,.94f,1.0f)),level+" restroom lobby approach "+room);
                    Check(door.name+" initially closed",!door.isOpen);
                    door.Toggle();yield return new WaitForSecondsRealtime(.7f);
                    Check(door.name+" open",Quaternion.Angle(door.transform.localRotation,Quaternion.identity)>95);
                    yield return Walk(unit.TransformPoint(new Vector3(x,.94f,4.9f)),level+" restroom washbasin access "+room);
                    if(room<2){
                        int stalls=room==0?2:1;
                        for(int stall=0;stall<stalls;stall++){
                            float cx=x-1+stall*2;
                            var cubicle=Array.Find(unit.GetComponentsInChildren<MarketSwingDoor>(),d=>d.name==level+" public washroom cubicle "+room+"-"+stall);
                            cubicle.Toggle();yield return new WaitForSecondsRealtime(.7f);
                            Check(cubicle.name+" opens",Quaternion.Angle(cubicle.transform.localRotation,Quaternion.identity)>95);
                            yield return Walk(unit.TransformPoint(new Vector3(cx,.94f,4.9f)),cubicle.name+" approach");
                            yield return Walk(unit.TransformPoint(new Vector3(cx,.94f,6.85f)),cubicle.name+" enter");
                            yield return Walk(unit.TransformPoint(new Vector3(cx,.94f,4.9f)),cubicle.name+" leave");
                            cubicle.Toggle();yield return new WaitForSecondsRealtime(.7f);
                            Check(cubicle.name+" closes",Quaternion.Angle(cubicle.transform.localRotation,Quaternion.identity)<1);
                        }
                        yield return Walk(unit.TransformPoint(new Vector3(x,.94f,4.9f)),level+" return from cubicles "+room);
                    }else{
                        yield return Walk(unit.TransformPoint(new Vector3(x,.94f,6.6f)),level+" family restroom toilet approach");
                        yield return Walk(unit.TransformPoint(new Vector3(x,.94f,4.9f)),level+" family restroom return");
                    }
                    yield return Walk(unit.TransformPoint(new Vector3(x,.94f,1.0f)),level+" restroom return to lobby "+room);
                    door.Toggle();yield return new WaitForSecondsRealtime(.7f);
                    Check(door.name+" closes",Quaternion.Angle(door.transform.localRotation,Quaternion.identity)<1);
                }
                yield return Walk(unit.TransformPoint(new Vector3(0,.94f,1.0f)),level+" restroom lobby return");
                yield return Walk(unit.TransformPoint(new Vector3(0,.94f,-1.6f)),level+" restroom return to shop concourse");
            }
            string[] views={"b1-concourse","b1-cafe","b1-pharmacy","b1-florist","b1-laundry","b1-washroom","west-stairs","b1-escalator","1f-appliances","2f-service","1f-family-room","1f-cross-aisle","1f-hardware","2f-fashion"};
            Vector3[] eyes={new(3,-3.35f,-25),new(-12,-3.35f,-16),new(12,-3.35f,-16),new(12,-3.35f,4),new(12,-3.35f,24),new(-14,-3.35f,26.7f),new(-47.5f,-3.35f,-34),new(38,-3.35f,-9),new(39,1.65f,22),new(18,6.65f,-27),new(-32,1.65f,-26),new(-15,1.65f,.5f),new(36,1.65f,-25),new(-44.5f,6.65f,11)};
            Vector3[] aims={new(2,-3.2f,15),new(-20,-3.8f,-16),new(21,-3.8f,-16),new(21,-3.8f,4),new(23,-3.8f,24),new(-22,-3.8f,26.7f),new(-47.5f,-1.2f,-40),new(38,-1.5f,2),new(39,1.1f,25),new(18,6.1f,-23),new(-35,1.4f,-31),new(16,1.5f,.5f),new(39,1.7f,-12),new(-29,6.2f,11)};
            eyes[10]=new Vector3(-44.6f,1.65f,-22);aims[10]=new Vector3(-46.8f,1.4f,-27.2f);
            camera.enabled=false;
            using var timings=new StreamWriter(Path.Combine(folder,"render-times.tsv"));
            timings.WriteLine("view\trender_sync_p50_ms\trender_sync_p95_ms\tsamples");
            for(int i=0;i<views.Length;i++){
                Warp(eyes[i]-Vector3.up*.65f);camera.transform.position=eyes[i];camera.transform.LookAt(aims[i]);
                foreach(var visibility in FindObjectsByType<MarketDetailVisibility>())visibility.Refresh();
                yield return null;camera.Render();RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,1920,1080),0,0,false);
                File.WriteAllBytes(Path.Combine(folder,views[i]+".jpg"),image.EncodeToJPG(92));
                yield return null; // snapshot and timed draw must not share an engine frame
                var samples=new List<double>();var timer=new System.Diagnostics.Stopwatch();
                for(int frame=0;frame<80;frame++){
                    timer.Restart();camera.Render();RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,1,1),0,0,false);timer.Stop();
                    if(frame>=20)samples.Add(timer.Elapsed.TotalMilliseconds);
                    yield return null;
                }
                samples.Sort();timings.WriteLine(FormattableString.Invariant($"{views[i]}\t{samples[30]:F3}\t{samples[57]:F3}\t60"));timings.Flush();
            }
            camera.enabled=true;
            foreach(var door in newDoors)door.Toggle();
            foreach(var door in FindObjectsByType<MarketSwingDoor>())if(door.name.Contains("fire door"))door.Toggle();
            yield return new WaitForSecondsRealtime(.8f);
            foreach(var door in FindObjectsByType<MarketSwingDoor>())if(door.name.Contains("fire door"))Check(door.name+" closed",Quaternion.Angle(door.transform.localRotation,Quaternion.identity)<1);
            foreach(var door in newDoors)Check(door.name+" closes",Quaternion.Angle(door.transform.localRotation,Quaternion.identity)<1);
            report.Dispose();report=null;finished=true;
            File.WriteAllText(Path.Combine(folder,"result.txt"),$"PASS: {checks} checks; six fire doors, both stairs B1-2F return, four escalator flights both directions, B1 concourse and ten tenant entrances including widened side entries, mall customer returns, five support rooms, 1F retail loop, 2F fashion and travel picking loops, 1F/2F women-men-family restroom returns and twelve privacy doors, appliance/kitchen/family doors, both fresh prep rooms and walk-ins. 1920x1080 Windows player. Timings are controlled render+GPU synchronization, not full-game FPS.");
            camera.targetTexture=null;RenderTexture.active=null;target.Release();
            Application.Quit();
        }

        void Warp(Vector3 p){controller.enabled=false;player.transform.position=p;controller.enabled=true;velocity=0;Physics.SyncTransforms();}
        IEnumerator Walk(Vector3 goal,string name)
        {
            const float dt=1f/60f;
            int steps=0;
            while(new Vector2(goal.x-player.transform.position.x,goal.z-player.transform.position.z).magnitude>.10f && steps++<2400){
                Vector3 delta=goal-player.transform.position;delta.y=0;
                if(controller.isGrounded && velocity<0)velocity=-2;
                velocity-=18*dt;
                controller.Move(Vector3.ClampMagnitude(delta,4.2f*dt)+Vector3.up*velocity*dt);
                if(steps%10==0)camera.transform.LookAt(player.transform.position+delta.normalized*4+Vector3.up*.65f);
                yield return null;
            }
            for(int i=0;i<8;i++){controller.Move(Vector3.down*.04f);yield return null;}
            Check(name,steps<2400 && Mathf.Abs(player.transform.position.y-goal.y)<.22f);
        }
        void Check(string name,bool pass)
        {
            var p=player.transform.position;
            report.WriteLine(FormattableString.Invariant($"{name}\t{(pass?"PASS":"FAIL")}\t{p.x:F3}\t{p.y:F3}\t{p.z:F3}"));
            checks++;
            if(!pass){
                foreach(var hit in Physics.OverlapSphere(p,.8f))report.WriteLine("NEARBY\t"+hit.name+"\t"+hit.bounds);
                throw new InvalidOperationException("Floor review failed: "+name+" at "+p);
            }
        }
        void OnLog(string message,string stack,LogType type)
        {
            if(finished || type!=LogType.Exception && type!=LogType.Error)return;
            finished=true;report?.Flush();File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL: "+message+"\n"+stack);Application.Quit(1);
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;report?.Dispose();}
    }
}
