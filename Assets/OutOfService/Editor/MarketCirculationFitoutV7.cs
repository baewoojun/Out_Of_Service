using UnityEngine;
using OutOfService;

public static partial class MarketPrototypeBuilderV7
{
    static void StorageSlidingDoor(string name,Vector3 p,float width,bool cold)
    {
        var steel=PrepSteel();var skin=cold?priceWhite:MallFinish("enamel",new Color(.38f,.43f,.40f),.15f,.36f);
        float height=2.60f,travel=width+.18f;
        Decor(name+" overhead track housing",PrimitiveType.Cube,p+new Vector3(-travel*.5f,height+.18f,0),new Vector3(width+travel+.2f,.16f,.20f),steel);
        foreach(int side in new[]{-1,1}){
            Decor(name+" fixed jamb",PrimitiveType.Cube,p+new Vector3(side*(width*.5f+.05f),height*.5f,.12f),new Vector3(.08f,height,.12f),steel);
            Decor(name+" fixed seal",PrimitiveType.Cube,p+new Vector3(side*width*.5f,height*.5f,.035f),new Vector3(.025f,height,.025f),dark);
        }
        var leaf=new GameObject(name).transform;leaf.SetParent(root);leaf.position=p;
        leaf.gameObject.AddComponent<MarketSlidingDoor>().openOffset=Vector3.left*travel;
        Box(name+" translating insulated leaf",p+new Vector3(0,height*.5f,0),new Vector3(width+.04f,height,.12f),skin,leaf);
        foreach(int face in new[]{-1,1}){
            Decor(name+" steel kick plate",PrimitiveType.Cube,p+new Vector3(0,.24f,face*.068f),new Vector3(width-.06f,.40f,.012f),steel,leaf);
            foreach(float dx in new[]{-.50f,.50f})Decor(name+" folded vertical edge",PrimitiveType.Cube,p+new Vector3(dx*width,height*.5f,face*.065f),new Vector3(.025f,height-.04f,.014f),steel,leaf);
            var h=p+new Vector3(width*.5f-.23f,1.16f,face*.15f);
            RailSegment(name+" vertical pull grip",h+Vector3.down*.16f,h+Vector3.up*.16f,.025f,steel,leaf);
            foreach(float dy in new[]{-.16f,.16f})RailSegment(name+" handle mount",h+Vector3.up*dy,h+Vector3.up*dy+Vector3.back*face*.085f,.021f,steel,leaf);
        }
        foreach(float x in new[]{-width*.30f,width*.30f}){
            Decor(name+" roller hanger",PrimitiveType.Cube,p+new Vector3(x,height+.07f,0),new Vector3(.04f,.14f,.06f),steel,leaf);
        }
        var old=root;root=leaf;
        Label(cold?"냉동창고  ← 밀어서 열기":"물품창고  ← 밀어서 열기",p+new Vector3(0,1.90f,-.071f),Vector3.zero,.15f);
        root.GetChild(root.childCount-1).GetComponent<TextMesh>().color=cold?new Color(.12f,.22f,.25f):new Color(.9f,.9f,.82f);
        root=old;
        // Flush sill is visual only, so pallet trolleys and the controller see no artificial step.
        Decor(name+" flush floor sill",PrimitiveType.Cube,p+new Vector3(0,.007f,0),new Vector3(width,.014f,.35f),steel);
    }

