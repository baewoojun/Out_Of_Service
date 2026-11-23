// LotteWorldWaterTool.cs  (v1, 11/20) - Tools > LotteWorld > Water
// Puts the LotteWorld/Water shader on the flume ride (A08) and the north-gate waterfall:
//   1. Scan       : finds every renderer slot whose material name has "Water" or "Pond" in the open scene(s) and guesses a preset
//                   from the object path (Flume / A08 -> Flume, NorthGate / Waterfall -> Waterfall, "Pond" -> Pond, else Skip).
//   2. Apply      : per ticked renderer -> copies the mesh (saved under Assets/Maps/LotteWorld/Water/Generated), bakes flow + impact
//                   foam into UV3 (LotteWorldWaterFlow.cs), swaps the water slots to the preset material, adds splash + mist
//                   particles at every landing spot (under scene root "LW_WaterFX"). Undo works for all of it.
//   3. URP        : switches on Depth Texture + Opaque Texture on every URP asset (the shader needs both for see-through water).
// Materials: Assets/Maps/LotteWorld/Water/LW_Water_Flume / _Waterfall / _Pond.mat - made once, then yours to tweak (Apply does
// not overwrite them; "Reset preset materials" does).
// Files: LotteWorldWater.shader anywhere under Assets; this file + LotteWorldWaterFlow.cs in an Editor folder.

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LotteWorld.Water
{
    public enum Preset { Skip, Flume, Waterfall, Pond }

    public class WaterWindow : EditorWindow
    {
        const string Root = "Assets/Maps/LotteWorld/Water";
        const string FxRoot = "LW_WaterFX";

        class Target
        {
            public MeshRenderer Renderer;
            public List<int> Slots = new List<int>();
            public string Path, MatNames;
            public Preset Preset;
            public bool On, Reverse;
        }

        readonly List<Target> targets = new List<Target>();
        Vector2 scroll;
        bool splash = true;
        string lastLog = "";

        [MenuItem("Tools/LotteWorld/Water")]
        public static void Open() { GetWindow<WaterWindow>("LotteWorld Water").Scan(); }

        // ------------------------------------------------------------------ GUI
        void OnGUI()
        {
            EditorGUILayout.LabelField("Realistic water for the flume ride + north-gate waterfall", EditorStyles.boldLabel);
            if (Shader.Find("LotteWorld/Water") == null)
                EditorGUILayout.HelpBox("Shader 'LotteWorld/Water' not found - put LotteWorldWater.shader anywhere under Assets.", MessageType.Error);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan scene", GUILayout.Height(24))) Scan();
            if (GUILayout.Button("URP: Depth + Opaque texture ON", GUILayout.Height(24))) lastLog = EnableUrpTextures();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Water found (" + targets.Count + ")", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(160));
            foreach (Target t in targets)
            {
                if (t.Renderer == null) continue;
                EditorGUILayout.BeginHorizontal();
                t.On = EditorGUILayout.Toggle(t.On, GUILayout.Width(18));
                t.Preset = (Preset)EditorGUILayout.EnumPopup(t.Preset, GUILayout.Width(90));
                t.Reverse = GUILayout.Toggle(t.Reverse, "reverse", GUILayout.Width(62));
                if (GUILayout.Button(t.Path + "  [" + t.MatNames + "]", EditorStyles.label)) { Selection.activeObject = t.Renderer.gameObject; EditorGUIUtility.PingObject(t.Renderer.gameObject); }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            splash = EditorGUILayout.Toggle("Add splash + mist particles", splash);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply to ticked", GUILayout.Height(30))) Apply();
            if (GUILayout.Button("Remove particles", GUILayout.Height(30), GUILayout.Width(130))) RemoveFx(null);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Reset preset materials")) { foreach (Preset p in new[] { Preset.Flume, Preset.Waterfall, Preset.Pond }) PresetMaterial(p, true); lastLog = "Preset materials reset."; }
            EditorGUILayout.HelpBox("Ticked by default: rows guessed as Flume / Waterfall / Pond. 'reverse' flips the baked flow if the flume water runs the wrong way. " +
                                    "Tweak colours in " + Root + "/LW_Water_*.mat while Play mode or Scene view is running (Always Refresh on).", MessageType.Info);
            if (!string.IsNullOrEmpty(lastLog)) EditorGUILayout.HelpBox(lastLog, MessageType.None);
        }

        // ------------------------------------------------------------------ scan
        static bool IsWaterMat(Material m)
        {
            if (m == null) return false;
            string n = m.name.ToLowerInvariant();
            return n.Contains("water") || n.Contains("pond") || (m.shader != null && m.shader.name == "LotteWorld/Water");
        }

        static string PathOf(Transform t)
        {
            string p = t.name;
            while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
            return p;
        }

        static Preset Guess(string path, string mats)
        {
            string p = path.ToLowerInvariant(), m = mats.ToLowerInvariant();
            if (m.Contains("lw_water_flume")) return Preset.Flume;
            if (m.Contains("lw_water_waterfall")) return Preset.Waterfall;
            if (m.Contains("lw_water_pond") || m.Contains("pond")) return Preset.Pond;
            if (p.Contains("magicisland")) return Preset.Skip;
            if (p.Contains("flume") || p.Contains("a08") || p.Contains("후룸")) return Preset.Flume;
            if (p.Contains("northgate") || p.Contains("waterfall") || p.Contains("falls") || p.Contains("폭포")) return Preset.Waterfall;
            return Preset.Skip;
        }

        void Scan()
        {
            targets.Clear();
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene sc = SceneManager.GetSceneAt(s);
                if (!sc.isLoaded) continue;
                foreach (GameObject root in sc.GetRootGameObjects())
                    foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (r.gameObject.name.StartsWith(FxRoot)) continue;
                        Material[] mats = r.sharedMaterials;
                        var t = new Target { Renderer = r, Path = PathOf(r.transform) };
                        var names = new List<string>();
                        for (int i = 0; i < mats.Length; i++) if (IsWaterMat(mats[i])) { t.Slots.Add(i); names.Add(mats[i].name); }
                        if (t.Slots.Count == 0) continue;
                        if (r.GetComponent<MeshFilter>() == null) continue;
                        t.MatNames = string.Join(", ", names.ToArray());
                        t.Preset = Guess(t.Path, t.MatNames);
                        t.On = t.Preset != Preset.Skip;
                        targets.Add(t);
                    }
            }
            targets.Sort((a, b) => b.On.CompareTo(a.On) != 0 ? b.On.CompareTo(a.On) : string.CompareOrdinal(a.Path, b.Path));
            int on = 0; foreach (Target t in targets) if (t.On) on++;
            lastLog = "Scan: " + targets.Count + " water renderers, " + on + " ticked (flume / waterfall / pond).";
            Repaint();
        }

        // ------------------------------------------------------------------ apply
        void Apply()
        {
            Shader sh = Shader.Find("LotteWorld/Water");
            if (sh == null) { lastLog = "Shader 'LotteWorld/Water' not found."; return; }
            var log = new List<string>();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("LotteWorld Water");
            EnsureFolder(Root + "/Generated");
            foreach (Target t in targets)
            {
                if (!t.On || t.Preset == Preset.Skip || t.Renderer == null) continue;
                try { log.Add(ApplyOne(t)); }
                catch (System.Exception e) { log.Add(t.Path + ": FAILED " + e.Message); Debug.LogException(e); }
            }
            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();
            foreach (Scene sc in LoadedScenes()) EditorSceneManager.MarkSceneDirty(sc);
            string urp = UrpStatus();
            lastLog = string.Join("\n", log.ToArray()) + (urp.Length > 0 ? "\n" + urp : "");
            Debug.Log("[Water] " + lastLog);
        }

        string ApplyOne(Target t)
        {
            MeshRenderer r = t.Renderer;
            MeshFilter mf = r.GetComponent<MeshFilter>();
            Mesh src = mf.sharedMesh;
            if (src == null) return t.Path + ": no mesh";
            src = MakeReadable(src);
            if (src == null) return t.Path + ": mesh not readable";

            Vector3[] vLocal = src.vertices;
            Matrix4x4 l2w = r.transform.localToWorldMatrix, w2l = r.transform.worldToLocalMatrix;
            var vWorld = new Vector3[vLocal.Length];
            for (int i = 0; i < vLocal.Length; i++) vWorld[i] = l2w.MultiplyPoint3x4(vLocal[i]);
            var tris = new List<int>();
            foreach (int slot in t.Slots) if (slot < src.subMeshCount) tris.AddRange(src.GetTriangles(slot));
            if (t.Slots.Count > 0 && t.Slots[t.Slots.Count - 1] >= src.subMeshCount && src.subMeshCount > 0)   // extra material slot re-renders the last sub-mesh
                tris.AddRange(src.GetTriangles(src.subMeshCount - 1));

            List<SplashSpot> spots; string report;
            Vector4[] flowW = FlowBake.Compute(vWorld, tris, t.Reverse, out spots, out report);
            var uv3 = new List<Vector4>(new Vector4[vLocal.Length]);
            var have = new List<Vector4>(); src.GetUVs(3, have);
            for (int i = 0; i < vLocal.Length; i++)
            {
                Vector4 f = flowW[i];
                Vector3 d = w2l.MultiplyVector(new Vector3(f.x, f.y, f.z));
                d = d.sqrMagnitude > 1e-10f ? d.normalized : Vector3.zero;
                uv3[i] = new Vector4(d.x, d.y, d.z, f.w);
            }
            // keep whatever non-water vertices already had in UV3 (normally nothing)
            if (have.Count == vLocal.Length)
            {
                var isWater = new bool[vLocal.Length]; foreach (int i in tris) isWater[i] = true;
                for (int i = 0; i < vLocal.Length; i++) if (!isWater[i]) uv3[i] = have[i];
            }

            Mesh copy = Object.Instantiate(src);
            copy.name = src.name.Replace("_LWflow", "") + "_LWflow";
            copy.SetUVs(3, uv3);
            string safe = Sanitize(PathOf(r.transform));
            string assetPath = Root + "/Generated/" + safe + "_LWflow.asset";
            Mesh old = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (old != null && old != mf.sharedMesh) { AssetDatabase.DeleteAsset(assetPath); old = null; }
            if (old != null) { EditorUtility.CopySerialized(copy, old); copy = old; EditorUtility.SetDirty(old); }
            else AssetDatabase.CreateAsset(copy, assetPath);

            Undo.RecordObject(mf, "Water mesh");
            mf.sharedMesh = copy;
            Undo.RecordObject(r, "Water material");
            Material[] mats = r.sharedMaterials;
            Material pm = PresetMaterial(t.Preset, false);
            foreach (int slot in t.Slots) if (slot < mats.Length) mats[slot] = pm;
            r.sharedMaterials = mats;
            r.shadowCastingMode = mats.Length == t.Slots.Count ? ShadowCastingMode.Off : r.shadowCastingMode;

            RemoveFx(r);
            if (splash && spots.Count > 0) AddFx(r, spots, t.Preset);
            return t.Path + " -> " + t.Preset + ": " + report;
        }

        static IEnumerable<Scene> LoadedScenes()
        {
            for (int s = 0; s < SceneManager.sceneCount; s++) { Scene sc = SceneManager.GetSceneAt(s); if (sc.isLoaded) yield return sc; }
        }

        static string Sanitize(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            s = s.Replace('/', '_').Replace(' ', '_');
            return s.Length > 90 ? s.Substring(s.Length - 90) : s;
        }

        static Mesh MakeReadable(Mesh m)
        {
            if (m.isReadable) return m;
            string path = AssetDatabase.GetAssetPath(m);
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) return null;
            mi.isReadable = true;
            mi.SaveAndReimport();
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path)) { var mm = o as Mesh; if (mm != null && mm.name == m.name) return mm; }
            return null;
        }

        // ------------------------------------------------------------------ materials
        static Material PresetMaterial(Preset p, bool reset)
        {
            EnsureFolder(Root);
            string path = Root + "/LW_Water_" + p + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool fresh = m == null;
            if (fresh) { m = new Material(Shader.Find("LotteWorld/Water")); AssetDatabase.CreateAsset(m, path); }
            if (!fresh && !reset) { if (m.shader == null || m.shader.name != "LotteWorld/Water") m.shader = Shader.Find("LotteWorld/Water"); return m; }
            m.shader = Shader.Find("LotteWorld/Water");
            switch (p)
            {
                case Preset.Flume:          // ride channel: shallow, clear-ish, fast, slight scum
                    Set(m, new Color(0.04f, 0.13f, 0.12f), new Color(0.72f, 0.90f, 0.86f), new Vector4(0.55f, 0.22f, 0.18f, 0), 0.9f,
                        0.55f, 0.6f, 1.3f, 6f, 0.25f, 0.25f, 1.0f, 0.5f, 0.15f);
                    break;
                case Preset.Waterfall:      // falls + pool: white sheets, churned landings
                    Set(m, new Color(0.05f, 0.14f, 0.15f), new Color(0.80f, 0.93f, 0.92f), new Vector4(0.45f, 0.20f, 0.17f, 0), 0.6f,
                        0.6f, 0.65f, 0.9f, 7f, 0.25f, 0.35f, 1.3f, 0.85f, 0.10f);
                    break;
                default:                    // pond: still, murky, neglected
                    Set(m, new Color(0.03f, 0.08f, 0.06f), new Color(0.55f, 0.70f, 0.55f), new Vector4(0.9f, 0.5f, 0.45f, 0), 1.6f,
                        0.9f, 0.35f, 0f, 3f, 0.5f, 0.12f, 1.0f, 0.6f, 0.45f);
                    break;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Set(Material m, Color deep, Color tint, Vector4 absorb, float turb, float waveScale, float waveStrength, float flow, float fall,
                        float ripple, float edgeFoam, float impactFoam, float fallWhite, float scum)
        {
            m.SetColor("_DeepColor", deep); m.SetColor("_Tint", tint); m.SetVector("_Absorption", absorb); m.SetFloat("_Turbidity", turb);
            m.SetFloat("_WaveScale", waveScale); m.SetFloat("_WaveStrength", waveStrength); m.SetFloat("_FlowSpeed", flow); m.SetFloat("_FallSpeed", fall);
            m.SetFloat("_Ripple", ripple); m.SetFloat("_EdgeFoam", edgeFoam); m.SetFloat("_ImpactFoam", impactFoam); m.SetFloat("_FallWhite", fallWhite);
            m.SetFloat("_Scum", scum);
        }

        // ------------------------------------------------------------------ particles
        static Transform FxHolder(Scene sc)
        {
            foreach (GameObject g in sc.GetRootGameObjects()) if (g.name == FxRoot) return g.transform;
            var go = new GameObject(FxRoot);
            SceneManager.MoveGameObjectToScene(go, sc);
            Undo.RegisterCreatedObjectUndo(go, "Water FX");
            return go.transform;
        }

        static string FxName(MeshRenderer r) { return "FX_" + Sanitize(PathOf(r.transform)); }

        static void RemoveFx(MeshRenderer r)
        {
            foreach (Scene sc in LoadedScenes())
                foreach (GameObject g in sc.GetRootGameObjects())
                {
                    if (g.name != FxRoot) continue;
                    if (r == null) { Undo.DestroyObjectImmediate(g); continue; }
                    Transform c = g.transform.Find(FxName(r));
                    if (c != null) Undo.DestroyObjectImmediate(c.gameObject);
                }
        }

        static void AddFx(MeshRenderer r, List<SplashSpot> spots, Preset p)
        {
            Transform holder = FxHolder(r.gameObject.scene);
            var grp = new GameObject(FxName(r));
            grp.transform.SetParent(holder, false);
            Undo.RegisterCreatedObjectUndo(grp, "Water FX");
            Material mat = ParticleMaterial();
            int k = 0;
            foreach (SplashSpot s in spots)
            {
                float rate = s.Width * (s.Big ? 22f : 18f);
                Particles(grp.transform, "Splash_" + k, s, mat, false, rate);
                Particles(grp.transform, "Mist_" + k, s, mat, true, rate * 0.12f);
                k++;
            }
        }

        static void Particles(Transform parent, string name, SplashSpot s, Material mat, bool mist, float rate)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = s.Position + Vector3.up * 0.15f;
            Vector3 side = Vector3.Cross(Vector3.up, s.Edge);
            go.transform.rotation = Quaternion.LookRotation(Vector3.up, side.sqrMagnitude > 1e-6f ? side : Vector3.forward);   // local Z = up, local X = along the edge
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = true; main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = mist ? 120 : 600;
            if (mist)
            {
                main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 3.5f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
                main.startSize = new ParticleSystem.MinMaxCurve(s.Big ? 1.6f : 1.0f, s.Big ? 3.2f : 2.0f);
                main.startColor = new Color(0.85f, 0.88f, 0.9f, 0.10f);
                main.gravityModifier = -0.02f;
            }
            else
            {
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 1.0f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(s.Big ? 2.0f : 1.5f, s.Big ? 4.5f : 3.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.15f, s.Big ? 0.6f : 0.45f);
                main.startColor = new Color(0.86f, 0.9f, 0.92f, 0.45f);
                main.gravityModifier = 0.9f;
            }
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var em = ps.emission; em.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = mist ? 50f : 28f;
            shape.radius = 0.5f;
            shape.scale = new Vector3(s.Width, 0.5f, 1f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, mist ? 1.8f : 1.4f)));
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = mat;
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            pr.shadowCastingMode = ShadowCastingMode.Off;
            pr.receiveShadows = false;
            pr.sortingFudge = mist ? 2f : 0f;
            ps.Play();
        }

        static Material ParticleMaterial()
        {
            EnsureFolder(Root);
            string path = Root + "/LW_WaterSplash.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            m = new Material(sh);
            m.SetTexture("_BaseMap", SplashTexture());
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SoftParticlesEnabled", 1f); m.SetFloat("_SoftParticlesNearFadeDistance", 0f); m.SetFloat("_SoftParticlesFarFadeDistance", 0.8f);
            m.SetVector("_SoftParticleFadeParams", new Vector4(0f, 1f / 0.8f, 0f, 0f));
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.EnableKeyword("_SOFTPARTICLES_ON");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Texture2D SplashTexture()
        {
            string path = Root + "/LW_WaterSplash.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) return tex;
            const int N = 64;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var rnd = new System.Random(7);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f, d = Mathf.Sqrt(u * u + v * v);
                    float n = Mathf.PerlinNoise(x * 0.18f, y * 0.18f) * 0.6f + Mathf.PerlinNoise(x * 0.45f + 9f, y * 0.45f) * 0.4f;
                    float a = Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d);
                    a *= Mathf.Lerp(0.55f, 1.15f, n) + (float)rnd.NextDouble() * 0.08f;
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
            t.Apply();
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = true;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ------------------------------------------------------------------ URP
        static IEnumerable<UniversalRenderPipelineAsset> UrpAssets()
        {
            var seen = new HashSet<UniversalRenderPipelineAsset>();
            var a = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (a != null && seen.Add(a)) yield return a;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                var q = QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset;
                if (q != null && seen.Add(q)) yield return q;
            }
        }

        static string UrpStatus()
        {
            var off = new List<string>();
            foreach (UniversalRenderPipelineAsset a in UrpAssets())
                if (!a.supportsCameraDepthTexture || !a.supportsCameraOpaqueTexture) off.Add(a.name);
            return off.Count == 0 ? "" : "WARNING: Depth/Opaque texture off on " + string.Join(", ", off.ToArray()) + " -> press 'URP: Depth + Opaque texture ON'.";
        }

        static string EnableUrpTextures()
        {
            var done = new List<string>();
            foreach (UniversalRenderPipelineAsset a in UrpAssets())
            {
                Undo.RecordObject(a, "URP textures");
                a.supportsCameraDepthTexture = true;
                a.supportsCameraOpaqueTexture = true;
                EditorUtility.SetDirty(a);
                done.Add(a.name);
            }
            int cams = 0;
            foreach (Scene sc in LoadedScenes())
                foreach (GameObject g in sc.GetRootGameObjects())
                    foreach (UniversalAdditionalCameraData cd in g.GetComponentsInChildren<UniversalAdditionalCameraData>(true))
                    {
                        if (cd.requiresDepthOption != CameraOverrideOption.Off && cd.requiresColorOption != CameraOverrideOption.Off) continue;
                        Undo.RecordObject(cd, "Camera textures");
                        if (cd.requiresDepthOption == CameraOverrideOption.Off) cd.requiresDepthOption = CameraOverrideOption.UsePipelineSettings;
                        if (cd.requiresColorOption == CameraOverrideOption.Off) cd.requiresColorOption = CameraOverrideOption.UsePipelineSettings;
                        EditorUtility.SetDirty(cd); cams++;
                    }
            AssetDatabase.SaveAssets();
            return done.Count == 0 ? "No URP asset found (Project Settings > Graphics)." :
                   "Depth + Opaque texture ON: " + string.Join(", ", done.ToArray()) + (cams > 0 ? " (+" + cams + " camera overrides fixed)" : "");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
