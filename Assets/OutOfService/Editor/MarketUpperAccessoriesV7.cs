using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    static Mesh RetailBagBody(bool hard)
    {
        return MallMesh("retail_travel_bag_v1_"+hard,(v,u,t)=>{
            const int rings=10,sides=40;
            for(int r=0;r<=rings;r++)for(int s=0;s<sides;s++){
                float h=r/(float)rings,a=s*2*Mathf.PI/sides,c=Mathf.Cos(a),sn=Mathf.Sin(a);
                float taper=hard?1:1-.16f*h;
                float rim=r==0||r==rings?.86f:1;
                float x=Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),.28f)*.5f*taper*rim;
                float z=Mathf.Sign(sn)*Mathf.Pow(Mathf.Abs(sn),.28f)*.5f*rim;
                if(!hard)z+=.018f*Mathf.Sin(a*7+h*11)*Mathf.Sin(h*Mathf.PI);
                v.Add(new Vector3(x,h,z));u.Add(new Vector2(s/(float)sides,h));
                if(r==rings)continue;int k=r*sides+s,n=r*sides+(s+1)%sides;
                t.AddRange(new[]{k,k+sides,n,n,k+sides,n+sides});
            }
            int b=v.Count;v.Add(Vector3.zero);u.Add(new(.5f,.5f));int top=v.Count;v.Add(Vector3.up);u.Add(new(.5f,.5f));
            for(int s=0;s<sides;s++){int n=(s+1)%sides;t.AddRange(new[]{b,s,n,top,rings*sides+n,rings*sides+s});}
        });
    }

    static void TravelAccessory(Vector3 p,int variant,bool hard,Transform parent)
    {
        var g=new GameObject(hard?"carry-on travel case":"stitched retail tote").transform;g.SetParent(parent);g.position=p;
        Color[] colors={new(.12f,.15f,.18f),new(.34f,.23f,.16f),new(.34f,.42f,.43f),new(.56f,.46f,.36f)};
        var surface=RetailSurface((hard?"molded luggage shell ":"grained tote leather ")+variant,colors[variant%4],0,hard?.32f:.22f);
        FineRelief(surface,hard?"case_fine_texture_v1":"bag_leather_grain_v1",(u,v)=>Mathf.PerlinNoise(u*144,v*144)*.06f,.4f);
        Vector3 size=hard?new(.42f,.61f,.26f):new(.33f,.33f,.145f);
        var origin=p+Vector3.up*(hard?.07f:0);
        HandmadeProp(hard?"curved molded suitcase shell":"soft gusseted tote body",RetailBagBody(hard),origin,size,surface,g);
        var trim=PrepSteel();
        if(hard){
            for(int rib=-3;rib<=3;rib++)RailSegment("luggage molded reinforcing rib",origin+new Vector3(rib*.049f,.10f,-.129f),origin+new Vector3(rib*.049f,.50f,-.129f),.012f,surface,g);
            foreach(int side in new[]{-1,1})foreach(int depth in new[]{-1,1}){
                var wheel=Decor("case twin spinner wheel",PrimitiveType.Cylinder,p+new Vector3(side*.15f,.042f,depth*.085f),new Vector3(.067f,.022f,.067f),dark,g);
                wheel.transform.rotation=Quaternion.Euler(0,0,90);
            }
            foreach(float x in new[]{-.085f,.085f})RailSegment("luggage telescopic handle shaft",origin+new Vector3(x,.56f,.073f),origin+new Vector3(x,.88f,.073f),.015f,trim,g);
            RailSegment("luggage telescopic grip",origin+new Vector3(-.10f,.88f,.073f),origin+new Vector3(.10f,.88f,.073f),.035f,dark,g);
            foreach(int side in new[]{-1,1})RailSegment("luggage case seam",origin+new Vector3(side*.208f,.07f,0),origin+new Vector3(side*.208f,.54f,0),.008f,dark,g);
        }else{
            foreach(int face in new[]{-1,1}){
                var points=new Vector3[19];
                for(int n=0;n<points.Length;n++){float a=Mathf.PI*n/(points.Length-1);points[n]=origin+new Vector3(-Mathf.Cos(a)*.079f,.29f+Mathf.Sin(a)*.15f,face*.057f);}
                PrepPipe("tote stitched handle",points,.014f,surface,g);
                foreach(float x in new[]{-.079f,.079f})Decor("tote handle rivet",PrimitiveType.Sphere,origin+new Vector3(x,.287f,face*.067f),new Vector3(.009f,.009f,.004f),trim,g);
            }
            RailSegment("tote top zipper",origin+new Vector3(-.13f,.331f,0),origin+new Vector3(.13f,.331f,0),.006f,trim,g);
            Decor("tote zipper pull",PrimitiveType.Cube,origin+new Vector3(.10f,.338f,-.013f),new Vector3(.023f,.004f,.012f),trim,g);
            Decor("tote sewn front pocket",PrimitiveType.Cube,origin+new Vector3(0,.135f,-.074f),new Vector3(.21f,.17f,.005f),surface,g);
        }
        var tag=Decor("accessory hanging stock ticket",PrimitiveType.Quad,origin+new Vector3(size.x*.20f,size.y*.72f,-size.z*.51f),new Vector3(.038f,.054f,1),GarmentTagInk(),g);
        g.rotation=Quaternion.Euler(0,(variant%3-1)*5,0);
    }

    static void UpperAccessoriesDepartment()
    {
        var wood=OakVeneer(new Color(.44f,.37f,.30f));
        for(int bay=0;bay<3;bay++){
            var old=root;root=new GameObject("2F accessories fitted bay "+bay).transform;root.SetParent(old);
            Box("accessory wall backing",new Vector3(0,1.55f,0),new Vector3(7.3f,3.1f,.10f),MallFinish("paint",new Color(.39f,.43f,.42f),0,.1f));
            foreach(float y in new[]{.14f,1.2f,2.15f}){
                Box("accessory display oak shelf",new Vector3(0,y,-.37f),new Vector3(7.3f,.055f,.68f),wood);
                for(int n=0;n<8;n++)TravelAccessory(new Vector3(-3.15f+n*.88f,y+.029f,-.40f),(n/2+bay)%4,y<.2f,root);
            }
            foreach(float x in new[]{-3.5f,0,3.5f})Decor("accessory shelf vertical bracket",PrimitiveType.Cube,new Vector3(x,1.5f,-.08f),new Vector3(.045f,2.8f,.035f),PrepSteel());
            Label(bay==1?"DAILY BAG  /  데일리 가방":"TRAVEL  /  여행가방",new Vector3(0,3.18f,-.08f),Vector3.zero,.23f);
            LocalLamp("accessory wall downlight",new Vector3(0,3.7f,-1.8f),new Vector3(0,1.3f,-.5f),new Color(.91f,.86f,.75f),1.6f,6);
            root.position=new Vector3(49.2f,5,21+bay*8);root.rotation=Quaternion.Euler(0,90,0);root=old;
        }
        foreach(float z in new[]{24f,35f}){
            var display=new GameObject("2F footwear destination display").transform;display.SetParent(root);
            ShoeDisplay(40,z,display);
        }
        DepartmentSign("accessories","신발 · 여행가방","2F  SHOES & BAGS",new Vector3(40,8.8f,18),6.5f);
    }
}
