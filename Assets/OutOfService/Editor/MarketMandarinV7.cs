using UnityEngine;
using UnityEditor;

public static partial class MarketPrototypeBuilderV7
{
    static void Mandarin(Vector3 p,float radius,int seed,Transform parent)
    {
        int variation=seed%3;
        const int rings=20,sides=32;
        var mesh=MallMesh("mandarin_oblate_v1_"+variation,(v,u,t)=>{
            for(int row=0;row<=rings;row++)for(int col=0;col<=sides;col++){
                float f=row/(float)rings,a=col*2*Mathf.PI/sides;
                float y=-.415f*Mathf.Cos(f*Mathf.PI),r=.5f*Mathf.Sin(f*Mathf.PI);
                r*=1+.018f*Mathf.Sin(a*3+variation)+.012f*Mathf.Sin(a*2+f*3);
                y-=Mathf.Sign(y)*.028f*Mathf.Pow(Mathf.Abs(y)/.415f,12);
                v.Add(new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r));
                u.Add(new Vector2(col/(float)sides,f));
                if(row==rings||col==sides)continue;
                int k=row*(sides+1)+col;t.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});
            }
        });
        if(preparedHarvestNormals.Add(mesh)){
            var normals=mesh.normals;
            for(int row=0;row<=rings;row++){
                int a=row*(sides+1),b=a+sides;var n=(normals[a]+normals[b]).normalized;
                normals[a]=normals[b]=n;
                if(row==0||row==rings)for(int col=0;col<=sides;col++)normals[a+col]=row==0?Vector3.down:Vector3.up;
            }
            mesh.normals=normals;mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);
        }
        var mat=RetailSurface("mandarin peel v1 "+variation,Color.white,0,.31f);
        if(!mat.mainTexture){
            mat.mainTexture=FinishTexture("MandarinPeelV1_"+variation,(u,v)=>{
                float n=VegetableMottle(u,v,5),fine=VegetableMottle(u,v,110);
                var color=Color.Lerp(new Color(.81f,.255f,.018f),new Color(.98f,.43f,.028f),n*.82f+variation*.055f);
                color*=.96f+.075f*fine;
                float crown=Mathf.Pow(Mathf.Clamp01((v-.965f)/.035f),2);
                return Color.Lerp(color,new Color(.36f,.36f,.085f),crown*.65f).linear;
            });
            FineRelief(mat,"MandarinPoresV1",(u,v)=>VegetableMottle(u,v,115)*.09f,.6f);
        }
        var fruit=HandmadeProp("settled loose mandarin",mesh,p,Vector3.one*(radius*2),mat,parent);
        fruit.transform.rotation=Quaternion.Euler((seed*19%17)-8,seed*137.5f,(seed*7%15)-7);
        Vector3 top=p+fruit.transform.up*(radius*.775f);
        var scar=Decor("small recessed mandarin calyx",PrimitiveType.Sphere,top,new Vector3(.006f,.0013f,.006f),green,parent);
        scar.transform.rotation=fruit.transform.rotation;
    }
}
