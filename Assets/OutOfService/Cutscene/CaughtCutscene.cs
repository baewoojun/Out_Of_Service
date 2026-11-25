using System;
using UnityEngine;

namespace OutOfService
{
    // 괴물에게 잡혔을 때의 1인칭 점프스케어 컷신 (시안 A, 약 3초).
    //   0.00 잡힘: 조작 잠금, 손전등 깜빡, 심장 소리
    //   0.15 괴물 쪽으로 카메라가 홱 돌아감
    //   0.45 비명 (Scream)
    //   1.35 덮침 (Attack, 0.5초 뒤 타격: 붉은 플래시 + 강한 흔들림)
    //   1.85 바닥으로 쓰러짐, 손전등 꺼짐
    //   2.55 암전 → 3.25 onFinished (사망 화면)
    // 괴물 모델은 Resources/OutOfServiceCutscene/CaughtMonster 프리팹을 쓰고, 없으면 잡은 괴물(attacker)을 그대로 쓴다.
    // 메뉴 Out Of Service ▸ Cutscene ▸ Build Caught Monster 로 프리팹을 만든다.
    public sealed class CaughtCutscene : MonoBehaviour
    {
        public const string MonsterResource = "OutOfServiceCutscene/CaughtMonster";

        public static bool Playing { get; private set; }

        const float TurnStart = 0.15f, TurnEnd = 0.42f;
        const float ScreamAt = 0.45f;
        const float AttackAt = 1.35f;
        const float HitAt = AttackAt + 0.5f;
        const float FallEnd = HitAt + 0.55f;
        const float FadeStart = 2.55f, FadeEnd = 3.05f;
        const float DoneAt = 3.25f;
        const float OverlayLinger = 0.6f;

        static readonly int ScreamId = Animator.StringToHash("Scream");
        static readonly int AttackId = Animator.StringToHash("Attack");
        static readonly int SpeedId = Animator.StringToHash("Speed");

        Camera cam;
        Transform camT;
        Light flashlight;
        bool flashlightWasOn;
        Action onFinished;

        Transform monster;
        Animator monsterAnim;
        Renderer[] hiddenAttackerRenderers;
        bool spawnedMonster;
        Vector3 monsterStart, monsterLunge;
        Light faceLight;

        Vector3 startPos;
        Quaternion startRot, lookRot;
        Vector3 floorPos;
        Quaternion floorRot;

        AudioSource audioSource;
        AudioClip heartbeat, scream, hit;

        float t;
        bool screamed, attacked, struck, done;
        float doneAt = -1f;
        float shake;
        float redFlash;
        float black;

        // 컷신을 시작한다. 카메라가 없으면 false 를 돌려주고 아무것도 하지 않는다.
        public static bool Play(MarketPlayer player, Transform attacker, Action onFinished)
        {
            if (Playing || !player || !player.viewCamera) return false;
            var go = new GameObject("CaughtCutscene");
            var cutscene = go.AddComponent<CaughtCutscene>();
            cutscene.Begin(player, attacker, onFinished);
            return true;
        }

