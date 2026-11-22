using System.Collections.Generic;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    // Original hanging garments, with an open neck, separate sleeves and sewn fronts.
    // Shared by the laundry and 2F racks; only three meshes and four cloth finishes.
    static Mesh LaundryGarment(int style)
    {
        return MallMesh("laundry_sewn_v4_"+style,(v,u,t)=>{
            void Grid(int rows,int cols,System.Func<int,int,Vector3> point)
            {
                int start=v.Count;
                for(int r=0;r<rows;r++)for(int c=0;c<=cols;c++){
                    v.Add(point(r,c));u.Add(new Vector2(c/(float)cols,r/(float)(rows-1)));
                    if(r<rows-1&&c<cols){int a=start+r*(cols+1)+c;t.AddRange(new[]{a,a+1,a+cols+1,a+1,a+cols+2,a+cols+1});}
                }
            }
            float length=style==1?1.02f:.74f;
            float[] ys=LaundryRowHeights(style);
            float[] widths=LaundryRowWidths(style);
            Grid(ys.Length,20,(r,c)=>{
                float a=c*Mathf.PI*2/20,s=Mathf.Sin(a),co=Mathf.Cos(a);
                float fold=LaundryFold(a,ys[r],r/(float)(ys.Length-1));
                float depth=r==ys.Length-1?.030f:.052f+.008f*Mathf.Sin(-ys[r]*5);
                float hem=r<2?.018f*Mathf.Abs(s):0;
                return new Vector3(s*(widths[r]+fold),ys[r]+hem,co*(depth+fold));
            });
            foreach(int side in new[]{-1,1}){
                int sleeveStart=v.Count;
                Grid(8,12,(r,c)=>{
                    float f=r/7f,a=c*Mathf.PI*2/12;
                    float reach=style==2?.30f:.62f;
                    var center=new Vector3(side*(.17f+.19f*f+.022f*Mathf.Sin(f*Mathf.PI)),-.10f-reach*f,-.030f*f*f+.021f*Mathf.Sin(f*Mathf.PI));
                    float radius=Mathf.Lerp(.080f,.046f,f)+.005f*Mathf.Sin(f*16);
                    return center+new Vector3(Mathf.Sin(a)*radius,side*Mathf.Sin(a)*radius*.20f,Mathf.Cos(a)*radius*.72f);
                });
                // Close the upper sleeve seam inside the shoulder; no open tube
                // remains visible when the player looks down at the hanging rack.
                int cap=v.Count;v.Add(new Vector3(side*.17f,-.10f,0));u.Add(new Vector2(.5f,.5f));
                for(int c=0;c<12;c++)t.AddRange(new[]{cap,sleeveStart+c+1,sleeveStart+c});
            }
            // Standing collar: two walls and a rounded folded top around the neck.
            Grid(4,16,(r,c)=>{
                float a=c*Mathf.PI*2/16;
                float radius=r<2?.082f:.075f;
                float y=r==0||r==3?-.012f:.026f;
                return new Vector3(Mathf.Sin(a)*radius,y,Mathf.Cos(a)*(radius*.45f));
            });
            void Panel(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                Vector3 AboveCloth(Vector3 p){p.z=Mathf.Min(p.z,LaundryFrontDepth(style,p.x,p.y)-.002f);return p;}
                a=AboveCloth(a);b=AboveCloth(b);c=AboveCloth(c);d=AboveCloth(d);
                int n=v.Count;v.AddRange(new[]{a,b,c,d});u.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});t.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
            }
            // Segmented placket follows the torso instead of floating over it.
            for(int i=0;i<7;i++){
                float y0=-.09f-i*(length-.16f)/7,y1=-.09f-(i+1)*(length-.16f)/7;
                Panel(new(-.014f,y0,-.062f),new(.014f,y0,-.062f),new(.014f,y1,-.062f),new(-.014f,y1,-.062f));
            }
            // Chest patch pocket, open lip, and two coat lapels where appropriate.
            Panel(new(.085f,-.17f,-.065f),new(.198f,-.17f,-.065f),new(.183f,-.287f,-.063f),new(.10f,-.287f,-.063f));
            Panel(new(.085f,-.17f,-.065f),new(.198f,-.17f,-.065f),new(.198f,-.182f,-.069f),new(.085f,-.182f,-.069f));
            if(style==1)foreach(int side in new[]{-1,1}){
                Panel(new(side*.073f,-.017f,-.04f),new(side*.162f,-.065f,-.06f),new(side*.075f,-.23f,-.082f),new(side*.015f,-.34f,-.067f));
            }
            MallTwoSides(v,u,t);
        });
    }
    static Material LaundryCotton(int shade)
    {
        Color[] colours={new(.60f,.57f,.50f),new(.18f,.24f,.30f),new(.68f,.69f,.65f),new(.35f,.23f,.25f)};
        var mat=RetailSurface("laundry woven oxford v2 "+shade,colours[shade],0,.16f);
        mat.mainTexture=FinishTexture("LaundryOxfordV2",(u,v)=>{
            float thread=.018f*Mathf.Sin(u*256*Mathf.PI)*Mathf.Sin(v*256*Mathf.PI);
            float grain=.028f*Mathf.PerlinNoise(u*110,v*110);
            float crease=.025f*Mathf.PerlinNoise(u*12+6,v*3);
            return new Color(.87f+thread+grain-crease,.87f+thread+grain-crease,.85f+thread+grain-crease,1);
        });
        FineRelief(mat,"LaundryWeaveReliefV2",(u,v)=>.055f*Mathf.Sin(u*256*Mathf.PI)*Mathf.Sin(v*256*Mathf.PI)+
            .38f*Mathf.PerlinNoise(u*15+4,v*3)+.06f*Mathf.PerlinNoise(u*70,v*14),1.4f);
        mat.mainTextureScale=new Vector2(1,1);
        return mat;
    }
}
