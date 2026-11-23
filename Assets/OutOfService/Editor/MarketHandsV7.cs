using UnityEngine;
public static partial class MarketPrototypeBuilderV7
{
    static void SculptHeldHand(Transform parent,bool gripping)
    {
        // Smooth anatomical masses: tapered wrist, palm, four articulated fingers and opposed thumb.
        Held(parent,"cloth forearm",PrimitiveType.Capsule,new Vector3(.42f,-.47f,.65f),new Vector3(.16f,.21f,.17f),dark,new Vector3(25,10,18));
        Held(parent,"ribbed cuff",PrimitiveType.Cylinder,new Vector3(.39f,-.32f,.74f),new Vector3(.145f,.035f,.15f),fabric,new Vector3(25,10,18));
        Held(parent,"wrist",PrimitiveType.Capsule,new Vector3(.38f,-.29f,.76f),new Vector3(.105f,.08f,.10f),skin,new Vector3(25,10,18));
        Held(parent,"palm",PrimitiveType.Sphere,new Vector3(.38f,-.24f,.80f),new Vector3(.14f,.16f,.085f),skin,new Vector3(12,0,8));
        for(int finger=0;finger<4;finger++)
        {
            float x=.325f+finger*.033f;
            float length=finger==3?.054f:finger==0?.066f:.075f;
            Vector3 a=new Vector3(x,-.19f,.805f);
            Vector3 b=a+new Vector3(0,gripping?.018f:.043f,length*.55f);
            Vector3 c=b+new Vector3(0,gripping?-.032f:.013f,length*.40f);
            HeldFinger(parent,"proximal finger",a,b,.031f);
            HeldFinger(parent,"curled fingertip",b,c,.027f);
        }
        HeldFinger(parent,"opposed thumb",new Vector3(.30f,-.25f,.81f),new Vector3(.30f,-.205f,.86f),.041f);
        HeldFinger(parent,"thumb tip",new Vector3(.30f,-.205f,.86f),new Vector3(.325f,-.19f,.875f),.035f);
    }
    static void HeldFinger(Transform parent,string name,Vector3 a,Vector3 b,float width)
    {
        var o=Held(parent,name,PrimitiveType.Capsule,(a+b)*.5f,new Vector3(width,(b-a).magnitude*.5f+width*.35f,width),skin,Vector3.zero);
        o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
    }
}