        void Begin(MarketPlayer player, Transform attacker, Action finished)
        {
            Playing = true;
            onFinished = finished;
            cam = player.viewCamera;
            camT = cam.transform;
            flashlight = player.flashlight;
            flashlightWasOn = flashlight && flashlight.enabled;
            player.SuspendInspection();

            startPos = camT.position;
            startRot = camT.rotation;

            Vector3 dir = attacker ? attacker.position - startPos : -camT.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = -camT.forward;
            dir.y = 0f;
            dir.Normalize();

            float floorY = FindFloor(player, startPos);
            float distance = 1.25f;
            if (Physics.Raycast(startPos, dir, out RaycastHit wall, distance + 0.4f, ~0, QueryTriggerInteraction.Ignore)
                && !wall.collider.transform.IsChildOf(player.transform)
                && (!attacker || !wall.collider.transform.IsChildOf(attacker)))
                distance = Mathf.Max(0.8f, wall.distance - 0.4f);

            Vector3 flatCam = new Vector3(startPos.x, floorY, startPos.z);
            monsterStart = flatCam + dir * distance;
            monsterLunge = flatCam + dir * Mathf.Max(0.7f, distance - 0.45f);

            SetupMonster(attacker, dir);

            headBone = FindHeadBone();
            Vector3 head = HeadPoint(dir);
            lookRot = Quaternion.LookRotation(head - startPos, Vector3.up);

            // 쓰러진 자세: 바닥 가까이, 옆으로 누운 채 괴물 쪽을 올려다본다.
            floorPos = new Vector3(startPos.x, floorY + 0.22f, startPos.z) - dir * 0.25f;
            Quaternion lookUp = Quaternion.LookRotation((head - floorPos).normalized, Vector3.up);
            floorRot = lookUp * Quaternion.Euler(0f, 0f, 72f);

            faceLight = new GameObject("CutsceneFaceLight").AddComponent<Light>();
            faceLight.type = LightType.Spot;
            faceLight.spotAngle = 55f;
            faceLight.range = 6f;
            faceLight.intensity = 0f;
            faceLight.color = new Color(1f, 0.93f, 0.82f);
            faceLight.shadows = LightShadows.None;
            faceLight.transform.SetParent(camT, false);
            faceLight.transform.localPosition = new Vector3(0.12f, -0.12f, 0f);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
            heartbeat = MakeHeartbeat();
            scream = MakeScream();
            hit = MakeHit();
            audioSource.PlayOneShot(heartbeat, 0.9f);
        }

