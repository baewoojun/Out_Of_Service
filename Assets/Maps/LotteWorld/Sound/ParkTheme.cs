// ParkTheme.cs - 망가진 롯데월드 테마송을 공원 스피커로 트는 관리자. LotteWorldSfx.cs 가 롯데월드 씬에 자동으로 만듦.
//
// 흐름: 놀이기구가 켜지면(RideDirector.ActivateAll, 테스트 F8) firstDelay 뒤에 한 번 크게 틀고,
//       그 뒤로는 끝날 때마다 gapMin~gapMax 초 쉬었다가 조금 작게 다시 틂.
// 팀원이 원하는 순간에 틀고 싶으면:  LotteWorld.Sound.ParkTheme.PlayNow();   그만:  ParkTheme.StopAll();
// 소리 파일: Resources/LotteWorldSfx/park_theme_creepy.wav  (Tools/MakeCreepyTheme.py 로 다시 만듦)
using LotteWorld.Rides;
using UnityEngine;

namespace LotteWorld.Sound
{
    public class ParkTheme : MonoBehaviour
    {
        [Tooltip("놀이기구가 켜지고 노래가 나오기까지 (초). 덜컥 소리 뒤에 나오게")] public float firstDelay = 2.5f;
        [Range(0, 1)] public float firstVolume = 0.75f;
        [Range(0, 1)] public float repeatVolume = 0.45f;
        public float gapMin = 25f, gapMax = 45f;
        [Tooltip("켜지면 놀이기구를 기다리지 않고 바로 반복 재생")] public bool playWithoutRides = false;

        static ParkTheme instance;
        AudioSource src;
        float nextPlay = -1f;
        bool firstDone;

        void Awake()
        {
            instance = this;
            src = gameObject.AddComponent<AudioSource>();
            src.clip = LotteWorldSfx.Clip("park_theme_creepy");
            src.playOnAwake = false; src.loop = false;
            src.spatialBlend = 0f;                     // 공원 전체 스피커: 어디서나 같은 크기
            src.priority = 0;                          // 다른 소리가 많아도 끊기지 않게
        }

        void OnEnable() { RideDirector.Activated += OnRides; }
        void OnDisable() { RideDirector.Activated -= OnRides; }
        void Start() { if (RideDirector.Active || playWithoutRides) OnRides(); }

        void OnRides() { if (nextPlay < 0f) nextPlay = Time.time + firstDelay; }

        void Update()
        {
            if (nextPlay < 0f || src.isPlaying || Time.time < nextPlay || !src.clip) return;
            src.volume = firstDone ? repeatVolume : firstVolume;
            src.Play();
            firstDone = true;
            nextPlay = Time.time + src.clip.length + Random.Range(gapMin, gapMax);
        }

        public static void PlayNow()
        {
            if (!instance) return;
            instance.src.Stop();
            instance.nextPlay = Time.time;
        }

        public static void StopAll()
        {
            if (!instance) return;
            instance.src.Stop();
            instance.nextPlay = -1f;
        }
    }
}
