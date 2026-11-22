using UnityEngine;
using OutOfService;

public static partial class MarketPrototypeBuilderV7
{
    static void TopLoadWasher(Vector3 p)
    {
        var enamel=RetailSurface("top load washer enamel",new Color(.62f,.64f,.61f),.12f,.42f);
        foreach(int side in new[]{-1,1})MerchBox("top loader side cabinet",p+new Vector3(side*.305f,.49f,0),new Vector3(.035f,.94f,.68f),enamel,root,true);
        foreach(int side in new[]{-1,1})MerchBox("top loader front rear panel",p+new Vector3(0,.49f,side*.322f),new Vector3(.60f,.94f,.035f),enamel,root,true);
        Box("washer recessed feet",p+Vector3.up*.04f,new Vector3(.55f,.08f,.58f),dark);
        HandmadeProp("top loader open inner drum",TurnedPackageMesh("OpenWasherDrumV1",
            new[]{.31f,.31f,.40f,.91f,.94f,.94f,.91f,.40f,.35f,.35f},
            new[]{0f,.21f,.26f,.26f,.255f,.235f,.23f,.20f,.18f,0f}),p,Vector3.one,PrepSteel(),root);
        foreach(int side in new[]{-1,1}){
            MerchBox("washer top deck side",p+new Vector3(side*.285f,.96f,0),new Vector3(.08f,.065f,.68f),enamel);
            MerchBox("washer deck front rear",p+new Vector3(0,.96f,side*.29f),new Vector3(.49f,.065f,.10f),enamel);
        }
        var hinge=new GameObject("top loader lid - E").transform;hinge.SetParent(root);
        hinge.position=p+new Vector3(0,1.004f,.24f);hinge.rotation=Quaternion.Euler(0,0,90);
        hinge.gameObject.AddComponent<MarketSwingDoor>().openAngle=-100;
        foreach(int side in new[]{-1,1}){
            Box("washer lid side frame",p+new Vector3(side*.255f,1.004f,-.015f),new Vector3(.035f,.030f,.52f),shelf,hinge);
            Box("washer lid cross frame",p+new Vector3(0,1.004f,-.015f+side*.245f),new Vector3(.49f,.030f,.035f),shelf,hinge);
        }
        Box("washer lid smoked glass",p+new Vector3(0,1.004f,-.015f),new Vector3(.476f,.016f,.455f),cabinetGlass,hinge);
        Box("washer lid recessed pull",p+new Vector3(0,1.018f,-.25f),new Vector3(.15f,.019f,.038f),shelfTop,hinge);
        MerchBox("washer raised rear control console",p+new Vector3(0,1.025f,.29f),new Vector3(.56f,.10f,.095f),shelf);
        for(int n=0;n<5;n++)Decor("washer cycle button",PrimitiveType.Cylinder,p+new Vector3(-.19f+n*.075f,1.082f,.28f),new Vector3(.025f,.006f,.025f),shelfTop);
        MerchBox("washer cycle display",p+new Vector3(.18f,1.08f,.28f),new Vector3(.10f,.006f,.035f),dark);
    }
}
