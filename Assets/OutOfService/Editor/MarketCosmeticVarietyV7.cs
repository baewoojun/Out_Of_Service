using UnityEngine;
using UnityEditor;

public static partial class MarketPrototypeBuilderV7
{
    static Material CosmeticInk(int type)
    {
        var mat=RetailSurface("original moa label "+type,Color.white,0,.22f);
        if(!mat.mainTexture)mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/MallDetails/moa-skincare-"+type+".png");
        if(!mat.mainTexture)throw new System.InvalidOperationException("Missing authored cosmetic label "+type);
        return mat;
    }
    static Mesh CosmeticTube()
    {
        return MallMesh("sealed_cosmetic_tube_v2",(v,u,t)=>{
            float[] ys={.028f,.034f,.055f,.12f,.167f,.176f};
            float[] widths={.017f,.027f,.032f,.037f,.040f,.040f};
            float[] depths={.014f,.020f,.022f,.017f,.0028f,.002f};
            const int columns=24;
            for(int row=0;row<ys.Length;row++)for(int c=0;c<=columns;c++){
                float a=c*Mathf.PI*2/columns;
                v.Add(new Vector3(Mathf.Sin(a)*widths[row],ys[row],Mathf.Cos(a)*depths[row]));u.Add(new Vector2(c/(float)columns,row/(float)(ys.Length-1)));
                if(row<ys.Length-1 && c<columns){int k=row*(columns+1)+c;t.AddRange(new[]{k,k+1,k+columns+1,k+1,k+columns+2,k+columns+1});}
            }
            int end=v.Count;v.Add(new Vector3(0,.176f,0));u.Add(Vector2.one*.5f);
            for(int c=0;c<columns;c++)t.AddRange(new[]{end,(ys.Length-1)*(columns+1)+c,(ys.Length-1)*(columns+1)+c+1});
        });
    }
    static void SpecialtyCosmetic(Vector3 position,int type,int variant,float yaw)
    {
        var parent=root;root=new GameObject("parody skincare product "+type).transform;root.SetParent(parent);
        Color[] colors={new(.66f,.70f,.64f),new(.31f,.40f,.33f),new(.63f,.47f,.43f)};
        var polymer=RetailSurface("cosmetic soft tube "+variant,colors[variant],0,.40f);
        var cap=RetailSurface("cosmetic ivory closure",new Color(.73f,.74f,.69f),0,.47f);
        if(type==3){
            HandmadeProp("flattened sealed cleansing tube",CosmeticTube(),Vector3.zero,Vector3.one,polymer,root);
            HandmadeProp("tube flip top cap",SoftShopEdge(new Vector3(.041f,.029f,.034f)),new Vector3(0,.0145f,0),Vector3.one,cap,root);
            MerchBox("tube lid hinge",new Vector3(0,.025f,.018f),new Vector3(.022f,.005f,.004f),cap);
            MerchBox("tube press tab",new Vector3(0,.023f,-.018f),new Vector3(.013f,.004f,.002f),shelf);
            MerchBox("tube heat seal",new Vector3(0,.171f,0),new Vector3(.080f,.008f,.005f),polymer);
            for(int n=0;n<13;n++)Decor("tube crimp impression",PrimitiveType.Cube,new Vector3(-.036f+n*.006f,.172f,-.0027f),new Vector3(.001f,.005f,.0006f),cap);
            var label=MallMesh("tube_front_label_v3",(v,u,t)=>{
                for(int row=0;row<=8;row++)for(int x=0;x<=8;x++){
                    float f=row/8f,h=.065f+f*.079f,a=(x/8f-.5f)*1.6f;
                    // Match the actual .055/.120/.167m body rings; an approximate
                    // taper intersected the tube near the printed upper corners.
                    float q=h<.12f?(h-.055f)/.065f:(h-.12f)/.047f;
                    float w=h<.12f?Mathf.Lerp(.032f,.037f,q):Mathf.Lerp(.037f,.040f,q);
                    float d=h<.12f?Mathf.Lerp(.022f,.017f,q):Mathf.Lerp(.017f,.0028f,q);
                    v.Add(new Vector3(Mathf.Sin(a)*(w+.0007f),h,-Mathf.Cos(a)*(d+.0012f)));u.Add(new Vector2(x/8f,f));
                    if(row<8 && x<8){int k=row*9+x;t.AddRange(new[]{k,k+9,k+1,k+1,k+9,k+10});}
                }
            });HandmadeProp("cleansing tube original label",label,Vector3.zero,Vector3.one,CosmeticInk(type),root);
        }else{
            float height=type==4?.074f:.131f,radius=type==4?.025f:.028f;
            var body=type==4?RetailSurface("serum amber coated glass",new Color(.23f,.115f,.047f),0,.76f):polymer;
            HandmadeProp(type==4?"dropper serum bottle":"oval airless pump body",TurnedPackageMesh("special_cosmetic_body_v2_"+type,
                new[]{0f,.004f,height*.77f,height*.91f,height*.94f,height*.94f},new[]{radius*.86f,radius,radius,radius*.63f,radius*.47f,0f}),
                Vector3.zero,new Vector3(1,1,type==5?.77f:1),body,root);
            if(type==4){
                Decor("dropper threaded collar",PrimitiveType.Cylinder,new Vector3(0,.079f,0),new Vector3(.035f,.011f,.035f),dark);
                HandmadeProp("rounded rubber dropper bulb",TurnedPackageMesh("serum_dropper_bulb_v2",new[]{0f,.006f,.025f,.032f,.035f},new[]{.014f,.014f,.011f,.007f,0f}),new Vector3(0,.089f,0),Vector3.one,dark,root);
            }else{
                Decor("airless brushed collar",PrimitiveType.Cylinder,new Vector3(0,.127f,0),new Vector3(.044f,.009f,.036f),PrepSteel());
                Decor("airless actuator stem",PrimitiveType.Cylinder,new Vector3(0,.145f,0),new Vector3(.011f,.012f,.011f),cap);
                HandmadeProp("airless flush actuator",SoftShopEdge(new Vector3(.038f,.014f,.039f)),new Vector3(0,.159f,-.004f),Vector3.one,cap,root);
                MerchBox("airless pump outlet",new Vector3(0,.158f,-.024f),new Vector3(.006f,.003f,.001f),shelf);
            }
            var label=MallMesh("special_cosmetic_label_v2",(v,u,t)=>{
                for(int n=0;n<=12;n++){
                    float a=(n/12f-.5f)*1.85f;v.Add(new Vector3(Mathf.Sin(a),0,-Mathf.Cos(a)));v.Add(new Vector3(Mathf.Sin(a),1,-Mathf.Cos(a)));
                    u.Add(new Vector2(n/12f,0));u.Add(new Vector2(n/12f,1));if(n<12){int k=n*2;t.AddRange(new[]{k,k+1,k+2,k+2,k+1,k+3});}
                }
            });
            HandmadeProp("serum or essence original curved label",label,new Vector3(0,height*.18f,0),new Vector3(radius+.0007f,height*.55f,(radius+.0007f)*(type==5?.77f:1)),CosmeticInk(type),root);
        }
        SetShoppedFacing(root,position,yaw,31+type,variant);root=parent;
    }
}
