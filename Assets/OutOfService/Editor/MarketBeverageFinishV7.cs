using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class MarketPrototypeBuilderV7
{
    // Original molds based on the construction visible in retail reference photos.
    // Body, liquid, neck, closure and fitted print share meshes across facings.
    static float BeverageRadius(string kind,float y)
    {
        float[] heights,radii;
        if(kind=="cola"){
            heights=new[]{0f,.035f,.12f,.25f,.39f,.66f,.73f,.80f,.87f,.92f,1f};
            radii=new[]{.27f,.43f,.48f,.46f,.405f,.405f,.47f,.435f,.29f,.165f,.165f};
        }else if(kind=="cider"){
            heights=new[]{0f,.035f,.11f,.32f,.39f,.69f,.73f,.80f,.88f,.94f,1f};
            radii=new[]{.28f,.43f,.48f,.465f,.48f,.48f,.49f,.45f,.30f,.17f,.17f};
        }else if(kind=="sport"){
            heights=new[]{0f,.035f,.10f,.29f,.39f,.69f,.75f,.82f,.90f,.94f,1f};
            radii=new[]{.30f,.44f,.48f,.46f,.48f,.48f,.49f,.45f,.31f,.23f,.23f};
        }else if(kind=="tea"){
            heights=new[]{0f,.025f,.09f,.34f,.40f,.69f,.76f,.83f,.91f,.95f,1f};
            radii=new[]{.31f,.43f,.48f,.47f,.475f,.475f,.46f,.37f,.22f,.17f,.17f};
        }else{
            heights=new[]{0f,.035f,.09f,.34f,.41f,.70f,.75f,.81f,.88f,.94f,1f};
            radii=new[]{.30f,.43f,.48f,.47f,.48f,.48f,.46f,.39f,.28f,.17f,.17f};
        }
        for(int i=1;i<heights.Length;i++)if(y<=heights[i]){
            float span=heights[i]-heights[i-1],s=(radii[i]-radii[i-1])/span;
            float left=i>1?(radii[i-1]-radii[i-2])/(heights[i-1]-heights[i-2]):s;
            float right=i<heights.Length-1?(radii[i+1]-radii[i])/(heights[i+1]-heights[i]):s;
            float m0=left*s>0?2*left*s/(left+s):0,m1=right*s>0?2*right*s/(right+s):0;
            float t=Mathf.InverseLerp(heights[i-1],heights[i],y),t2=t*t,t3=t2*t;
            return (2*t3-3*t2+1)*radii[i-1]+(t3-2*t2+t)*span*m0+(-2*t3+3*t2)*radii[i]+(t3-t2)*span*m1;
        }
        return radii[radii.Length-1];
    }

    static Mesh BeverageMold(string kind,bool liquid=false)
    {
        string key="BeverageMoldV2_"+kind+(liquid?"_liquid":"_shell");
        if(handmadeMeshes.TryGetValue(key,out var mesh))return mesh;
        string path="Assets/OutOfService/Generated/"+key+".asset";
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){
            const int sides=32,rings=33;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int row=0;row<rings;row++)for(int s=0;s<=sides;s++){
                float y=(float)row/(rings-1),angle=s*Mathf.PI*2/sides;
                if(liquid)y*=.94f; // Visible air gap just below the closure.
                float radius=BeverageRadius(kind,y);
                if(!liquid){
                    float low=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.10f,.16f,y))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.34f,.40f,y)));
                    if(kind.StartsWith("water"))radius-=low*.012f*(.5f+.5f*Mathf.Cos((y*22+(kind=="water_spiral"?angle*.22f:0))*Mathf.PI*2));
                    if(kind=="cider"||kind=="cola")radius-=low*.009f*Mathf.Pow(.5f+.5f*Mathf.Cos(angle*10+y*18),3);
                    float foot=Mathf.Clamp01(1-y/.09f);
                    radius-=foot*.065f*(.5f+.5f*Mathf.Cos(angle*5));
                    y+=foot*.018f*(.5f+.5f*Mathf.Cos(angle*5));
                }else{
                    float foot=Mathf.Clamp01(1-y/.09f);
                    radius=(radius-foot*.065f*(.5f+.5f*Mathf.Cos(angle*5)))*.966f;
                    y+=foot*.018f*(.5f+.5f*Mathf.Cos(angle*5));
                }
                vertices.Add(new Vector3(Mathf.Sin(angle)*radius,y,-Mathf.Cos(angle)*radius));uv.Add(new Vector2((float)s/sides,y));
            }
            for(int row=0;row<rings-1;row++)for(int s=0;s<sides;s++){
                int a=row*(sides+1)+s,b=a+1,c=a+sides+1,d=c+1;
                triangles.AddRange(new[]{a,c,b,b,c,d});
            }
            if(liquid){
                int center=vertices.Count;vertices.Add(new Vector3(0,.94f,0));uv.Add(new Vector2(.5f,.5f));
                for(int s=0;s<sides;s++)triangles.AddRange(new[]{center,(rings-1)*(sides+1)+s+1,(rings-1)*(sides+1)+s});
            }
            mesh=new Mesh{name=key};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
            var normals=mesh.normals;
            for(int row=0;row<rings;row++){
                int a=row*(sides+1),b=a+sides;var n=(normals[a]+normals[b]).normalized;normals[a]=normals[b]=n;
            }
            mesh.normals=normals;mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[key]=mesh;return mesh;
    }

    static Material BeveragePET()
    {
        const string key="clear beverage PET v1";
        if(retailSurfaces.TryGetValue(key,out var mat))return mat;
        var shader=Shader.Find("OutOfService/Thin PET");
        if(!shader)throw new System.InvalidOperationException("Missing Thin PET shader");
        mat=new Material(shader){name=key,enableInstancing=true,renderQueue=(int)RenderQueue.Transparent};
        mat.SetColor("_BaseColor",new Color(.985f,.995f,1,1));mat.SetFloat("_Smoothness",.88f);
        mat.SetFloat("_RefractionPixels",8);mat.SetFloat("_EdgeOpacity",.26f);mat.SetFloat("_EdgeLoss",.55f);
        retailSurfaces[key]=mat;return mat;
    }

    static Mesh BeverageLabel(string kind,bool back)
    {
        string key="BeverageLabelV2_"+kind+(back?"_back":"_front");
        if(handmadeMeshes.TryGetValue(key,out var mesh))return mesh;
        string path="Assets/OutOfService/Generated/"+key+".asset";mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){
            const int cols=24,rows=8;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            float bottom=kind=="cola"?.40f:.42f,top=kind=="cola"?.665f:.70f;
            for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++){
                float u=(float)x/cols,h=Mathf.Lerp(bottom,top,(float)y/rows);
                float angle=Mathf.Lerp(back?75:-75,back?285:75,u)*Mathf.Deg2Rad;
                float r=BeverageRadius(kind,h)+.006f;
                v.Add(new Vector3(Mathf.Sin(angle)*r,h,-Mathf.Cos(angle)*r));uv.Add(new Vector2(u,(float)y/rows));
                if(x==cols||y==rows)continue;
                int a=y*(cols+1)+x,b=a+1,c=a+cols+1,d=c+1;t.AddRange(new[]{a,c,b,b,c,d});
            }
            mesh=new Mesh{name=key};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[key]=mesh;return mesh;
    }

    static Mesh BeverageClosure()
    {
        const string key="BeverageClosureV1";
        if(handmadeMeshes.TryGetValue(key,out var mesh))return mesh;
        string path="Assets/OutOfService/Generated/"+key+".asset";mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){
            const int sides=64;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            float[] h={0,.09f,.16f,.24f,.82f,.94f,1f,1f},r={.47f,.48f,.46f,.49f,.49f,.48f,.45f,0};
            for(int ring=0;ring<h.Length;ring++)for(int s=0;s<=sides;s++){
                float angle=s*Mathf.PI*2/sides;
                float radius=r[ring]-(ring>=3&&ring<=4&&s%2==0?.010f:0);
                v.Add(new Vector3(Mathf.Sin(angle)*radius,h[ring],-Mathf.Cos(angle)*radius));uv.Add(new Vector2((float)s/sides,h[ring]));
                if(ring==h.Length-1||s==sides)continue;
                int a=ring*(sides+1)+s,b=a+1,c=a+sides+1,d=c+1;t.AddRange(new[]{a,c,b,b,c,d});
            }
            mesh=new Mesh{name=key};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var n=mesh.normals;for(int row=0;row<h.Length;row++){int a=row*(sides+1),b=a+sides;var norm=(n[a]+n[b]).normalized;n[a]=n[b]=norm;}mesh.normals=n;
            mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        handmadeMeshes[key]=mesh;return mesh;
    }

    static void FinishedBeverage(int family,int variant,Vector3 p,Material ink,Transform group)
    {
        bool water=family==10,cola=family==11&&(variant==0||variant==2),cider=family==11&&variant==1;
        string kind=water?(variant%2==0?"water_ring":"water_spiral"):cola?"cola":family==12?"sport":family==14?"tea":"cider";
        float w=water&&variant%2==0?.103f:water?.070f:family==13&&variant==3?.098f:.076f;
        float h=water&&variant%2==0?.31f:family==13&&variant==3?.181f:.226f;
        var scale=new Vector3(w,h,w);
        var shell=HandmadeProp("thin clear PET with formed base",BeverageMold(kind),p,scale,BeveragePET(),group);
        shell.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        bool clear=water||cider||family==12&&variant==0;
        if(!clear){
            Color color=cola?new Color(.065f,.022f,.008f):family==11&&variant==4?new Color(.91f,.90f,.79f):
                family==12?(variant==1?new Color(.81f,.89f,.65f):variant==2?new Color(.16f,.055f,.22f):variant==3?new Color(.82f,.86f,.70f):new Color(.94f,.58f,.12f)):
                family==14?Color.Lerp(new Color(.16f,.065f,.015f),new Color(.47f,.32f,.10f),variant%3*.3f):
                family==13&&variant==2?new Color(.68f,.57f,.33f):family==13&&variant==5?new Color(.61f,.36f,.08f):new Color(.91f,.40f,.025f);
            HandmadeProp("filled beverage with air gap",BeverageMold(kind,true),p,scale,RetailSurface("beverage liquid v1 "+family+"-"+variant,color,0,.44f),group);
        }
        Color capColor=cola?(variant==2?new Color(.07f,.07f,.075f):new Color(.64f,.018f,.024f)):cider?new Color(.015f,.36f,.045f):
            family==11&&variant==3?new Color(.93f,.26f,.025f):water&&variant>=4?new Color(.12f,.36f,.64f):new Color(.87f,.88f,.85f);
        float capWidth=w*(kind=="sport"?.49f:.37f);
        HandmadeProp("ribbed closure and separate tamper groove",BeverageClosure(),p+Vector3.up*(h-.002f),new Vector3(capWidth,.016f,capWidth),
            RetailSurface("beverage cap v1 "+ColorUtility.ToHtmlStringRGB(capColor),capColor,0,.30f),group);
        HandmadeProp("fitted beverage front label",BeverageLabel(kind,false),p,scale,ink,group);
        HandmadeProp("fitted full wrap consumer information",BeverageLabel(kind,true),p,scale,InformationInk(family,variant),group);
    }
}
