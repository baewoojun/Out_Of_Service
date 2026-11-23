using UnityEngine;
using UnityEditor;

public static partial class MarketPrototypeBuilderV7
{
    static Mesh GarmentButtons(int style)
    {
        return MallMesh("sewn_button_strip_v2_"+style,(v,u,t)=>{
            int count=style==1?5:7;
            for(int i=0;i<count;i++) {
                float y=style==1?-.36f-i*.12f:-.137f-i*.082f;
                float radius=style==1?.006f:.0044f,z=LaundryFrontDepth(style,0,y)-.0046f;
                int start=v.Count;v.Add(new Vector3(0,y,z-.0008f));u.Add(new Vector2(.5f,.5f));
                for(int n=0;n<=16;n++) {
                    float a=n*Mathf.PI*2/16;
                    v.Add(new Vector3(Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,z));u.Add(new Vector2(.5f+.5f*Mathf.Cos(a),.5f+.5f*Mathf.Sin(a)));
                    if(n<16)t.AddRange(new[]{start,start+n+2,start+n+1});
                }
            }
        });
    }
    static void GarmentDetails(Vector3 neck,Quaternion rotation,float scale,int style,Transform parent)
    {
        var thread=RetailSurface("tailoring muted cotton thread",new Color(.39f,.40f,.37f),0,.12f);
        var seam=HandmadeProp("stitched placket pocket and hem",LaundrySeams(style),neck,Vector3.one*scale,thread,parent);
        seam.transform.rotation=rotation;
        if(style==2)return;
        var button=RetailSurface("sewn ivory four hole button",new Color(.78f,.79f,.74f),0,.32f);
        button.mainTexture=FinishTexture("ButtonFourHolesV1",(u,v)=>{
            float dx=Mathf.Abs(u-.5f)-.10f,dy=Mathf.Abs(v-.5f)-.10f;
            float nearest=Mathf.Sqrt(dx*dx+dy*dy);
            float value=Mathf.Lerp(.10f,.96f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.030f,.055f,nearest)));
            return new Color(value,value,value,1);
        });
        var buttons=HandmadeProp("stitched buttons",GarmentButtons(style),neck,Vector3.one*scale,button,parent);buttons.transform.rotation=rotation;
    }
    static Material GarmentTagInk()
    {
        var mat=RetailSurface("original SOOIM clothing hang tag",Color.white,0,.12f);
        mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Prints/sooim-hangtag.png");return mat;
    }
}
