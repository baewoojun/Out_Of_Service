using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    // Original tenant props. All positions are local to MallUnit before its final rotation.
    static Mesh MallMesh(string key,Action<List<Vector3>,List<Vector2>,List<int>> make)
    {
        key="MallDetailV3_"+key;
        if(handmadeMeshes.TryGetValue(key,out var mesh))return mesh;
        string path="Assets/OutOfService/Generated/"+key+".asset";
        mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){var v=new List<Vector3>();var u=new List<Vector2>();var t=new List<int>();make(v,u,t);
            foreach(var p in v)if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.sqrMagnitude))throw new InvalidOperationException("Invalid tenant mesh vertex: "+key);
            if(v.Count==0||t.Count==0)throw new InvalidOperationException("Empty tenant mesh: "+key);
            mesh=new Mesh{name=key};mesh.SetVertices(v);mesh.SetUVs(0,u);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);}
        handmadeMeshes[key]=mesh;return mesh;
    }
    static void MallTwoSides(List<Vector3> v,List<Vector2> u,List<int> t)
    {
        int n=v.Count,count=t.Count;v.AddRange(v.ToArray());u.AddRange(u.ToArray());
        for(int i=0;i<count;i+=3)t.AddRange(new[]{t[i]+n,t[i+2]+n,t[i+1]+n});
    }
    static Mesh FloristLeaf(int style)
    {
        return MallMesh("leaf_botanical_v4_"+style,(v,u,t)=>{
            const int rows=16,cols=6;
            for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++){
                float f=y/(float)rows,s=x/(float)cols*2-1;
                float width=Mathf.Pow(Mathf.Max(.0001f,Mathf.Sin(Mathf.Pow(f,.82f)*Mathf.PI)),style==0?.85f:.64f)*(style==2?.23f:style==0?.24f:.34f);
                width*=1+.035f*Mathf.Sin(f*87+style)+s*.045f;
                float center=.11f*Mathf.Sin(f*Mathf.PI)-.14f*f*f;
                v.Add(new Vector3(s*width+.025f*Mathf.Sin(f*4),center-.065f*Mathf.Abs(s)+.025f*s*Mathf.Sin(f*12+style),f));u.Add(new Vector2(x/(float)cols,f));
                if(y<rows&&x<cols){int k=y*(cols+1)+x;t.AddRange(new[]{k,k+cols+1,k+1,k+1,k+cols+1,k+cols+2});}
            }
            MallTwoSides(v,u,t);
        });
    }
    static Material FloristGreen(int shade)
    {
        var mat=RetailSurface("florist leaf satin "+shade,shade==0?new Color(.19f,.31f,.12f):new Color(.28f,.38f,.17f),0,.30f);
        mat.mainTexture=FinishTexture("FloristVeinsV4",(u,v)=>{
            float vein=Mathf.Exp(-Mathf.Abs(u-.5f)*170)*.27f;
            float lateral=Mathf.Pow(Mathf.Max(0,Mathf.Cos((v-Mathf.Abs(u-.5f)*.75f)*60)),35)*.12f;
            float grain=Mathf.PerlinNoise(u*95,v*110)*.06f;
            float mottling=Mathf.PerlinNoise(u*9+12,v*13)*.18f;
            return new Color(.52f+vein+lateral+grain+mottling,.64f+vein+lateral+grain+mottling,.40f+vein+grain+mottling,1);
        });return mat;
    }
    static Mesh FlowerHead(int style,int variant)
    {
        if(style==0)return NaturalRose(variant);
        return MallMesh("petal_rounded_v6_"+style+"_"+variant,(v,u,t)=>{
            int layers=style==0?3:2;
            for(int layer=0;layer<layers;layer++){
            int count=style==0?(layer==0?6:layer==1?5:3):(layer==0?12:10);
            for(int petal=0;petal<count;petal++){
                int start=v.Count;float angle=(petal+layer*.43f)*Mathf.PI*2/count+.075f*Mathf.Sin(petal*9+variant*4);
                var outward=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var cross=new Vector3(-outward.z,0,outward.x);
                float reach=(style==0?1-layer*.32f:1-layer*.15f)*(1+.065f*Mathf.Sin(petal*13+variant));
                const int rows=4;int cols=style==0?7:4;
                for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++){
                    float f=y/(float)rows,s=x/(float)cols*2-1;
                    if(style==0){
                        // A curved, rounded lobe with a rolled lip, not a triangular
                        // wedge. Four longitudinal spans resolve the curl at eye height.
                        f*=.78f+.22f*Mathf.Cos(s*Mathf.PI*.5f);
                        float a=angle+s*(Mathf.PI*1.44f/count)*(.22f+.78f*Mathf.Sin(f*Mathf.PI*.5f))+.11f*f;
                        float r=(.055f+reach*Mathf.Sin(f*Mathf.PI*.5f))*(1-.12f*s*s);
                        float h=.03f+f*(.54f+layer*.20f)-.17f*Mathf.Pow(f,4)*(1-s*s)+.018f*s*Mathf.Sin(petal*4+variant);
                        h+=.016f*Mathf.Pow(f,3)*Mathf.Sin(s*8+petal*3+variant);
                        v.Add(new Vector3(Mathf.Cos(a)*r,h,Mathf.Sin(a)*r));
                    }else{
                        float wide=.16f*Mathf.Sqrt(Mathf.Max(.001f,Mathf.Sin(f*Mathf.PI*.87f)))*reach;
                        float r=.17f+f*reach-.12f*s*s*f*f;
                        v.Add(outward*r+cross*(s*wide)+Vector3.up*(.025f+layer*.05f+.10f*Mathf.Sin(f*3)-.065f*s*s+.04f*f*Mathf.Sin(petal*3)));
                    }
                    u.Add(new Vector2(x/(float)cols,f));
                    if(y<rows&&x<cols){int k=start+y*(cols+1)+x;t.AddRange(new[]{k,k+cols+1,k+1,k+1,k+cols+1,k+cols+2});}
                }
            }}
            MallTwoSides(v,u,t);
        });
    }
    static Mesh RoundedRose(int variant)
    {
        // Twenty-four rounded disks, each with two curved rings. 1,200 vertices
        // including backs: still eligible for the existing small-prop batching.
        return MallMesh("rose_elliptic_v7_"+variant,(v,u,t)=>{
            int[] counts={9,7,5,3};
            for(int layer=0;layer<4;layer++)for(int petal=0;petal<counts[layer];petal++){
                int start=v.Count;float angle=(petal+layer*.39f)*Mathf.PI*2/counts[layer]+.07f*Mathf.Sin(petal*7+variant*5);
                var outwards=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var cross=new Vector3(-outwards.z,0,outwards.x);
                float reach=(1-layer*.235f)*(1+.05f*Mathf.Sin(petal*13+variant));
                float width=(.48f-layer*.085f)*(1+.08f*Mathf.Sin(petal*4+variant));
                void Point(float x,float y){
                    float f=(y+1)*.5f;
                    float r=.035f+reach*(.06f+.94f*f);
                    float h=.04f+f*(.43f+layer*.12f)+.10f*x*x-.15f*Mathf.Pow(f,4);
                    h+=.025f*x*Mathf.Sin(petal*4+variant)+.013f*f*Mathf.Sin(x*6+petal);
                    v.Add(outwards*r+cross*(x*width)+Vector3.up*h);
                    u.Add(new Vector2(x*.5f+.5f,f));
                }
                Point(0,0);
                foreach(float radius in new[]{.52f,1f})for(int n=0;n<12;n++){
                    float a=n*Mathf.PI*2/12;Point(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius);
                }
                for(int n=0;n<12;n++){
                    int next=(n+1)%12;
                    t.AddRange(new[]{start,start+1+n,start+1+next});
                    t.AddRange(new[]{start+1+n,start+13+n,start+1+next,start+1+next,start+13+n,start+13+next});
                }
            }
            MallTwoSides(v,u,t);
        });
    }
    static Mesh FloristVessel(bool wrap,int variant=0)
    {
        return MallMesh(wrap?"folded_paper_sleeve_v4_"+variant:"open_tapered_bucket",(v,u,t)=>{
            int count=wrap?36:32;
            float[] ys=wrap?new[]{0f,.10f,.19f,.30f,.44f,.56f,.66f}:new[]{0f,.015f,.54f,.55f,.54f,.04f,.04f};
            float[] rs=wrap?new[]{.045f,.055f,.07f,.135f,.22f,.27f,.29f}:new[]{.15f,.16f,.23f,.23f,.216f,.145f,0f};
            for(int row=0;row<ys.Length;row++)for(int a=0;a<=count;a++){
                float f=a/(float)count,theta=f*Mathf.PI*2;
                float fold=wrap?(1+.10f*Mathf.Cos(theta*3+variant)+.045f*Mathf.Sin(theta*9+row*.4f)):1;
                float y=ys[row]+(wrap?.11f*Mathf.Pow(row/6f,3)*Mathf.Sin(theta*2+.7f+variant):0);
                v.Add(new Vector3(Mathf.Cos(theta)*rs[row]*fold,y,Mathf.Sin(theta)*rs[row]*fold));u.Add(new Vector2(f,row/(float)(ys.Length-1)));
                if(row<ys.Length-1&&a<count){int k=row*(count+1)+a;t.AddRange(new[]{k,k+count+1,k+1,k+1,k+count+1,k+count+2});}
            }
            if(wrap)MallTwoSides(v,u,t);
        });
    }
    static void MallPrint(string title,int cell,Vector3 p,Vector2 size,Vector3 rotation=default)
    {
        const string key="mall tenant print atlas";
        if(!retailSurfaces.TryGetValue(key,out var m)){
            string path="Assets/OutOfService/Art/MallDetails/mall-detail-atlas.png";
            AssetDatabase.ImportAsset(path);var ti=(TextureImporter)AssetImporter.GetAtPath(path);
            ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.anisoLevel=4;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
            m=RetailSurface(key,Color.white,0,.16f);m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        HandmadeProp(title,MerchQuad(cell),p,new Vector3(size.x,size.y,1),m,root).transform.rotation=Quaternion.Euler(rotation);
    }
    static void FlowerStem(Vector3 bottom,Vector3 head,int colour,int form,int seed,bool leaves=true)
    {
        Color[] colours={new(.68f,.055f,.105f),new(.96f,.65f,.68f),new(.91f,.84f,.62f),new(.96f,.67f,.13f),new(.58f,.27f,.58f)};
        var stem=FloristGreen(0);var mid=Vector3.Lerp(bottom,head,.55f)+new Vector3(.018f,0,-.014f);
        RailSegment("fresh cut flower lower stem",bottom,mid,.007f,stem,root);RailSegment("fresh cut flower upper stem",mid,head,.006f,stem,root);
        for(int i=0;leaves&&i<2;i++){
            var leaf=HandmadeProp("pointed veined cut-flower leaf",FloristLeaf(0),head-Vector3.up*(.18f-i*.07f),new Vector3(.12f,.15f,.14f),FloristGreen(i),root);
            leaf.transform.rotation=Quaternion.Euler(-20,seed*137+i*165,12);
        }
        var petal=RetailSurface("petal diffuse "+colour,colours[colour%5],0,.13f);
        // Centimetre-thin petals cannot support the scene's metre-scale shadow bias.
        // Stems, foliage and vessels still cast; no per-petal shadow draw calls.
        petal.SetShaderPassEnabled("ShadowCaster",false);
        petal.mainTexture=FinishTexture("FloristPetalV5",(u,v)=>{
            float fiber=Mathf.PerlinNoise(u*80,v*6)*.022f;
            float value=.74f+.18f*Mathf.Sqrt(v)+fiber+.012f*Mathf.Sin(u*42+v*8);
            return new Color(value,value*.97f,value*.94f,1);
        });
        FineRelief(petal,"FloristPetalReliefV5",(u,v)=>.05f*Mathf.PerlinNoise(u*45,v*5)+.025f*Mathf.Sin(u*32+v*5),.8f);
        // One mesh per flower head, including all folded petals; no petal GameObjects.
        float size=(form==0?.058f:.053f)*(.88f+(seed%5)*.055f);
        var flower=HandmadeProp("layered "+(form==0?"rose":"daisy")+" flower",FlowerHead(form,seed%3),head,Vector3.one*size,petal,root);
        flower.transform.rotation=Quaternion.Euler(seed%3*9-9,seed*47,seed%5*5-10);
        if(form==1){var pollen=MallFinish("pollen",new Color(.46f,.29f,.055f),0,.04f);
            Decor("daisy textured pollen disc",PrimitiveType.Sphere,head+Vector3.up*.008f,new Vector3(.025f,.013f,.025f),pollen);}
    }
    static void CutFlowerBucket(Vector3 p,int colour,int form,int seed)
    {
        var enamel=MallFinish("bucket",new Color(.78f,.79f,.72f),0,.28f);
        HandmadeProp("open florist water bucket",FloristVessel(false),p,Vector3.one,enamel,root);
        DetailRing("rolled bucket rim",p+Vector3.up*.545f,new Vector3(.46f,.46f,.04f),enamel,null,new Vector3(90,0,0));
        Decor("water shadow inside bucket",PrimitiveType.Cylinder,p+Vector3.up*.18f,new Vector3(.33f,.004f,.33f),dark);
        for(int n=0;n<9;n++){
            float angle=(n*137.5f+seed*18)*Mathf.Deg2Rad,r=n==0?0:.075f+.015f*(n%4);
            var end=p+new Vector3(Mathf.Cos(angle)*r,.80f+(n%4)*.045f,Mathf.Sin(angle)*r);
            FlowerStem(p+new Vector3(Mathf.Cos(angle)*.03f,.12f,Mathf.Sin(angle)*.03f),end,colour,form,seed+n);
        }
        MallPrint("cut flower price card",0,p+new Vector3(.225f,.36f,0),new Vector2(.19f,.16f),new Vector3(0,-90,0));
    }
    static void PottedFoliage(Vector3 p,int style,float scale)
    {
        BuildBotanicalPot(p,style,scale);
    }
    static void FloristScissors(Vector3 p)
    {
        foreach(int side in new[]{-1,1}){
            DetailRing("scissors handle loop",p+new Vector3(side*.035f,0,.055f),new Vector3(.07f,.10f,.028f),dark,null,new Vector3(90,0,side*10));
            HandmadeProp("florist scissors tapered flat blade",PrepKnifeBlade(),p,new Vector3(.65f,1,.65f),PrepSteel(),root).transform.rotation=Quaternion.Euler(0,-90+side*6,0);
        }
        Decor("scissors pivot",PrimitiveType.Cylinder,p,new Vector3(.025f,.008f,.025f),shelfTop);
    }
    static void BuildFloristDetails()
    {
        var wood=MallFinish("wood",new Color(.40f,.29f,.17f),0,.20f);
        // Recessed, stepped cut-flower wall, accessed from the central shop aisle.
        Box("cut flower display backing",new Vector3(-6.63f,1.25f,7.5f),new Vector3(.15f,2.5f,8.4f),shelf);
        for(int tier=0;tier<2;tier++){
            float x=-5.48f-tier*.67f,y=.20f+tier*.54f;
            Box("flower bucket stepped bench",new Vector3(x,y*.5f,7.5f),new Vector3(.62f,y,8.1f),wood);
            for(int n=0;n<7;n++)CutFlowerBucket(new Vector3(x,y,4+n*1.16f),n%5,(n+1)%2,31+n+tier*9);
        }
        // Packaged bouquets get their own low, front-facing stand, not the cut-flower wall.
        Box("bouquet island timber base",new Vector3(-3.25f,.26f,4.2f),new Vector3(1.45f,.52f,2.4f),wood);
        var papers=new[]{MallFinish("paper ivory",new Color(.78f,.75f,.63f),0,.09f),MallFinish("paper rose",new Color(.64f,.40f,.40f),0,.09f),MallFinish("paper kraft",new Color(.49f,.36f,.20f),0,.08f)};
        for(int n=0;n<6;n++){
            var p=new Vector3(-3.65f+(n%2)*.72f,.52f,3.45f+(n/2)*.72f);
            HandmadeProp("folded open bouquet paper",FloristVessel(true,n%3),p,Vector3.one,papers[n%3],root);
            for(int k=0;k<9;k++){float a=k*137.5f*Mathf.Deg2Rad;FlowerStem(p+Vector3.up*.10f,p+new Vector3(Mathf.Cos(a)*.15f,.76f+(k%3)*.035f,Mathf.Sin(a)*.15f),n%5,n%2,k+n,false);}
            DetailRing("bouquet ribbon tie",p+Vector3.up*.19f,new Vector3(.18f,.18f,.04f),priceWhite,null,new Vector3(90,0,0));
            MallPrint("bouquet gift tag",1,p+new Vector3(0,.37f,-.20f),new Vector2(.12f,.14f));
        }
        for(int tier=0;tier<2;tier++){
            float y=.34f+tier*.57f,x=5.05f+tier*.95f;
            Box("potted foliage nursery bench",new Vector3(x,y*.5f,8.0f),new Vector3(1.05f,y,7.1f),wood);
            for(int n=0;n<5;n++)PottedFoliage(new Vector3(x,y,5.1f+n*1.38f),(n+tier)%2,.58f+(n%3)*.13f);
        }
        MallPrint("plant care wall poster",3,new Vector3(6.84f,2.15f,8),new Vector2(.75f,.85f),new Vector3(0,90,0));
        // Small sale pots occupy the existing gaps; the shop aisle stays clear.
        AccentSucculent(new Vector3(5.05f,.34f,5.80f),18);
        AccentSucculent(new Vector3(5.05f,.34f,8.56f),143,.92f);
        AccentSucculent(new Vector3(6.0f,.91f,9.94f),258,1.05f);
        Box("florist wrapping bench",new Vector3(0,.94f,14.5f),new Vector3(5,.12f,1.2f),wood);
        TableLegs(new Vector3(0,0,14.5f),4.6f,.95f,.88f);
        for(int i=0;i<3;i++){
            var p=new Vector3(-1.8f+i*.27f,1.08f,14.58f);
            Decor("paper roll open cardboard core",PrimitiveType.Cylinder,p,new Vector3(.13f,.44f,.13f),papers[i]).transform.rotation=Quaternion.Euler(90,0,0);
            DetailRing("paper roll tube opening",p+new Vector3(0,0,-.446f),new Vector3(.065f,.065f,.02f),wood);
        }
        var sheet=RetailSurface("thin florist wrapping paper",new Color(.74f,.65f,.49f),0,.08f);
        sheet.SetShaderPassEnabled("ShadowCaster",false);
        MerchBox("partly unrolled wrapping sheet",new Vector3(-.30f,1.003f,14.5f),new Vector3(.80f,.002f,.75f),sheet);
        FloristScissors(new Vector3(.4f,1.015f,14.3f));
        for(int i=0;i<4;i++){
            var p=new Vector3(.9f+i*.23f,1.045f,14.7f);
            Decor("ribbon spool",PrimitiveType.Cylinder,p,new Vector3(.17f,.045f,.17f),papers[i%3]);
            foreach(float y in new[]{-.046f,.046f})Decor("ribbon spool paper flange",PrimitiveType.Cylinder,p+Vector3.up*y,new Vector3(.19f,.003f,.19f),cream);
        }
        MallPrint("flower order on wrapping table",13,new Vector3(1.68f,1.009f,14.12f),new Vector2(.23f,.28f),new Vector3(90,0,0));
        MallPrint("florist bouquet prices",1,new Vector3(0,2.35f,15.87f),new Vector2(.80f,.85f));
    }
    static void SewingStation(Vector3 p)
    {
        var enamel=MallFinish("enamel",new Color(.73f,.75f,.67f),.15f,.24f);
        MerchBox("sewing bed cast base",p+new Vector3(0,.025f,0),new Vector3(.57f,.05f,.23f),enamel);
        MerchBox("sewing column",p+new Vector3(.21f,.20f,0),new Vector3(.13f,.35f,.19f),enamel);
        MerchBox("sewing cast overhead arm",p+new Vector3(.015f,.35f,0),new Vector3(.48f,.12f,.17f),enamel);
        MerchBox("sewing needle head",p+new Vector3(-.21f,.29f,0),new Vector3(.11f,.20f,.18f),enamel);
        // Open throat space under the arm, fine needle and a separate presser foot.
        RailSegment("needle shaft",p+new Vector3(-.21f,.20f,0),p+new Vector3(-.21f,.052f,0),.004f,shelfTop,root);
        MerchBox("presser foot",p+new Vector3(-.21f,.054f,-.02f),new Vector3(.022f,.008f,.05f),shelfTop);
        MerchBox("needle plate",p+new Vector3(-.20f,.051f,0),new Vector3(.12f,.002f,.13f),PrepSteel());
        DetailRing("sewing handwheel",p+new Vector3(.31f,.31f,0),new Vector3(.18f,.18f,.07f),enamel,null,new Vector3(0,90,0));
        foreach(float x in new[]{-.22f,.18f})foreach(float y in new[]{.25f,.37f})
            Decor("cast housing recessed screw",PrimitiveType.Cylinder,p+new Vector3(x,y,-.096f),new Vector3(.011f,.001f,.011f),shelfTop).transform.rotation=Quaternion.Euler(90,0,0);
        Decor("stitch length control dial",PrimitiveType.Cylinder,p+new Vector3(.20f,.17f,-.106f),new Vector3(.058f,.012f,.058f),dark).transform.rotation=Quaternion.Euler(90,0,0);
        MerchBox("sewing model badge",p+new Vector3(-.01f,.35f,-.089f),new Vector3(.16f,.041f,.003f),blue);
        RailSegment("reverse stitching lever",p+new Vector3(.22f,.23f,-.10f),p+new Vector3(.27f,.21f,-.13f),.014f,shelfTop,root);
        Decor("handwheel hub",PrimitiveType.Cylinder,p+new Vector3(.32f,.31f,0),new Vector3(.07f,.025f,.07f),dark).transform.rotation=Quaternion.Euler(0,0,90);
        RailSegment("spool rack upright",p+new Vector3(.45f,0,.20f),p+new Vector3(.45f,.68f,.20f),.014f,shelfTop,root);
        RailSegment("spool guide arm",p+new Vector3(.45f,.65f,.20f),p+new Vector3(.12f,.65f,.20f),.011f,shelfTop,root);
        foreach(float x in new[]{.16f,.36f}){
            Decor("thread cone",PrimitiveType.Cylinder,p+new Vector3(x,.12f,.25f),new Vector3(.095f,.10f,.095f),x<.2f?cream:dark);
            PrepPipe("sewing thread",new[]{p+new Vector3(x,.22f,.25f),p+new Vector3(x,.65f,.20f),p+new Vector3(-.20f,.39f,.01f),p+new Vector3(-.21f,.20f,.01f)},.001f,cream,root);
        }
        MerchBox("sewing foot pedal",new Vector3(p.x,.08f,p.z-.12f),new Vector3(.32f,.045f,.22f),dark).transform.rotation=Quaternion.Euler(-15,0,0);
        RailSegment("pedal linkage",new Vector3(p.x,.10f,p.z),p+new Vector3(.18f,-.13f,0),.008f,shelfTop,root);
    }
    static void LaundryRack(Vector3 p)
    {
        const float length=3.5f,top=1.92f;
        foreach(float end in new[]{-length*.5f,length*.5f}){
            RailSegment("laundry rack upright",p+new Vector3(0,.10f,end),p+new Vector3(0,top,end),.035f,shelfTop,root);
            RailSegment("laundry rack foot",p+new Vector3(-.48f,.10f,end),p+new Vector3(.48f,.10f,end),.04f,shelfTop,root);
            foreach(float x in new[]{-.40f,.40f})Decor("rack caster",PrimitiveType.Sphere,p+new Vector3(x,.07f,end),new Vector3(.09f,.09f,.06f),dark);
        }
        RailSegment("laundry hanger rail",p+new Vector3(0,top,-length*.5f),p+new Vector3(0,top,length*.5f),.032f,shelfTop,root);
        for(int i=0;i<11;i++){
            float z=-1.45f+i*.29f+.020f*Mathf.Sin(i*2.17f);int style=i%3;
            var cloth=GarmentCloth(style,i%4);
            var rotation=Quaternion.Euler(0,32+(i%5-2)*4,0);
            var neck=p+new Vector3(0,top-.16f,z);
            HandmadeProp("pressed customer garment",LaundryGarment(style),neck,Vector3.one,cloth,root).transform.rotation=rotation;
            GarmentDetails(neck,rotation,1,style,root);
            var h=p+new Vector3(0,top-.10f,z);var l=h+new Vector3(-.25f,-.10f,0);var r=h+new Vector3(.25f,-.10f,0);
            l=h+rotation*(l-h);r=h+rotation*(r-h);
            RailSegment("laundry wire hanger",h,l,.007f,shelfTop,root);RailSegment("laundry wire hanger",h,r,.007f,shelfTop,root);RailSegment("hanger bar",l,r,.007f,shelfTop,root);
            DetailRing("hanger hook",h+Vector3.up*.085f,new Vector3(.05f,.065f,.015f),shelfTop);
            MallPrint("numbered laundry claim tag",i%2==0?4:5,h+rotation*new Vector3(.13f,-.13f,-.072f),new Vector2(.050f,.072f),rotation.eulerAngles);
        }
        var collider=root.gameObject.AddComponent<BoxCollider>();collider.center=p+Vector3.up*.96f;collider.size=new Vector3(.98f,1.92f,3.62f);
    }
    static Mesh IroningTop()
    {
        return MallMesh("ironing_board",(v,u,t)=>{
            const int n=32;
            foreach(float y in new[]{-.012f,.012f})for(int i=0;i<n;i++){
                float a=i*Mathf.PI*2/n,z=Mathf.Sin(a)*.62f;
                v.Add(new Vector3(Mathf.Cos(a)*(.195f-.035f*Mathf.Sin(a)),y,z));u.Add(new Vector2((Mathf.Cos(a)+1)*.5f,(Mathf.Sin(a)+1)*.5f));
            }
            for(int i=0;i<n;i++){int j=(i+1)%n;t.AddRange(new[]{i,n+i,j,j,n+i,n+j});}
            // Independent cap vertices keep the padded top flat under a flashlight.
            for(int face=0;face<2;face++){
                int start=v.Count;
                for(int i=0;i<n;i++){v.Add(v[face*n+i]);u.Add(u[face*n+i]);}
                for(int i=1;i<n-1;i++)t.AddRange(face==0?new[]{start,start+i,start+i+1}:new[]{start,start+i+1,start+i});
            }
        });
    }
    static Mesh SteamIronShell()
    {
        return MallMesh("steam_iron_curved_reservoir",(v,u,t)=>{
            const int n=24;
            float[] heights={0,.014f,.065f,.09f,.10f};float[] sizes={1f,1f,.88f,.57f,0f};
            for(int row=0;row<heights.Length;row++)for(int i=0;i<=n;i++){
                float a=i*Mathf.PI*2/n,z=Mathf.Sin(a)*.145f;
                v.Add(new Vector3(Mathf.Cos(a)*(.0775f-.0175f*Mathf.Sin(a))*sizes[row],heights[row],z*sizes[row]));u.Add(new Vector2(i/(float)n,row/4f));
                if(row<heights.Length-1&&i<n){int k=row*(n+1)+i;t.AddRange(new[]{k,k+n+1,k+1,k+1,k+n+1,k+n+2});}
            }
        });
    }
    static void BuildLaundryDetails()
    {
        for(int n=0;n<4;n++)Washer(new Vector3(-4.5f+n*1.1f,0,13.5f));
        Box("washer utility counter",new Vector3(-2.85f,.98f,13.5f),new Vector3(4.15f,.08f,.81f),cream);
        Box("repair workbench",new Vector3(-4.3f,.82f,7),new Vector3(3.6f,.12f,1.1f),cream);
        TableLegs(new Vector3(-4.3f,0,7),3.3f,.85f,.76f);
        SewingStation(new Vector3(-4.1f,.881f,7));
        MerchBox("fabric cutting mat",new Vector3(-5.35f,.883f,7),new Vector3(.70f,.004f,.64f),green);
        FloristScissors(new Vector3(-5.30f,.90f,6.88f));
        for(int i=0;i<5;i++){
            var p=new Vector3(-5.65f+i*.16f,.94f,7.37f);
            Decor("repair thread spool",PrimitiveType.Cylinder,p,new Vector3(.085f,.055f,.085f),i%2==0?cream:dark);
            Decor("thread spool central core",PrimitiveType.Cylinder,p+Vector3.up*.06f,new Vector3(.025f,.01f,.025f),shelfTop);
        }
        LaundryRack(new Vector3(5.45f,0,10));
        Box("laundry receiving counter",new Vector3(3,.52f,5),new Vector3(3,1.04f,.8f),cream);
        MallPrint("laundry receiving services",6,new Vector3(3,.63f,4.589f),new Vector2(.64f,.66f));
        MallPrint("customer duplicate claim ticket",4,new Vector3(3.2f,1.045f,4.85f),new Vector2(.20f,.24f),new Vector3(90,0,0));
        RetailTerminal(new Vector3(2.2f,1.045f,5.02f),0,root);
        // Tapered ironing board, crossed legs, open iron handle and hanging cable.
        var b=new Vector3(-3.5f,.86f,10.3f);
        var top=HandmadeProp("padded tapered ironing board",IroningTop(),b,Vector3.one,fabric,root);top.AddComponent<BoxCollider>().size=new Vector3(.44f,.025f,1.24f);
        foreach(float x in new[]{-.15f,.15f}){
            RailSegment("ironing board crossed leg",b+new Vector3(x,-.02f,-.44f),b+new Vector3(x,-.79f,.40f),.025f,shelfTop,root);
            RailSegment("ironing board crossed leg",b+new Vector3(x,-.02f,.44f),b+new Vector3(x,-.79f,-.40f),.025f,shelfTop,root);
        }
        var iron=b+new Vector3(0,.055f,-.27f);
        HandmadeProp("iron pointed sole",IroningTop(),iron,new Vector3(.58f,.65f,.26f),PrepSteel(),root);
        HandmadeProp("steam iron curved reservoir",SteamIronShell(),iron+Vector3.up*.006f,Vector3.one,priceWhite,root);
        Decor("iron thermostat dial",PrimitiveType.Cylinder,iron+new Vector3(0,.11f,-.018f),new Vector3(.065f,.009f,.065f),dark);
        MerchBox("iron water level window",iron+new Vector3(-.079f,.046f,-.036f),new Vector3(.004f,.024f,.085f),blue);
        PrepPipe("open steam iron handle",new[]{iron+new Vector3(0,.07f,-.08f),iron+new Vector3(0,.17f,-.08f),iron+new Vector3(0,.18f,.055f),iron+new Vector3(0,.08f,.075f)},.035f,dark,root);
        PrepPipe("iron power cable",new[]{iron+new Vector3(0,.09f,-.12f),iron+new Vector3(.22f,.04f,-.20f),b+new Vector3(.33f,-.35f,-.5f),b+new Vector3(.35f,-.75f,-.57f)},.008f,dark,root);
        for(int i=0;i<3;i++){
            var p=new Vector3(-.3f+i*.70f,0,14.8f);
            PrepCrate(p,new Vector3(.58f,.39f,.67f),cream,root);
            for(int n=0;n<3;n++)MerchBox("folded customer towels",p+new Vector3(.015f*n,.20f+n*.07f,0),new Vector3(.46f,.055f,.49f),n%2==0?fabric:priceWhite);
        }
        MallPrint("laundry care wall notice",14,new Vector3(3,2.1f,15.87f),new Vector2(.65f,.80f));
    }
    static void BuildCafeDetails(int kind)
    {
        var timber=RetailSurface("coffee counter walnut veneer",new Color(.24f,.14f,.075f),0,.20f);
        for(int i=0;i<9;i++)MerchBox("counter front veneer panel",new Vector3(-4+i,.58f,11.442f),new Vector3(.988f,.94f,.025f),timber);
        MerchBox("counter recessed foot plinth",new Vector3(0,.08f,11.46f),new Vector3(9,.14f,.04f),dark);
        // No props intrude into the staff door, central public aisle, or chair clearances.
        for(int row=0;row<3;row++)foreach(float x in new[]{-5.4f,-2.7f,2.7f,5.4f}){
            var p=new Vector3(x,.79f,3+row*3);
            MerchBox("table folded napkin holder",p+new Vector3(.18f,.07f,.10f),new Vector3(.14f,.14f,.06f),timber);
            MerchBox("table paper napkins",p+new Vector3(.18f,.11f,.10f),new Vector3(.12f,.17f,.025f),cream);
            if(row==1&&x<0)CeramicCup(p+new Vector3(-.12f,0,-.10f));
        }
        Box("self service side cabinet",new Vector3(6.2f,.46f,11.5f),new Vector3(1.05f,.92f,2.1f),timber);
        MerchBox("self service worktop",new Vector3(6.2f,.95f,11.5f),new Vector3(1.1f,.06f,2.15f),PrepSteel());
        MallPrint("self service station wall notice",10,new Vector3(6.85f,2.0f,11.4f),new Vector2(.50f,.56f),new Vector3(0,90,0));
        for(int n=0;n<4;n++){
            var p=new Vector3(6.2f,.98f+n*.045f,11);
            CeramicCup(p,null,true);
        }
        MerchBox("paper napkin dispenser",new Vector3(6.2f,1.10f,11.45f),new Vector3(.23f,.24f,.20f),shelfTop);
        MerchBox("napkin dispenser dark slot",new Vector3(6.072f,1.11f,11.45f),new Vector3(.005f,.04f,.14f),dark);
        var pitcher=new Vector3(6.2f,.98f,11.90f);
        HandmadeProp("insulated carafe tapered steel body",TurnedPackageMesh("MallInsulatedCarafeV1",new[]{0f,.02f,.08f,.70f,.88f,.92f,1f},new[]{.40f,.48f,.50f,.45f,.33f,.30f,.30f}),pitcher,new Vector3(.20f,.40f,.20f),PrepSteel(),root);
        Decor("insulated carafe lid",PrimitiveType.Cylinder,pitcher+Vector3.up*.41f,new Vector3(.14f,.019f,.14f),dark);
        MerchBox("carafe pouring lip",pitcher+new Vector3(-.095f,.39f,0),new Vector3(.12f,.035f,.075f),dark);
        DetailRing("carafe handle",new Vector3(6.2f,1.20f,12.04f),new Vector3(.13f,.23f,.04f),dark,null,new Vector3(0,90,0));
        Box("tray return cabinet",new Vector3(-6.2f,.48f,11.6f),new Vector3(.95f,.96f,1.75f),timber);
        for(int n=0;n<5;n++)HandmadeProp("stacked returned serving tray",ProduceShell(false,false),new Vector3(-6.2f,.97f+n*.025f,11.4f),new Vector3(.39f,.035f,.29f),dark,root);
        MallPrint("tray return station sign",kind==0?9:15,new Vector3(-6.84f,1.8f,11.6f),new Vector2(.55f,.63f),new Vector3(0,-90,0));
        MallPrint("coffee customer receipt",12,new Vector3(-.15f,1.155f,11.85f),new Vector2(.10f,.21f),new Vector3(90,0,0));
        PrepSink(new Vector3(-5.3f,0,15.15f),root);
        PrepBench(new Vector3(-2.4f,0,15.15f),new Vector2(3.8f,1.05f),root);
        Box("underbench utility refrigerator",new Vector3(-2.3f,.43f,15.15f),new Vector3(1.9f,.86f,.96f),PrepSteel());
        foreach(float x in new[]{-2.78f,-1.82f}){
            MerchBox("utility refrigerator recessed door",new Vector3(x,.45f,14.655f),new Vector3(.92f,.77f,.03f),shelfTop);
            RailSegment("utility fridge vertical handle",new Vector3(x+.33f,.51f,14.61f),new Vector3(x+.33f,.73f,14.61f),.025f,dark,root);
        }
        Box("kitchen wall crockery shelf",new Vector3(-2.4f,1.78f,15.55f),new Vector3(3.7f,.035f,.60f),PrepSteel());
        for(int n=0;n<12;n++){
            int stack=n/4;float y=1.805f+(n%4)*.035f;
            Decor("stacked ceramic serving plate",PrimitiveType.Cylinder,new Vector3(-3.55f+stack*.5f,y,15.48f),new Vector3(.28f,.014f,.28f),cream);
        }
        for(int i=0;i<4;i++)CeramicCup(new Vector3(-1.9f+i*.20f,1.805f,15.48f));
        for(int i=0;i<2;i++){
            var p=new Vector3(-1.5f+i*.50f,.98f,15.20f);
            HandmadeProp("kitchen storage container",ProduceShell(false,false),p,new Vector3(.35f,.16f,.28f),priceWhite,root);
            MerchBox("storage tub removable lid",p+Vector3.up*.165f,new Vector3(.37f,.025f,.30f),cream);
        }
        if(kind==1){
            var p=new Vector3(-3.25f,.99f,15.10f);
            var shell=RetailSurface("rice warmer champagne enamel",new Color(.54f,.48f,.37f),.25f,.32f);
            HandmadeProp("rice cooker rounded insulated shell",TurnedPackageMesh("MallRiceWarmerBodyV1",new[]{0f,.04f,.12f,.68f,.89f,1f},new[]{.36f,.44f,.50f,.50f,.46f,.40f}),p,new Vector3(.46f,.32f,.40f),shell,root);
            HandmadeProp("rice cooker rounded locking lid",TurnedPackageMesh("MallRiceWarmerLidV1",new[]{0f,.12f,.40f,.85f,1f},new[]{.43f,.47f,.46f,.35f,0f}),p+Vector3.up*.32f,new Vector3(.46f,.07f,.40f),dark,root);
            PrepPipe("rice cooker lid carry handle",new[]{p+new Vector3(-.11f,.37f,0),p+new Vector3(-.10f,.43f,0),p+new Vector3(.10f,.43f,0),p+new Vector3(.11f,.37f,0)},.032f,dark,root);
            Decor("rice cooker steam cap",PrimitiveType.Cylinder,p+new Vector3(0,.385f,.11f),new Vector3(.062f,.012f,.062f),shelfTop);
            MerchBox("rice cooker control panel",p+new Vector3(0,.18f,-.201f),new Vector3(.18f,.10f,.018f),dark);
            Label("보온",p+new Vector3(-.025f,.19f,-.213f),Vector3.zero,.045f);
            foreach(float x in new[]{-.055f,.055f})Decor("rice cooker control button",PrimitiveType.Cylinder,p+new Vector3(x,.15f,-.214f),new Vector3(.025f,.004f,.025f),x<0?cream:red).transform.rotation=Quaternion.Euler(90,0,0);
            foreach(int side in new[]{-1,1}){
                MerchBox("rice cooker side grip",p+new Vector3(side*.235f,.25f,0),new Vector3(.065f,.045f,.11f),dark);
                foreach(float z in new[]{-.10f,.10f})Decor("rice cooker rubber foot",PrimitiveType.Cylinder,p+new Vector3(side*.13f,-.008f,z),new Vector3(.045f,.012f,.045f),dark);
            }
        }
        for(int n=0;n<3;n++){
            MerchBox("staff storage wall cabinet",new Vector3(-4.5f+n*1.4f,2.45f,15.73f),new Vector3(1.35f,.85f,.35f),cream);
            RailSegment("wall cabinet handle",new Vector3(-4.8f+n*1.4f,2.30f,15.53f),new Vector3(-4.65f+n*1.4f,2.30f,15.53f),.016f,shelfTop,root);
        }
    }
}
