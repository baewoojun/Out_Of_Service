using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    // Authored garment patterns, in metres: long shirt, cotton coat, crew-neck tee.
    // The UV distance is physical, so collars and pockets keep the same weave size.
    static Mesh LaundryGarment(int style)
    {
        return MallMesh("laundry_sewn_v10_"+style,(v,u,t)=>{
            void Grid(int rows,int cols,System.Func<int,int,Vector3> point,bool body=false)
            {
                int start=v.Count;
                for(int r=0;r<rows;r++) {
                    float around=0;
                    for(int c=0;c<=cols;c++) {
                        var p=point(r,c);if(c>0)around+=Vector3.Distance(p,v[v.Count-1]);
                        v.Add(p);
                        float along=body?p.y:r==0?0:u[start+(r-1)*(cols+1)+c].y+Vector3.Distance(p,v[start+(r-1)*(cols+1)+c]);
                        u.Add(new Vector2(around,along));
                        bool armhole=body&&r>=7&&r<11&&(c>=4&&c<8||c>=16&&c<20);
                        if(r<rows-1&&c<cols&&!armhole){int a=start+r*(cols+1)+c;t.AddRange(new[]{a,a+1,a+cols+1,a+1,a+cols+2,a+cols+1});}
                    }
                }
            }
            float length=style==1?1.02f:.74f;
            var ys=LaundryRowHeights(style);var widths=LaundryRowWidths(style);
            Grid(ys.Length,24,(r,c)=>{
                float a=c*Mathf.PI*2/24,s=Mathf.Sin(a),co=Mathf.Cos(a);
                float fold=LaundryFold(a,ys[r],r/(float)(ys.Length-1));
                float depth=r==ys.Length-1?.021f:.032f+.006f*Mathf.Sin(-ys[r]*5);
                float hem=r<2?.018f*Mathf.Abs(s):0;
                return new Vector3(s*(widths[r]+fold),ys[r]+hem,co*(depth+fold));
            },true);
            foreach(int side in new[]{-1,1}) {
                // The sleeve shares the boundary vertices of a real torso
                // armhole. No intersecting tube, uncovered rim or z-fighting.
                var boundary=new List<int>(16);int c0=side>0?4:20,step=side>0?1:-1;
                for(int c=0;c<=4;c++)boundary.Add(11*25+c0+c*step);
                for(int r=10;r>=7;r--)boundary.Add(r*25+c0+4*step);
                for(int c=3;c>=0;c--)boundary.Add(7*25+c0+c*step);
                for(int r=8;r<11;r++)boundary.Add(r*25+c0);
                int[] previous=boundary.ToArray();
                for(int row=0;row<10;row++) {
                    float f=row/9f;
                    var center=new Vector3(side*(.285f+.062f*f),-.16f-(style==2?.23f:.54f)*f,-.020f*f*f);
                    float radius=Mathf.Lerp(.066f,style==2?.061f:.043f,f)+.0024f*Mathf.Sin(f*19+side)*Mathf.Sin(f*Mathf.PI);
                    if(style!=2&&f>.89f)radius+=.002f;
                    var current=new int[16];
                    for(int s=0;s<16;s++) {
                        float a=(135+s*22.5f)*Mathf.Deg2Rad;
                        current[s]=v.Count;
                        // The upper armhole must reach the OUTER sleeve edge.
                        // Reversing this correspondence folded the shoulder
                        // inward through the lower armhole into a black notch.
                        v.Add(center+new Vector3(-side*Mathf.Cos(a)*radius*.98f,-Mathf.Cos(a)*radius*.20f,Mathf.Sin(a)*radius*.45f));
                        u.Add(new Vector2(s/16f*Mathf.PI*2*radius,center.y));
                    }
                    void Face(int a,int b,int c) {
                        // Boundary order is consistent. Per-triangle centre tests
                        // flipped concave shoulder faces and cancelled normals.
                        if(side>0)t.AddRange(new[]{a,b,c});else t.AddRange(new[]{a,c,b});
                    }
                    for(int s=0;s<16;s++){int next=(s+1)%16;Face(previous[s],current[s],previous[next]);Face(previous[next],current[s],current[next]);}
                    previous=current;
                }
            }
            // Relax the shoulder transition across the connected armhole.
            // Keep neck, pocket/placket region and cuffs fixed; bound the move
            // so this remains the same sewn pattern and hanger clearance.
            var adjacent=new HashSet<int>[v.Count];
            for(int i=0;i<adjacent.Length;i++)adjacent[i]=new HashSet<int>();
            for(int i=0;i<t.Count;i+=3)for(int k=0;k<3;k++){
                int a=t[i+k];adjacent[a].Add(t[i+(k+1)%3]);adjacent[a].Add(t[i+(k+2)%3]);
            }
            for(int pass=0;pass<2;pass++){
                var before=v.ToArray();
                for(int i=0;i<before.Length;i++){
                    var p=before[i];float weight=Mathf.InverseLerp(-.20f,-.13f,p.y)*(1-Mathf.InverseLerp(-.035f,-.012f,p.y))*Mathf.InverseLerp(.13f,.22f,Mathf.Abs(p.x));
                    if(weight<=0||adjacent[i].Count==0)continue;
                    var mean=Vector3.zero;foreach(int n in adjacent[i])mean+=before[n];mean/=adjacent[i].Count;
                    var move=Vector3.ClampMagnitude((mean-p)*(.38f*weight),.008f);move.z=Mathf.Clamp(move.z,-.001f,.001f);v[i]=p+move;
                }
            }
            // A low folded neckband, with pointed front leaves on shirts/coats.
            // A shallow ribbed crew-neck replaces the old upright tube on tees.
            Grid(4,20,(r,c)=>{
                float a=c*Mathf.PI*2/20,front=Mathf.Max(0,-Mathf.Cos(a));
                float radius=r<2?.080f:.073f;
                float y=r==0||r==3?-.012f:style==2?.003f:.017f;
                y-=front*(style==2?.010f:.005f);
                return new Vector3(Mathf.Sin(a)*radius,y,Mathf.Cos(a)*radius*.45f);
            });
            void Panel(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float lift=.002f)
            {
                Vector3 Above(Vector3 p){p.z=LaundryFrontDepth(style,p.x,p.y)-lift;return p;}
                a=Above(a);b=Above(b);c=Above(c);d=Above(d);
                int n=v.Count;v.AddRange(new[]{a,b,c,d});
                foreach(var p in new[]{a,b,c,d})u.Add(new Vector2(p.x,p.y));
                t.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
            }
            if(style!=2) {
                for(int i=0;i<7;i++) {
                    float y0=-.075f-i*(length-.13f)/7,y1=-.075f-(i+1)*(length-.13f)/7;
                    Panel(new(-.013f,y0,-.060f),new(.013f,y0,-.060f),new(.013f,y1,-.060f),new(-.013f,y1,-.060f));
                }
                foreach(int side in new[]{-1,1}) {
                    Panel(new(side*.073f,-.010f,-.032f),new(side*.121f,-.052f,-.052f),
                        new(side*.079f,-.148f,-.083f),new(side*.014f,-.054f,-.067f),.004f);
                    if(style==1)Panel(new(side*.116f,-.058f,-.055f),new(side*.177f,-.112f,-.067f),
                        new(side*.024f,-.352f,-.081f),new(side*.061f,-.188f,-.080f),.006f);
                }
            }
            if(style!=1) {
                Panel(new(.083f,-.183f,-.065f),new(.188f,-.183f,-.065f),new(.177f,-.292f,-.064f),new(.094f,-.292f,-.064f));
                Panel(new(.083f,-.183f,-.065f),new(.188f,-.183f,-.065f),new(.188f,-.194f,-.069f),new(.083f,-.194f,-.069f),.003f);
            }else foreach(int side in new[]{-1,1}) {
                Panel(new(side*.095f,-.49f,-.063f),new(side*.235f,-.49f,-.055f),new(side*.23f,-.66f,-.055f),new(side*.10f,-.66f,-.064f));
            }
            MallTwoSides(v,u,t);
        });
    }

    static Material SurveyedCloth(string id,Color colour)
    {
        var mat=RetailSurface("physical cotton v1 "+id+ColorUtility.ToHtmlStringRGB(colour),colour,0,.15f);
        mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(SurveyedFinishFolder+id+(id=="cotton_jersey"?"_Diffuse":"_col_1")+"_2k.jpg");
        mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(SurveyedFinishFolder+id+"_nor_gl_2k.jpg"));
        mat.SetFloat("_BumpScale",.20f);mat.EnableKeyword("_NORMALMAP");BindSurveyedMasks(mat,id);
        float period=id=="cotton_jersey"?.264f:.4f;
        foreach(string property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap","_OcclusionMap"})mat.SetTextureScale(property,Vector2.one/period);
        return mat;
    }
    static Material LaundryCotton(int shade)
    {
        Color[] colours={new(.73f,.69f,.59f),new(.12f,.21f,.30f),new(.86f,.88f,.83f),new(.38f,.16f,.17f)};
        return SurveyedCloth("cotton_jersey",colours[shade%4]);
    }
    static Material GarmentCloth(int style,int shade)
    {
        if(style==2)return LaundryCotton(shade);
        if(style==0&&shade==3)return SurveyedCloth("fabric_pattern_07",new Color(.79f,.83f,.82f));
        Color[] colours={new(.58f,.69f,.79f),new(.22f,.30f,.37f),new(.86f,.87f,.83f),new(.56f,.48f,.36f)};
        var mat=RetailSurface("fine woven shirt v5 "+style+" "+shade,colours[shade%4],0,.16f);
        mat.mainTexture=FinishTexture("OxfordFineV5",(u,v)=>{
            float warp=.012f*Mathf.Sin(u*128*Mathf.PI),weft=.010f*Mathf.Sin(v*128*Mathf.PI);
            float grain=.015f*Mathf.PerlinNoise(u*180+3,v*180);
            return new Color(.94f+warp+weft-grain,.94f+warp+weft-grain,.93f+warp+weft-grain,1);
        });
        FineRelief(mat,"OxfordFineReliefV5",(u,v)=>.012f*Mathf.Sin(u*128*Mathf.PI)*Mathf.Sin(v*128*Mathf.PI)+.006f*Mathf.PerlinNoise(u*90,v*90),.22f);
        mat.SetTextureScale("_BaseMap",Vector2.one*20);mat.SetTextureScale("_BumpMap",Vector2.one*20);
        return mat;
    }
}
