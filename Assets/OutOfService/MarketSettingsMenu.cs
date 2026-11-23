using UnityEngine;

namespace OutOfService
{
    public sealed partial class MarketSettings
    {
        static readonly string[] ActionNames={"앞으로 이동","뒤로 이동","왼쪽 이동","오른쪽 이동","달리기","점프","상호작용 · 집기 · 놓기","손전등 켜기 / 끄기","맨손 선택","손전등 선택","물품 돌려보기 (누르고 유지)","실패 후 다시 시작"};
        GUIStyle heading,label,value,button,small,tabStyle;
        Texture2D buttonNormal,buttonHover,buttonActive;
        void InitStyles()
        {
            if(heading!=null)return;
            Texture2D Flat(Color color){var t=new Texture2D(1,1);t.SetPixel(0,0,color);t.Apply();return t;}
            buttonNormal=Flat(new Color(.16f,.20f,.20f));buttonHover=Flat(new Color(.23f,.32f,.30f));buttonActive=Flat(new Color(.29f,.43f,.38f));
            heading=new GUIStyle(GUI.skin.label){fontSize=31,fontStyle=FontStyle.Bold,normal={textColor=new Color(.94f,.95f,.90f)}};
            label=new GUIStyle(GUI.skin.label){fontSize=20,alignment=TextAnchor.MiddleLeft,normal={textColor=new Color(.85f,.88f,.85f)}};
            value=new GUIStyle(label){alignment=TextAnchor.MiddleCenter,fontSize=18};
            small=new GUIStyle(label){fontSize=16,wordWrap=true};
            button=new GUIStyle(GUI.skin.button){fontSize=18,alignment=TextAnchor.MiddleCenter,border=new RectOffset(0,0,0,0),normal={background=buttonNormal,textColor=Color.white},hover={background=buttonHover,textColor=Color.white},active={background=buttonActive,textColor=Color.white},focused={background=buttonHover,textColor=Color.white}};
            tabStyle=new GUIStyle(button){fontSize=21,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(20,8,0,0)};
        }
        void Fill(Rect r,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        bool Button(Rect r,string text)=>GUI.Button(r,text,button);
        int Choice(float y,string title,int selected,string[] choices)
        {
            GUI.Label(new Rect(670,y,330,42),title,label);
            if(Button(new Rect(1058,y+2,44,38),"‹"))selected=(selected+choices.Length-1)%choices.Length;
            GUI.Label(new Rect(1108,y,360,42),choices[Mathf.Clamp(selected,0,choices.Length-1)],value);
            if(Button(new Rect(1474,y+2,44,38),"›"))selected=(selected+1)%choices.Length;
            return selected;
        }
        float Slider(float y,string title,float current,float min,float max,string suffix="",bool integer=false)
        {
            GUI.Label(new Rect(670,y,350,42),title,label);
            current=GUI.HorizontalSlider(new Rect(1062,y+17,322,24),current,min,max);
            if(integer)current=Mathf.Round(current);
            GUI.Label(new Rect(1400,y,118,42),(integer?current.ToString("F0"):current.ToString("F2"))+suffix,value);return current;
        }
        internal void DrawMenu()
        {
            if(!open)return;InitStyles();var matrix=GUI.matrix;int depth=GUI.depth;GUI.depth=-100;
            float scale=Mathf.Min(Screen.width/1920f,Screen.height/1080f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1920*scale)*.5f,(Screen.height-1080*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);
            Fill(new Rect(-1920,-1080,5760,3240),new Color(.005f,.009f,.010f,.91f));
            Fill(new Rect(330,137,1260,806),new Color(.055f,.075f,.076f,.98f));
            Fill(new Rect(330,137,1260,5),new Color(.70f,.20f,.14f));
            GUI.Label(new Rect(370,173,800,48),"OUT OF SERVICE  /  설정",heading);
            GUI.Label(new Rect(370,226,800,30),"일시정지  ·  변경 후 적용하면 다음 실행에도 유지됩니다",small);
            bool enabled=GUI.enabled;bool blocked=DisplayPending||listening>=0;GUI.enabled=!blocked;
            string[] tabs={"화면 · 그래픽","마우스 · 조작","소리 · 기타"};
            for(int i=0;i<3;i++){
                if(tab==i)Fill(new Rect(366,294+i*62,242,49),new Color(.22f,.33f,.29f));
                if(GUI.Button(new Rect(370,296+i*62,234,45),tabs[i],tabStyle))SetTab(i);
            }
            Fill(new Rect(634,287,1,502),new Color(.22f,.28f,.27f));
            if(tab==0){
                string[] sizes=new string[resolutions.Count];int selected=0;
                for(int i=0;i<sizes.Length;i++){sizes[i]=resolutions[i].x+" × "+resolutions[i].y;if(resolutions[i].x==Draft.width&&resolutions[i].y==Draft.height)selected=i;}
                int resolution=Choice(293,"해상도",selected,sizes);Draft.width=resolutions[resolution].x;Draft.height=resolutions[resolution].y;
                Draft.displayMode=Choice(345,"화면 모드",Draft.displayMode,new[]{"창 모드","테두리 없는 전체 화면","전체 화면"});
                Draft.renderScale=Slider(397,"렌더링 해상도",Draft.renderScale*100,70,120," %",true)/100;
                Draft.shadowQuality=Choice(449,"그림자 품질",Draft.shadowQuality,new[]{"낮음","보통","높음"});
                Draft.textureQuality=Choice(501,"텍스처 품질",Draft.textureQuality,new[]{"높음 · 원본","보통","낮음"});
                Draft.antialiasing=Choice(553,"가장자리 다듬기",Draft.antialiasing,new[]{"끄기","FXAA","SMAA · 높음"});
                Draft.vSync=Choice(605,"수직 동기화",Draft.vSync?1:0,new[]{"끄기","켜기"})==1;
                GUI.enabled=!blocked&&!Draft.vSync;
                int[] caps={0,60,90,120,144,165,240};int cap=System.Array.IndexOf(caps,Draft.frameLimit);
                Draft.frameLimit=caps[Choice(657,"프레임 제한",cap,new[]{"제한 없음","60 FPS","90 FPS","120 FPS","144 FPS","165 FPS","240 FPS"})];
                GUI.enabled=!blocked;
                GUI.Label(new Rect(670,721,848,55),"화면 모드·해상도 변경 후 15초 안에 유지하지 않으면 이전 설정으로 복구됩니다.",small);
            }else if(tab==1){
                Draft.sensitivity=Slider(284,"마우스 감도",Draft.sensitivity,.10f,3," 배");
                Draft.fieldOfView=Slider(331,"시야각",Draft.fieldOfView,60,100,"°",true);
                Draft.invertY=Choice(378,"마우스 상하 반전",Draft.invertY?1:0,new[]{"끄기","켜기"})==1;
                for(int i=0;i<ActionNames.Length;i++){
                    int col=i/6,row=i%6;float x=670+col*438,y=442+row*48;
                    GUI.Label(new Rect(x,y,245,38),ActionNames[i],small);
                    if(Button(new Rect(x+247,y,166,36),listening==i?"입력 대기…":Draft.bindings[i].Display))BeginBinding((MarketAction)i);
                }
                GUI.Label(new Rect(670,750,848,30),"Esc: 메뉴 / 키 변경 취소 (고정)  ·  중복 키는 등록되지 않습니다.",small);
            }else{
                Draft.masterVolume=Slider(305,"전체 음량",Draft.masterVolume*100,0,100," %",true)/100;
                Draft.ambienceVolume=Slider(365,"환경음",Draft.ambienceVolume*100,0,100," %",true)/100;
                Draft.pauseOnFocusLoss=Choice(425,"다른 창 사용 시 일시정지",Draft.pauseOnFocusLoss?1:0,new[]{"끄기","켜기"})==1;
                GUI.Label(new Rect(670,510,830,65),"전체 음량과 환경음은 게임으로 돌아가면 적용된 크기로 재생됩니다.",small);
                if(Button(new Rect(670,615,250,45),"처음부터 다시 시작"))RestartGame();
                if(Button(new Rect(940,615,250,45),"게임 종료"))Application.Quit();
            }
            GUI.enabled=!blocked;
            GUI.Label(new Rect(370,809,1150,49),Status,small);
            if(Button(new Rect(370,873,202,42),"기본값 불러오기"))RestoreDefaults();
            if(Button(new Rect(1094,873,202,42),"적용 · 저장"))ApplyDraft();
            if(Button(new Rect(1314,873,238,42),"게임으로 돌아가기"))Close();
            GUI.enabled=enabled;
            if(DisplayPending){
                Fill(new Rect(330,137,1260,806),new Color(0,0,0,.76f));Fill(new Rect(610,376,700,286),new Color(.08f,.12f,.12f));
                GUI.Label(new Rect(648,410,620,45),"이 화면 설정을 유지할까요?",heading);
                GUI.Label(new Rect(648,469,620,65),Mathf.CeilToInt(Mathf.Max(0,confirmUntil-Time.realtimeSinceStartup))+"초 후 자동으로 이전 화면으로 돌아갑니다.",label);
                if(Button(new Rect(648,573,290,46),"유지 · 저장"))ConfirmDisplay();
                if(Button(new Rect(960,573,310,46),"이전 화면으로 되돌리기"))RevertDisplay();
            }
            if(listening>=0){
                Fill(new Rect(330,137,1260,806),new Color(0,0,0,.76f));Fill(new Rect(610,376,700,286),new Color(.08f,.12f,.12f));
                GUI.Label(new Rect(648,410,620,45),"새 키 또는 마우스 버튼 입력",heading);
                GUI.Label(new Rect(648,470,620,45),ActionNames[listening]+"  ·  Esc로 취소",label);
                GUI.Label(new Rect(648,535,620,84),Status,small);
            }
            GUI.matrix=matrix;GUI.depth=depth;
        }
    }
}
