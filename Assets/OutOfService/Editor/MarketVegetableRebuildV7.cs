using System.Collections.Generic;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    static readonly HashSet<Mesh> preparedHarvestNormals=new HashSet<Mesh>();
    // Original geometry informed by the referenced retail photographs. Dimensions
    // below are authored game sizes, not measurements supplied by the retailers.
    static Mesh HarvestBody(int kind,int variation)
    {
        const int rings=20,sides=32;
        var mesh=MallMesh((kind==7?"harvest_body_v2_":"harvest_body_v1_")+kind+"_"+variation,(v,u,t)=>{
            for(int row=0;row<=rings;row++)for(int col=0;col<=sides;col++){
                float f=(float)row/rings,a=2*Mathf.PI*col/sides;
                float radius=.5f*Mathf.Sin(f*Mathf.PI),y=-.5f*Mathf.Cos(f*Mathf.PI);
                float xShift=.012f*Mathf.Sin(f*Mathf.PI)*Mathf.Sin(variation+f*3);
                if(kind==7){
                    // A nearly parallel-sided fruit with short rounded ends;
                    // stretching a sphere produces a pointed spindle instead.
                    y=f-.5f;
                    float end=Mathf.Min(f,1-f);
                    radius=.47f*Mathf.Sin(Mathf.Min(1,end/.115f)*Mathf.PI*.5f);
                    radius*=1+.025f*Mathf.Sin(a*3+f*9+variation)+.08f*Mathf.Sin(f*4+variation*.7f);
                    xShift=(.10f+.028f*variation)*Mathf.Sin(f*Mathf.PI);
                }else if(kind==6){
                    radius*=1+.012f*Mathf.Cos(a*5+variation)*Mathf.Pow(Mathf.Abs(y)*2,3);
                    y*=.91f;
                    y-=.065f*Mathf.Pow(Mathf.Clamp01((f-.78f)/.22f),3);
                    y+=.022f*Mathf.Pow(Mathf.Clamp01((.16f-f)/.16f),3);
                }else if(kind==12){
                    radius*=1+.115f*Mathf.Cos(a*4+variation*.27f);
                    radius*=1+.20f*y;
                    y-=Mathf.Sign(y)*.135f*Mathf.Pow(Mathf.Abs(y)*2,7);
                }else{
                    radius*=1+.055f*Mathf.Sin(a*3+variation)+.035f*Mathf.Sin(a*2-f*4);
                    xShift+=.045f*Mathf.Sin(f*4+variation)*Mathf.Sin(f*Mathf.PI);
                }
                v.Add(new Vector3(Mathf.Cos(a)*radius+xShift,y,Mathf.Sin(a)*radius));
                u.Add(new Vector2((float)col/sides,f));
                if(row==rings||col==sides)continue;
                int k=row*(sides+1)+col;t.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});
            }
        });
        // Weld UV seam shading without welding UV coordinates.
        if(!preparedHarvestNormals.Add(mesh))return mesh;
        var normals=mesh.normals;
        for(int row=0;row<=rings;row++){
            int first=row*(sides+1),last=first+sides;
            var n=(normals[first]+normals[last]).normalized;normals[first]=normals[last]=n;
            if(row==0||row==rings)for(int col=0;col<=sides;col++)normals[first+col]=row==0?Vector3.down:Vector3.up;
        }
        mesh.normals=normals;mesh.RecalculateTangents();UnityEditor.EditorUtility.SetDirty(mesh);
        return mesh;
    }
    static float VegetableMottle(float u,float v,float frequency)
    {
        float a=u*2*Mathf.PI,r=Mathf.Sin(v*Mathf.PI);
        float x=Mathf.Cos(a)*r,y=-Mathf.Cos(v*Mathf.PI),z=Mathf.Sin(a)*r;
        // Sample a continuous 3D field: latitude strips otherwise collapse to
        // radial wood-grain streaks at the tips of round fruit and potatoes.
        return (Mathf.PerlinNoise(17+x*frequency,11+y*frequency)+
            Mathf.PerlinNoise(37+y*frequency,23+z*frequency)+Mathf.PerlinNoise(71+z*frequency,19+x*frequency))/3f;
    }
    static float CucumberPip(float u,float v)
    {
        float x=u*16,y=v*34;int col=Mathf.FloorToInt(x),row=Mathf.FloorToInt(y);
        if(ProduceNoise(col+53,row)<.48f)return 0;
        float dx=x-col-.24f-.49f*ProduceNoise(col,row);
        float dy=y-row-.24f-.49f*ProduceNoise(row+37,col);
        return Mathf.Exp(-(dx*dx+dy*dy)*55)*(.35f+.65f*ProduceNoise(col+71,row));
    }
    static float PotatoEye(float u,float v,float radius)
    {
        float a=u*2*Mathf.PI,r=Mathf.Sin(v*Mathf.PI);
        var p=new Vector3(Mathf.Cos(a)*r,-Mathf.Cos(v*Mathf.PI),Mathf.Sin(a)*r);
        float value=0;
        for(int n=0;n<11;n++){
            float y=-.84f+n*.168f,angle=n*2.39996f+.73f,ring=Mathf.Sqrt(1-y*y);
            var eye=new Vector3(Mathf.Cos(angle)*ring,y,Mathf.Sin(angle)*ring);
            value=Mathf.Max(value,Mathf.Exp(-(p-eye).sqrMagnitude/(radius*radius)));
        }
        return value;
    }
    static Material HarvestSkin(int kind)
    {
        var mat=RetailSurface("harvest skin v4 "+kind,Color.white,0,kind==6?.46f:kind==7?.32f:.15f);
        if(mat.mainTexture)return mat;
        mat.mainTexture=FinishTexture("HarvestSkinV4_"+kind,(u,v)=>{
            float n=VegetableMottle(u,v,4),fine=VegetableMottle(u,v,75);
            Color c;
            if(kind==6)c=Color.Lerp(new Color(.72f,.09f,.025f),new Color(.91f,.20f,.045f),n);
            else if(kind==7){
                float green=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.84f,v))+.19f*(n-.5f);
                c=Color.Lerp(new Color(.62f,.72f,.20f),new Color(.11f,.28f,.075f),green);
                c*=.87f+.18f*n-.20f*CucumberPip(u,v);
            }else {
                c=Color.Lerp(new Color(.53f,.40f,.23f),new Color(.66f,.52f,.33f),n);
                c*=1-.28f*PotatoEye(u,v,.09f)-.25f*PotatoEye(u,v,.025f);
                c*=.91f+.14f*VegetableMottle(u,v,170);
            }
            c*=.96f+.08f*fine;
            return c.linear;
        });
        FineRelief(mat,"HarvestReliefV3_"+kind,(u,v)=>kind==7?CucumberPip(u,v)*.20f+VegetableMottle(u,v,120)*.035f:
            VegetableMottle(u,v,110)*(kind==6?.035f:.11f)-(kind==4?PotatoEye(u,v,.038f)*.13f:0),1);
        return mat;
    }
    static Mesh TomatoCalyx(int variant)
    {
        return MallMesh("tomato_calyx_v1_"+variant,(v,u,t)=>{
            for(int leaf=0;leaf<5;leaf++){
                float a=(leaf*72+variant*13)*Mathf.Deg2Rad;
                var axis=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=Vector3.Cross(Vector3.up,axis);
                for(int j=0;j<=5;j++){
                    float f=j/5f,r=.018f+f*.28f;
                    float w=Mathf.Sin(f*Mathf.PI)*.042f+.009f*(1-f);
                    var p=axis*r+Vector3.up*(.008f+.038f*Mathf.Sin(f*Mathf.PI)-.015f*f*f);
                    v.Add(p-side*w);v.Add(p+side*w);u.Add(new Vector2(0,f));u.Add(new Vector2(1,f));
                    if(j==5)continue;int k=leaf*12+j*2;t.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});
                }
            }
            MallTwoSides(v,u,t);
        });
    }
    static void HarvestTomato(Vector3 p,int seed,Transform parent)
    {
        float diameter=.091f+ProduceNoise(seed,19)*.018f;
        var fruit=HandmadeProp("ripe tomato with shallow crown",HarvestBody(6,seed%5),p,
            new Vector3(diameter,diameter*.91f,diameter),HarvestSkin(6),parent);
        fruit.transform.rotation=Quaternion.Euler(ProduceNoise(seed,9)*16-8,seed*137.5f,ProduceNoise(seed,8)*16-8);
        var crown=p+fruit.transform.up*diameter*.357f;
        var leaf=HandmadeProp("five pointed tomato sepals",TomatoCalyx(seed%3),crown,Vector3.one*diameter,
            RetailSurface("tomato dry green sepals",new Color(.18f,.27f,.08f),0,.18f),parent);
        leaf.transform.rotation=fruit.transform.rotation;
        RailSegment("short tomato pedicel",crown,crown+fruit.transform.up*.008f+fruit.transform.right*.003f,.0027f,green,parent);
    }
    static void HarvestTomatoPunnet(Vector3 p,Transform parent)
    {
        int seed=Mathf.Abs(Mathf.RoundToInt(p.x*89+p.z*113));
        var pack=new GameObject("six ripe tomato retail punnet").transform;pack.SetParent(parent);pack.position=p;parent=pack;
        var plastic=ChilledWrap();
        HandmadeProp("ribbed transparent tomato punnet base",ProduceShell(false,false),p,new Vector3(.36f,.027f,.25f),plastic,parent);
        for(int row=0;row<2;row++)for(int col=0;col<3;col++)
            HarvestTomato(p+new Vector3((col-1)*.113f,.058f,(row-.5f)*.115f),seed+row*7+col,parent);
        var cover=HandmadeProp("clear tomato clamshell",ProduceShell(true,false),p,new Vector3(.367f,.119f,.257f),plastic,parent);
        cover.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        foreach(int side in new[]{-1,1})for(int n=0;n<7;n++)
            Decor("clamshell pressed side rib",PrimitiveType.Cube,p+new Vector3((n-3)*.048f,.042f,side*.123f),new Vector3(.003f,.062f,.004f),plastic,parent);
        var ticket=Decor("tomato retail paper label",PrimitiveType.Cube,p+new Vector3(.087f,.121f,-.043f),new Vector3(.092f,.001f,.055f),priceWhite,parent);
        var old=root;root=parent;
        Label("완숙 토마토\n국내산 · 6입",p+new Vector3(.087f,.122f,-.043f),new Vector3(90,0,0),.011f);
        root.GetChild(root.childCount-1).GetComponent<TextMesh>().color=new Color(.12f,.15f,.11f);
        root=old;
        pack.rotation=Quaternion.Euler(0,seed%7-3,0);
    }
    static Mesh HarvestLeaf(int variation)
    {
        return MallMesh("ssam_leaf_curved_v2_"+variation,(v,u,t)=>{
            const int rows=16,cols=10;
            for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++){
                float f=(float)row/rows,s=2f*col/cols-1;
                float width=.026f+(variation==0?.33f:variation==1?.46f:.50f)*Mathf.Pow(Mathf.Max(0,Mathf.Sin(f*Mathf.PI)),variation==1?.48f:.72f);
                float edge=Mathf.Pow(Mathf.Abs(s),4)*(.022f*Mathf.Sin(f*36+variation)+.014f*Mathf.Sin(f*63+1));
                float y=.12f*Mathf.Sin(f*Mathf.PI)+(variation==2?.22f:.12f)*s*s*Mathf.Sin(f*Mathf.PI)+edge-.09f*f*f;
                v.Add(new Vector3(s*width,y,f));u.Add(new Vector2((s+1)*.5f,f));
                if(row==rows||col==cols)continue;int k=row*(cols+1)+col;
                t.AddRange(new[]{k,k+cols+1,k+1,k+1,k+cols+1,k+cols+2});
            }
            MallTwoSides(v,u,t);
        });
    }
    static Material HarvestLeafSkin()
    {
        var mat=RetailSurface("ssam branching veins v1",Color.white,0,.22f);
        if(mat.mainTexture)return mat;
        float Veins(float u,float v){float s=Mathf.Abs(u-.5f);float branch=Mathf.Abs(Mathf.Sin((v-s*.58f)*Mathf.PI*9));return Mathf.Max(Mathf.Exp(-s*120),Mathf.Exp(-branch*36)*Mathf.Clamp01(s*12))*(.55f+.45f*(1-v));}
        mat.mainTexture=FinishTexture("SsamLeafColorV1",(u,v)=>{
            float n=Mathf.PerlinNoise(u*17,v*20),rib=Veins(u,v);
            return Color.Lerp(Color.Lerp(new Color(.13f,.29f,.045f),new Color(.36f,.51f,.13f),n),new Color(.57f,.65f,.30f),rib*.56f).linear;
        });
        FineRelief(mat,"SsamLeafVeinsV1",(u,v)=>Veins(u,v)*.12f+Mathf.PerlinNoise(u*70,v*80)*.025f,1);
        return mat;
    }
    static void HarvestScallions(Vector3 p,int seed,Transform parent)
    {
        var old=root;root=new GameObject("natural bundled scallions").transform;root.SetParent(parent);
        var shank=RetailSurface("scallion tubular gradient v3",Color.white,0,.26f);
        if(!shank.mainTexture)shank.mainTexture=FinishTexture("ScallionTubularColorV3",(u,v)=>{
            float green=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.23f,.57f,v)),grain=.96f+.06f*Mathf.PerlinNoise(u*45,v*3);
            return (Color.Lerp(new Color(.83f,.85f,.69f),new Color(.13f,.32f,.13f),green)*grain).linear;
        });
        var greenLeaf=RetailSurface("scallion tubular green",new Color(.13f,.30f,.12f),0,.30f);
        var rootFibre=RetailSurface("scallion root ivory",new Color(.58f,.48f,.32f),0,.13f);
        for(int stalk=0;stalk<4;stalk++){
            var start=new Vector3((stalk-1.5f)*.025f,.012f,-.24f+ProduceNoise(seed,stalk)*.025f);
            float length=.46f+.045f*ProduceNoise(stalk,seed),turn=(stalk-1.5f)*1.5f;
            var stem=HandmadeProp("white to green scallion stem",ScallionTube(stalk),start,new Vector3(1,1,length),shank,root);
            stem.transform.rotation=Quaternion.Euler(0,turn,0);
            for(int leaf=0;leaf<2;leaf++){
                var branch=HandmadeProp("hollow branching scallion leaf",ScallionTube((leaf+stalk)%4),start+new Vector3(.004f,.005f,.21f+leaf*.018f),new Vector3(.68f,.74f,.23f+leaf*.027f),greenLeaf,root);
                branch.transform.rotation=Quaternion.Euler(-2,turn+(leaf==0?-8:9)+ProduceNoise(seed,leaf+stalk)*4,0);
            }
            for(int fibre=0;fibre<3;fibre++){
                var foot=start+new Vector3((fibre-1)*.004f,-.002f,0);
                RailSegment("trimmed scallion root",foot,foot+new Vector3((fibre-1)*.003f,.001f,-.012f-.008f*ProduceNoise(seed+fibre,stalk)),.0012f,rootFibre,root);
            }
        }
        foreach(float y in new[]{.001f,.027f})Decor("thin scallion paper binding",PrimitiveType.Cube,new Vector3(0,y,-.10f),new Vector3(.108f,.0014f,.025f),cream,root);
        foreach(float x in new[]{-.054f,.054f})Decor("folded binding edge",PrimitiveType.Cube,new Vector3(x,.014f,-.10f),new Vector3(.0014f,.027f,.025f),cream,root);
        var bundleBounds=ModelBounds(root.gameObject);
        if(bundleBounds.size.x>.28f || bundleBounds.size.y>.12f || bundleBounds.size.z>.65f || float.IsNaN(bundleBounds.size.sqrMagnitude))
            throw new System.InvalidOperationException("Scallion bundle exceeded its crate allocation: "+seed+" "+bundleBounds.size);
        root.position=p;root.rotation=Quaternion.Euler(0,(ProduceNoise(seed,71)-.5f)*12,0);root=old;
    }
    static Mesh ScallionTube(int variation)
    {
        return MallMesh("scallion_tubular_v4_"+variation,(v,u,t)=>{
            const int rows=18,sides=10;
            for(int row=0;row<=rows;row++)for(int col=0;col<=sides;col++){
                float f=(float)row/rows,a=col*Mathf.PI*2/sides;
                float radius=.0085f*(1-.82f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.32f,1,f)));
                float x=(variation-1.5f)*.006f*f*f+.008f*Mathf.Sin(f*4+variation)*f;
                v.Add(new Vector3(x+Mathf.Cos(a)*radius,.006f*Mathf.Sin(f*Mathf.PI)+Mathf.Sin(a)*radius,f));u.Add(new Vector2((float)col/sides,f));
                if(row<rows&&col<sides){int k=row*(sides+1)+col;t.AddRange(new[]{k,k+1,k+sides+1,k+1,k+sides+2,k+sides+1});}
            }
            // The white basal plate is closed; only cut green tips are hollow.
            int basal=v.Count;v.Add(Vector3.zero);u.Add(new Vector2(.5f,0));
            for(int side=0;side<sides;side++)t.AddRange(new[]{basal,side+1,side});
            MallTwoSides(v,u,t);
        });
    }
}
