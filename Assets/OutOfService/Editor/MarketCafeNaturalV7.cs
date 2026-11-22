using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    static Mesh CafeHollowCup(bool pitcher=false)
    {
        return MallMesh(pitcher?"milk_pitcher_v1":"ceramic_hollow_v1",(v,u,t)=>{
            float[] ys={0,.004f,.010f,.020f,.065f,.092f,.096f,.096f,.090f,.064f,.020f,.014f,.014f};
            float[] rs={.027f,.030f,.032f,.034f,.041f,.045f,.045f,.041f,.041f,.037f,.030f,.028f,0};
            const int count=28;
            for(int r=0;r<ys.Length;r++)for(int c=0;c<=count;c++){
                float a=c*Mathf.PI*2/count;
                float spout=pitcher?Mathf.Pow(Mathf.Max(0,-Mathf.Cos(a)),18)*Mathf.Pow(ys[r]/.096f,7):0;
                v.Add(new Vector3(Mathf.Sin(a)*rs[r],ys[r]+spout*.008f,Mathf.Cos(a)*(rs[r]+spout*.012f)));
                u.Add(new Vector2(c/(float)count,r/(float)(ys.Length-1)));
                if(r<ys.Length-1&&c<count){int k=r*(count+1)+c;t.AddRange(new[]{k,k+1,k+count+1,k+1,k+count+2,k+count+1});}
            }
        });
    }

    static void CafeServiceDetails(Vector3 p)
    {
        var steel=CafeBrushedSteel();
        var rubber=RetailSurface("espresso moulded rubber",new Color(.028f,.031f,.030f),0,.16f);
        // These are an original commercial machine design, not branded replicas.
        MerchBox("espresso brushed control fascia",p+new Vector3(0,.447f,-.049f),new Vector3(.80f,.15f,.008f),steel);
        var gauge=p+new Vector3(0,.447f,-.060f);
        Decor("espresso pressure gauge face",PrimitiveType.Cylinder,gauge,new Vector3(.065f,.0015f,.065f),priceWhite).transform.rotation=Quaternion.Euler(90,0,0);
        DetailRing("gauge steel bezel",gauge+Vector3.back*.002f,new Vector3(.071f,.071f,.023f),steel);
        for(int i=0;i<9;i++){
            float a=(-130+i*32.5f)*Mathf.Deg2Rad;
            var d=new Vector3(Mathf.Sin(a),Mathf.Cos(a),0);
            RailSegment("pressure scale tick",gauge+d*.023f+Vector3.back*.004f,gauge+d*.027f+Vector3.back*.004f,.0015f,dark,root);
        }
        RailSegment("pressure gauge needle",gauge+Vector3.back*.006f,gauge+new Vector3(.014f,.016f,-.006f),.002f,dark,root);
        foreach(int side in new[]{-1,1}){
            var knob=p+new Vector3(side*.355f,.448f,-.083f);
            Decor("steam valve moulded knob",PrimitiveType.Cylinder,knob,new Vector3(.064f,.025f,.064f),rubber).transform.rotation=Quaternion.Euler(90,0,0);
            Decor("steam valve metal center",PrimitiveType.Cylinder,knob+Vector3.back*.026f,new Vector3(.027f,.001f,.027f),steel).transform.rotation=Quaternion.Euler(90,0,0);
            for(int i=0;i<5;i++)MerchBox("side panel cooling slot",p+new Vector3(side*.456f,.30f+i*.025f,.12f),new Vector3(.001f,.006f,.19f),dark);
            RailSegment("warming tray side guard",p+new Vector3(side*.39f,.63f,-.03f),p+new Vector3(side*.39f,.63f,.25f),.008f,steel,root);
        }
        RailSegment("warming tray rear guard",p+new Vector3(-.39f,.63f,.25f),p+new Vector3(.39f,.63f,.25f),.008f,steel,root);
        var pitcher=p+new Vector3(-.62f,0,-.12f);
        HandmadeProp("milk pitcher open spout",CafeHollowCup(true),pitcher,new Vector3(1.2f,1.4f,1.2f),steel,root);
        DetailRing("milk pitcher handle",pitcher+new Vector3(.065f,.074f,.012f),new Vector3(.055f,.075f,.045f),steel);
        var station=p+new Vector3(-.62f,.006f,.15f);
        MerchBox("rubber tamping mat",station,new Vector3(.24f,.009f,.17f),rubber);
        Decor("tamper flat base",PrimitiveType.Cylinder,station+new Vector3(.04f,.012f,0),new Vector3(.060f,.008f,.060f),steel);
        HandmadeProp("tamper rounded grip",TurnedPackageMesh("tamper_grip_v1",new[]{0f,.18f,.65f,.85f,1f},new[]{.23f,.22f,.48f,.50f,.31f}),station+new Vector3(.04f,.02f,0),new Vector3(.045f,.060f,.045f),rubber,root);
        // A single surface in the hopper, with bean-sized texture relief in colour;
        // no per-bean objects or transparent stacks.
        var beans=RetailSurface("coffee bean fill v1",new Color(.20f,.105f,.055f),0,.22f);
        beans.mainTexture=FinishTexture("RoastedBeansV1",(u,v)=>{
            float row=Mathf.Floor(v*15),x=Mathf.Repeat(u*18+row*.47f,1)-.5f,y=Mathf.Repeat(v*15,1)-.5f;
            float edge=Mathf.Clamp01(1-(x*x*5+y*y*2.8f));
            float seam=Mathf.Exp(-Mathf.Pow((x+.045f*Mathf.Sin(y*9))*24,2))*.25f;
            float b=.46f+edge*.43f-seam;
            return new Color(b,b*.88f,b*.72f,1);
        });
        Decor("roasted coffee in hopper",PrimitiveType.Cylinder,p+new Vector3(.80f,.515f,.05f),new Vector3(.206f,.072f,.206f),beans);
    }
}
