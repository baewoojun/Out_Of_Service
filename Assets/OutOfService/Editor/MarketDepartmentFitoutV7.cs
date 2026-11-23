using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    // Metre-authored original fit-out. No downloaded shop photographs are used as textures.
    static Color TenantWallTint(int kind)
    {
        Color[] tones={new(.73f,.68f,.57f),new(.66f,.65f,.56f),new(.72f,.76f,.71f),
            new(.78f,.81f,.77f),new(.48f,.56f,.43f),new(.68f,.74f,.75f),
            new(.74f,.65f,.67f),new(.69f,.65f,.55f),new(.42f,.46f,.48f),new(.63f,.60f,.52f)};
        return tones[Mathf.Clamp(kind,0,9)];
    }

    static Material DepartmentTile(int kind)
    {
        Color[] colors={new(.69f,.74f,.62f),new(.79f,.78f,.70f),new(.43f,.60f,.61f),new(.69f,.76f,.76f)};
        var mat=RetailSurface("department glazed ceramic "+kind,colors[kind],0,.44f);
        if(mat.mainTexture)return mat;
        float Edge(float u,float v){
            float row=Mathf.Floor(v*8),x=Mathf.Repeat(u*4+(row%2)*.5f,1),y=Mathf.Repeat(v*8,1);
            return Mathf.Min(Mathf.Min(x,1-x)*.30f,Mathf.Min(y,1-y)*.15f);
        }
        mat.mainTexture=FinishTexture("DepartmentTileV1_"+kind,(u,v)=>{
            float edge=Edge(u,v),joint=Mathf.SmoothStep(0,1,Mathf.Clamp01((edge-.0013f)/.003f));
            float tile=.87f+.07f*Mathf.PerlinNoise(Mathf.Floor(u*4)*3+kind,Mathf.Floor(v*8)*5)+.035f*Mathf.PerlinNoise(u*83,v*83);
            return Color.Lerp(new Color(.39f,.41f,.38f),Color.white*tile,joint);
        });
        FineRelief(mat,"DepartmentTileReliefV1",(u,v)=>Mathf.SmoothStep(0,1,Mathf.Clamp01((Edge(u,v)-.0013f)/.003f))*.32f,.65f);
        return mat;
    }

    static void FinishPanel(string name,Vector3 p,float width,float height,float yaw,Material mat)
    {
        string key="fitout_panel_v1_"+width.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"_"+height.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);
        var mesh=MallMesh(key,(v,u,t)=>{
            FinishQuad(v,u,t,new(-width*.5f,0,0),new(width*.5f,0,0),new(width*.5f,height,0),new(-width*.5f,height,0));
            for(int i=0;i<u.Count;i++)u[i]/=1.2f;
            for(int i=0;i<t.Count;i+=3){int a=t[i+1];t[i+1]=t[i+2];t[i+2]=a;}
        });
        var panel=HandmadeProp(name,mesh,p,Vector3.one,mat,root);panel.transform.rotation=Quaternion.Euler(0,yaw,0);
    }

    static void TenantIdentityFinish(int kind)
    {
        bool tiled=kind==1||kind==2||kind==3||kind==4||kind==5;
        if(tiled){
            float height=kind==2?3.55f:kind==3?1.35f:1.8f;
            int tile=kind==4?0:kind==1||kind==3?1:3;
            foreach(int side in new[]{-1,1})FinishPanel("tenant washable ceramic course "+kind,
                new Vector3(side*6.825f,.15f,8),15.6f,height,side<0?-90:90,DepartmentTile(tile));
            FinishPanel("tenant rear ceramic course "+kind,new Vector3(0,.15f,15.825f),13.6f,height,0,DepartmentTile(tile));
        }
        if(kind==0||kind==7||kind==6){
            var trim=kind==6?RetailSurface("beauty satin brass",new Color(.65f,.50f,.30f),.65f,.42f):OakVeneer(new Color(.37f,.28f,.18f));
            // A fitted wall treatment between furniture, not freestanding obstacles.
            foreach(int side in new[]{-1,1})for(int bay=0;bay<4;bay++){
                float z=2+bay*3.0f;
                foreach(float dz in new[]{-1.10f,1.10f})Decor("tenant wall framed panel stile",PrimitiveType.Cube,new Vector3(side*6.80f,2.20f,z+dz),new Vector3(.045f,1.65f,.035f),trim);
                foreach(float y in new[]{1.38f,3.02f})Decor("tenant wall framed panel rail",PrimitiveType.Cube,new Vector3(side*6.80f,y,z),new Vector3(.045f,.035f,2.2f),trim);
                if(kind==0)for(int slat=0;slat<9;slat++)Decor("cafe acoustic oak batten",PrimitiveType.Cube,new Vector3(side*6.78f,2.2f,z-.96f+slat*.24f),new Vector3(.055f,1.58f,.035f),trim);
            }
        }
        if(kind==8||kind==9){
            for(int side=-1;side<=1;side+=2)for(int bay=0;bay<7;bay++){
                Decor("tenant wall service panel reveal",PrimitiveType.Cube,new Vector3(side*6.82f,2.15f,1.1f+bay*2.2f),new Vector3(.03f,3.3f,.025f),shelf);
            }
        }
        if(kind==4||kind==5){
            foreach(float z in new[]{2,7,12}){
                Decor("utility wall impact guard",PrimitiveType.Cube,new Vector3(6.79f,.86f,z),new Vector3(.06f,.12f,3.4f),kind==4?PrepSteel():cream);
                foreach(float dz in new[]{-1.4f,1.4f})Decor("utility rail end cap",PrimitiveType.Cube,new Vector3(6.75f,.86f,z+dz),new Vector3(.045f,.14f,.06f),dark);
            }
        }
    }

    static void RetailDepartmentFitout()
    {
        // Produce wall has real upper cladding, pilasters and an accessible picking aisle.
        var timber=OakVeneer(new Color(.42f,.34f,.22f));
        for(int bay=0;bay<5;bay++){
            float z=bay==0?-18.7f:-22+bay*11f,span=bay==0?3.8f:10.3f;
            FinishPanel("produce upper green ceramic wall",new Vector3(-46.65f,3.68f,z),span,.65f,-90,DepartmentTile(0));
            foreach(int end in new[]{-1,1})Decor("produce oak bay pilaster",PrimitiveType.Cube,new Vector3(-46.30f,2.5f,z+end*span*.5f),new Vector3(.22f,3.65f,.13f),timber);
            for(int slat=0;slat<Mathf.FloorToInt(span/.24f);slat++)Decor("produce fascia timber slat",PrimitiveType.Cube,new Vector3(-46.345f,3.56f,z-span*.5f+.12f+slat*.24f),new Vector3(.038f,.18f,.095f),timber);
        }
        UpperFloorWallMerchandising();
        UpperAccessoriesDepartment();
        OpenMallAndUpperInfill();
    }

    static void FoldedClothStack(Vector3 p,int style,int tone,Transform parent)
    {
        var cloth=GarmentCloth(style,tone);
        for(int layer=0;layer<4;layer++){
            var q=p+new Vector3(layer==3?.009f:0,.025f+layer*.046f,layer==3?-.012f:0);
            var body=HandmadeProp("folded cotton knit stock",FoldedKnitMold(style),q,Vector3.one,cloth,parent);
            body.transform.rotation=Quaternion.Euler(0,layer==3?(tone-1)*2:0,0);
            if(layer==3){
                var collar=HandmadeProp("folded knit neck binding",FoldedKnitCollar(),q,Vector3.one,cloth,parent);
                collar.transform.rotation=body.transform.rotation;
                Decor("folded size tab",PrimitiveType.Cube,q+new Vector3(.10f,.024f,-.06f),new Vector3(.034f,.001f,.060f),cream,parent);
                var print=Decor("folded garment price ticket",PrimitiveType.Quad,q+new Vector3(.10f,.025f,-.06f),new Vector3(.034f,.05f,1),GarmentTagInk(),parent);
                print.transform.rotation=Quaternion.Euler(90,0,0);
            }
        }
    }

    static Mesh FoldedKnitMold(int style)
    {
        return MallMesh("folded_knit_soft_v1_"+style,(v,u,t)=>{
            const int sides=48,rings=5;
            float[] heights={-.021f,-.016f,0,.015f,.021f},radii={.94f,1,1,.985f,.93f};
            for(int ring=0;ring<rings;ring++)for(int j=0;j<sides;j++){
                float a=j*2*Mathf.PI/sides,c=Mathf.Cos(a),s=Mathf.Sin(a);
                float x=Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),.24f)*.167f*radii[ring];
                float z=Mathf.Sign(s)*Mathf.Pow(Mathf.Abs(s),.24f)*.201f*radii[ring];
                float waviness=.0028f*Mathf.Sin(a*7+style)+.0016f*Mathf.Sin(a*13+ring*.4f);
                v.Add(new Vector3(x,heights[ring]+waviness,z));u.Add(new Vector2(x*3+.5f,z*2.5f+.5f));
                if(ring==rings-1)continue;int k=ring*sides+j,next=ring*sides+(j+1)%sides;
                t.AddRange(new[]{k,k+sides,next,next,k+sides,next+sides});
            }
            int top=v.Count;v.Add(new Vector3(0,.024f,0));u.Add(new Vector2(.5f,.5f));
            int bottom=v.Count;v.Add(new Vector3(0,-.021f,0));u.Add(new Vector2(.5f,.5f));
            for(int j=0;j<sides;j++){
                int next=(j+1)%sides;t.AddRange(new[]{top,(rings-1)*sides+next,(rings-1)*sides+j,bottom,j,next});
            }
        });
    }

    static Mesh FoldedKnitCollar()
    {
        return MallMesh("folded_knit_neck_binding_v1",(v,u,t)=>{
            const int sides=32;
            for(int ring=0;ring<4;ring++)for(int j=0;j<=sides;j++){
                float a=j*2*Mathf.PI/sides,r=ring==0||ring==3?1:1.15f;
                float y=ring==0?.022f:ring==1?.035f:ring==2?.037f:.024f;
                v.Add(new Vector3(Mathf.Cos(a)*.050f*r,y,Mathf.Sin(a)*.037f*r+.115f));u.Add(new Vector2(j/(float)sides,ring/3f));
                if(ring==3||j==sides)continue;int k=ring*(sides+1)+j;
                t.AddRange(new[]{k,k+1,k+sides+1,k+1,k+sides+2,k+sides+1});
            }
            MallTwoSides(v,u,t);
        });
    }

    static void UpperFashionBay(Vector3 p,float yaw,int kind)
    {
        var old=root;root=new GameObject("2F fitted wall clothing bay "+kind).transform;root.SetParent(old);
        var wood=OakVeneer(new Color(.64f,.57f,.44f));
        Box("fashion recessed back panel",new Vector3(0,1.55f,.04f),new Vector3(7.5f,3.1f,.10f),MallFinish("paint",kind==1?new Color(.50f,.58f,.59f):new Color(.72f,.68f,.60f),0,.12f));
        Box("fashion wall stock plinth",new Vector3(0,.15f,-.40f),new Vector3(7.5f,.30f,.9f),wood);
        for(int side=-1;side<=1;side+=2){
            float cx=side*1.85f;
            foreach(float dx in new[]{-1.55f,1.55f}){
                Decor("wall standard upright",PrimitiveType.Cube,new Vector3(cx+dx,1.65f,-.045f),new Vector3(.035f,2.7f,.035f),PrepSteel());
                for(int slot=0;slot<32;slot++)Decor("wall standard adjustment slot",PrimitiveType.Cube,new Vector3(cx+dx,.42f+slot*.077f,-.064f),new Vector3(.010f,.028f,.002f),dark);
            }
            Box("wall merchandising top shelf",new Vector3(cx,2.55f,-.35f),new Vector3(3.18f,.04f,.66f),wood);
            RailSegment("wall garment hanging rail",new Vector3(cx-1.5f,2.30f,-.47f),new Vector3(cx+1.5f,2.30f,-.47f),.032f,PrepSteel(),root);
            foreach(float dx in new[]{-1.45f,1.45f})RailSegment("wall rail bracket",new Vector3(cx+dx,2.3f,-.05f),new Vector3(cx+dx,2.3f,-.47f),.024f,PrepSteel(),root);
            for(int n=0;n<4;n++){
                int style=(kind+n/2)%3;float scale=kind==1?.72f:1;
                var neck=new Vector3(cx-1.12f+n*.75f,2.12f,-.48f);
                var rotation=Quaternion.Euler(0,(n%3-1)*5,0);
                var shirt=HandmadeProp("wall facing sewn garment",LaundryGarment(style),neck,Vector3.one*scale,GarmentCloth(style,n/2),root);
                shirt.transform.rotation=rotation;GarmentDetails(neck,rotation,scale,style,root);
                RailSegment("wall garment hanger left",neck+Vector3.up*.06f,neck+new Vector3(-.23f,-.05f,0)*scale,.010f,dark,root);
                RailSegment("wall garment hanger right",neck+Vector3.up*.06f,neck+new Vector3(.23f,-.05f,0)*scale,.010f,dark,root);
                RailSegment("wall garment hanger hook",neck+Vector3.up*.06f,neck+Vector3.up*.19f,.007f,PrepSteel(),root);
                FoldedClothStack(new Vector3(cx-1.15f+n*.75f,.31f,-.43f),style,n%3,root);
                if(n<3)FoldedClothStack(new Vector3(cx-.9f+n*.9f,2.58f,-.34f),style,n%3,root);
            }
        }
        Decor("wall bay oak header",PrimitiveType.Cube,new Vector3(0,3.17f,-.12f),new Vector3(7.65f,.22f,.23f),wood);
        Label(kind==1?"KIDS  /  데일리웨어":kind==2?"BASIC  /  남성의류":"SOOIM  /  여성의류",new Vector3(0,3.18f,-.245f),Vector3.zero,.20f);
        // One limited practical per bay exposes its silhouette; the surrounding floor stays after-hours dark.
        LocalLamp("fashion wall practical",new Vector3(0,3.65f,-1.5f),new Vector3(0,1.45f,-.4f),new Color(.97f,.86f,.68f),1.65f,5.5f);
        root.position=p;root.rotation=Quaternion.Euler(0,yaw,0);root=old;
    }

    static void UpperFloorWallMerchandising()
    {
        for(int i=0;i<6;i++)UpperFashionBay(new Vector3(-49.15f,5,-25+i*9),-90,i<3?0:1);
        foreach(float x in new[]{-30,-18,-6})UpperFashionBay(new Vector3(x,5,-44.55f),180,2);
        var wood=OakVeneer(new Color(.64f,.57f,.44f));
        foreach(float z in new[]{-19f,1f,21f}){
            Vector3 p=new Vector3(-42.6f,5,z);
            Box("2F folded knit display tabletop",p+Vector3.up*.83f,new Vector3(2.4f,.08f,1.45f),wood);
            foreach(int a in new[]{-1,1})foreach(int b in new[]{-1,1})Box("fold table tapered metal leg",p+new Vector3(a*1.04f,.40f,b*.56f),new Vector3(.045f,.80f,.045f),shelf);
            Box("fold table lower stock shelf",p+Vector3.up*.24f,new Vector3(2.25f,.035f,1.30f),wood);
            for(int row=0;row<2;row++)for(int col=0;col<5;col++){
                FoldedClothStack(p+new Vector3((col-2)*.43f,.875f,(row-.5f)*.59f),col%3,row+1,root);
                if(col%2==0)FoldedClothStack(p+new Vector3((col-2)*.43f,.26f,(row-.5f)*.59f),col%3,row,root);
            }
        }
    }
}
