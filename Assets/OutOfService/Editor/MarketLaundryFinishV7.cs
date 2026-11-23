using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    static float LaundryHeight(int kind)=>kind==0?.292f:kind==3?.305f:kind==1?.267f:kind==4?.254f:kind==2?.181f:.174f;
    static Vector4 LaundrySection(int kind,float f)
    {
        bool jug=kind==0||kind==3,pump=kind==2||kind==5;
        float[] levels={0,.025f,.065f,.18f,.43f,.58f,.69f,.79f,.87f,.92f,1};
        float[] sizes=jug?new[]{0f,.88f,.99f,1f,1f,.94f,.77f,.58f,.40f,.29f,.29f}:
            pump?new[]{0f,.83f,.98f,1f,.96f,.90f,.92f,.79f,.47f,.28f,.28f}:
            new[]{0f,.77f,.94f,1f,1f,.95f,.88f,.71f,.43f,.31f,.31f};
        int k=0;while(k<levels.Length-2&&f>levels[k+1])k++;
        float radius=Mathf.Lerp(sizes[k],sizes[k+1],Mathf.InverseLerp(levels[k],levels[k+1],f));
        float rx=(jug?.080f:pump?.039f:.054f)*radius;
        float rz=(jug?.052f:pump?.032f:.042f)*radius;
        if(kind==3){rx*=1.07f;rz*=1.025f;}
        if(kind==4){rx*=1.06f;rz*=.96f;}
        float offset=jug?-.030f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.43f,.92f,f)):0;
        return new Vector4(offset,rx,rz,jug?.68f:pump?.84f:.93f);
    }
    static Mesh LaundryBody(int kind)
    {
        var mesh=MallMesh("laundry_mold_v1_"+kind,(v,u,t)=>{
            const int rows=32,sides=32;
            for(int row=0;row<=rows;row++)for(int col=0;col<=sides;col++){
                float f=row/(float)rows,a=col*2*Mathf.PI/sides;var s=LaundrySection(kind,f);
                float c=Mathf.Cos(a),sn=Mathf.Sin(a);
                v.Add(new Vector3(s.x+Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),s.w)*s.y,f*LaundryHeight(kind),Mathf.Sign(sn)*Mathf.Pow(Mathf.Abs(sn),s.w)*s.z));
                u.Add(new Vector2(col/(float)sides,f));
                if(row==rows||col==sides)continue;
                int k=row*(sides+1)+col;t.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});
            }
        });
        if(preparedHarvestNormals.Add(mesh)){
            var normals=mesh.normals;
            for(int row=0;row<=32;row++){
                int start=row*33,end=start+32;
                normals[start]=normals[end]=(normals[start]+normals[end]).normalized;
            }
            mesh.normals=normals;mesh.RecalculateTangents();UnityEditor.EditorUtility.SetDirty(mesh);
        }
        return mesh;
    }
    static Mesh LaundryPrint(int kind,bool rear)
    {
        return MallMesh("laundry_adhered_label_v2_"+kind+"_"+rear,(v,u,t)=>{
            const int rows=10,cols=16;
            bool jug=kind==0||kind==3,pump=kind==2||kind==5;
            float low=jug?.063f:pump?.030f:.061f,high=jug?.181f:pump?.101f:.168f;
            for(int row=0;row<=rows;row++)for(int col=0;col<=cols;col++){
                float fy=row/(float)rows,fx=col/(float)cols,y=Mathf.Lerp(low,high,fy);
                var s=LaundrySection(kind,y/LaundryHeight(kind));
                float margin=.78f*(1-.075f*Mathf.Pow(Mathf.Abs(fy-.5f)*2,8));
                float x=(fx*2-1)*s.y*margin;
                float z=s.z*Mathf.Pow(1-Mathf.Pow(Mathf.Abs(x/s.y),2/s.w),s.w*.5f)+.0012f;
                v.Add(new Vector3(s.x+x,y,rear?z:-z));
                u.Add(new Vector2(rear?.004f+.726f*(1-fx):fx,fy));
                if(row==rows||col==cols)continue;int k=row*(cols+1)+col;
                if(rear)t.AddRange(new[]{k,k+1,k+cols+1,k+1,k+cols+2,k+cols+1});
                else t.AddRange(new[]{k,k+cols+1,k+1,k+1,k+cols+1,k+cols+2});
            }
        });
    }
    static Mesh LaundryHandle(int kind)
    {
        return MallMesh("laundry_integral_handle_v2_"+kind,(v,u,t)=>{
            const int rows=32,sides=10;float scale=kind==3?1.045f:1;
            Vector2[] curve={new(.003f,.247f),new(.035f,.253f),new(.065f,.251f),new(.089f,.232f),new(.100f,.204f),new(.097f,.177f),new(.080f,.156f),new(.058f,.146f)};
            Vector2 Point(float f){
                float q=Mathf.Clamp01(f)*(curve.Length-1);int n=Mathf.Min((int)q,curve.Length-2);float b=q-n;
                var p0=curve[Mathf.Max(n-1,0)];var p1=curve[n];var p2=curve[n+1];var p3=curve[Mathf.Min(n+2,curve.Length-1)];
                return .5f*((2*p1)+(-p0+p2)*b+(2*p0-5*p1+4*p2-p3)*b*b+(-p0+3*p1-3*p2+p3)*b*b*b);
            }
            for(int row=0;row<=rows;row++)for(int col=0;col<=sides;col++){
                float f=row/(float)rows,b=col*2*Mathf.PI/sides;var point=Point(f)*scale;
                var tangent=(Point(f+.001f)-Point(f-.001f)).normalized;
                var n=new Vector3(-tangent.y,tangent.x,0);
                v.Add(new Vector3(point.x,point.y,0)+n*(.010f*Mathf.Cos(b))+Vector3.forward*(.014f*Mathf.Sin(b)));
                u.Add(new Vector2(row/(float)rows,col/(float)sides));
                if(row==rows||col==sides)continue;int k=row*(sides+1)+col;
                t.AddRange(new[]{k,k+1,k+sides+1,k+1,k+sides+2,k+sides+1});
            }
        });
    }
    static Mesh LaundryCap()
    {
        return MallMesh("laundry_grip_cap_v1",(v,u,t)=>{
            const int sides=64;float[] ys={0,.025f,.08f,.90f,.97f,1,1},rs={.44f,.49f,.5f,.5f,.47f,.43f,0};
            for(int row=0;row<ys.Length;row++)for(int col=0;col<=sides;col++){
                float a=col*2*Mathf.PI/sides,r=rs[row]+(row==2||row==3?.009f*Mathf.Cos(a*32):0);
                v.Add(new Vector3(Mathf.Cos(a)*r,ys[row],Mathf.Sin(a)*r));u.Add(new Vector2(col/(float)sides,ys[row]));
                if(row==ys.Length-1||col==sides)continue;int k=row*(sides+1)+col;
                t.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});
            }
        });
    }
    static void LaundryFacing(Vector3 p,int kind,Transform parent)
    {
        Color[] colours={new(.66f,.075f,.035f),new(.81f,.77f,.69f),new(.62f,.63f,.22f),new(.075f,.28f,.13f),new(.34f,.59f,.72f),new(.32f,.48f,.25f)};
        var plastic=RetailSurface("laundry retail HDPE v1 "+kind,colours[kind],0,.43f);
        FineRelief(plastic,"LaundryFineHDPEV1",(u,v)=>Mathf.PerlinNoise(u*160,v*160)*.014f,.20f);
        float height=LaundryHeight(kind);bool jug=kind==0||kind==3,pump=kind==2||kind==5;
        HandmadeProp("rounded blow molded cleaning bottle",LaundryBody(kind),p,Vector3.one,plastic,parent);
        if(jug){
            HandmadeProp("integral open pouring handle",LaundryHandle(kind),p,Vector3.one,plastic,parent);
        }
        var cap=RetailSurface("cleaning closure resin "+(pump?2:kind==3?1:0),pump?new Color(.84f,.84f,.77f):kind==3?new Color(.46f,.095f,.045f):new Color(.12f,.29f,.44f),0,.35f);
        Vector3 neck=p+new Vector3(LaundrySection(kind,1).x,height,0);
        if(pump){
            HandmadeProp("ribbed pump closure",LaundryCap(),neck,new Vector3(.030f,.012f,.030f),cap,parent);
            Decor("dispensing pump piston",PrimitiveType.Cylinder,neck+Vector3.up*.022f,new Vector3(.010f,.010f,.010f),cap,parent);
            HandmadeProp("rounded push pump head",SoftShopEdge(new Vector3(.029f,.012f,.057f)),neck+new Vector3(0,.037f,-.011f),Vector3.one,cap,parent);
            Decor("downward pump outlet",PrimitiveType.Cylinder,neck+new Vector3(0,.031f,-.036f),new Vector3(.009f,.006f,.009f),cap,parent);
        }else{
            float diameter=jug?.055f:.039f;
            HandmadeProp("ribbed measuring screw closure",LaundryCap(),neck,new Vector3(diameter,jug?.030f:.034f,diameter),cap,parent);
        }
        HandmadeProp("conforming front cleaning label",LaundryPrint(kind,false),p,Vector3.one,PackMaterial(27,kind),parent);
        HandmadeProp("fitted full wrap consumer information",LaundryPrint(kind,true),p,Vector3.one,InformationInk(27,kind),parent);
    }
}
