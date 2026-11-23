using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public static partial class MarketPrototypeBuilderV7
{
    // Composite employee space, not a surveyed store room. Existing exits and
    // floor height are preserved. Finishes use metre-sized modules.
    static void FinishStaffRoomEnvelope()
    {
        var vinyl=RetailSurface("staff grey vinyl tile v1",new Color(.53f,.55f,.53f),0,.37f);
        vinyl.mainTexture=FinishTexture("StaffVinylDiffuseV2",(u,v)=>{
            float edge=Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(v,1-v));
            float grain=Mathf.PerlinNoise(u*176+12,v*176+8);
            float chip=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.54f,.78f,grain))*.15f;
            float shade=.79f+chip-.04f*Mathf.PerlinNoise(u*15,v*15);
            if(edge<.0018f)shade*=.64f;
            return new Color(shade,shade,shade,1);
        });
        vinyl.SetTextureScale("_BaseMap",new Vector2(35.4f,27.4f));
        Box("staff resilient half metre vinyl floor",new Vector3(18,5.03f,38),new Vector3(17.7f,.04f,13.7f),vinyl);
        var acoustic=RetailSurface("staff acoustic ceiling tile v1",new Color(.73f,.74f,.70f),0,.10f);
        acoustic.mainTexture=FinishTexture("StaffAcousticDiffuseV2",(u,v)=>{
            float noise=Mathf.PerlinNoise(u*230+5,v*230+7);
            float value=.92f-.19f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,.8f,noise));
            return new Color(value,value,value,1);
        });
        acoustic.SetTextureScale("_BaseMap",new Vector2(29.5f,22.8f));
        Box("staff lowered acoustic ceiling",new Vector3(18,8.15f,38),new Vector3(17.7f,.10f,13.7f),acoustic);
        var grid=RetailSurface("staff ceiling T rail enamel",new Color(.70f,.72f,.70f),.1f,.21f);
        for(float x=9.15f;x<26.9f;x+=.60f)Decor("ceiling exposed T grid",PrimitiveType.Cube,new Vector3(x,8.094f,38),new Vector3(.019f,.010f,13.7f),grid);
        for(float z=31.15f;z<44.9f;z+=.60f)Decor("ceiling exposed T grid",PrimitiveType.Cube,new Vector3(18,8.094f,z),new Vector3(17.7f,.010f,.019f),grid);
        foreach(float x in new[]{9.14f,26.86f})Decor("staff skirting",PrimitiveType.Cube,new Vector3(x,5.105f,38),new Vector3(.025f,.11f,13.7f),dark);
        Decor("staff rear skirting",PrimitiveType.Cube,new Vector3(18,5.105f,44.83f),new Vector3(17.7f,.11f,.025f),dark);
        for(int i=0;i<6;i++) {
            var p=new Vector3(i%2==0?15.6f:22.2f,8.045f,34.8f+i/2*4.2f);
            bool working=i==2||i==5;
            Decor("staff recessed fluorescent housing",PrimitiveType.Cube,p,new Vector3(1.18f,.07f,.29f),shelfTop);
            Decor("staff prismatic diffuser",PrimitiveType.Cube,p+Vector3.down*.040f,new Vector3(1.10f,.012f,.24f),working?fluorescent:priceWhite);
            if(!working)continue;
            var lamp=new GameObject("staff remaining task light").AddComponent<Light>();lamp.transform.SetParent(root);lamp.transform.position=p+Vector3.down*.07f;
            lamp.transform.rotation=Quaternion.Euler(90,0,0);lamp.type=LightType.Spot;lamp.range=6.4f;lamp.spotAngle=118;lamp.innerSpotAngle=80;
            lamp.intensity=3.7f;lamp.color=new Color(.91f,.95f,1);lamp.shadows=LightShadows.Soft;lamp.shadowBias=.025f;lamp.shadowNormalBias=.035f;
            lamp.GetUniversalAdditionalLightData().softShadowQuality=SoftShadowQuality.Low;
            var setting=new SerializedObject(lamp.GetUniversalAdditionalLightData());setting.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=0;setting.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach(float x in new[]{12f,24f}) {
            Decor("staff return air frame",PrimitiveType.Cube,new Vector3(x,8.083f,39),new Vector3(.55f,.025f,.55f),grid);
            for(int i=0;i<16;i++)Decor("staff return air narrow slot",PrimitiveType.Cube,new Vector3(x,8.066f,38.77f+i*.030f),new Vector3(.47f,.004f,.010f),dark);
        }
        // A small real clock replaces the oversized placeholder wall writing.
        var clock=new Vector3(18.9f,7.33f,44.52f);
        var rim=Decor("staff clock rim",PrimitiveType.Cylinder,clock,new Vector3(.33f,.019f,.33f),shelfTop);rim.transform.rotation=Quaternion.Euler(90,0,0);
        var face=Decor("staff clock cream face",PrimitiveType.Cylinder,clock+Vector3.back*.021f,new Vector3(.302f,.003f,.302f),cream);face.transform.rotation=Quaternion.Euler(90,0,0);
        for(int i=0;i<12;i++) {
            float a=i*Mathf.PI/6;var tick=Decor("staff clock tick",PrimitiveType.Cube,clock+new Vector3(Mathf.Sin(a)*.127f,Mathf.Cos(a)*.127f,-.026f),new Vector3(.007f,.017f,.002f),dark);tick.transform.rotation=Quaternion.Euler(0,0,-i*30);
        }
        RailSegment("staff clock hour hand",clock+Vector3.back*.029f,clock+new Vector3(-.050f,.045f,-.029f),.008f,dark,root);
        RailSegment("staff clock minute hand",clock+Vector3.back*.031f,clock+new Vector3(.083f,.055f,-.031f),.005f,dark,root);
        // A second dining cluster fills usable area while retaining a clear aisle.
        var table=new Vector3(22.4f,5.05f,37.4f);
        var top=RetailSurface("staff oatmeal laminate",new Color(.59f,.58f,.51f),0,.26f);
        MerchBox("staff secondary dining top",table+Vector3.up*.724f,new Vector3(1.66f,.028f,.90f),top,null,true);
        foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})RailSegment("staff secondary table leg",table+new Vector3(side*.74f,.015f,end*.36f),table+new Vector3(side*.74f,.71f,end*.36f),.038f,shelf,root);
        for(int i=0;i<2;i++)foreach(int side in new[]{-1,1})RetailDiningChair(table+new Vector3(-.46f+i*.90f,0,side*(i==0?.85f:.78f)),side<0?177+i*4:5-i*7,3,RetailSurface("staff polypropylene chair",new Color(.22f,.30f,.31f),0,.22f),shelf);
        CeramicCup(table+new Vector3(.34f,.741f,-.16f),null,true);
        MerchBox("staff tissue box",table+new Vector3(-.43f,.80f,.04f),new Vector3(.23f,.12f,.12f),cream);
        // Low bench at the lockers, clear of their 104 degree door arcs.
        var bench=new Vector3(13.3f,5.05f,41.65f);
        MerchBox("locker changing bench seat",bench+Vector3.up*.43f,new Vector3(2.3f,.055f,.42f),OakVeneer(new Color(.78f,.73f,.60f)),null,true);
        foreach(float x in new[]{-.94f,.94f})foreach(float z in new[]{-.14f,.14f})RailSegment("locker bench tubular support",bench+new Vector3(x,.02f,z),bench+new Vector3(x,.41f,z),.032f,shelf,root);
    }
}
