using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    // A fictional five-stick selling unit. Opaque contents batch with shelf stock;
    // only the outer sleeve uses transparency, rather than ten overlapping films.
    static void StringCheesePack(Vector3 p,Transform parent)
    {
        const float width=.129f,height=.194f,depth=.025f;
        var dairy=RetailSurface("string cheese ivory cut curd",new Color(.84f,.80f,.65f),0,.26f);
        var liner=RetailSurface("string cheese wrapper ivory liner",new Color(.77f,.80f,.75f),0,.40f);
        var seams=RetailSurface("string cheese pressed seal",new Color(.69f,.76f,.73f),0,.51f);
        HandmadeProp("sealed cheese pack backing",SoftShopEdge(new Vector3(width,.188f,.002f)),p+new Vector3(0,.097f,.011f),Vector3.one,liner,parent);
        var stick=TurnedPackageMesh("string_cheese_stick_v1",new[]{0f,.018f,.05f,.94f,.98f,1f},new[]{0f,.37f,.49f,.48f,.35f,0f});
        for(int i=0;i<5;i++){
            float x=(i-2)*.023f,y=.021f+(i%3)*.0018f;
            var cheese=HandmadeProp("individually sealed string cheese",stick,p+new Vector3(x,y,0),new Vector3(.018f,.128f,.018f),dairy,parent);
            cheese.transform.rotation=Quaternion.Euler(0,0,(i%2==0?1:-1)*1.2f);
            foreach(float end in new[]{y-.004f,y+.132f})
                Decor("single stick flat crimp",PrimitiveType.Cube,p+new Vector3(x,end,-.0004f),new Vector3(.021f,.004f,.0011f),seams,parent);
            Decor("single stick longitudinal weld",PrimitiveType.Cube,p+new Vector3(x+.0104f,y+.064f,.0001f),new Vector3(.0008f,.136f,.0008f),seams,parent);
        }
        HandmadeProp("one clear outer sleeve",NoodlePacket(false,true),p,new Vector3(width,height,depth),ChilledWrap(),parent);
        Decor("string cheese paper header",PrimitiveType.Quad,p+new Vector3(0,.170f,-.0068f),new Vector3(.123f,.043f,1),PantryInk("StringCheeseHeader",0,.27f),parent);
        Decor("string cheese lower selling unit",PrimitiveType.Quad,p+new Vector3(0,.010f,-.008f),new Vector3(.123f,.017f,1),PantryInk("StringCheeseFooter",0,.27f),parent);
        var back=MallMesh("string_cheese_back_print_v1",(v,u,t)=>{
            v.AddRange(new[]{new Vector3(-.055f,.025f,.0122f),new Vector3(.055f,.025f,.0122f),new Vector3(.055f,.166f,.0122f),new Vector3(-.055f,.166f,.0122f)});
            u.AddRange(new[]{new Vector2(.730f,.005f),new Vector2(.004f,.005f),new Vector2(.004f,.995f),new Vector2(.730f,.995f)});
            t.AddRange(new[]{0,1,2,0,2,3});
        });
        HandmadeProp("fitted full wrap consumer information",back,p,Vector3.one,InformationInk(25,3),parent);
    }
}
