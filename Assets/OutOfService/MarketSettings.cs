using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace OutOfService
{
    [DefaultExecutionOrder(-200)]
    public sealed partial class MarketSettings:MonoBehaviour
    {
        public static MarketSettings Instance {get;private set;}
        public static bool IsOpen=>Instance&&Instance.open;
        public static bool InputBlocked=>Instance&&(Instance.open||Time.frameCount<=Instance.ignoreGameplayUntil)||PauseMenu.IsOpen;
        public MarketSettingsData Current {get;private set;}
        public MarketSettingsData Draft {get;private set;}
        public string StoragePath {get;private set;}
        public bool DisplayPending=>pending!=null;
        public string ActiveDisplayLabel=>Screen.width+" × "+Screen.height+" · "+(Screen.fullScreenMode==FullScreenMode.Windowed?"창 모드":Screen.fullScreenMode==FullScreenMode.ExclusiveFullScreen?"전체 화면":"테두리 없는 전체 화면");
        public int ListeningAction=>listening;
        public string Status {get;private set;}="";
        public IReadOnlyList<Vector2Int> Resolutions=>resolutions;
        public UniversalRenderPipelineAsset RuntimePipeline=>runtimePipeline;
        readonly List<Vector2Int> resolutions=new();
        MarketSettingsData pending;
        UniversalRenderPipelineAsset runtimePipeline;
        RenderPipelineAsset originalQualityPipeline;
        float baseShadowDistance,previousTimeScale,confirmUntil,listenAfter;
        int baseMainAtlas,baseAdditionalAtlas,ignoreGameplayUntil=-1,listening=-1,tab;
        bool open,previousAudioPause,previousBackground,automatedReview,quitting;
        string loadWarning="";
        int originalVSync,originalFrameLimit,originalMipLimit;
        float originalVolume;
        MarketSettingsView menuView;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){Instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterMarketScene(){SceneManager.sceneLoaded-=BootForScene;SceneManager.sceneLoaded+=BootForScene;}
        static void BootForScene(Scene scene,LoadSceneMode mode){Boot();}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(MarketPlayer.Instance&&!Instance)new GameObject("Game settings and pause menu").AddComponent<MarketSettings>();}
        void Awake()
        {
            if(Instance&&Instance!=this){Destroy(gameObject);return;}
            Instance=this;DontDestroyOnLoad(gameObject);
            menuView=gameObject.AddComponent<MarketSettingsView>();menuView.settings=this;menuView.enabled=false;
            string[] args=Environment.GetCommandLineArgs();
            automatedReview=Array.Exists(args,a=>a.StartsWith("-")&&(a.EndsWith("Review")||a.EndsWith("Walk")||a.EndsWith("FirstVisit")||a.EndsWith("Study")));
            int profile=Array.IndexOf(args,"-settingsProfile");
            StoragePath=profile>=0&&profile+1<args.Length?Path.GetFullPath(args[profile+1]):Path.Combine(Application.persistentDataPath,"market-settings-v1.json");
            Current=automatedReview&&profile<0?new MarketSettingsData():MarketSettingsStore.Load(StoragePath,out loadWarning);
            // Existing walk reviews measure uncapped frames. Set that before startup
            // preparation, rather than changing the frame limiter mid-measurement.
            if(automatedReview&&profile<0)Current.frameLimit=0;
            Current.Validate();Draft=Current.Copy();
            foreach(var resolution in Screen.resolutions)AddResolution(resolution.width,resolution.height);
            AddResolution(Current.width,Current.height);AddResolution(Screen.width,Screen.height);AddResolution(1920,1080);
            resolutions.Sort((a,b)=>a.x==b.x?a.y.CompareTo(b.y):a.x.CompareTo(b.x));
            originalQualityPipeline=QualitySettings.renderPipeline;
            originalVSync=QualitySettings.vSyncCount;originalFrameLimit=Application.targetFrameRate;
            originalMipLimit=QualitySettings.globalTextureMipmapLimit;originalVolume=AudioListener.volume;
            var source=UniversalRenderPipeline.asset;
            if(source){
                baseShadowDistance=source.shadowDistance;baseMainAtlas=source.mainLightShadowmapResolution;baseAdditionalAtlas=source.additionalLightsShadowmapResolution;
                runtimePipeline=Instantiate(source);runtimePipeline.name="Runtime graphics settings";runtimePipeline.hideFlags=HideFlags.DontSave;
                QualitySettings.renderPipeline=runtimePipeline;
            }
            SceneManager.sceneLoaded+=SceneLoaded;
            ApplyRuntime(Current,!Application.isEditor&&(!automatedReview||profile>=0));
        }
        void AddResolution(int w,int h){var value=new Vector2Int(w,h);if(w>=960&&h>=540&&!resolutions.Contains(value))resolutions.Add(value);}
        void SceneLoaded(Scene scene,LoadSceneMode mode)
        {
            if(!MarketPlayer.Instance){Destroy(gameObject);return;}
            if(open)Close();ApplyRuntime(Current,false);
        }
        void OnApplicationQuit(){quitting=true;}
        void OnApplicationFocus(bool focus){if(!focus&&!automatedReview&&Current!=null&&Current.pauseOnFocusLoss&&MarketRenderPreparation.IsReady){if(PauseMenu.Instance)PauseMenu.Instance.Pause();else Open();}}
        void Update()
        {
            if(!MarketPlayer.Instance||!MarketRenderPreparation.IsReady)return;
            var keyboard=Keyboard.current;
            if(open&&listening>=0){
                if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame){CancelBinding();return;}
                if(Time.realtimeSinceStartup>=listenAfter){
                    if(keyboard!=null)foreach(var key in keyboard.allKeys)if(key.wasPressedThisFrame){TryBind(new MarketBinding(key.keyCode));return;}
                    for(int button=0;button<5;button++){var value=MarketBinding.Mouse(button);if(value.Control?.wasPressedThisFrame??false){TryBind(value);return;}}
                }
                return;
            }
            if(pending!=null&&Time.realtimeSinceStartup>=confirmUntil)RevertDisplay();
            if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame){if(DisplayPending)RevertDisplay();else if(open)Close();else if(!PauseMenu.Instance)Open();}
        }
        public void Open()
        {
            if(open||!MarketRenderPreparation.IsReady)return;
            MarketPlayer.Instance?.SuspendInspection();
            open=true;previousTimeScale=Time.timeScale;previousAudioPause=AudioListener.pause;
            previousBackground=Application.runInBackground;Application.runInBackground=true;
            menuView.enabled=true;
            Time.timeScale=0;AudioListener.pause=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            Draft=Current.Copy();listening=-1;Status=string.IsNullOrEmpty(loadWarning)?"변경 후 적용을 누르면 저장됩니다.":loadWarning;
        }
        public void Close()
        {
            if(!open)return;if(DisplayPending)RevertDisplay();
            open=false;listening=-1;Time.timeScale=previousTimeScale;AudioListener.pause=previousAudioPause;
            Application.runInBackground=previousBackground;
            menuView.enabled=false;
            bool terminal=MarketGame.Instance&&(MarketGame.Instance.failed||MarketGame.Instance.finished);
            Cursor.lockState=terminal?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=terminal;
            ignoreGameplayUntil=Time.frameCount+1;Draft=Current.Copy();
        }
        public void SetTab(int index){tab=Mathf.Clamp(index,0,2);CancelBinding();}
        public void BeginBinding(MarketAction action){if(!open||DisplayPending)return;listening=(int)action;listenAfter=Time.realtimeSinceStartup+.18f;Status="새 키 또는 마우스 버튼을 누르세요. Esc: 취소";}
        public void CancelBinding(){listening=-1;Status="변경 후 적용을 누르면 저장됩니다.";}
        public bool TryBind(MarketBinding value)
        {
            if(listening<0||!value.Valid){Status="Esc는 메뉴 열기·취소에 고정되어 있습니다.";return false;}
            for(int i=0;i<Draft.bindings.Length;i++)if(i!=listening&&value.Same(Draft.bindings[i])){Status="이미 '"+ActionNames[i]+"'에 사용 중입니다. 다른 키를 선택하세요.";return false;}
            Draft.bindings[listening]=value;listening=-1;Status="키가 변경되었습니다. 적용을 눌러 저장하세요.";return true;
        }
        public void RestoreDefaults(){Draft=new MarketSettingsData();CancelBinding();Status="기본값을 불러왔습니다. 적용 전까지 저장되지 않습니다.";}
        public bool ApplyDraft()
        {
            if(DisplayPending)return false;
            CancelBinding();Draft.Validate();var candidate=Draft.Copy();
            if(!Current.SameDisplay(candidate)&&!Application.isEditor){
                pending=candidate;ApplyRuntime(candidate,true);confirmUntil=Time.realtimeSinceStartup+15;Status="화면이 정상적으로 보이면 유지하세요.";return true;
            }
            return Commit(candidate);
        }
        bool Commit(MarketSettingsData candidate)
        {
            ApplyRuntime(candidate,false);
            if(!MarketSettingsStore.Save(StoragePath,candidate,out string error)){ApplyRuntime(Current,true);Status=error;return false;}
            Current=candidate.Copy();Draft=Current.Copy();loadWarning="";Status="설정을 저장했습니다.";return true;
        }
        public void ConfirmDisplay()
        {
            if(pending==null)return;
            var candidate=pending;pending=null;
            // Windows can reject an exclusive mode and Unity falls back to
            // borderless. Persist the accepted display, never the failed request.
            int mode=Screen.fullScreenMode==FullScreenMode.Windowed?0:Screen.fullScreenMode==FullScreenMode.ExclusiveFullScreen?2:1;
            bool fallback=candidate.displayMode!=mode||candidate.width!=Screen.width||candidate.height!=Screen.height;
            candidate.displayMode=mode;candidate.width=Screen.width;candidate.height=Screen.height;
            if(Commit(candidate)&&fallback)Status="요청한 화면 대신 실제 적용된 "+ActiveDisplayLabel+" 설정을 저장했습니다.";
        }
        public void RevertDisplay(){if(pending==null)return;pending=null;ApplyRuntime(Current,true);Draft=Current.Copy();Status="화면 설정을 이전 상태로 되돌렸습니다.";}
        void ApplyRuntime(MarketSettingsData data,bool screen)
        {
            if(runtimePipeline){
                runtimePipeline.renderScale=data.renderScale;
                int atlas=data.shadowQuality==0?512:data.shadowQuality==1?1024:baseAdditionalAtlas;
                runtimePipeline.additionalLightsShadowmapResolution=atlas;
                runtimePipeline.mainLightShadowmapResolution=data.shadowQuality==2?baseMainAtlas:atlas;
                runtimePipeline.shadowDistance=Mathf.Min(baseShadowDistance,data.shadowQuality==0?20:data.shadowQuality==1?35:baseShadowDistance);
            }
            QualitySettings.vSyncCount=data.vSync?1:0;Application.targetFrameRate=data.frameLimit==0?-1:data.frameLimit;
            QualitySettings.globalTextureMipmapLimit=data.textureQuality;
            AudioListener.volume=data.masterVolume;
            foreach(var ambience in FindObjectsByType<MarketAmbience>()){
                var source=ambience.GetComponent<AudioSource>();if(source)source.volume=.18f*data.ambienceVolume;
            }
            var player=MarketPlayer.Instance;
            if(player&&player.viewCamera){
                player.viewCamera.fieldOfView=data.fieldOfView;
                MarketRenderProfile.Select(player.viewCamera,runtimePipeline);
                var camera=player.viewCamera.GetUniversalAdditionalCameraData();
                camera.antialiasing=data.antialiasing==0?AntialiasingMode.None:data.antialiasing==1?AntialiasingMode.FastApproximateAntialiasing:AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camera.antialiasingQuality=AntialiasingQuality.High;
            }
            if(screen&&!Application.isEditor)Screen.SetResolution(data.width,data.height,data.displayMode==0?FullScreenMode.Windowed:data.displayMode==1?FullScreenMode.FullScreenWindow:FullScreenMode.ExclusiveFullScreen);
        }
        void RestartGame(){Close();Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
        void OnDestroy()
        {
            if(Instance!=this)return;SceneManager.sceneLoaded-=SceneLoaded;
            if(open&&!quitting){Time.timeScale=previousTimeScale;AudioListener.pause=previousAudioPause;Application.runInBackground=previousBackground;}
            if(QualitySettings.renderPipeline==runtimePipeline)QualitySettings.renderPipeline=originalQualityPipeline;
            if(!quitting){QualitySettings.vSyncCount=originalVSync;Application.targetFrameRate=originalFrameLimit;QualitySettings.globalTextureMipmapLimit=originalMipLimit;AudioListener.volume=originalVolume;}
            if(buttonNormal)Destroy(buttonNormal);if(buttonHover)Destroy(buttonHover);if(buttonActive)Destroy(buttonActive);
            if(runtimePipeline)Destroy(runtimePipeline);Instance=null;
        }
    }
}
