// InteriorVisibilityZone.cs - 플레이어가 실내 건물에서 멀리 나가 있으면 실내 물체를 꺼 둔다 (런타임).
// 매직아일랜드(야외)에서는 실내가 벽에 가려 안 보이는데도 전부 그려져서 가장 무거웠다. 그걸 막는다.
// Map3_LotteWorld 씬이 열리면 자동으로 생긴다. 씬에 따로 붙일 필요 없다.
//
// 끄는 대상: Map3_LotteWorld 씬의 루트 오브젝트 중, 렌더러가 전부 건물 껍데기(LotteWorld_Shell) 안에 들어 있는 것.
// 남기는 것: 껍데기·외벽·블록아웃, 플레이어·카메라·조명, 움직이는 캐릭터(CharacterController / NavMeshAgent 가 달린 루트).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LotteWorld.Performance
{
    public class InteriorVisibilityZone : MonoBehaviour
    {
        /// <summary>false 면 아무것도 끄지 않는다 (측정·디버그용).</summary>
        public static bool Enabled = true;

        const string InteriorScene = "Map3_LotteWorld";
        const string ShellName = "LotteWorld_Shell";
        // 다리 쪽 출입구가 뚫려 있어 가까이서는 안이 들여다보인다. 출입구가 아주 작게 보이는 거리에서만 끈다.
        const float HideDistance = 110f;  // 건물에서 이만큼 벗어나면 끈다
        const float ShowDistance = 95f;   // 이 안으로 들어오면 다시 켠다
        const float ContainTolerance = 8f; // 실내 판정 여유 (m)
        const float Interval = 0.25f;
        const int RootsPerFrame = 12;     // 한 프레임에 켜고 끄는 루트 수 (끊김을 나눠서)

        static readonly HashSet<string> KeepNames = new HashSet<string>
        {
            ShellName, "LotteWorld_Facade", "Blockout", "LotteWorld_RideDirector",
        };

        static InteriorVisibilityZone instance;
        readonly List<GameObject> roots = new List<GameObject>();
        Bounds volume;
        bool hasVolume, hidden, dirty = true;
        int cursor = -1;
        float nextTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance = null; Enabled = true; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                if (scene.name != InteriorScene) return;
                if (!instance)
                {
                    var go = new GameObject("InteriorVisibilityZone");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<InteriorVisibilityZone>();
                }
                instance.dirty = true;
            };
            SceneManager.sceneUnloaded += scene => { if (instance && scene.name == InteriorScene) instance.dirty = true; };
        }

        /// <summary>실내 볼륨(껍데기 경계)과, 멀리서 꺼도 되는 루트 목록을 구한다. 에디터 확인용으로도 쓴다.</summary>
        public static bool FindInterior(out Bounds volume, List<GameObject> result)
        {
            volume = default;
            result.Clear();
            var scene = SceneManager.GetSceneByName(InteriorScene);
            if (!scene.IsValid() || !scene.isLoaded) return false;
            var all = scene.GetRootGameObjects();

            GameObject shell = null;
            foreach (var r in all) if (r.name == ShellName) { shell = r; break; }
            if (!shell || !TryBounds(shell, out volume)) return false;

            // 바닥 아래 기초나 벽 장식이 경계를 조금 삐져나오는 것은 실내로 본다.
            var loose = volume; loose.Expand(ContainTolerance * 2f);

            foreach (var r in all)
            {
                if (KeepNames.Contains(r.name)) continue;
                if (r.GetComponentInChildren<Camera>(true)) continue;
                if (r.GetComponentInChildren<CharacterController>(true)) continue;
                if (r.GetComponentInChildren<UnityEngine.AI.NavMeshAgent>(true)) continue;
                var light = r.GetComponent<Light>();
                if (light && light.type == LightType.Directional) continue;
                if (!TryBounds(r, out var b)) continue;                       // 렌더러가 없는 관리용 오브젝트는 그대로 둔다
                if (!loose.Contains(b.min) || !loose.Contains(b.max)) continue;   // 건물 밖으로 크게 삐져나온 것은 그대로 둔다
                result.Add(r);
            }
            return true;
        }

        static bool TryBounds(GameObject go, out Bounds b)
        {
            b = default;
            bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any;
        }

        void Collect()
        {
            if (hidden) foreach (var r in roots) if (r) r.SetActive(true);
            hidden = false; cursor = -1;
            hasVolume = FindInterior(out volume, roots);
            // 처음부터 꺼져 있던 루트는 건드리지 않는다.
            roots.RemoveAll(r => !r.activeSelf);
            dirty = false;
        }

        void OnDestroy()
        {
            foreach (var r in roots) if (r) r.SetActive(true);
        }

        void LateUpdate()
        {
            if (dirty) Collect();
            if (!hasVolume) return;

            // 진행 중인 켜기/끄기를 여러 프레임에 나눠 처리한다.
            if (cursor >= 0)
            {
                int end = Mathf.Min(roots.Count, cursor + RootsPerFrame);
                for (; cursor < end; cursor++) if (roots[cursor]) roots[cursor].SetActive(!hidden);
                if (cursor >= roots.Count) cursor = -1;
                return;
            }

            if (Time.unscaledTime < nextTime) return;
            nextTime = Time.unscaledTime + Interval;

            var cam = Camera.main;
            if (!cam) return;
            float d = Mathf.Sqrt(volume.SqrDistance(cam.transform.position)); // 건물 안이면 0
            bool wantHidden = Enabled && (hidden ? d > ShowDistance : d > HideDistance);
            if (wantHidden == hidden) return;
            hidden = wantHidden;
            cursor = 0;
        }
    }
}
