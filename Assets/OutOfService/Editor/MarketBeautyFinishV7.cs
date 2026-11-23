using UnityEngine;
using UnityEditor;

public static partial class MarketPrototypeBuilderV7
{
    static Mesh RoundedSkincareContainer(int type,float height,float radius)
    {
        var mesh=MallMesh("rounded_skincare_v2_"+type,(v,u,t)=>{
            const int sides=32;
            float[] y={0,.006f,.020f,.78f,.83f,.88f,.90f,.91f,.91f};
            float[] r=type==2?new[]{0f,.90f,1,1,1,.99f,.97f,.95f,0}:new[]{0f,.88f,1,1,.975f,.81f,.53f,.48f,0};
            for(int row=0;row<y.Length;row++)for(int col=0;col<=sides;col++){
                float a=col*2*Mathf.PI/sides;
                v.Add(new Vector3(Mathf.Cos(a)*r[row]*radius,y[row]*height,Mathf.Sin(a)*r[row]*radius));
                u.Add(new Vector2(col/(float)sides,y[row]));
                if(row==y.Length-1||col==sides)continue;
                int k=row*(sides+1)+col;t.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});
            }
        });
        if(preparedHarvestNormals.Add(mesh)){
            var n=mesh.normals;
            for(int row=0;row<9;row++){
                int first=row*33,last=first+32;
                n[first]=n[last]=(n[first]+n[last]).normalized;
                if(row==0||row==8)for(int col=0;col<=32;col++)n[first+col]=row==0?Vector3.down:Vector3.up;
            }
            mesh.normals=n;mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);
        }
        return mesh;
    }

    static void BeautyTesterAccessories(float x,float z,int side)
    {
        var tray=RetailSurface("beauty tester washable trays",new Color(.32f,.34f,.29f),0,.27f);
        for(int group=0;group<3;group++){
            float gz=z-.64f+group*.64f;
            HandmadeProp("used tester tray",SoftShopEdge(new Vector3(.28f,.012f,.58f)),new Vector3(x,.996f,gz),Vector3.one,tray,root);
            foreach(float edge in new[]{-.142f,.142f})
                MerchBox("tester tray low rim",new Vector3(x+edge,1.003f,gz),new Vector3(.009f,.012f,.58f),tray);
            var card=RetailSurface("moa tester price card "+group,Color.white,0,.10f);
            if(!card.mainTexture)card.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Packs/BeautyTesterCard"+group+".png");
            var p=new Vector3(x-side*.27f,1.003f,gz);
            var sign=Decor("small tester price card",PrimitiveType.Quad,p,new Vector3(.11f,.0825f,1),card);
            sign.transform.rotation=Quaternion.Euler(72,side*90,group==1?2:0);
        }
        var paper=RetailSurface("beauty disposable paper",new Color(.79f,.80f,.74f),0,.06f);
        HandmadeProp("tester tissue box",SoftShopEdge(new Vector3(.15f,.082f,.20f)),new Vector3(x+side*.34f,1.031f,z-.92f),Vector3.one,cream,root);
        MerchBox("tissue box opening",new Vector3(x+side*.34f,1.073f,z-.92f),new Vector3(.018f,.002f,.115f),shelf);
        var tissue=Decor("partly pulled tester tissue",PrimitiveType.Quad,new Vector3(x+side*.34f,1.101f,z-.94f),new Vector3(.095f,.076f,1),paper);
        tissue.transform.rotation=Quaternion.Euler(36,side*90,14);
        var wipe=Decor("folded sample wipe",PrimitiveType.Quad,new Vector3(x+side*.33f,.9915f,z-.61f),new Vector3(.082f,.057f,1),paper);
        wipe.transform.rotation=Quaternion.Euler(90,side*90,12);
    }
}
