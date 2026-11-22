using System.Collections;
using System.Diagnostics;
using UnityEngine;

namespace OutOfService
{
    // Real player startup, not a benchmark-only warmup. First-use rendering work
    // happens behind a loading cover before input and simulation are released.
    public sealed class MarketRenderPreparation : MonoBehaviour
    {
        public static MarketRenderPreparation Instance { get; private set; }
        public static bool IsReady => !Instance || Instance.ready;
        public static double ElapsedMilliseconds => Instance ? Instance.elapsed : 0;
        public static int PreparedViews => Instance ? Instance.prepared : 0;
        public static int PreparedOutputFrames => Instance ? Instance.outputFrames : 0;
        public bool Completed { get; private set; }
        bool ready, captured, restored, playerEnabled, cameraEnabled, torchEnabled;
        float oldTimeScale, cover = 1;
        int prepared, outputFrames;
        double elapsed;
        MarketPlayer player;
        Camera view;
        MarketThreat[] threats;
        bool[] threatStates;
        MarketDetailVisibility[] groups;
        Vector3 cameraPosition;
        Quaternion cameraRotation;
        RenderTexture oldTarget, target;
        Texture2D pixel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetInstance() { Instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnableUnfocusedReview()
        {
            // The automated player is launched hidden. Configure it before any
            // coroutine can wait for startup preparation; otherwise focus loss
            // can pause loading before a review reaches its own background flag.
            if(Application.isEditor)return;
            var args=System.Environment.GetCommandLineArgs();
            foreach(string flag in new[]{"-vegetableBuildingReview","-merchReview","-freshPrepReview","-mallDetailReview","-floorReview","-pepperFirstVisit","-mallFirstVisit","-refinementReview"})
                if(System.Array.IndexOf(args,flag)>=0){Application.runInBackground=true;break;}
        }
        void Awake() { Instance = this; }

        IEnumerator Start()
        {
            var timer = Stopwatch.StartNew();
            try
            {
                player = MarketPlayer.Instance;
                if (!player || !player.viewCamera) yield break;
                view = player.viewCamera;
                playerEnabled = player.enabled;
                cameraEnabled = view.enabled;
                torchEnabled = player.flashlight && player.flashlight.enabled;
                cameraPosition = view.transform.localPosition;
                cameraRotation = view.transform.localRotation;
                oldTarget = view.targetTexture;
                oldTimeScale = Time.timeScale;
                threats = FindObjectsByType<MarketThreat>();
                threatStates = new bool[threats.Length];
                for (int i = 0; i < threats.Length; i++)
                {
                    threatStates[i] = threats[i].enabled;
                    threats[i].enabled = false;
                }
                captured = true;
                player.enabled = false;
                Time.timeScale = 0;
                // Let visibility components cache the enabled main camera first.
                yield return null;
                groups = FindObjectsByType<MarketDetailVisibility>();
                target = new RenderTexture(Mathf.Clamp(Screen.width, 640, 1920),
                    Mathf.Clamp(Screen.height, 360, 1080), 24);
                pixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
                view.enabled = false;
                view.targetTexture = target;
                for (int index = 0; index < 7; index++)
                {
                    view.transform.localPosition = cameraPosition;
                    view.transform.localRotation = cameraRotation;
                    if (player.flashlight) player.flashlight.enabled = index != 0;
                    if (index >= 2)
                    {
                        string shop = index < 5 ? "잎새 화원" : index == 5 ? "모퉁이 커피" : "바늘 세탁·수선";
                        var unit = GameObject.Find("B1 tenant - " + shop);
                        if (!unit) continue;
                        Vector3 eye, aim;
                        switch (index)
                        {
                            case 2: eye = new(0, 1.65f, -2); aim = new(5.1f, 1.25f, 4); break;
                            case 3: eye = new(-4.55f, 1.65f, 6.5f); aim = new(-6.15f, 1.2f, 7.1f); break;
                            case 4: eye = new(3.4f, 1.6f, 7); aim = new(5.1f, 1.1f, 7.5f); break;
                            case 5: eye = new(1.65f, 1.65f, 11.28f); aim = new(1.8f, 1.38f, 11.96f); break;
                            default: eye = new(4.7f, 1.55f, 7.9f); aim = new(5.45f, 1.47f, 8.6f); break;
                        }
                        view.transform.position = unit.transform.TransformPoint(eye);
                        view.transform.LookAt(unit.transform.TransformPoint(aim));
                    }
                    foreach (var group in groups) group.Refresh();
                    // LateUpdate refreshes slab visibility at the temporary eye.
                    yield return null;
                    for (int frame = 0; frame < 2; frame++)
                    {
                        var previous = RenderTexture.active;
                        try
                        {
                            view.Render();
                            RenderTexture.active = target;
                            pixel.ReadPixels(new Rect(target.width / 2, target.height / 2, 1, 1), 0, 0, false);
                        }
                        finally { RenderTexture.active = previous; }
                        yield return null;
                    }
                    prepared++;
                }
                RestoreCamera();
                foreach (var group in groups) group.Refresh();
                yield return null;
                // Prepare the real destination as well as temporary scene views.
                // Do this behind the opaque cover, before input or AI resumes.
                // A second frame lets deferred render-target/shadow work settle.
                view.enabled=false;
                for(int output=0;output<4;output++){
                    if(player.flashlight)player.flashlight.enabled=output<2;
                    var previous=RenderTexture.active;
                    try{
                        view.Render();
                        var syncTarget=oldTarget?oldTarget:target;
                        RenderTexture.active=syncTarget;
                        pixel.ReadPixels(new Rect(syncTarget.width/2,syncTarget.height/2,1,1),0,0,false);
                    }finally{RenderTexture.active=previous;}
                    outputFrames++;yield return null;
                }
                RestoreCamera();
                while (cover > 0)
                {
                    cover = Mathf.MoveTowards(cover, 0, Time.unscaledDeltaTime / .15f);
                    yield return null;
                }
                Completed = true;
            }
            finally
            {
                Restore();
                elapsed = timer.Elapsed.TotalMilliseconds;
                ready = true;
                UnityEngine.Debug.Log($"Market render preparation: {prepared}/7 views + {outputFrames} destination frames, {elapsed:F2} ms, completed={Completed}. Input and simulation restored.");
            }
        }

        void RestoreCamera()
        {
            if (!captured || !view) return;
            view.transform.localPosition = cameraPosition;
            view.transform.localRotation = cameraRotation;
            view.targetTexture = oldTarget;
            view.enabled = cameraEnabled;
            if (player && player.flashlight) player.flashlight.enabled = torchEnabled;
        }
        void Restore()
        {
            if (restored) return;
            restored = true;
            RestoreCamera();
            if (captured)
            {
                Time.timeScale = oldTimeScale;
                if (player) player.enabled = playerEnabled;
                for (int i = 0; i < threats.Length; i++) if (threats[i]) threats[i].enabled = threatStates[i];
                if (groups != null) foreach (var group in groups) if (group) group.Refresh();
            }
            if (target) { target.Release(); Destroy(target); }
            if (pixel) Destroy(pixel);
        }
        void OnDisable() { StopAllCoroutines(); Restore(); ready = true; }
        void OnDestroy() { Restore(); if (Instance == this) Instance = null; }
        void OnGUI()
        {
            if (ready) return;
            var previous = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = -10000;
            GUI.color = new Color(0, 0, 0, cover);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            float width = Mathf.Min(240, Screen.width * .25f);
            GUI.color = new Color(.16f, .16f, .16f, cover);
            GUI.DrawTexture(new Rect((Screen.width - width) * .5f, Screen.height * .85f, width, 3), Texture2D.whiteTexture);
            GUI.color = new Color(.7f, .7f, .7f, cover);
            GUI.DrawTexture(new Rect((Screen.width - width) * .5f, Screen.height * .85f, width * prepared / 7f, 3), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.depth = previousDepth;
        }
    }
}
