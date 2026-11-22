using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    static Mesh NaturalRose(int variant)
    {
        // The inspected close roses get 20 boundary samples. Only this shared
        // 1,952-vertex family has a bounded exception in BatchSmallProps.
        return MallMesh("rose_cupped_v9_"+variant,(v,u,t)=>{
            const int segments=20;
            int[] counts={7,5,3,1};
            for(int layer=0;layer<4;layer++)for(int p=0;p<counts[layer];p++){
                int start=v.Count;
                float angle=(p+layer*.37f)*Mathf.PI*2/counts[layer]+.14f*Mathf.Sin(p*7+variant*5);
                float reach=(1-layer*.23f)*(1+.075f*Mathf.Sin(p*13+variant));
                float width=(.61f-layer*.13f)*(1+.10f*Mathf.Sin(p*4+variant));
                void Point(float x,float y){
                    float f=(y+1)*.5f;
                    float r=.025f+reach*(.07f+.93f*f);
                    float theta=angle+x*(layer==3?1.8f:.25f)*f;
                    var along=new Vector3(Mathf.Cos(theta),0,Mathf.Sin(theta));
                    var across=new Vector3(-along.z,0,along.x);
                    float h=.035f+layer*.075f+f*(.46f+layer*.055f);
                    h+=.16f*Mathf.Sin(f*Mathf.PI)-.10f*Mathf.Pow(f,6)+.075f*x*x;
                    h+=.018f*x*Mathf.Sin(p*4+variant);
                    v.Add(along*r+across*(x*width)+Vector3.up*h);
                    u.Add(new Vector2(x*.5f+.5f,f));
                }
                Point(0,0);
                foreach(float radius in new[]{.34f,.69f,1f})for(int n=0;n<segments;n++){
                    float a=n*Mathf.PI*2/segments;Point(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius);
                }
                for(int n=0;n<segments;n++){
                    int next=(n+1)%segments;t.AddRange(new[]{start,start+1+n,start+1+next});
                    for(int ring=0;ring<2;ring++){
                        int a=start+1+ring*segments+n,b=start+1+ring*segments+next;
                        t.AddRange(new[]{a,a+segments,b,b,a+segments,b+segments});
                    }
                }
            }
            MallTwoSides(v,u,t);
        });
    }

    static Mesh BotanicalLeaf(int style,int variant)
    {
        return MallMesh("potted_botanical_v2_"+style+"_"+variant,(v,u,t)=>{
            const int rows=16,cols=8;
            for(int r=0;r<=rows;r++)for(int c=0;c<=cols;c++){
                float f=r/(float)rows,s=c/(float)cols*2-1;
                float width=Mathf.Pow(Mathf.Max(.00001f,Mathf.Sin(f*Mathf.PI)),style==0?.65f:.82f)*(style==0?.25f:.22f);
                width*=1+.035f*s*Mathf.Sin(f*12+variant);
                float center=(style==0?.11f:.20f)*Mathf.Sin(f*Mathf.PI)-(style==0?.12f:.27f)*f*f;
                float rib=style==0?.002f:.006f;
                float y=center-.055f*s*s+rib*Mathf.Abs(s)*Mathf.Sin(f*43-Mathf.Abs(s)*5);
                v.Add(new Vector3(s*width+.015f*Mathf.Sin(f*6+variant)*f,y,f));
                u.Add(new Vector2(c/(float)cols,f));
                if(r<rows&&c<cols){int k=r*(cols+1)+c;t.AddRange(new[]{k,k+cols+1,k+1,k+1,k+cols+1,k+cols+2});}
            }
            MallTwoSides(v,u,t);
        });
    }

    static void PlantNode(int style,int n,out Vector3 origin,out Vector3 leafBase,out Quaternion rotation,out Vector3 size)
    {
        float a=(n*137.5f+style*27)*Mathf.Deg2Rad;var outwards=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
        if(style==0){
            float h=.43f+n*.065f;
            origin=new Vector3(.035f*(h-.28f)/.88f,h,0);
            leafBase=origin+outwards*.075f+Vector3.up*.025f;
            size=new Vector3(1,1,1)*(.34f+.055f*Mathf.Sin(n*3));
            rotation=Quaternion.Euler(n>=8?-45-(n-8)*12:-12-(n%3)*10,90-a*Mathf.Rad2Deg,(n%3-1)*7);
        }else{
            origin=outwards*.035f+Vector3.up*.28f;
            leafBase=outwards*(.065f+(n%3)*.025f)+Vector3.up*(.46f+(n%4)*.075f);
            size=new Vector3(1,1,1)*(.37f+(n%3)*.055f);
            rotation=Quaternion.Euler(-10-(n%3)*9,90-a*Mathf.Rad2Deg,(n%3-1)*6);
        }
    }

    static void BuildBotanicalPot(Vector3 p,int style,float scale)
    {
        var clay=MallFinish("clay",new Color(.47f,.25f,.13f),0,.12f);
        HandmadeProp("tapered ceramic plant pot",FloristVessel(false),p,new Vector3(scale,.60f*scale,scale),clay,root);
        var soil=RetailSurface("coarse potting compost v1",new Color(.11f,.080f,.045f),0,.04f);
        soil.mainTexture=FinishTexture("PottingCompostV1",(u,v)=>{
            float fleck=Mathf.PerlinNoise(u*68,v*68),bark=Mathf.PerlinNoise(u*19+3,v*19);
            float a=.34f+.55f*fleck+.10f*bark;
            return new Color(a,a*.86f,a*.64f,1);
        });
        Decor("granular potting compost",PrimitiveType.Cylinder,p+Vector3.up*(.28f*scale),new Vector3(.40f*scale,.012f,.40f*scale),soil);
        var stem=FloristGreen(0);
        int count=style==0?11:13;
        var stalks=MallMesh("potted_stems_v2_"+style,(v,u,t)=>{
            void Tube(Vector3 a,Vector3 b,float radius){
                var axis=(b-a).normalized;var across=Vector3.Cross(axis,Vector3.forward).normalized;
                if(across.sqrMagnitude<.1f)across=Vector3.right;
                var other=Vector3.Cross(axis,across);int start=v.Count;
                for(int r=0;r<2;r++)for(int c=0;c<=6;c++){
                    float angle=c*Mathf.PI/3;
                    v.Add((r==0?a:b)+(across*Mathf.Cos(angle)+other*Mathf.Sin(angle))*radius);
                    u.Add(new Vector2(c/6f,r));
                    if(r==0&&c<6){int k=start+c;t.AddRange(new[]{k,k+1,k+7,k+1,k+8,k+7});}
                }
            }
            if(style==0)Tube(new(0,.28f,0),new(.035f,1.16f,0),.011f);
            for(int n=0;n<count;n++){
                PlantNode(style,n,out var a,out var b,out _,out _);
                var mid=Vector3.Lerp(a,b,.60f)+Vector3.up*.018f;
                Tube(a,mid,style==0?.0045f:.005f);Tube(mid,b,.0035f);
            }
        });
        HandmadeProp(style==0?"rubber plant woody nodes":"peace lily arching petioles",stalks,p,Vector3.one*scale,stem,root);
        var leafMat=RetailSurface("botanical satin leaf "+style,style==0?new Color(.105f,.23f,.12f):new Color(.20f,.34f,.10f),0,style==0?.42f:.29f);
        leafMat.mainTexture=FinishTexture("BotanicalVeinsV1_"+style,(u,v)=>{
            float rib=Mathf.Exp(-Mathf.Abs(u-.5f)*130)*.21f;
            float vein=Mathf.Pow(Mathf.Max(0,Mathf.Cos((v-Mathf.Abs(u-.5f)*.37f)*56)),24)*.08f;
            float light=.77f+rib+vein+.06f*Mathf.PerlinNoise(u*12,v*19);
            return new Color(light*.94f,light,light*.87f,1);
        });
        FineRelief(leafMat,"BotanicalRibReliefV1",(u,v)=>.35f*Mathf.Exp(-Mathf.Abs(u-.5f)*95)+
            .03f*Mathf.Sin((v-Mathf.Abs(u-.5f)*.37f)*56),1);
        for(int n=0;n<count;n++){
            PlantNode(style,n,out _,out var b,out var rot,out var size);
            HandmadeProp(style==0?"elliptic rubber leaf":"arched peace lily leaf",BotanicalLeaf(style,n%3),p+b*scale,size*scale,leafMat,root).transform.rotation=rot;
        }
        if(style==1)for(int n=0;n<2;n++){
            var b=p+new Vector3((n-.5f)*.16f,.96f+n*.12f,.035f)*scale;
            RailSegment("peace lily flower stem",p+Vector3.up*.29f*scale,b,.006f*scale,stem,root);
            var spathe=HandmadeProp("white curved peace lily spathe",BotanicalLeaf(1,n),b,new Vector3(.18f,.18f,.23f)*scale,CafeCeramic(true),root);
            spathe.GetComponent<Renderer>().sharedMaterial=RetailSurface("peace lily ivory spathe",new Color(.84f,.86f,.69f),0,.18f);
            spathe.transform.rotation=Quaternion.Euler(-65,25+n*140,0);
            Decor("cream spadix",PrimitiveType.Capsule,b+Vector3.up*.065f*scale,new Vector3(.015f,.050f,.015f)*scale,cream);
        }
    }
}