        void SetupMonster(Transform attacker, Vector3 dir)
        {
            var prefab = Resources.Load<GameObject>(MonsterResource);
            if (prefab)
            {
                var go = Instantiate(prefab, monsterStart, Quaternion.LookRotation(-dir, Vector3.up));
                go.name = "CaughtMonster";
                foreach (var b in go.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled = false;
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                var cc = go.GetComponent<CharacterController>();
                if (cc) cc.enabled = false;
                var lod = go.GetComponentInChildren<LODGroup>();
                if (lod) lod.ForceLOD(0);
                monster = go.transform;
                monsterAnim = go.GetComponentInChildren<Animator>();
                if (monsterAnim)
                {
                    monsterAnim.applyRootMotion = false;
                    monsterAnim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    monsterAnim.SetFloat(SpeedId, 0f);
                }
                spawnedMonster = true;

                if (attacker)
                {
                    hiddenAttackerRenderers = attacker.GetComponentsInChildren<Renderer>();
                    foreach (var r in hiddenAttackerRenderers) r.enabled = false;
                }
            }
            else if (attacker)
            {
                monster = attacker;
                monsterStart = new Vector3(monsterStart.x, attacker.position.y, monsterStart.z);
                monsterLunge = new Vector3(monsterLunge.x, attacker.position.y, monsterLunge.z);
                monster.SetPositionAndRotation(monsterStart, Quaternion.LookRotation(-dir, Vector3.up));
            }
        }

        Transform headBone;

        Transform FindHeadBone()
        {
            if (!monster) return null;
            foreach (var t in monster.GetComponentsInChildren<Transform>())
                if (t.name.EndsWith("Head", StringComparison.OrdinalIgnoreCase)) return t;
            return null;
        }

        // 카메라가 바라볼 괴물 얼굴 위치. 머리 뼈가 있으면 애니메이션을 따라간다.
        Vector3 HeadPoint(Vector3 fallbackDir)
        {
            if (headBone) return headBone.position + Vector3.up * 0.12f;
            if (monster) return monster.position + Vector3.up * 1.5f;
            return startPos + fallbackDir;
        }

        static float FindFloor(MarketPlayer player, Vector3 from)
        {
            float best = float.MaxValue, y = from.y - 1.6f;
            foreach (var h in Physics.RaycastAll(from, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.transform.IsChildOf(player.transform)) continue;
                if (h.distance < best) { best = h.distance; y = h.point.y; }
            }
            return y;
        }

        void Update()
        {
            t += Time.deltaTime;

            if (!screamed && t >= ScreamAt)
            {
                screamed = true;
                if (monsterAnim) monsterAnim.SetTrigger(ScreamId);
                audioSource.PlayOneShot(scream, 1f);
                shake = 0.35f;
            }
            if (!attacked && t >= AttackAt)
            {
                attacked = true;
                if (monsterAnim) monsterAnim.SetTrigger(AttackId);
            }
            if (!struck && t >= HitAt)
            {
                struck = true;
                Vector3 head = HeadPoint(camT.forward);
                floorRot = Quaternion.LookRotation((head - floorPos).normalized, Vector3.up) * Quaternion.Euler(0f, 0f, 72f);
                audioSource.PlayOneShot(hit, 1f);
                redFlash = 1f;
                shake = 1f;
            }

            UpdateMonster();
            UpdateCamera();
            UpdateLights();

            shake = Mathf.MoveTowards(shake, struck ? 0.15f : 0.12f, Time.deltaTime * 1.4f);
            redFlash = Mathf.MoveTowards(redFlash, 0f, Time.deltaTime * 1.6f);
            black = Mathf.Clamp01(Mathf.InverseLerp(FadeStart, FadeEnd, t));

            if (!done && t >= DoneAt)
            {
                done = true;
                doneAt = t;
                Playing = false;
                var cb = onFinished;
                onFinished = null;
                cb?.Invoke();
            }
            if (done && t >= doneAt + OverlayLinger) Destroy(gameObject);
        }

        void UpdateMonster()
        {
            if (!monster) return;
            float k = Mathf.InverseLerp(AttackAt + 0.2f, HitAt, t);
            k = k * k * (3f - 2f * k);
            monster.position = Vector3.Lerp(monsterStart, monsterLunge, k);
        }

        void UpdateCamera()
        {
            Vector3 pos;
            Quaternion rot;
            if (t < HitAt)
            {
                float k = Mathf.InverseLerp(TurnStart, TurnEnd, t);
                k = 1f - Mathf.Pow(1f - k, 3f);
                pos = startPos;
                // 비명·덮침 동작 중에도 얼굴을 계속 따라간다 (부드럽게).
                Quaternion live = Quaternion.LookRotation(HeadPoint(camT.forward) - startPos, Vector3.up);
                lookRot = Quaternion.Slerp(lookRot, live, 1f - Mathf.Exp(-10f * Time.deltaTime));
                rot = Quaternion.Slerp(startRot, lookRot, k);
                // 덮치기 직전 살짝 뒤로 젖혀진다.
                float lean = Mathf.InverseLerp(AttackAt, HitAt, t);
                rot = rot * Quaternion.Euler(-6f * lean, 0f, 3f * lean);
            }
            else
            {
                float k = Mathf.InverseLerp(HitAt, FallEnd, t);
                float fall = k * k;
                pos = Vector3.Lerp(startPos, floorPos, fall);
                if (k >= 1f) pos += Vector3.up * Mathf.Sin(Mathf.Clamp01((t - FallEnd) * 6f) * Mathf.PI) * 0.03f;
                rot = Quaternion.Slerp(lookRot, floorRot, 1f - Mathf.Pow(1f - k, 2f));
            }

            float s = shake * shake;
            float n = t * 38f;
            Vector3 offset = new Vector3(Mathf.PerlinNoise(n, 0.1f) - 0.5f, Mathf.PerlinNoise(0.3f, n) - 0.5f, 0f) * 0.09f * s;
            Quaternion jitter = Quaternion.Euler(
                (Mathf.PerlinNoise(n, 1.7f) - 0.5f) * 9f * s,
                (Mathf.PerlinNoise(2.9f, n) - 0.5f) * 9f * s,
                (Mathf.PerlinNoise(n, 4.3f) - 0.5f) * 6f * s);
            camT.SetPositionAndRotation(pos + rot * offset, rot * jitter);
        }

        void UpdateLights()
        {
            bool flicker = Mathf.PerlinNoise(t * 22f, 0.5f) > (struck ? 0.75f : 0.38f);
            if (flashlight && flashlightWasOn) flashlight.enabled = t < FallEnd + 0.2f && flicker;

            if (faceLight)
            {
                float on = t < TurnEnd ? 0f : t < FallEnd ? 1f : Mathf.Clamp01(1f - (t - FallEnd) * 1.5f);
                float flick = flicker ? 1f : 0.25f;
                faceLight.intensity = 3.2f * on * flick;
                if (monster) faceLight.transform.rotation = Quaternion.LookRotation(monster.position + Vector3.up * 1.6f - faceLight.transform.position);
            }
        }

        void OnGUI()
        {
            GUI.depth = -1000;
            float w = Screen.width, h = Screen.height;
            var prev = GUI.color;

            float pulse = 0.18f + 0.12f * Mathf.Sin(t * 9f);
            float vignette = done ? 0f : Mathf.Clamp01(pulse + redFlash * 0.5f);
            GUI.color = new Color(0.45f, 0f, 0f, vignette);
            float edge = Mathf.Min(w, h) * 0.14f;
            GUI.DrawTexture(new Rect(0, 0, w, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, h - edge, w, edge), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, 0, edge, h), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(w - edge, 0, edge, h), Texture2D.whiteTexture);

            if (redFlash > 0f)
            {
                GUI.color = new Color(0.85f, 0.04f, 0.03f, redFlash * 0.55f);
                GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            }

            float a = black;
            if (done) a = 1f - Mathf.Clamp01((t - doneAt) / OverlayLinger);
            if (a > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, a);
                GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            }
            GUI.color = prev;
        }

