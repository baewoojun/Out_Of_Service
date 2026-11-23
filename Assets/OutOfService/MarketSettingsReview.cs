using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering.Universal;

namespace OutOfService
{
    // Opt-in standalone acceptance checks. Uses an isolated settings profile and real Input System events.
    public sealed class MarketSettingsReview:MonoBehaviour
    {
        static bool Reload=>Array.IndexOf(Environment.GetCommandLineArgs(),"-settingsReloadReview")>=0;
        static bool Requested=>Reload||Array.IndexOf(Environment.GetCommandLineArgs(),"-settingsReview")>=0;
        readonly List<string> checks=new();string folder;bool done;
        Keyboard keyboard;Mouse mouse;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Background(){if(Requested){Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(!Application.isEditor&&Requested)new GameObject("Settings standalone acceptance review").AddComponent<MarketSettingsReview>();}
        void Check(string title,bool pass){checks.Add((pass?"PASS":"FAIL")+"\t"+title);File.WriteAllLines(Path.Combine(folder,"checks.tsv"),checks);}
        void Error(string message,string trace,LogType type){if(!done&&(type==LogType.Error||type==LogType.Exception)){done=true;File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL "+message+"\n"+trace);Application.Quit(2);}}
        IEnumerator KeyPress(Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return new WaitForEndOfFrame();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return new WaitForEndOfFrame();
        }
        IEnumerator MouseMove(Vector2 delta)
        {
            InputSystem.QueueStateEvent(mouse,new MouseState{delta=delta});yield return null;yield return new WaitForEndOfFrame();
            InputSystem.QueueStateEvent(mouse,new MouseState());yield return null;yield return new WaitForEndOfFrame();
        }
        IEnumerator Capture(string name)
        {
            for(int i=0;i<3;i++)yield return null;
            yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());Destroy(image);
        }
        IEnumerator Start()
        {
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../"+(Reload?"SettingsReloadReview":"SettingsReview")));
            Directory.CreateDirectory(folder);Application.logMessageReceived+=Error;
            yield return null;while(!MarketRenderPreparation.IsReady)yield return null;
            for(int i=0;i<5;i++)yield return null;
            var settings=MarketSettings.Instance;var player=MarketPlayer.Instance;var controller=player.GetComponent<CharacterController>();
            Check("settings booted and render preparation restored player",settings&&player.enabled&&Time.timeScale==1);
            Check("isolated test profile explicitly supplied",Array.IndexOf(Environment.GetCommandLineArgs(),"-settingsProfile")>=0);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-settingsProfile")<0){Finish();yield break;}
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();keyboard.MakeCurrent();mouse.MakeCurrent();
            var threats=FindObjectsByType<MarketThreat>();foreach(var ai in threats)ai.enabled=false;
            if(Reload){
                Check("saved sensitivity and inversion restored in a new process",Mathf.Abs(settings.Current.sensitivity-1.6f)<.001f&&settings.Current.invertY);
                Check("saved keyboard binding restored",settings.Current.bindings[(int)MarketAction.Interact].key==Key.G&&settings.Current.bindings[(int)MarketAction.Forward].key==Key.UpArrow);
                Check("saved mouse binding restored",settings.Current.bindings[(int)MarketAction.Inspect].mouse==3);
                Check("graphics, FOV and volume effective after restart",Mathf.Abs(settings.RuntimePipeline.renderScale-.9f)<.001f&&player.viewCamera.fieldOfView==82&&Mathf.Abs(AudioListener.volume-.7f)<.001f&&Application.targetFrameRate==90);
                Check("display dimensions restored after restart",Screen.width==1920&&Screen.height==1080);
                yield return KeyPress(Key.Escape);Check("Escape opens menu after restarting",MarketSettings.IsOpen&&Time.timeScale==0);
                settings.SetTab(1);yield return Capture("persisted-controls");
                yield return KeyPress(Key.Escape);Check("Escape resumes simulation after restarting",!MarketSettings.IsOpen&&Time.timeScale==1);
                yield return KeyPress(Key.Digit2);yield return KeyPress(Key.F);Check("existing flashlight slot and battery still operate",player.SelectedSlot==1&&player.flashlight.enabled&&player.Battery>0);
                Finish();yield break;
            }
            Check("normal 1920 by 1080 backbuffer",Screen.width==1920&&Screen.height==1080&&player.viewCamera.targetTexture==null);
            Check("HUD remains disabled and no calculator added",!MarketGame.Instance.showHud&&(!player.calculatorModel||!player.calculatorModel.activeSelf));
            yield return KeyPress(Key.Digit2);yield return KeyPress(Key.F);
            yield return KeyPress(Key.Escape);
            Check("Escape pauses gameplay, audio and cursor",MarketSettings.IsOpen&&Time.timeScale==0&&AudioListener.pause&&Cursor.lockState==CursorLockMode.None);
            Vector3 stopped=player.transform.position;float battery=player.Battery,stamina=player.Stamina;int damage=player.DamageStacks;
            if(threats.Length>0){threats[0].transform.position=player.transform.position+Vector3.forward;threats[0].enabled=true;}
            yield return KeyPress(Key.W);yield return new WaitForSecondsRealtime(.3f);
            Check("pause blocks movement, stamina, torch drain and nearby AI attacks",Vector3.Distance(stopped,player.transform.position)<.001f&&player.Battery==battery&&player.Stamina==stamina&&player.DamageStacks==damage);
            foreach(var ai in threats)ai.enabled=false;
            yield return Capture("graphics");settings.SetTab(1);yield return Capture("controls");settings.SetTab(2);yield return Capture("audio");
            settings.SetTab(1);settings.BeginBinding(MarketAction.Interact);yield return new WaitForSecondsRealtime(.2f);
            yield return KeyPress(Key.W);Check("duplicate input rejected without replacing action",settings.ListeningAction==(int)MarketAction.Interact&&settings.Draft.bindings[(int)MarketAction.Interact].key==Key.E);
            yield return KeyPress(Key.Escape);Check("Escape cancels capture without closing menu",settings.ListeningAction==-1&&MarketSettings.IsOpen);
            settings.BeginBinding(MarketAction.Interact);yield return new WaitForSecondsRealtime(.2f);yield return KeyPress(Key.G);
            Check("real G input captured",settings.ListeningAction==-1&&settings.Draft.bindings[(int)MarketAction.Interact].key==Key.G);
            settings.BeginBinding(MarketAction.Forward);yield return new WaitForSecondsRealtime(.2f);yield return KeyPress(Key.UpArrow);
            settings.BeginBinding(MarketAction.Inspect);yield return new WaitForSecondsRealtime(.2f);
            InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Back));yield return null;yield return new WaitForEndOfFrame();
            InputSystem.QueueStateEvent(mouse,new MouseState());yield return null;
            Check("mouse side button captured",settings.Draft.bindings[(int)MarketAction.Inspect].mouse==3);
            settings.Draft.sensitivity=1.6f;settings.Draft.invertY=true;settings.Draft.fieldOfView=82;
            settings.Draft.renderScale=.9f;settings.Draft.shadowQuality=1;settings.Draft.antialiasing=1;settings.Draft.frameLimit=90;settings.Draft.masterVolume=.7f;settings.Draft.ambienceVolume=.4f;
            Check("apply saves isolated profile",settings.ApplyDraft()&&File.Exists(settings.StoragePath));
            Check("runtime graphics and camera updated",Mathf.Abs(settings.RuntimePipeline.renderScale-.9f)<.001f&&settings.RuntimePipeline.additionalLightsShadowmapResolution==1024&&player.viewCamera.fieldOfView==82);
            Check("volume and frame cap applied",Mathf.Abs(AudioListener.volume-.7f)<.001f&&Application.targetFrameRate==90);
            yield return Capture("rebound-controls");
            settings.Draft.sensitivity=2.9f;yield return KeyPress(Key.Escape);yield return null;
            Check("closing discards unapplied draft and restores game",!MarketSettings.IsOpen&&Time.timeScale==1&&!AudioListener.pause&&Mathf.Abs(settings.Current.sensitivity-1.6f)<.001f);
            Cursor.lockState=CursorLockMode.Locked;
            float yaw=player.transform.eulerAngles.y,pitch=player.viewCamera.transform.localEulerAngles.x;
            yield return MouseMove(new Vector2(20,10));
            Check("sensitivity drives real yaw and inverted pitch",Mathf.Abs(Mathf.DeltaAngle(yaw,player.transform.eulerAngles.y)-3.84f)<.12f&&Mathf.Abs(Mathf.DeltaAngle(pitch,player.viewCamera.transform.localEulerAngles.x)-1.92f)<.12f);
            yield return MouseMove(new Vector2(-20,-10));
            // Use the already verified 1F circulation aisle, not the spawn's checkout fixtures.
            controller.enabled=false;player.transform.position=new Vector3(-15,.92f,-21);player.transform.rotation=Quaternion.Euler(0,90,0);controller.enabled=true;Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.1f);
            Vector3 origin=player.transform.position;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return new WaitForSecondsRealtime(.25f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            Check("old forward key no longer moves player",Vector2.Distance(new Vector2(origin.x,origin.z),new Vector2(player.transform.position.x,player.transform.position.z))<.02f);
            origin=player.transform.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.UpArrow));yield return new WaitForSecondsRealtime(.25f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            float moved=Vector3.Distance(origin,player.transform.position);
            Check("rebound forward key moves actual character controller: "+moved.ToString("F3")+" m",moved>.5f);
            var product=FindFirstObjectByType<MarketProduct>();
            if(product){
                var collider=product.GetComponent<Collider>();
                product.transform.position+=player.viewCamera.transform.position+player.viewCamera.transform.forward*1.4f-collider.bounds.center;Physics.SyncTransforms();
                yield return KeyPress(Key.G);
                bool held=product.transform.parent==player.viewCamera.transform;
                Check("rebound interaction picks up a real scene product",held);
                if(held){
                    InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Back));yield return null;yield return new WaitForEndOfFrame();
                    Check("rebound side button starts product inspection",player.viewCamera.fieldOfView==29);
                    yield return KeyPress(Key.Escape);Check("menu exits inspection zoom before changing FOV",player.viewCamera.fieldOfView==82);
                    settings.Draft.fieldOfView=86;settings.ApplyDraft();settings.Close();yield return null;yield return null;yield return null;
                    InputSystem.QueueStateEvent(mouse,new MouseState());yield return null;yield return new WaitForEndOfFrame();
                    Check("inspection release restores newly saved FOV",player.viewCamera.fieldOfView==86);
                    settings.Open();settings.Draft.fieldOfView=82;settings.ApplyDraft();settings.Close();yield return null;yield return null;
                    yield return KeyPress(Key.G);Check("rebound interaction drops held product",product.transform.parent!=player.viewCamera.transform&&collider.enabled);
                }
            }else Check("scene pickup product available",false);
            var doors=FindObjectsByType<MarketSlidingDoor>();
            if(doors.Length>0){
                var door=doors[0];Vector3 eye=door.transform.position+Vector3.up*1.2f-door.transform.forward*2;
                controller.enabled=false;player.transform.rotation=Quaternion.LookRotation(door.transform.forward);player.transform.position+=eye-player.viewCamera.transform.position;controller.enabled=true;Physics.SyncTransforms();
                bool before=door.isOpen;
                yield return KeyPress(Key.E);Check("old interaction key leaves actual warehouse door unchanged",door.isOpen==before);
                Check("actual warehouse door is in interaction ray",Physics.Raycast(player.viewCamera.transform.position,player.viewCamera.transform.forward,out var hit,3)&&hit.collider.GetComponentInParent<MarketSlidingDoor>()==door);
                yield return KeyPress(Key.G);Check("rebound key opens actual warehouse door",door.isOpen!=before);
            }else Check("warehouse sliding door exists",false);
            yield return KeyPress(Key.Escape);settings.SetTab(0);
            string saved=File.ReadAllText(settings.StoragePath);
            settings.Draft.width=1280;settings.Draft.height=720;settings.ApplyDraft();
            yield return new WaitForSecondsRealtime(.6f);
            Check("display preview changes actual backbuffer and keeps saved profile untouched",settings.DisplayPending&&Screen.width==1280&&Screen.height==720&&File.ReadAllText(settings.StoragePath)==saved);
            yield return Capture("display-preview");
            yield return new WaitForSecondsRealtime(15.2f);
            Check("unconfirmed display automatically rolls back while time is paused",!settings.DisplayPending&&Screen.width==1920&&Screen.height==1080&&Time.timeScale==0);
            settings.Draft.width=1280;settings.Draft.height=720;settings.ApplyDraft();yield return new WaitForSecondsRealtime(.4f);yield return KeyPress(Key.Escape);yield return new WaitForSecondsRealtime(.3f);
            Check("Escape reverts display without leaving menu",!settings.DisplayPending&&MarketSettings.IsOpen&&Screen.width==1920);
            settings.Draft.width=1280;settings.Draft.height=720;settings.ApplyDraft();yield return new WaitForSecondsRealtime(.4f);settings.ConfirmDisplay();
            Check("confirmed display persists",settings.Current.width==1280&&MarketSettingsStore.Load(settings.StoragePath,out _).width==1280);
            settings.Draft.width=1920;settings.Draft.height=1080;settings.ApplyDraft();yield return new WaitForSecondsRealtime(.4f);settings.ConfirmDisplay();
            Check("1920 by 1080 confirmed and restored",Screen.width==1920&&Screen.height==1080&&settings.Current.width==1920);
            foreach(int mode in new[]{1,2}){
                settings.Draft.displayMode=mode;settings.ApplyDraft();yield return new WaitForSecondsRealtime(1);
                var expected=mode==1?FullScreenMode.FullScreenWindow:FullScreenMode.ExclusiveFullScreen;
                Check("display mode preview "+expected+" -> "+Screen.fullScreenMode,settings.DisplayPending&&Screen.fullScreenMode==expected);
                settings.RevertDisplay();yield return new WaitForSecondsRealtime(.5f);
                Check("fullscreen preview restores windowed 1080p",Screen.fullScreenMode==FullScreenMode.Windowed&&Screen.width==1920&&Screen.height==1080);
            }
            var preferred=settings.Current.Copy();
            settings.Draft.shadowQuality=0;settings.Draft.textureQuality=1;settings.Draft.antialiasing=0;settings.Draft.vSync=true;settings.ApplyDraft();yield return null;
            Check("lower quality options and vSync are connected to actual renderer",settings.RuntimePipeline.shadowDistance==20&&settings.RuntimePipeline.additionalLightsShadowmapResolution==512&&QualitySettings.globalTextureMipmapLimit==1&&QualitySettings.vSyncCount==1&&player.viewCamera.GetUniversalAdditionalCameraData().antialiasing==AntialiasingMode.None);
            settings.Draft.shadowQuality=2;settings.Draft.antialiasing=2;settings.ApplyDraft();yield return null;
            Check("high shadow and SMAA options apply",settings.RuntimePipeline.additionalLightsShadowmapResolution==2048&&player.viewCamera.GetUniversalAdditionalCameraData().antialiasing==AntialiasingMode.SubpixelMorphologicalAntiAliasing);
            settings.Draft.shadowQuality=preferred.shadowQuality;settings.Draft.textureQuality=preferred.textureQuality;settings.Draft.antialiasing=preferred.antialiasing;settings.Draft.vSync=preferred.vSync;settings.ApplyDraft();
            string corrupt=Path.Combine(folder,"invalid-profile.json");File.WriteAllText(corrupt,"{invalid");var fallback=MarketSettingsStore.Load(corrupt,out string warning);
            Check("damaged settings safely load defaults with warning",fallback.sensitivity==1&&!string.IsNullOrEmpty(warning));
            var invalid=new MarketSettingsData{sensitivity=float.NaN,renderScale=9,fieldOfView=-10};invalid.bindings[1]=invalid.bindings[0];invalid.Validate();
            Check("invalid numeric values and duplicate file bindings repaired",invalid.sensitivity==1&&invalid.renderScale==1.2f&&invalid.fieldOfView==60&&invalid.bindings[1].key==Key.S);
            settings.RestoreDefaults();Check("reset remains a draft until applied",settings.Draft.sensitivity==1&&settings.Current.sensitivity==1.6f);
            settings.Close();yield return null;yield return null;
            Check("resumed simulation and disabled GUI dispatch after all menu operations",Time.timeScale==1&&!MarketSettings.InputBlocked&&!AudioListener.pause&&!settings.GetComponent<MarketSettingsView>().enabled);
            Finish();
        }
        void Finish(){done=true;bool pass=!checks.Exists(s=>s.StartsWith("FAIL"));File.WriteAllText(Path.Combine(folder,"result.txt"),(pass?"PASS":"FAIL")+": "+checks.Count+" standalone settings checks; Input System events, real camera/controller/door, isolated profile.");Application.Quit(pass?0:1);}
        void OnDestroy(){Application.logMessageReceived-=Error;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);}
    }
}
