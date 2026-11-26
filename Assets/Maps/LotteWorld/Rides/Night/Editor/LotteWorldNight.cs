// LotteWorldNight.cs — 롯데월드 낮/밤 전환
// 메뉴: Tools > LotteWorld > Night > 낮·밤 전환 (클릭 UI)
//       Tools > LotteWorld > Night > Apply Night / Apply Day
// DaySnapshot.asset / NightSnapshot.asset 에 각각 저장. 전환 시 두 씬 저장.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LotteWorld.Night
{
    public static class LotteWorldNight
    {
        const string DaySnapPath = "Assets/Maps/LotteWorld/Rides/Night/DaySnapshot.asset";
        const string NightSnapPath = "Assets/Maps/LotteWorld/Rides/Night/NightSnapshot.asset";
        const string TodStatePath = "Assets/Maps/LotteWorld/Rides/Night/TodState.asset";
        const string NightDir = "Assets/Maps/LotteWorld/Rides/Night";
        const string ProfilePath = "Assets/Maps/LotteWorld/Rides/Night/LW_NightProfile.asset";
        const string SkyMatPath = "Assets/Maps/LotteWorld/Rides/Night/LW_NightSkybox.mat";
        const string CeilNightMatPath = "Assets/Maps/LotteWorld/Rides/Night/LW_Shell_CeilLightNight.mat";
        const string CeilOffMatPath = "Assets/Maps/LotteWorld/Rides/Night/LW_Shell_CeilLightOffNight.mat";
        const string CeilOnMatPath = "Assets/Maps/LotteWorld/Shell/Materials/LW_Shell_CeilLightOn.mat";
        const string VolumeName = "LW_NightVolume";
        const string Map3Path = "Assets/Maps/LotteWorld/Map3_LotteWorld.unity";
        const string MagicPath = "Assets/Maps/LotteWorld/Map3_MagicIsland.unity";
        const int Seed = 1120;

        // HEAD 커밋 기준 낮 Directional / 환경
        static readonly Color DayDirColor = new Color(1f, 0.95686275f, 0.8392157f);
        const float DayDirIntensity = 1.15f;
        static readonly Color DaySunColor = new Color(1f, 0.93f, 0.82f);
        const float DaySunIntensity = 1.25f;
        static readonly Color DayAmbSky = new Color(0.212f, 0.227f, 0.259f);
        static readonly Color DayAmbEq = new Color(0.114f, 0.125f, 0.133f);
        static readonly Color DayAmbGnd = new Color(0.047f, 0.043f, 0.035f);

        static readonly Color MoonColor = new Color(0.55f, 0.65f, 0.9f);
        static readonly Color WarmLamp = new Color(1f, 0.82f, 0.55f);
        static readonly Color AmbSky = new Color(0.02f, 0.022f, 0.035f);
        static readonly Color AmbEq = new Color(0.012f, 0.012f, 0.018f);
        static readonly Color AmbGnd = new Color(0.005f, 0.005f, 0.006f);
        static readonly Color FogCol = new Color(0.02f, 0.022f, 0.03f);
        static readonly Color EmitOn = new Color(4f, 3.2f, 1.8f);

        static readonly string[] BulbMatKeys =
        {
            "GlobeOn", "GlobeOff", "BulbOn", "BulbOff", "LampGlow", "LampOn",
            "LightW", "LightR"
        };

        // -------------------------------------------------------------- menus

        [MenuItem("Tools/LotteWorld/Night/낮·밤 전환", false, 0)]
        public static void OpenSwitcher()
        {
            LotteWorldDayNightWindow.Open();
        }

        [MenuItem("Tools/LotteWorld/Night/Apply Night", false, 20)]
        public static void ApplyNightMenu()
        {
            SwitchToNight(saveScenes: true);
        }

        [MenuItem("Tools/LotteWorld/Night/Apply Day", false, 21)]
        public static void ApplyDayMenu()
        {
            SwitchToDay(saveScenes: true);
        }

        [MenuItem("Tools/LotteWorld/Night/Restore Day", false, 22)]
        public static void RestoreDayMenu()
        {
            SwitchToDay(saveScenes: true);
        }

        [MenuItem("Tools/LotteWorld/Night/Bootstrap 낮·밤 스냅샷", false, 40)]
        public static void BootstrapMenu()
        {
            BootstrapSnapshots(saveScenes: true, leaveAsNight: true);
        }

        // -------------------------------------------------------------- public API

        public static TodState GetTodState() => LoadOrCreateTod();

        public static bool SwitchToNight(bool saveScenes)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Tod] Play 모드에서는 낮/밤 전환을 할 수 없습니다. Play를 끈 뒤 다시 눌러 주세요.");
                return false;
            }
            EnsureFolder(NightDir);
            EnsureBootstrapped(saveScenes);
            var night = LoadSnap(NightSnapPath);
            if (night == null || night.directionals.Count == 0 || !LooksLikeNightSnap(night))
            {
                Debug.Log("[Tod] NightSnapshot 없거나 낮 값으로 오염됨 — procedural 밤 재생성");
                ProceduralApplyNight();
                night = LoadOrCreateSnap(NightSnapPath);
                CaptureLiveInto(night);
            }
            else
            {
                ApplyFromSnap(night, isNight: true);
            }

            var tod = LoadOrCreateTod();
            tod.isNight = true;
            tod.bootstrapped = true;
            EditorUtility.SetDirty(tod);
            // 하위호환 DaySnapshot.nightApplied
            var day = LoadSnap(DaySnapPath);
            if (day != null) { day.nightApplied = true; EditorUtility.SetDirty(day); }
            AssetDatabase.SaveAssets();

            if (saveScenes) SaveOpenMapScenes();
            Debug.Log("[Tod] 밤 적용" + (saveScenes ? " + 씬 저장" : ""));
            return true;
        }

        public static bool SwitchToDay(bool saveScenes)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Tod] Play 모드에서는 낮/밤 전환을 할 수 없습니다. Play를 끈 뒤 다시 눌러 주세요.");
                return false;
            }
            EnsureFolder(NightDir);
            EnsureBootstrapped(saveScenes);
            var day = LoadSnap(DaySnapPath);
            if (day == null || day.directionals.Count == 0)
            {
                Debug.Log("[Tod] DaySnapshot 비어 있음 — 프리팹/HEAD 기준으로 낮 생성");
                day = BuildDaySnapshot();
            }
            ApplyFromSnap(day, isNight: false);

            var tod = LoadOrCreateTod();
            tod.isNight = false;
            tod.bootstrapped = true;
            EditorUtility.SetDirty(tod);
            if (day != null) { day.nightApplied = false; EditorUtility.SetDirty(day); }
            AssetDatabase.SaveAssets();

            if (saveScenes) SaveOpenMapScenes();
            Debug.Log("[Tod] 낮 적용" + (saveScenes ? " + 씬 저장" : ""));
            return true;
        }

        public static void BootstrapSnapshots(bool saveScenes, bool leaveAsNight)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Tod] Play 모드에서는 Bootstrap 할 수 없습니다. Play를 끈 뒤 다시 실행하세요.");
                return;
            }
            EnsureFolder(NightDir);

            // 1) 낮 스냅 먼저 확정 (프리팹/HEAD 기준)
            var day = BuildDaySnapshot();
            ApplyFromSnap(day, isNight: false);
            CaptureLiveInto(day);
            EditorUtility.SetDirty(day);
            AssetDatabase.SaveAssets();
            Debug.Log("[Tod] DaySnapshot 생성 — dir=" + day.directionals.Count
                + " lights=" + day.lights.Count + " ceil=" + day.ceilLightRenderers.Count);

            // 2) procedural 밤 적용 후 캡처 (현재 씬이 낮이어도 오염되지 않음)
            ProceduralApplyNight();
            var night = LoadOrCreateSnap(NightSnapPath);
            CaptureLiveInto(night);
            Debug.Log("[Tod] NightSnapshot 캡처 — dir=" + night.directionals.Count
                + " lights=" + night.lights.Count + " ceil=" + night.ceilLightRenderers.Count
                + " fog=" + night.fog + " amb=" + night.ambientMode
                + " sky=" + (night.skybox ? night.skybox.name : "null"));

            var tod = LoadOrCreateTod();
            tod.bootstrapped = true;

            if (leaveAsNight)
            {
                // 이미 procedural 밤 상태. 스냅 재적용으로 일치시킴
                ApplyFromSnap(night, isNight: true);
                tod.isNight = true;
                day.nightApplied = true;
            }
            else
            {
                ApplyFromSnap(day, isNight: false);
                tod.isNight = false;
                day.nightApplied = false;
            }
            EditorUtility.SetDirty(tod);
            EditorUtility.SetDirty(day);
            AssetDatabase.SaveAssets();
            if (saveScenes) SaveOpenMapScenes();
            Debug.Log("[Tod] Bootstrap 완료 — 현재=" + (tod.isNight ? "밤" : "낮"));
        }

        /// <summary>NightSnapshot 이 실제 밤인지 (낮 값으로 덮인 경우 false).</summary>
        static bool LooksLikeNightSnap(DaySnapshot snap)
        {
            if (snap == null) return false;
            if (snap.ambientMode == AmbientMode.Trilight) return true;
            if (snap.fog && snap.fogDensity >= 0.02f) return true;
            if (snap.skybox != null && snap.skybox.name.IndexOf("Night", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            foreach (var d in snap.directionals)
            {
                if (d.path != null && d.path.EndsWith("Directional Light") && d.enabled && d.intensity <= 0.2f)
                    return true;
                if (d.path != null && d.path.Contains("Sun") && !d.enabled)
                    return true;
            }
            return false;
        }

        static void EnsureBootstrapped(bool saveScenes)
        {
            var night = LoadSnap(NightSnapPath);
            var day = LoadSnap(DaySnapPath);
            bool nightOk = night != null && night.directionals.Count > 0 && LooksLikeNightSnap(night);
            bool dayOk = day != null && day.directionals.Count > 0;
            if (nightOk && dayOk)
            {
                var tod = LoadOrCreateTod();
                if (!tod.bootstrapped)
                {
                    tod.bootstrapped = true;
                    EditorUtility.SetDirty(tod);
                    AssetDatabase.SaveAssets();
                }
                return;
            }
            BootstrapSnapshots(saveScenes, leaveAsNight: true);
        }

        // -------------------------------------------------------------- capture / apply

        static void CaptureLiveInto(DaySnapshot snap)
        {
            snap.scenePath = SceneManager.GetActiveScene().path;
            snap.directionals.Clear();
            snap.lights.Clear();
            snap.emissionRendererPaths.Clear();
            snap.ceilLightRenderers.Clear();

            CaptureRenderSettings(snap);

            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (l.type != LightType.Directional) continue;
                snap.directionals.Add(MakeDirSnap(l));
            }

            Type flickerType = FindFlickerType();
            foreach (var l in CollectNightLights())
            {
                bool hasFlick = flickerType != null && l.GetComponent(flickerType) != null;
                snap.lights.Add(new DaySnapshot.LightSnap
                {
                    path = PathOf(l.transform),
                    enabled = l.enabled,
                    goActive = l.gameObject.activeSelf,
                    color = l.color,
                    intensity = l.intensity,
                    range = l.range,
                    shadows = l.shadows,
                    hadFlicker = hasFlick,
                    weAddedFlicker = false
                });
            }

            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (r.sharedMaterials == null) continue;
                bool hit = false;
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.name.IndexOf("CeilLight", StringComparison.OrdinalIgnoreCase) >= 0)
                    { hit = true; break; }
                if (!hit) continue;
                snap.ceilLightRenderers.Add(new DaySnapshot.RendererMatSnap
                {
                    path = PathOf(r.transform),
                    materials = r.sharedMaterials
                });
            }

            bool hasVol = false;
            foreach (var v in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include))
                if (v.name == VolumeName) { hasVol = true; break; }
            snap.hasNightVolume = hasVol;
            snap.createdNightVolume = hasVol;

            var cam = Camera.main ?? UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include)
                .FirstOrDefault(c => c.CompareTag("MainCamera"));
            if (cam != null)
            {
                snap.mainCamPath = PathOf(cam.transform);
                var uad = cam.GetComponent<UniversalAdditionalCameraData>();
                snap.mainCamHadPost = uad != null && uad.renderPostProcessing;
            }

            EditorUtility.SetDirty(snap);
            AssetDatabase.SaveAssets();
        }

        static void ApplyFromSnap(DaySnapshot snap, bool isNight)
        {
            var dirtyScenes = new HashSet<Scene>();
            var map3 = FindOpenScene(Map3Path);
            var magic = FindOpenScene(MagicPath);
            if (!magic.IsValid() || !magic.isLoaded)
                Debug.LogWarning("[Tod] Map3_MagicIsland 씬이 열려 있지 않습니다.");
            if (map3.IsValid() && map3.isLoaded)
                SceneManager.SetActiveScene(map3);

            // RenderSettings (Map3 active)
            RenderSettings.skybox = snap.skybox;
            RenderSettings.ambientMode = snap.ambientMode;
            RenderSettings.ambientIntensity = snap.ambientIntensity;
            RenderSettings.ambientSkyColor = snap.ambientSky;
            RenderSettings.ambientEquatorColor = snap.ambientEquator;
            RenderSettings.ambientGroundColor = snap.ambientGround;
            RenderSettings.ambientLight = snap.ambientLight;
            RenderSettings.defaultReflectionMode = snap.defaultReflectionMode;
            RenderSettings.reflectionIntensity = snap.reflectionIntensity;
            RenderSettings.fog = snap.fog;
            RenderSettings.fogMode = snap.fogMode;
            RenderSettings.fogColor = snap.fogColor;
            RenderSettings.fogDensity = snap.fogDensity;
            RenderSettings.fogStartDistance = snap.fogStart;
            RenderSettings.fogEndDistance = snap.fogEnd;
            if (!string.IsNullOrEmpty(snap.sunPath))
            {
                var st = FindByPath(snap.sunPath);
                RenderSettings.sun = st != null ? st.GetComponent<Light>() : null;
            }
            else RenderSettings.sun = null;
            if (map3.IsValid()) dirtyScenes.Add(map3);

            int miss = 0;
            foreach (var d in snap.directionals)
            {
                var t = FindByPath(d.path);
                if (t == null) { miss++; continue; }
                var l = t.GetComponent<Light>();
                if (l == null) { miss++; continue; }
                Undo.RecordObject(l, "Tod Directional");
                Undo.RecordObject(l.gameObject, "Tod Directional GO");
                l.enabled = d.enabled;
                if (l.gameObject.activeSelf != d.goActive) l.gameObject.SetActive(d.goActive);
                l.color = d.color;
                l.intensity = d.intensity;
                l.shadows = d.shadows;
                // URP: Light.shadowResolution 는 Built-In 전용 — 설정하지 않음
                l.shadowStrength = d.shadowStrength;
                EditorUtility.SetDirty(l);
                dirtyScenes.Add(l.gameObject.scene);
            }

            Type flickerType = FindFlickerType();
            foreach (var s in snap.lights)
            {
                var t = FindByPath(s.path);
                if (t == null) { miss++; continue; }
                var l = t.GetComponent<Light>();
                if (l == null) { miss++; continue; }
                Undo.RecordObject(l, "Tod Light");
                Undo.RecordObject(l.gameObject, "Tod Light GO");
                if (l.gameObject.activeSelf != s.goActive) l.gameObject.SetActive(s.goActive);
                l.enabled = s.enabled;
                l.color = s.color;
                l.intensity = s.intensity;
                l.range = s.range;
                l.shadows = s.shadows;

                if (flickerType != null)
                {
                    var existing = l.GetComponent(flickerType);
                    if (s.weAddedFlicker || s.hadFlicker)
                    {
                        if (existing == null && isNight && s.weAddedFlicker)
                            Undo.AddComponent(l.gameObject, flickerType);
                    }
                    else if (existing != null && !isNight)
                    {
                        // 낮: 프리팹에 없던 깜빡이만 제거는 BuildDay에서 hadFlicker로 반영.
                        // 캡처 기반이면 hadFlicker=false 인 경우 제거
                        Undo.DestroyObjectImmediate(existing);
                    }
                    if (!isNight && existing != null && !s.hadFlicker)
                    {
                        var again = l.GetComponent(flickerType);
                        if (again != null) Undo.DestroyObjectImmediate(again);
                    }
                }
                EditorUtility.SetDirty(l);
                dirtyScenes.Add(l.gameObject.scene);
            }

            // Emission MPB
            if (!isNight)
            {
                foreach (var p in snap.emissionRendererPaths)
                {
                    var t = FindByPath(p);
                    if (t == null) continue;
                    var r = t.GetComponent<Renderer>();
                    if (r == null) continue;
                    Undo.RecordObject(r, "Tod Clear Emission");
                    r.SetPropertyBlock(null);
                    dirtyScenes.Add(r.gameObject.scene);
                }
                // 밤에 심어 둔 emission 경로도 낮에서 비움
                var night = LoadSnap(NightSnapPath);
                if (night != null)
                {
                    foreach (var p in night.emissionRendererPaths)
                    {
                        var t = FindByPath(p);
                        if (t == null) continue;
                        var r = t.GetComponent<Renderer>();
                        if (r == null) continue;
                        r.SetPropertyBlock(null);
                        dirtyScenes.Add(r.gameObject.scene);
                    }
                }
            }
            else
            {
                foreach (var p in snap.emissionRendererPaths)
                {
                    var t = FindByPath(p);
                    if (t == null) continue;
                    var r = t.GetComponent<Renderer>();
                    if (r == null) continue;
                    // 밤 캡처는 MPB를 저장하지 않으므로, 점등 여부는 라이트 스냅으로 재적용
                }
                // 밤 스냅 라이트 기준으로 bulb emission 재설정
                foreach (var s in snap.lights)
                {
                    var t = FindByPath(s.path);
                    if (t == null) continue;
                    var l = t.GetComponent<Light>();
                    if (l == null) continue;
                    SetBulbEmission(l, s.enabled, snap);
                    dirtyScenes.Add(l.gameObject.scene);
                }
            }

            foreach (var cm in snap.ceilLightRenderers)
            {
                var t = FindByPath(cm.path);
                if (t == null) { miss++; continue; }
                var r = t.GetComponent<Renderer>();
                if (r == null) { miss++; continue; }
                Undo.RecordObject(r, "Tod Ceil mats");
                r.sharedMaterials = cm.materials;
                EditorUtility.SetDirty(r);
                dirtyScenes.Add(r.gameObject.scene);
            }

            // Volume
            Volume vol = null;
            foreach (var v in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include))
                if (v.name == VolumeName) { vol = v; break; }

            if (isNight)
            {
                if (vol == null)
                    EnsureNightVolume(snap, dirtyScenes);
                else
                {
                    var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
                    if (profile != null)
                    {
                        ConfigureProfile(profile);
                        vol.sharedProfile = profile;
                        EditorUtility.SetDirty(vol);
                    }
                    dirtyScenes.Add(vol.gameObject.scene);
                }
            }
            else if (vol != null)
            {
                dirtyScenes.Add(vol.gameObject.scene);
                Undo.DestroyObjectImmediate(vol.gameObject);
            }

            // 낮 전용 후처리·실내 반사(LW_DayLook)는 밤에 끈다
            LotteWorld.Look.LotteWorldDayLook.SetForTod(isNight, dirtyScenes);

            if (!string.IsNullOrEmpty(snap.mainCamPath))
            {
                var ct = FindByPath(snap.mainCamPath);
                var cam = ct != null ? ct.GetComponent<Camera>() : Camera.main;
                if (cam != null)
                {
                    var uad = cam.GetComponent<UniversalAdditionalCameraData>();
                    if (uad == null) uad = Undo.AddComponent<UniversalAdditionalCameraData>(cam.gameObject);
                    Undo.RecordObject(uad, "Tod Camera Post");
                    uad.renderPostProcessing = isNight || snap.mainCamHadPost;
                    EditorUtility.SetDirty(uad);
                    dirtyScenes.Add(cam.gameObject.scene);
                }
            }

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                foreach (var sc in dirtyScenes)
                    if (sc.IsValid() && sc.isLoaded)
                        EditorSceneManager.MarkSceneDirty(sc);
            }

            DynamicGI.UpdateEnvironment();
            if (miss > 0) Debug.LogWarning("[Tod] Apply 중 못 찾은 항목 " + miss);
        }

        static DaySnapshot BuildDaySnapshot()
        {
            var snap = LoadOrCreateSnap(DaySnapPath);
            snap.directionals.Clear();
            snap.lights.Clear();
            snap.emissionRendererPaths.Clear();
            snap.ceilLightRenderers.Clear();
            snap.hasNightVolume = false;
            snap.createdNightVolume = false;

            // 환경 (HEAD 낮)
            snap.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            snap.ambientMode = AmbientMode.Skybox;
            snap.ambientIntensity = 1f;
            snap.ambientSky = DayAmbSky;
            snap.ambientEquator = DayAmbEq;
            snap.ambientGround = DayAmbGnd;
            snap.ambientLight = DayAmbSky;
            snap.defaultReflectionMode = DefaultReflectionMode.Skybox;
            snap.reflectionIntensity = 1f;
            snap.fog = false;
            snap.fogMode = FogMode.ExponentialSquared;
            snap.fogColor = new Color(0.5f, 0.5f, 0.5f);
            snap.fogDensity = 0.01f;
            snap.fogStart = 0f;
            snap.fogEnd = 300f;
            snap.sunPath = "";

            Light rootDir = FindRootDirectional();
            Light sun = FindMagicSun();
            if (rootDir != null)
            {
                snap.directionals.Add(new DaySnapshot.DirSnap
                {
                    path = PathOf(rootDir.transform),
                    enabled = true,
                    goActive = true,
                    color = DayDirColor,
                    intensity = DayDirIntensity,
                    shadows = LightShadows.Soft,
                    shadowRes = rootDir.shadowResolution,
                    shadowStrength = rootDir.shadowStrength
                });
                snap.sunPath = PathOf(rootDir.transform);
            }
            if (sun != null)
            {
                snap.directionals.Add(new DaySnapshot.DirSnap
                {
                    path = PathOf(sun.transform),
                    enabled = true,
                    goActive = true,
                    color = DaySunColor,
                    intensity = DaySunIntensity,
                    shadows = LightShadows.Soft,
                    shadowRes = sun.shadowResolution,
                    shadowStrength = sun.shadowStrength
                });
            }

            Type flickerType = FindFlickerType();
            foreach (var l in CollectNightLights())
            {
                var src = PrefabUtility.GetCorrespondingObjectFromSource(l) as Light;
                Light use = src != null ? src : l;
                bool prefabFlick = flickerType != null && src != null
                    && PrefabUtility.GetCorrespondingObjectFromSource(l.gameObject) != null
                    && (src.GetComponent(flickerType) != null);
                // 프리팹 소스 GO에서 flicker 확인
                if (flickerType != null && src != null)
                {
                    var srcGo = PrefabUtility.GetCorrespondingObjectFromSource(l.gameObject) as GameObject;
                    if (srcGo != null)
                        prefabFlick = srcGo.GetComponent(flickerType) != null
                            || srcGo.GetComponentInChildren(flickerType, true) != null
                              && srcGo.GetComponentInChildren(flickerType, true).gameObject == src.gameObject;
                    prefabFlick = src.gameObject.GetComponent(flickerType) != null;
                }

                snap.lights.Add(new DaySnapshot.LightSnap
                {
                    path = PathOf(l.transform),
                    enabled = use.enabled,
                    goActive = use.gameObject.activeSelf,
                    color = use.color,
                    intensity = use.intensity,
                    range = use.range,
                    shadows = use.shadows,
                    hadFlicker = prefabFlick,
                    weAddedFlicker = false
                });
            }

            var ceilOn = AssetDatabase.LoadAssetAtPath<Material>(CeilOnMatPath);
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (r.sharedMaterials == null) continue;
                bool hit = false;
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.name.IndexOf("CeilLight", StringComparison.OrdinalIgnoreCase) >= 0)
                    { hit = true; break; }
                if (!hit) continue;
                var mats = r.sharedMaterials;
                if (ceilOn != null)
                {
                    for (int i = 0; i < mats.Length; i++)
                        if (mats[i] != null && mats[i].name.IndexOf("CeilLight", StringComparison.OrdinalIgnoreCase) >= 0)
                            mats[i] = ceilOn;
                }
                snap.ceilLightRenderers.Add(new DaySnapshot.RendererMatSnap
                {
                    path = PathOf(r.transform),
                    materials = mats
                });
            }

            var cam = Camera.main ?? UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include)
                .FirstOrDefault(c => c.CompareTag("MainCamera"));
            if (cam != null)
            {
                snap.mainCamPath = PathOf(cam.transform);
                var uad = cam.GetComponent<UniversalAdditionalCameraData>();
                snap.mainCamHadPost = uad != null && uad.renderPostProcessing;
            }

            snap.nightApplied = false;
            EditorUtility.SetDirty(snap);
            AssetDatabase.SaveAssets();
            return snap;
        }

        // -------------------------------------------------------------- procedural night (첫 생성용)

        public static void ProceduralApplyNight()
        {
            EnsureFolder(NightDir);
            var dirtyScenes = new HashSet<Scene>();
            var map3 = FindOpenScene(Map3Path);
            var magic = FindOpenScene(MagicPath);
            if (!magic.IsValid() || !magic.isLoaded)
                Debug.LogWarning("[Night] Map3_MagicIsland 씬이 열려 있지 않습니다.");
            if (map3.IsValid() && map3.isLoaded)
                SceneManager.SetActiveScene(map3);
            else
                Debug.LogWarning("[Night] Map3_LotteWorld 가 열려 있지 않습니다.");

            var scratch = ScriptableObject.CreateInstance<DaySnapshot>();

            Light rootDir = FindRootDirectional();
            Light sun = FindMagicSun();

            if (rootDir != null)
            {
                Undo.RecordObject(rootDir, "Night Directional");
                rootDir.enabled = true;
                if (!rootDir.gameObject.activeSelf) rootDir.gameObject.SetActive(true);
                rootDir.intensity = 0.045f;
                rootDir.color = MoonColor;
                rootDir.shadows = LightShadows.Soft;
                EditorUtility.SetDirty(rootDir);
                dirtyScenes.Add(rootDir.gameObject.scene);
            }
            if (sun != null)
            {
                Undo.RecordObject(sun, "Night Disable Sun");
                sun.enabled = false;
                EditorUtility.SetDirty(sun);
                dirtyScenes.Add(sun.gameObject.scene);
            }

            var sky = EnsureNightSkybox();
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbSky;
            RenderSettings.ambientEquatorColor = AmbEq;
            RenderSettings.ambientGroundColor = AmbGnd;
            RenderSettings.reflectionIntensity = 0.08f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = FogCol;
            RenderSettings.fogDensity = 0.05f;
            if (rootDir != null) RenderSettings.sun = rootDir;
            if (map3.IsValid()) dirtyScenes.Add(map3);

            var targets = CollectNightLights();
            var rng = new System.Random(Seed);
            Type flickerType = FindFlickerType();
            var order = Enumerable.Range(0, targets.Count).OrderBy(_ => rng.Next()).ToList();
            int offCount = Mathf.RoundToInt(targets.Count * 0.40f);
            var offSet = new HashSet<int>(order.Take(offCount));
            var onOrder = order.Where(i => !offSet.Contains(i)).OrderBy(_ => rng.Next()).ToList();
            int flickCount = Mathf.RoundToInt(targets.Count * 0.10f);
            var flickSet = new HashSet<int>(onOrder.Take(flickCount));

            for (int i = 0; i < targets.Count; i++)
            {
                var l = targets[i];
                Undo.RecordObject(l, "Night Light");
                if (!l.gameObject.activeSelf) l.gameObject.SetActive(true);
                l.color = WarmLamp;
                l.range = 4.5f + (float)rng.NextDouble() * 2f;
                l.shadows = LightShadows.None;
                if (offSet.Contains(i))
                {
                    l.enabled = false;
                    SetBulbEmission(l, false, scratch);
                }
                else
                {
                    l.enabled = true;
                    // 낮 프리팹 intensity(2.5 등)를 그대로 두면 밤이 밝아짐 — 클램프
                    l.intensity = 0.55f + (float)rng.NextDouble() * 0.45f;
                    SetBulbEmission(l, true, scratch);
                    if (flickSet.Contains(i) && flickerType != null && l.GetComponent(flickerType) == null)
                        Undo.AddComponent(l.gameObject, flickerType);
                }
                EditorUtility.SetDirty(l);
                dirtyScenes.Add(l.gameObject.scene);
            }

            int ceilNight = 0, ceilOff = 0, ceilKeep = 0;
            ApplyCeilLightPanels(scratch, dirtyScenes, ref ceilNight, ref ceilOff, ref ceilKeep);
            EnsureNightVolume(scratch, dirtyScenes);

            var cam = Camera.main ?? UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include)
                .FirstOrDefault(c => c.CompareTag("MainCamera"));
            if (cam != null)
            {
                var uad = cam.GetComponent<UniversalAdditionalCameraData>();
                if (uad == null) uad = Undo.AddComponent<UniversalAdditionalCameraData>(cam.gameObject);
                Undo.RecordObject(uad, "Night Camera Post");
                uad.renderPostProcessing = true;
                EditorUtility.SetDirty(uad);
                dirtyScenes.Add(cam.gameObject.scene);
            }
            EnsureForwardPlus(scratch);

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                foreach (var sc in dirtyScenes)
                    if (sc.IsValid() && sc.isLoaded)
                        EditorSceneManager.MarkSceneDirty(sc);
            }
            DynamicGI.UpdateEnvironment();
            UnityEngine.Object.DestroyImmediate(scratch);
        }

        // -------------------------------------------------------------- ceiling / volume helpers

        static void ApplyCeilLightPanels(DaySnapshot snap, HashSet<Scene> dirtyScenes,
            ref int nightN, ref int offN, ref int keepN)
        {
            var panels = new List<Renderer>();
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (r.sharedMaterials == null) continue;
                bool hit = false;
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.name == "LW_Shell_CeilLightOn") { hit = true; break; }
                if (hit) panels.Add(r);
            }
            if (panels.Count == 0)
            {
                Debug.LogWarning("[Night] LW_Shell_CeilLightOn 렌더러를 못 찾음");
                return;
            }

            var srcMat = panels[0].sharedMaterials.First(m => m != null && m.name == "LW_Shell_CeilLightOn");
            var nightMat = EnsureCeilNightMat(srcMat);
            var offMat = EnsureCeilOffMat(srcMat);
            var rng = new System.Random(Seed + 7);

            foreach (var r in panels)
            {
                snap.ceilLightRenderers.Add(new DaySnapshot.RendererMatSnap
                {
                    path = PathOf(r.transform),
                    materials = r.sharedMaterials
                });
                double roll = rng.NextDouble();
                Material replace = null;
                if (roll < 0.70) { replace = nightMat; nightN++; }
                else if (roll < 0.90) { replace = offMat; offN++; }
                else { keepN++; continue; }

                Undo.RecordObject(r, "Night Ceil mats");
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name == "LW_Shell_CeilLightOn")
                        mats[i] = replace;
                r.sharedMaterials = mats;
                EditorUtility.SetDirty(r);
                dirtyScenes.Add(r.gameObject.scene);
            }
        }

        static Material EnsureCeilNightMat(Material src)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(CeilNightMatPath);
            if (m == null)
            {
                m = new Material(src);
                m.name = "LW_Shell_CeilLightNight";
                AssetDatabase.CreateAsset(m, CeilNightMatPath);
            }
            else m.CopyPropertiesFromMaterial(src);
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor"))
            {
                Color e = src.HasProperty("_EmissionColor") ? src.GetColor("_EmissionColor") : Color.white;
                m.SetColor("_EmissionColor", e * 0.15f);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material EnsureCeilOffMat(Material src)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(CeilOffMatPath);
            if (m == null)
            {
                m = new Material(src);
                m.name = "LW_Shell_CeilLightOffNight";
                AssetDatabase.CreateAsset(m, CeilOffMatPath);
            }
            else m.CopyPropertiesFromMaterial(src);
            m.DisableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void SetBulbEmission(Light light, bool on, DaySnapshot snap)
        {
            Transform root = light.transform.parent != null ? light.transform.parent : light.transform;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                bool hit = false;
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    string mn = m.name;
                    for (int i = 0; i < BulbMatKeys.Length; i++)
                        if (mn.IndexOf(BulbMatKeys[i], StringComparison.OrdinalIgnoreCase) >= 0) { hit = true; break; }
                    if (hit) break;
                }
                if (!hit) continue;
                string path = PathOf(r.transform);
                if (snap != null && !snap.emissionRendererPaths.Contains(path))
                    snap.emissionRendererPaths.Add(path);
                Undo.RecordObject(r, "Night Bulb Emission");
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", on ? EmitOn : Color.black);
                if (on) block.SetColor("_BaseColor", WarmLamp);
                r.SetPropertyBlock(block);
            }
        }

        static bool EnsureNightVolume(DaySnapshot snap, HashSet<Scene> dirtyScenes)
        {
            Volume vol = null;
            foreach (var v in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include))
                if (v.name == VolumeName) { vol = v; break; }

            bool created = false;
            if (vol == null)
            {
                var go = new GameObject(VolumeName);
                var map3 = FindOpenScene(Map3Path);
                if (map3.IsValid() && map3.isLoaded)
                    SceneManager.MoveGameObjectToScene(go, map3);
                Undo.RegisterCreatedObjectUndo(go, "Create Night Volume");
                vol = Undo.AddComponent<Volume>(go);
                vol.isGlobal = true;
                vol.priority = 100f;
                created = true;
                dirtyScenes.Add(go.scene);
            }
            else
            {
                Undo.RecordObject(vol, "Night Volume");
                dirtyScenes.Add(vol.gameObject.scene);
            }

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            Undo.RecordObject(profile, "Night Profile");
            ConfigureProfile(profile);
            vol.sharedProfile = profile;
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(vol);
            if (snap != null)
            {
                snap.createdNightVolume = created;
                snap.hasNightVolume = true;
            }
            return created;
        }

        static void ConfigureProfile(VolumeProfile profile)
        {
            var tone = GetOrAdd<Tonemapping>(profile);
            tone.mode.overrideState = true; tone.mode.value = TonemappingMode.ACES;

            var color = GetOrAdd<ColorAdjustments>(profile);
            color.postExposure.overrideState = true; color.postExposure.value = -0.75f;
            color.saturation.overrideState = true; color.saturation.value = -30f;

            var bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.overrideState = true; bloom.threshold.value = 1f;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.6f;

            var vig = GetOrAdd<Vignette>(profile);
            vig.intensity.overrideState = true; vig.intensity.value = 0.35f;

            var grain = GetOrAdd<FilmGrain>(profile);
            grain.intensity.overrideState = true; grain.intensity.value = 0.2f;
            grain.type.overrideState = true; grain.type.value = FilmGrainLookup.Medium1;
        }

        static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T c)) return c;
            return profile.Add<T>(true);
        }

        static string EnsureForwardPlus(DaySnapshot snap)
        {
            var rp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (rp == null) return "URP 에셋 없음";
            var so = new SerializedObject(rp);
            var list = so.FindProperty("m_RendererDataList");
            var def = so.FindProperty("m_DefaultRendererIndex");
            int idx = def != null ? def.intValue : 0;
            if (list == null || list.arraySize == 0) return "Renderer Data 없음";
            if (idx < 0 || idx >= list.arraySize) idx = 0;
            var rd = list.GetArrayElementAtIndex(idx).objectReferenceValue as UniversalRendererData;
            if (rd == null) return "UniversalRendererData 없음";
            string path = AssetDatabase.GetAssetPath(rd);
            if (snap != null)
            {
                snap.urpRendererPath = path;
                snap.urpPrevMode = rd.renderingMode;
            }
            if (rd.renderingMode == RenderingMode.ForwardPlus)
            {
                if (snap != null) snap.urpModeChanged = false;
                return path + " 이미 Forward+";
            }
            Undo.RecordObject(rd, "URP Forward+");
            rd.renderingMode = RenderingMode.ForwardPlus;
            if (snap != null) snap.urpModeChanged = true;
            EditorUtility.SetDirty(rd);
            AssetDatabase.SaveAssets();
            return path + " → Forward+";
        }

        // -------------------------------------------------------------- util

        static void CaptureRenderSettings(DaySnapshot snap)
        {
            snap.skybox = RenderSettings.skybox;
            snap.ambientMode = RenderSettings.ambientMode;
            snap.ambientIntensity = RenderSettings.ambientIntensity;
            snap.ambientSky = RenderSettings.ambientSkyColor;
            snap.ambientEquator = RenderSettings.ambientEquatorColor;
            snap.ambientGround = RenderSettings.ambientGroundColor;
            snap.ambientLight = RenderSettings.ambientLight;
            snap.defaultReflectionMode = RenderSettings.defaultReflectionMode;
            snap.reflectionIntensity = RenderSettings.reflectionIntensity;
            snap.fog = RenderSettings.fog;
            snap.fogMode = RenderSettings.fogMode;
            snap.fogColor = RenderSettings.fogColor;
            snap.fogDensity = RenderSettings.fogDensity;
            snap.fogStart = RenderSettings.fogStartDistance;
            snap.fogEnd = RenderSettings.fogEndDistance;
            snap.sunPath = RenderSettings.sun != null ? PathOf(RenderSettings.sun.transform) : "";
        }

        static DaySnapshot.DirSnap MakeDirSnap(Light l)
        {
            return new DaySnapshot.DirSnap
            {
                path = PathOf(l.transform),
                enabled = l.enabled,
                goActive = l.gameObject.activeSelf,
                color = l.color,
                intensity = l.intensity,
                shadows = l.shadows,
                shadowRes = l.shadowResolution,
                shadowStrength = l.shadowStrength
            };
        }

        static Light FindRootDirectional()
        {
            Light rootDir = null;
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (l.type != LightType.Directional) continue;
                if (l.name == "Directional Light" && l.transform.parent == null && l.gameObject.scene.path == Map3Path)
                    return l;
                if (l.name == "Directional Light" && rootDir == null) rootDir = l;
            }
            return rootDir;
        }

        static Light FindMagicSun()
        {
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (l.type != LightType.Directional) continue;
                string p = PathOf(l.transform);
                if (p == "MagicIsland/10_Lighting/Sun" || (l.name == "Sun" && p.Contains("MagicIsland")))
                    return l;
            }
            return null;
        }

        static List<Light> CollectNightLights()
        {
            var list = new List<Light>();
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (l.type == LightType.Directional) continue;
                // 손전등은 제외
                if (l.name == "Flashlight" || l.GetComponent<LotteWorldFlashlight>() != null) continue;
                // Fest + Deco/Props 램프(LightPt) + MagicIsland LampLight 등 전부
                if (l.name.Contains("LampLight") || l.name.Contains("RoomLight") || l.name.Contains("LightPt")
                    || UnderNamed(l.transform, "LotteWorld_Fest")
                    || UnderNamed(l.transform, "LotteWorld_Deco")
                    || UnderNamed(l.transform, "LotteWorld_Props")
                    || UnderNamed(l.transform, "LotteWorld_Props_NorthGate"))
                    list.Add(l);
            }
            return list;
        }

        static bool UnderNamed(Transform t, string rootName)
        {
            while (t != null)
            {
                if (t.name == rootName) return true;
                t = t.parent;
            }
            return false;
        }

        static void SaveOpenMapScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i);
                if (!sc.isLoaded || string.IsNullOrEmpty(sc.path)) continue;
                if (sc.path == Map3Path || sc.path == MagicPath)
                    EditorSceneManager.SaveScene(sc);
            }
        }

        static DaySnapshot LoadSnap(string path) =>
            AssetDatabase.LoadAssetAtPath<DaySnapshot>(path);

        static DaySnapshot LoadOrCreateSnap(string path)
        {
            var snap = AssetDatabase.LoadAssetAtPath<DaySnapshot>(path);
            if (snap != null) return snap;
            snap = ScriptableObject.CreateInstance<DaySnapshot>();
            AssetDatabase.CreateAsset(snap, path);
            return snap;
        }

        static TodState LoadOrCreateTod()
        {
            var s = AssetDatabase.LoadAssetAtPath<TodState>(TodStatePath);
            if (s != null) return s;
            EnsureFolder(NightDir);
            s = ScriptableObject.CreateInstance<TodState>();
            AssetDatabase.CreateAsset(s, TodStatePath);
            return s;
        }

        // 하위호환
        static DaySnapshot LoadOrCreateSnap() => LoadOrCreateSnap(DaySnapPath);

        static Type FindFlickerType()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType("LotteWorld.Props.LightFlicker");
                if (t != null) return t;
            }
            return null;
        }

        static Scene FindOpenScene(string path)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.path == path) return s;
            }
            return default;
        }

        static string PathOf(Transform t)
        {
            var s = new List<string>();
            while (t != null) { s.Add(t.name); t = t.parent; }
            s.Reverse();
            return string.Join("/", s);
        }

        static Transform FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var parts = path.Split('/');
            for (int si = 0; si < SceneManager.sceneCount; si++)
            {
                var scene = SceneManager.GetSceneAt(si);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name != parts[0]) continue;
                    Transform cur = root.transform;
                    bool ok = true;
                    for (int i = 1; i < parts.Length; i++)
                    {
                        Transform next = null;
                        for (int c = 0; c < cur.childCount; c++)
                            if (cur.GetChild(c).name == parts[i]) { next = cur.GetChild(c); break; }
                        if (next == null) { ok = false; break; }
                        cur = next;
                    }
                    if (ok) return cur;
                }
            }
            return null;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        static Material EnsureNightSkybox()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(SkyMatPath);
            if (existing != null) return existing;
            var shader = Shader.Find("Skybox/Procedural") ?? Shader.Find("Skybox/Cubemap");
            var mat = new Material(shader);
            mat.name = "LW_NightSkybox";
            if (mat.HasProperty("_Exposure")) mat.SetFloat("_Exposure", 0.25f);
            if (mat.HasProperty("_AtmosphereThickness")) mat.SetFloat("_AtmosphereThickness", 0.35f);
            if (mat.HasProperty("_SkyTint")) mat.SetColor("_SkyTint", new Color(0.08f, 0.1f, 0.22f));
            if (mat.HasProperty("_GroundColor")) mat.SetColor("_GroundColor", new Color(0.02f, 0.02f, 0.03f));
            if (mat.HasProperty("_SunSize")) mat.SetFloat("_SunSize", 0.02f);
            AssetDatabase.CreateAsset(mat, SkyMatPath);
            return mat;
        }
    }

    public class LotteWorldDayNightWindow : EditorWindow
    {
        bool saveScenes = true;

        public static void Open()
        {
            var w = GetWindow<LotteWorldDayNightWindow>("롯데월드 낮·밤");
            w.minSize = new Vector2(280, 160);
            w.Show();
        }

        void OnGUI()
        {
            var tod = LotteWorldNight.GetTodState();
            string cur = tod != null && tod.isNight ? "밤" : "낮";
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("현재", cur, EditorStyles.boldLabel);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("Play 중에는 전환할 수 없습니다. Play를 끈 뒤 낮/밤을 바꾸세요.", MessageType.Warning);
                return;
            }
            EditorGUILayout.HelpBox(
                "낮 / 밤 버튼을 누르면 스냅샷을 적용하고 두 씬(Map3_LotteWorld, Map3_MagicIsland)을 저장합니다.",
                MessageType.Info);
            saveScenes = EditorGUILayout.ToggleLeft("적용 후 씬 저장", saveScenes);
            EditorGUILayout.Space(12);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.92f, 0.55f);
            if (GUILayout.Button("낮", GUILayout.Height(48)))
            {
                LotteWorldNight.SwitchToDay(saveScenes);
                Repaint();
            }
            GUI.backgroundColor = new Color(0.45f, 0.55f, 0.95f);
            if (GUILayout.Button("밤", GUILayout.Height(48)))
            {
                LotteWorldNight.SwitchToNight(saveScenes);
                Repaint();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(16);
            if (GUILayout.Button("스냅샷 다시 만들기 (낮·밤 재생성)"))
            {
                LotteWorldNight.BootstrapSnapshots(saveScenes, leaveAsNight: tod == null || tod.isNight);
                Repaint();
            }
        }
    }
}
