using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace OutOfService
{
    public enum MarketAction { Forward, Backward, Left, Right, Sprint, Jump, Interact, Flashlight, Hands, TorchSlot, Inspect, Restart }

    [Serializable]
    public struct MarketBinding
    {
        public Key key;
        public int mouse;
        public MarketBinding(Key value){key=value;mouse=-1;}
        public static MarketBinding Mouse(int button)=>new(){key=Key.None,mouse=button};
        public bool Valid=>mouse>=0?mouse<=4:key!=Key.None&&key!=Key.Escape&&Enum.IsDefined(typeof(Key),key);
        public bool Same(MarketBinding other)=>mouse==other.mouse&&(mouse>=0||key==other.key);
        public ButtonControl Control {
            get {
                // Bindings are validated when loading/applying, not in the per-frame input path.
                if(mouse<0)return Keyboard.current!=null&&key!=Key.None?Keyboard.current[key]:null;
                var m=UnityEngine.InputSystem.Mouse.current;if(m==null)return null;
                return mouse switch{0=>m.leftButton,1=>m.rightButton,2=>m.middleButton,3=>m.backButton,4=>m.forwardButton,_=>null};
            }
        }
        public string Display=>mouse>=0?new[]{"마우스 왼쪽","마우스 오른쪽","마우스 가운데","마우스 뒤로","마우스 앞으로"}[Mathf.Clamp(mouse,0,4)]:key switch {
            Key.LeftShift=>"왼쪽 Shift",Key.RightShift=>"오른쪽 Shift",Key.LeftCtrl=>"왼쪽 Ctrl",Key.RightCtrl=>"오른쪽 Ctrl",Key.LeftAlt=>"왼쪽 Alt",Key.RightAlt=>"오른쪽 Alt",Key.Space=>"Space",Key.Digit1=>"1",Key.Digit2=>"2",_=>Control?.displayName??key.ToString() };
    }

    [Serializable]
    public sealed class MarketSettingsData
    {
        public int version=1,width=1920,height=1080,displayMode=0,frameLimit=120,shadowQuality=2,textureQuality=0,antialiasing=2;
        public bool vSync,invertY,pauseOnFocusLoss=true;
        public float renderScale=1,fieldOfView=75,sensitivity=1,masterVolume=1,ambienceVolume=1;
        public MarketBinding[] bindings=DefaultBindings();
        public static MarketBinding[] DefaultBindings()=>new[]{new MarketBinding(Key.W),new(Key.S),new(Key.A),new(Key.D),new(Key.LeftShift),new(Key.Space),new(Key.E),new(Key.F),new(Key.Digit1),new(Key.Digit2),MarketBinding.Mouse(1),new(Key.R)};
        public MarketSettingsData Copy()=>JsonUtility.FromJson<MarketSettingsData>(JsonUtility.ToJson(this));
        public void Validate()
        {
            version=1;width=Mathf.Clamp(width,960,7680);height=Mathf.Clamp(height,540,4320);displayMode=Mathf.Clamp(displayMode,0,2);
            frameLimit=Array.IndexOf(new[]{0,60,90,120,144,165,240},frameLimit)>=0?frameLimit:120;
            shadowQuality=Mathf.Clamp(shadowQuality,0,2);textureQuality=Mathf.Clamp(textureQuality,0,2);antialiasing=Mathf.Clamp(antialiasing,0,2);
            float Safe(float value,float fallback,float min,float max)=>float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp(value,min,max);
            renderScale=Safe(renderScale,1,.70f,1.20f);fieldOfView=Safe(fieldOfView,75,60,100);sensitivity=Safe(sensitivity,1,.10f,3);
            masterVolume=Safe(masterVolume,1,0,1);ambienceVolume=Safe(ambienceVolume,1,0,1);
            var defaults=DefaultBindings();bool invalid=bindings==null||bindings.Length!=defaults.Length;
            if(!invalid)for(int i=0;i<bindings.Length;i++){
                if(!bindings[i].Valid){invalid=true;break;}
                for(int j=0;j<i;j++)if(bindings[i].Same(bindings[j]))invalid=true;
            }
            if(invalid)bindings=defaults;
        }
        public bool SameDisplay(MarketSettingsData other)=>width==other.width&&height==other.height&&displayMode==other.displayMode;
    }

    public static class MarketSettingsStore
    {
        public static MarketSettingsData Load(string path,out string warning)
        {
            warning="";if(!File.Exists(path))return new MarketSettingsData();
            try {
                var data=JsonUtility.FromJson<MarketSettingsData>(File.ReadAllText(path));
                if(data==null||data.version!=1)throw new InvalidDataException("Unsupported settings version");
                data.Validate();return data;
            }catch(Exception error)when(error is IOException||error is ArgumentException||error is UnauthorizedAccessException){
                warning="설정 파일을 읽지 못해 기본값을 불러왔습니다. 적용하면 새로 저장됩니다.";return new MarketSettingsData();
            }
        }
        public static bool Save(string path,MarketSettingsData data,out string error)
        {
            error="";
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporary=path+".tmp";File.WriteAllText(temporary,JsonUtility.ToJson(data,true));
                if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);
                return true;
            }catch(Exception e)when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException){error="설정을 저장하지 못했습니다. 변경 전 상태로 돌아갑니다.";return false;}
        }
    }

    public static class MarketControls
    {
        static readonly MarketSettingsData defaults=new();
        static MarketSettingsData Data=>MarketSettings.Instance?MarketSettings.Instance.Current:defaults;
        public static bool Held(MarketAction action)=>!MarketSettings.InputBlocked&&(Data.bindings[(int)action].Control?.isPressed??false);
        public static bool Pressed(MarketAction action)=>!MarketSettings.InputBlocked&&(Data.bindings[(int)action].Control?.wasPressedThisFrame??false);
        public static string Label(MarketAction action)=>Data.bindings[(int)action].Display;
        public static float Sensitivity=>Data.sensitivity;
        public static float VerticalSign=>Data.invertY?-1:1;
    }
}
