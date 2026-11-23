using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OutOfService
{
    // Tests real colliders and moving door leaves in the standalone player.
    public sealed class MarketFitoutReview:MonoBehaviour
    {
        readonly List<string> checks=new();string folder;bool done;
        static bool Requested=>Array.IndexOf(Environment.GetCommandLineArgs(),"-fitoutReview")>=0;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Background(){if(Requested)Application.runInBackground=true;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(!Application.isEditor&&Requested)new GameObject("Fitout standalone review").AddComponent<MarketFitoutReview>();}
        void Check(string name,bool pass){checks.Add((pass?"PASS":"FAIL")+"\t"+name);File.WriteAllLines(Path.Combine(folder,"checks.tsv"),checks);}
        void Error(string message,string trace,LogType type){if(!done&&(type==LogType.Error||type==LogType.Exception)){File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL "+message+"\n"+trace);Application.Quit(2);}}
        IEnumerator Start()
        {
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../FitoutReview"));Directory.CreateDirectory(folder);Application.logMessageReceived+=Error;
            yield return null;while(!MarketRenderPreparation.IsReady)yield return null;
            var player=MarketPlayer.Instance;player.enabled=false;player.GetComponent<CharacterController>().enabled=false;
            foreach(var ai in FindObjectsByType<MarketThreat>())ai.enabled=false;
            var doors=FindObjectsByType<MarketSlidingDoor>();Check("five operational warehouse sliders",doors.Length==5);
            foreach(var door in doors){
                Vector3 closed=door.transform.localPosition;Quaternion rotation=door.transform.localRotation;
                Vector3 center=door.transform.position;
                var visuals=door.GetComponentsInChildren<Renderer>();var centers=new Vector3[visuals.Length];
                for(int n=0;n<visuals.Length;n++)centers[n]=visuals[n].bounds.center;
                Physics.SyncTransforms();
                Vector3 eye=center+Vector3.up*1.2f-door.transform.forward*2;
                Check(door.name+" reachable closed leaf",Physics.Raycast(eye,door.transform.forward,out var closedHit,2.2f)&&closedHit.collider.GetComponentInParent<MarketSlidingDoor>()==door);
                door.Toggle();yield return new WaitForSecondsRealtime(1.6f);Physics.SyncTransforms();
                Check(door.name+" fully opens without rotating",door.OpenFraction>.999f&&Quaternion.Angle(rotation,door.transform.localRotation)<.01f);
                Vector3 travel=door.transform.position-center;bool visualMotion=visuals.Length>4;
                for(int n=0;n<visuals.Length;n++)if(visuals[n].isPartOfStaticBatch||Vector3.Distance(visuals[n].bounds.center-centers[n],travel)>.015f)visualMotion=false;
                Check(door.name+" leaf, handles and lettering render at their moved positions",visualMotion);
                Check(door.name+" clears original opening",!Physics.Raycast(eye,door.transform.forward,out var openHit,2.2f)||openHit.collider.GetComponentInParent<MarketSlidingDoor>()!=door);
                var body=new GameObject("temporary door obstruction controller");body.transform.position=center+Vector3.up*.95f;
                var hit=body.AddComponent<CharacterController>();hit.height=1.8f;hit.radius=.32f;Physics.SyncTransforms();
                door.Toggle();yield return new WaitForSecondsRealtime(2.7f);
                Check(door.name+" reopens when a character blocks closing",door.isOpen&&door.OpenFraction>.99f);
                Destroy(body);yield return null;Physics.SyncTransforms();
                door.Toggle();yield return new WaitForSecondsRealtime(1.6f);
                Check(door.name+" returns exactly closed",!door.isOpen&&Vector3.Distance(closed,door.transform.localPosition)<.001f);
                door.transform.hasChanged=false;for(int frame=0;frame<10;frame++)yield return null;
                Check(door.name+" resting transform remains untouched",!door.transform.hasChanged);
            }
            int tomatoes=0,prepared=0,fashion=0,accessoryBays=0,accessories=0;
            foreach(var t in FindObjectsByType<Transform>()){
                if(t.name=="six ripe tomato retail punnet")tomatoes++;
                if(t.name=="간편대파"||t.name=="깐감자"||t.name=="카레용 채소")prepared++;
                if(t.name.StartsWith("2F fitted wall clothing bay "))fashion++;
                if(t.name.StartsWith("2F accessories fitted bay "))accessoryBays++;
                if(t.name=="carry-on travel case"||t.name=="stitched retail tote")accessories++;
            }
            Check("tomato retail punnets increased beyond former maximum 64: "+tomatoes,tomatoes>64);
            Check("prepared vegetable front plus reserve stock: "+prepared,prepared>=204);
            Check("nine fitted 2F wall departments",fashion==9);
            Check("three fitted accessories bays",accessoryBays==3);
            Check("existing wall stock plus 22 arrival-display luggage and tote products: "+accessories,accessories>=94);
            bool pass=!checks.Exists(s=>s.StartsWith("FAIL"));
            File.WriteAllText(Path.Combine(folder,"result.txt"),(pass?"PASS":"FAIL")+": "+checks.Count+" standalone stock / articulated door checks. Not a multiplayer synchronization test.");
            done=true;Application.Quit(pass?0:1);
        }
        void OnDestroy(){Application.logMessageReceived-=Error;}
    }
}