        void OnDestroy()
        {
            if (Playing && !done) Playing = false;
            if (faceLight) Destroy(faceLight.gameObject);
            if (spawnedMonster && monster) Destroy(monster.gameObject);
            if (hiddenAttackerRenderers != null)
                foreach (var r in hiddenAttackerRenderers) if (r) r.enabled = true;
            if (heartbeat) Destroy(heartbeat);
            if (scream) Destroy(scream);
            if (hit) Destroy(hit);
        }

        // ---- 효과음은 코드로 합성한다 (별도 오디오 파일 없음).

        const int Rate = 44100;

        static AudioClip MakeHeartbeat()
        {
            float len = 1.1f;
            var data = new float[(int)(Rate * len)];
            for (int i = 0; i < data.Length; i++)
            {
                float x = i / (float)Rate;
                data[i] = Thump(x, 0f, 0.9f) + Thump(x, 0.24f, 0.65f);
            }
            return Clip("CutsceneHeartbeat", data);
        }

        static float Thump(float x, float at, float amp)
        {
            float d = x - at;
            if (d < 0f) return 0f;
            float env = Mathf.Exp(-d * 18f) * Mathf.Clamp01(d * 400f);
            float f = 46f + 30f * Mathf.Exp(-d * 30f);
            return amp * env * Mathf.Sin(2f * Mathf.PI * f * d);
        }

        static AudioClip MakeScream()
        {
            float len = 1.6f;
            var data = new float[(int)(Rate * len)];
            var rng = new System.Random(7);
            float p1 = 0f, p2 = 0f, p3 = 0f, low = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float x = i / (float)Rate;
                float env = Mathf.Clamp01(x * 30f) * Mathf.Clamp01((len - x) * 2.2f);
                float pitch = 520f + 260f * Mathf.Sin(x * 3.1f) + 90f * Mathf.Sin(x * 41f) - 120f * x;
                p1 += pitch / Rate;
                p2 += pitch * 1.497f / Rate;
                p3 += pitch * 0.503f / Rate;
                float saw = (p1 % 1f) * 2f - 1f;
                float saw2 = (p2 % 1f) * 2f - 1f;
                float saw3 = (p3 % 1f) * 2f - 1f;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                low += (noise - low) * 0.25f;
                float v = saw * 0.35f + saw2 * 0.22f + saw3 * 0.3f + low * 0.55f;
                v = (float)Math.Tanh(v * 2.6f);
                data[i] = v * env * 0.55f;
            }
            return Clip("CutsceneScream", data);
        }

        static AudioClip MakeHit()
        {
            float len = 0.9f;
            var data = new float[(int)(Rate * len)];
            var rng = new System.Random(3);
            float low = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float x = i / (float)Rate;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                low += (noise - low) * 0.12f;
                float crack = noise * Mathf.Exp(-x * 40f) * 0.6f;
                float body = low * Mathf.Exp(-x * 7f) * 1.4f;
                float boom = Thump(x, 0f, 1f) * 1.2f;
                data[i] = (float)Math.Tanh((crack + body + boom) * 1.8f) * 0.8f;
            }
            return Clip("CutsceneHit", data);
        }

        static AudioClip Clip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
