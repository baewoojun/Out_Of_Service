using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    // Width, standing height, packed depth and the height of the fullest cross section.
    // Small berries settle into the base. Whole strawberries, broccoli and dumplings
    // support the film at fewer points; flat dumplings and cutlets use shallow packs.
    static readonly Vector4[] dumplingFilm={
        new(.218f,.250f,.111f,.43f),new(.218f,.238f,.066f,.40f),new(.202f,.265f,.102f,.48f),
        new(.207f,.248f,.091f,.39f),new(.226f,.238f,.114f,.36f),new(.213f,.267f,.083f,.46f)};
    static readonly Vector4[] vegetableFilm={
        new(.204f,.263f,.103f,.28f),new(.219f,.272f,.125f,.46f),new(.213f,.238f,.091f,.32f),
        new(.223f,.253f,.127f,.41f),new(.196f,.273f,.107f,.34f),new(.215f,.238f,.064f,.27f)};
    static readonly Vector4[] mealFilm={
        new(.216f,.223f,.079f,.33f),Vector4.zero,new(.195f,.264f,.097f,.50f),
        new(.215f,.254f,.068f,.44f),new(.215f,.232f,.104f,.35f),new(.194f,.252f,.085f,.47f)};

    static bool HasFrozenFilm(int family,int v)=>family==20 || family==23 || family==22 && v!=1;

    static void FrozenFacing(int family,int v,Vector3 p,Transform parent)
    {
        var spec=(family==20?dumplingFilm:family==23?vegetableFilm:mealFilm)[v];
        string key=$"FrozenFilm_{family}_{v}";
        var ink=RetailSurface(key,Color.white,0,.39f);
        if(!ink.mainTexture)ink.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Packs/"+key+".png");
        string path="Assets/OutOfService/Generated/"+key+".asset";
        if(!handmadeMeshes.TryGetValue(path,out var mesh)){
            mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool create=!mesh;
            if(create)mesh=new Mesh();
            FrozenFilmMesh(mesh,family,v,spec.w);
            if(create)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);
            handmadeMeshes[path]=mesh;
        }
        HandmadeProp("gusseted frozen pack",mesh,p,new Vector3(spec.x,spec.y,spec.z),ink,parent);
    }

    static void FrozenFilmMesh(Mesh result,int family,int v,float fill)
    {
        bool zipper=family==23 && (v==0 || v==3 || v==4) || family==20 && (v==0 || v==2);
        bool chunky=family==20 && v!=1 || family==23 && (v==1 || v==3) || family==22 && v==2;
        bool pillow=family==22 && (v==0 || v==3 || v==5) || family==20 && v==1 || family==23 && !zipper;
        float top=zipper?.903f:.951f-.009f*(v%3);
        float[] rows={0,.022f,.058f,.13f,.23f,.34f,.46f,.58f,.70f,.80f,top-.034f,top,top+.010f,top+.020f,.979f,.99f,1};
        var verts=new List<Vector3>();var uvs=new List<Vector2>();var tris=new List<int>();
        for(int face=0;face<4;face++){
            bool side=face%2==1;
            int cols=side?4:12;
            for(int r=0;r<rows.Length;r++)for(int c=0;c<=cols;c++){
                float y=rows[r],u=(float)c/cols, ax=face==0?2*u-1:face==2?1-2*u:face==1?1:-1;
                float az=face==0?-1:face==2?1:face==1?2*u-1:1-2*u;
                float edge=Mathf.Abs(ax), phase=v*.87f+family*.32f;
                float body=Mathf.Sin(Mathf.PI*Mathf.Clamp01(y/top));
                float radius=y<fill?Mathf.Lerp(.13f,.49f,Mathf.Sin(Mathf.PI*.5f*y/fill)):
                    Mathf.Lerp(.49f,.018f,Mathf.Pow(Mathf.Clamp01((y-fill)/(top-fill)),1.26f));
                if(r==11 || r==13)radius+=zipper?.040f:.008f;
                if(r>=14)radius=.014f+.005f*(r%2);
                float pressure=chunky?.086f:family==23 && v==4?.05f:.025f;
                float ripple=pressure*Mathf.Sin(ax*(chunky?6:11)+phase+az*.9f)*Mathf.Cos(y*12+phase);
                float diagonal=ax*(v%2==0?1:-1)-(.28f+.11f*(v%4))+(y-fill)*(pillow?.70f:1.58f);
                float fold=Mathf.Exp(-diagonal*diagonal*64f)*body;
                float tuck=zipper?Mathf.Exp(-Mathf.Pow((edge-.69f+y*1.85f)*8,2))*Mathf.Clamp01(1-y*2.4f):0;
                float load=chunky?.075f*Mathf.Exp(-Mathf.Pow((ax-.20f*Mathf.Sin(phase))*2.8f,2)
                    -Mathf.Pow((y-fill)*5.2f,2)):0;
                float depth=az*(radius*(1-.58f*Mathf.Pow(edge,3))+(ripple*body+load-.081f*fold)*(1-edge)-.061f*tuck);
                if(face==2 && pillow)depth+=.063f*Mathf.Exp(-Mathf.Pow((ax-.67f)*9,2))*body;
                float x=ax*(.448f+.048f*body+.032f*y)-ax*(side?.060f*(1-Mathf.Abs(az))*body:0)
                    +.022f*Mathf.Sin(y*5+phase)*body+.017f*y*y*Mathf.Sin(phase);
                // Loose IQF produce settles on a broad base. Pillow bags pull
                // inward under the upper jaw, while the unfilled film flares above it.
                float shoulder=Mathf.Exp(-Mathf.Pow((y-(.75f+.025f*(v%3)))*6.2f,2));
                float bottom=Mathf.Clamp01(1-y*3.5f);
                x+=ax*((zipper?.045f:-.053f)*bottom-(pillow?.095f:chunky?.041f:.022f)*shoulder);
                if(chunky)x+=ax*.031f*Mathf.Sin(y*16+phase+ax*1.8f)*body*edge;
                if(zipper && r==13)x-=.027f*ax*Mathf.Pow(edge,12);
                float teeth=(!zipper && r==rows.Length-1)?.006f*Mathf.Cos((ax+1)*Mathf.PI*6):0;
                float sag=.030f*ax*Mathf.Sin(phase)*y+.019f*Mathf.Sin(3*ax+phase)*body+teeth;
                verts.Add(new Vector3(x,y+sag,depth));
                float min=face==0?0:face==2?.5f:.875f;
                float max=face==0?.5f:face==2?.867f:1;
                uvs.Add(new Vector2(Mathf.Lerp(min+.001f,max-.001f,u),.002f+y*.996f));
                if(c>0 && r>0){int d=verts.Count-1,b=d-cols-1;tris.AddRange(new[]{b-1,d-1,b,b,d-1,d});}
            }
            // The sides have their own UV islands, with matching perimeter vertices at both ends.
            if(face==3){
                for(int end=0;end<2;end++){
                    int row=end==0?0:rows.Length-1;
                    int f=row*13, back=(13+5)*rows.Length+row*13;
                    for(int j=0;j<12;j++){
                        int a=f+j,b=f+j+1,d=back+12-j,e=d-1;
                        if(end==0)tris.AddRange(new[]{a,b,d,b,e,d});else tris.AddRange(new[]{a,d,b,b,d,e});
                    }
                }
            }
        }
        result.Clear();
        result.SetVertices(verts);result.SetUVs(0,uvs);result.SetTriangles(tris,0);
        result.RecalculateNormals();result.RecalculateBounds();
    }
}
