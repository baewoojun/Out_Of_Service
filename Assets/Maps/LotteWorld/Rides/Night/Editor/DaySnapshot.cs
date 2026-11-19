// DaySnapshot / NightSnapshot 공용 조명 스냅샷 (파일명=클래스명 필수)
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LotteWorld.Night
{
    public class DaySnapshot : ScriptableObject
    {
        public bool nightApplied;
        public string scenePath;

        public Material skybox;
        public AmbientMode ambientMode;
        public float ambientIntensity;
        public Color ambientSky, ambientEquator, ambientGround, ambientLight;
        public DefaultReflectionMode defaultReflectionMode;
        public float reflectionIntensity;
        public bool fog;
        public FogMode fogMode;
        public Color fogColor;
        public float fogDensity, fogStart, fogEnd;
        public string sunPath;

        [Serializable]
        public class DirSnap
        {
            public string path;
            public bool enabled, goActive;
            public Color color;
            public float intensity;
            public LightShadows shadows;
            public LightShadowResolution shadowRes;
            public float shadowStrength;
        }
        public List<DirSnap> directionals = new List<DirSnap>();

        [Serializable]
        public class LightSnap
        {
            public string path;
            public bool enabled, goActive;
            public Color color;
            public float intensity, range;
            public LightShadows shadows;
            public bool hadFlicker;
            public bool weAddedFlicker;
        }
        public List<LightSnap> lights = new List<LightSnap>();

        public List<string> emissionRendererPaths = new List<string>();

        [Serializable]
        public class RendererMatSnap
        {
            public string path;
            public Material[] materials;
        }
        public List<RendererMatSnap> ceilLightRenderers = new List<RendererMatSnap>();

        public bool createdNightVolume;
        public bool hasNightVolume;
        public bool mainCamHadPost;
        public string mainCamPath;
        public string urpRendererPath;
        public RenderingMode urpPrevMode;
        public bool urpModeChanged;
        public string skyboxAssetPath;
    }
}
