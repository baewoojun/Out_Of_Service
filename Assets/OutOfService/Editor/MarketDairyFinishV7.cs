using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    static Mesh DairyFoilSeal()=>MallMesh("dairy_printed_foil_v1",(v,u,t)=>{
        const int sides=48;
        v.Add(Vector3.up);u.Add(new Vector2(.5f,.5f));
        for(int i=0;i<=sides;i++){
            float a=i*2*Mathf.PI/sides,x=Mathf.Sin(a)*.5f,z=Mathf.Cos(a)*.5f;
            v.Add(new Vector3(x,1,z));u.Add(new Vector2(.5f+x,.5f+z));
            if(i>0)t.AddRange(new[]{0,i,i+1});
        }
        int bottom=v.Count;v.Add(Vector3.zero);u.Add(new Vector2(.5f,.5f));
        for(int i=0;i<=sides;i++){
            float a=i*2*Mathf.PI/sides,x=Mathf.Sin(a)*.5f,z=Mathf.Cos(a)*.5f;
            v.Add(new Vector3(x,0,z));u.Add(new Vector2(.5f+x,.5f+z));
            if(i>0){int a0=i,b0=bottom+i;t.AddRange(new[]{bottom,b0+1,b0,a0,b0,a0+1,a0+1,b0,b0+1});}
        }
    });
    static float DairyRadius(int recipe,float y)
    {
        float[] heights=recipe==4?new[]{0f,.04f,.23f,.48f,.66f,.86f,.96f,1f}:
            recipe==5?new[]{0f,.025f,.10f,.85f,.94f,.97f,1f}:new[]{0f,.04f,.18f,.48f,.69f,.83f,.91f,1f};
        float[] radii=recipe==4?new[]{.32f,.36f,.47f,.5f,.45f,.30f,.29f,.31f}:
            recipe==5?new[]{.35f,.39f,.39f,.49f,.49f,.515f,.515f}:new[]{.34f,.43f,.47f,.45f,.36f,.35f,.26f,.26f};
        for(int i=1;i<heights.Length;i++)if(y<=heights[i])return Mathf.Lerp(radii[i-1],radii[i],Mathf.InverseLerp(heights[i-1],heights[i],y));
        return radii[radii.Length-1];
    }
    static Mesh DairyMold(int recipe)
    {
        return MallMesh("dairy_smooth_container_v1_"+recipe,(v,u,t)=>{
            const int rows=24,cols=40;
            for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++){
                float y=row/(float)rows,a=col*Mathf.PI*2/cols,r=DairyRadius(recipe,y);
                v.Add(new Vector3(Mathf.Sin(a)*r,y,-Mathf.Cos(a)*r));u.Add(new Vector2(col/(float)cols,y));
                if(row==0||col==0)continue;int d=v.Count-1,b=d-cols-1;t.AddRange(new[]{b-1,d-1,b,b,d-1,d});
            }
            int bottom=v.Count;v.Add(Vector3.zero);u.Add(new Vector2(.5f,.5f));
            for(int c=0;c<cols;c++)t.AddRange(new[]{bottom,c+1,c});
        });
    }
    static void DairyPrintedBand(Transform pack,int recipe)
    {
        float lo=recipe==5?.11f:recipe==4?.19f:.12f,hi=recipe==5?.87f:recipe==4?.66f:.67f;
        foreach(bool back in new[]{false,true}){
            var mesh=MallMesh("dairy_fitted_band_v1_"+recipe+(back?"_back":"_front"),(v,u,t)=>{
                const int cols=20,rows=16;
                for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++){
                    float x=col/(float)cols,f=row/(float)rows,y=Mathf.Lerp(lo,hi,f),a=(Mathf.Lerp(-75,75,x)+(back?180:0))*Mathf.Deg2Rad;
                    float r=DairyRadius(recipe,y)+.011f;
                    v.Add(new Vector3(Mathf.Sin(a)*r,y,-Mathf.Cos(a)*r));
                    u.Add(back?new Vector2(.004f+.726f*x,.005f+.99f*f):new Vector2(x,f));
                    if(row==0||col==0)continue;int d=v.Count-1,b=d-cols-1;t.AddRange(new[]{b-1,d-1,b,b,d-1,d});
                }
            });
            var go=new GameObject(back?"fitted dairy reverse print":"fitted dairy front print");go.transform.SetParent(pack,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=back?InformationInk(26,recipe):PackMaterial(26,recipe);
        }
    }
}
