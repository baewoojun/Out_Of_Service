using System;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    // Original, shared surface relief. No imported photo pixels and no per-object
    // texture copies. Mipmaps keep the fine weave from shimmering at a distance.
    static void FineRelief(Material material,string key,Func<float,float,float> height,float strength)
    {
        if(material.GetTexture("_BumpMap"))return;
        material.SetTexture("_BumpMap",FinishTexture(key,(u,v)=>{
            const float d=1f/512;
            var n=new Vector3((height(u-d,v)-height(u+d,v))*strength,
                (height(u,v-d)-height(u,v+d))*strength,1).normalized;
            return new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
        }));
        material.SetFloat("_BumpScale",1);
        material.EnableKeyword("_NORMALMAP");
    }

    static Material CafeCeramic(bool paper)
    {
        var mat=RetailSurface(paper?"uncoated paper cup v2":"ivory fired ceramic glaze v2",
            paper?new Color(.67f,.57f,.41f):new Color(.82f,.80f,.73f),0,paper?.13f:.64f);
        FineRelief(mat,paper?"PaperCupFiberV2":"CeramicGlazeReliefV2",(u,v)=>
            paper?Mathf.PerlinNoise(u*130,v*47)*.06f:Mathf.PerlinNoise(u*130,v*130)*.018f,1f);
        return mat;
    }

    static Material CafeBrushedSteel()
    {
        var mat=RetailSurface("cafe brushed sheet steel v2",new Color(.62f,.65f,.66f),.86f,.56f);
        FineRelief(mat,"CafeSheetBrushingV2",(u,v)=>Mathf.PerlinNoise(u*4+13,v*180)*.065f,1);
        if(!mat.GetTexture("_MetallicGlossMap")){
            mat.SetTexture("_MetallicGlossMap",FinishTexture("CafeSheetWearV2",(u,v)=>{
                float wipe=Mathf.PerlinNoise(u*6+29,v*12+11);
                float brush=Mathf.PerlinNoise(u*4,v*160);
                return new Color(.86f,0,0,.62f+.18f*brush-.12f*wipe);
            }));
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        return mat;
    }

    // Front offset follows the interpolated hanging torso. Pocket/placket seams
    // therefore remain above the cloth instead of disappearing inside its folds.
    static float[] LaundryRowHeights(int style)
    {
        float length=style==1?1.02f:.74f;
        return new[]{-length,-length+.025f,-length*.86f,-length*.73f,-length*.61f,-length*.49f,-.30f,-.24f,-.18f,-.13f,-.09f,-.05f,-.008f};
    }
    static float[] LaundryRowWidths(int style)=>new[]{style==1?.30f:.255f,style==1?.299f:.256f,
        style==1?.29f:.252f,style==1?.28f:.246f,style==1?.27f:.242f,style==1?.26f:.246f,.255f,.26f,.26f,.255f,.247f,.20f,.077f};
    static float LaundryFold(float a,float y,float f)=>
        (.012f*Mathf.Sin(a*7+y*4)+.004f*Mathf.Sin(a*13-y*8)+.010f*Mathf.Sin(a*15+y*19)*Mathf.Exp(-Mathf.Abs(y+.23f)*10))*Mathf.Sin(Mathf.PI*f);

    static float LaundryFrontDepth(int style,float x,float y)
    {
        var ys=LaundryRowHeights(style);var widths=LaundryRowWidths(style);
        int row=0;while(row<ys.Length-2 && y>ys[row+1])row++;
        float f=Mathf.InverseLerp(ys[row],ys[row+1],y),r=row+f;
        float width=Mathf.Lerp(widths[row],widths[row+1],f);
        float a=Mathf.PI-Mathf.Asin(Mathf.Clamp(x/width,-.98f,.98f));
        float fold=LaundryFold(a,y,r/(ys.Length-1));
        float d0=.052f+.008f*Mathf.Sin(-ys[row]*5);
        float d1=row+1==ys.Length-1?.030f:.052f+.008f*Mathf.Sin(-ys[row+1]*5);
        return Mathf.Cos(a)*(Mathf.Lerp(d0,d1,f)+fold);
    }

    static Mesh LaundrySeams(int style)
    {
        return MallMesh("tailoring_front_seams_v2_"+style,(v,u,t)=>{
            void Stitch(Vector2 a,Vector2 b,float lift=.0022f){
                int count=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.027f));
                for(int i=0;i<count;i++){
                    Vector2 p=Vector2.Lerp(a,b,i/(float)count),q=Vector2.Lerp(a,b,(i+.65f)/count);
                    var d=(q-p).normalized;var side=new Vector2(-d.y,d.x)*.00065f;
                    int k=v.Count;
                    foreach(var xy in new[]{p-side,p+side,q+side,q-side}){
                        v.Add(new Vector3(xy.x,xy.y,LaundryFrontDepth(style,xy.x,xy.y)-lift));
                        u.Add(Vector2.zero);
                    }
                    t.AddRange(new[]{k,k+2,k+1,k,k+3,k+2});
                }
            }
            float length=style==1?1.02f:.74f;
            foreach(float x in new[]{-.012f,.012f})Stitch(new(x,-.10f),new(x,-length+.075f),.004f);
            Stitch(new(.088f,-.174f),new(.104f,-.281f),.006f);
            Stitch(new(.104f,-.281f),new(.18f,-.281f),.006f);
            Stitch(new(.18f,-.281f),new(.193f,-.174f),.006f);
            for(int i=0;i<12;i++){
                float x0=-.235f+i*.47f/12,x1=-.235f+(i+1)*.47f/12;
                Stitch(new(x0,-length+.015f+.016f*Mathf.Abs(x0)/.25f),new(x1,-length+.015f+.016f*Mathf.Abs(x1)/.25f));
            }
            MallTwoSides(v,u,t);
        });
    }
}