    static void DoorCasingDetail(string name,Vector3 p,float width,Transform leaf)
    {
        var steel=PrepSteel();
        foreach(float x in new[]{-.055f,width+.055f}){
            Decor(name+" folded steel jamb",PrimitiveType.Cube,p+new Vector3(x,1.24f,0),new Vector3(.07f,2.48f,.19f),steel);
            Decor(name+" perimeter compression gasket",PrimitiveType.Cube,p+new Vector3(x<0?-.013f:width+.013f,1.22f,0),new Vector3(.012f,2.44f,.10f),dark);
            foreach(float y in new[]{.22f,1.2f,2.24f})foreach(int face in new[]{-1,1})
                Decor(name+" frame anchor screw",PrimitiveType.Sphere,p+new Vector3(x,y,face*.098f),new Vector3(.014f,.014f,.004f),steel);
        }
        Decor(name+" frame head",PrimitiveType.Cube,p+new Vector3(width*.5f,2.48f,0),new Vector3(width+.18f,.075f,.19f),steel);
        foreach(int face in new[]{-1,1}){
            foreach(float dx in new[]{-.135f,.135f})Decor(name+" vision frame side",PrimitiveType.Cube,p+new Vector3(width*.5f+dx,1.8f,face*.058f),new Vector3(.025f,.55f,.016f),steel,leaf);
            foreach(float y in new[]{1.535f,2.065f})Decor(name+" vision frame crosspiece",PrimitiveType.Cube,p+new Vector3(width*.5f,y,face*.058f),new Vector3(.29f,.025f,.016f),steel,leaf);
        }
        Decor(name+" latch edge mortise",PrimitiveType.Cube,p+new Vector3(width-.025f,1.10f,0),new Vector3(.012f,.16f,.055f),steel,leaf);
    }

    static void StockroomOpenRack(Vector3 p,float width,bool cold)
    {
        var frame=PrepSteel();
        foreach(int side in new[]{-1,1})foreach(int depth in new[]{-1,1}){
            var leg=p+new Vector3(side*(width*.5f-.04f),1.35f,depth*.52f);
            Box("warehouse uprights",leg,new Vector3(.055f,2.7f,.055f),frame);
            Decor("warehouse bolted foot",PrimitiveType.Cube,leg+Vector3.down*1.32f,new Vector3(.15f,.06f,.15f),frame);
        }
        for(int tier=0;tier<4;tier++){
            float y=.17f+tier*.78f;
            Box("warehouse open shelf deck",p+Vector3.up*y,new Vector3(width,.045f,1.10f),cold?frame:shelf);
            foreach(int edge in new[]{-1,1})Decor("warehouse load beam",PrimitiveType.Cube,p+new Vector3(0,y-.055f,edge*.52f),new Vector3(width,.065f,.045f),cold?frame:RetailSurface("stock rack safety orange",new Color(.63f,.24f,.06f),.08f,.26f));
        }
        foreach(int side in new[]{-1,1})RailSegment("warehouse rear diagonal brace",p+new Vector3(side*width*.46f,.2f,.54f),p+new Vector3(-side*width*.46f,2.55f,.54f),.018f,frame,root);
    }

    static void StairFlightDetail(string side,float cx,float low,float startZ,float dir)
    {
        var concrete=MallFinish("washable",new Color(.37f,.39f,.36f),0,.12f);
        var soffit=Decor(side+" cast stair flight soffit",PrimitiveType.Cube,new Vector3(cx,low+1.12f,startZ+dir*2.5f),new Vector3(2.18f,.16f,5.59f),concrete);
        soffit.transform.rotation=Quaternion.Euler(dir<0?26.565f:-26.565f,0,0);
        for(int step=0;step<15;step++){
            float y=low+(step+1)/6f,z=startZ+dir*(step+.5f)/3f;
            Decor(side+" aluminium stair nose edge",PrimitiveType.Cube,new Vector3(cx,y-.010f,z-dir*.16f),new Vector3(2.16f,.024f,.016f),PrepSteel());
            foreach(int end in new[]{-1,1})Decor(side+" stair nose fixing",PrimitiveType.Cylinder,new Vector3(cx+end*.98f,y+.007f,z-dir*.14f),new Vector3(.012f,.002f,.012f),PrepSteel());
        }
        foreach(int edge in new[]{-1,1}){
            Vector3 a=new(cx+edge*1.12f,low+1,startZ),b=new(a.x,low+3.5f,startZ+dir*5);
            RailSegment(side+" guard intermediate rail",a-Vector3.up*.43f,b-Vector3.up*.43f,.022f,PrepSteel(),root);
            foreach(var end in new[]{a,b}){
                RailSegment(side+" safe handrail downward return",end,end-Vector3.up*.16f,.045f,PrepSteel(),root);
            }
            for(int post=0;post<=5;post++){
                var q=Vector3.Lerp(a,b,post/5f)-Vector3.up*.94f;
                Decor(side+" baluster anchor plate",PrimitiveType.Cube,q,new Vector3(.14f,.035f,.14f),PrepSteel());
            }
        }
    }
}
