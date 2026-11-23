using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    // Original finishes. Reference photos inform structure; their pixels are not used.
    static Material MallFinish(string kind,Color tint,float metallic,float gloss)
    {
        if(kind=="wood")return OakVeneer(tint);
        var mat=RetailSurface("B1 finish v4 "+kind+" "+ColorUtility.ToHtmlStringRGB(tint),tint,metallic,gloss);
        mat.mainTexture=FinishTexture("B1SurfaceV4_"+kind.Replace(" ","_"),(u,v)=>{
            float fine=Mathf.PerlinNoise(u*180+7,v*180+19),broad=Mathf.PerlinNoise(u*6+11,v*9+7);
            float value=.87f+.08f*fine;
            if(kind=="wood")value=.55f+.23f*Mathf.PerlinNoise(u*180+Mathf.Sin(v*6)*3,v*3)+.10f*broad;
            else if(kind=="clay")value=.65f+.19f*fine+.08f*Mathf.Sin(v*240)+.07f*broad;
            else if(kind=="pollen")value=.40f+.58f*Mathf.Pow(fine,1.5f);
            else if(kind.StartsWith("paper"))value=.78f+.10f*fine+.06f*Mathf.PerlinNoise(u*120,v*3);
            else if(kind=="ceiling")value=.78f+.09f*broad-.29f*Mathf.Pow(Mathf.Clamp01((fine-.58f)*3),2);
            else if(kind=="paint")value=.78f+.11f*broad+.055f*fine;
            else if(kind=="washable"){
                float scuff=Mathf.Pow(Mathf.PerlinNoise(u*48,v*3+9),9)*Mathf.Exp(-v*6)*.9f;
                value=.88f+.06f*fine-scuff;
            }else if(kind=="bucket")value=.89f+.04f*fine-.15f*Mathf.Pow(Mathf.PerlinNoise(u*16,v*70),5);
            return new Color(value,value,value,1);
        });
        if(kind=="paint"){
            mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Textures/painted_plaster_wall_Diffuse.jpg");
            mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Textures/painted_plaster_wall_nor_dx.jpg"));
            mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",.30f);
        }
        return mat;
    }

    static void FinishQuad(List<Vector3> v,List<Vector2> uv,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector2 offset=default)
    {
        int k=v.Count;v.AddRange(new[]{a,b,c,d});
        float w=Vector3.Distance(a,b),h=Vector3.Distance(b,c);
        uv.AddRange(new[]{offset,offset+new Vector2(w,0),offset+new Vector2(w,h),offset+new Vector2(0,h)});
        t.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
    }
    static void FinishCuboid(List<Vector3> v,List<Vector2> uv,List<int> t,Vector3 p,Vector3 size)
    {
        foreach(var n in new[]{Vector3.up,Vector3.down,Vector3.left,Vector3.right,Vector3.forward,Vector3.back}){
            var a=Vector3.Cross(Mathf.Abs(n.y)>.5f?Vector3.forward:Vector3.up,n);
            var b=Vector3.Cross(n,a);var half=size*.5f;
            var center=p+Vector3.Scale(n,half);a=Vector3.Scale(a,half);b=Vector3.Scale(b,half);
            FinishQuad(v,uv,t,center-a-b,center+a-b,center+a+b,center-a+b);
        }
    }
    static GameObject FinishGeometry(string name,Mesh mesh,Material material)
    {
        var go=HandmadeProp(name,mesh,Vector3.zero,Vector3.one,material,root);
        go.isStatic=true;return go;
    }
    static void SuspendedCeiling(string key,float width,float length,float y,float z0,bool serviceOpening)
    {
        int nx=Mathf.RoundToInt(width/.6f),nz=Mathf.RoundToInt(length/.6f);
        float dx=width/nx,dz=length/nz;
        var board=MallFinish("ceiling",new Color(.64f,.64f,.58f),0,.05f);
        var frame=MallFinish("enamel",new Color(.63f,.65f,.61f),.15f,.24f);
        var tiles=MallMesh("ceiling_tiles_v4_"+key,(v,u,t)=>{
            for(int x=0;x<nx;x++)for(int z=0;z<nz;z++){
                if(serviceOpening && x==nx-4 && (z==nz-5 || z==nz-6))continue;
                float a=-width*.5f+x*dx+.012f,b=z0+z*dz+.012f;
                FinishQuad(v,u,t,new Vector3(a,y,b),new Vector3(a+dx-.024f,y,b),new Vector3(a+dx-.024f,y,b+dz-.024f),new Vector3(a,y,b+dz-.024f),new Vector2(x*.61f,z*.73f));
            }
        });
        FinishGeometry("B1 suspended mineral ceiling panels "+key,tiles,board);
        var grid=MallMesh("ceiling_grid_v4_"+key,(v,u,t)=>{
            for(int x=0;x<=nx;x++)FinishCuboid(v,u,t,new Vector3(-width*.5f+x*dx,y-.012f,z0+length*.5f),new Vector3(.024f,.025f,length));
            for(int z=0;z<=nz;z++)FinishCuboid(v,u,t,new Vector3(0,y-.012f,z0+z*dz),new Vector3(width,.025f,.024f));
        });
        FinishGeometry("B1 ceiling T grid "+key,grid,frame);
        if(serviceOpening){
            float x=-width*.5f+(nx-3.5f)*dx,z=z0+(nz-5)*dz;
            // Two removed panels expose a shallow dark plenum and fixed services above head clearance.
            Decor("ceiling inspection recess",PrimitiveType.Cube,new Vector3(x,y+.24f,z),new Vector3(.59f,.04f,1.2f),shelf);
            RailSegment("exposed conduit in service opening",new Vector3(x-.17f,y+.12f,z-.60f),new Vector3(x-.17f,y+.12f,z+.60f),.028f,PrepSteel(),root);
            RailSegment("plenum suspension rod",new Vector3(x+.18f,y+.01f,z+.35f),new Vector3(x+.18f,y+.4f,z+.35f),.008f,PrepSteel(),root);
            var stain=RetailSurface("B1 old ceiling leak",new Color(.64f,.64f,.58f),0,.04f);
            stain.mainTexture=FinishTexture("B1CeilingLeakV1",(u,v)=>{
                float r=Vector2.Distance(new Vector2(u*.85f,v),new Vector2(.55f,.48f));
                float noise=Mathf.PerlinNoise(u*17+3,v*17);
                float ring=Mathf.Exp(-Mathf.Pow((r+noise*.06f-.28f)*34,2))*.25f;
                float center=Mathf.Clamp01((.30f-r)*5)*.12f;
                return new Color(.84f-center-ring*.6f,.84f-center-ring,.81f-center-ring*1.5f,1);
            });
            var patch=MallMesh("ceiling_local_leak_v1",(v,u,t)=>{
                FinishQuad(v,u,t,new(-.287f,0,-.287f),new(.287f,0,-.287f),new(.287f,0,.287f),new(-.287f,0,.287f));
                for(int i=0;i<u.Count;i++)u[i]/=.574f;
            });
            HandmadeProp("old water tide mark beside service hatch",patch,new Vector3(x-dx,y-.002f,z-dz*.5f),Vector3.one,stain,root);
        }
    }
    static void CeilingVent(Vector3 p)
    {
        var frame=MallFinish("enamel",new Color(.63f,.65f,.61f),.15f,.24f);
        var mesh=MallMesh("louvred_ceiling_vent_v4",(v,u,t)=>{
            foreach(int side in new[]{-1,1}){
                FinishCuboid(v,u,t,new Vector3(side*.29f,0,0),new Vector3(.035f,.03f,.61f));
                FinishCuboid(v,u,t,new Vector3(0,0,side*.29f),new Vector3(.55f,.03f,.035f));
            }
            for(int n=0;n<9;n++)FinishCuboid(v,u,t,new Vector3(0,.008f,-.24f+n*.06f),new Vector3(.55f,.035f,.022f));
        });
        HandmadeProp("ceiling return grille",mesh,p,Vector3.one,frame,root);
        Decor("dark air return throat",PrimitiveType.Cube,p+Vector3.up*.028f,new Vector3(.55f,.016f,.55f),dark);
    }
    static void TenantFinish(int kind)
    {
        SuspendedCeiling("tenant",13.8f,15.6f,3.94f,.2f,true);
        var paint=MallFinish("paint",kind==4?new Color(.47f,.52f,.41f):new Color(.64f,.62f,.54f),0,.12f);
        var lower=MallFinish("washable",kind==4?new Color(.25f,.32f,.26f):new Color(.37f,.39f,.36f),0,.23f);
        var walls=MallMesh("tenant_upper_wall_v4",(v,u,t)=>{
            FinishQuad(v,u,t,new(-6.89f,1.18f,.1f),new(-6.89f,1.18f,15.89f),new(-6.89f,3.92f,15.89f),new(-6.89f,3.92f,.1f));
            FinishQuad(v,u,t,new(6.89f,1.18f,15.89f),new(6.89f,1.18f,.1f),new(6.89f,3.92f,.1f),new(6.89f,3.92f,15.89f));
            FinishQuad(v,u,t,new(-6.89f,1.18f,15.89f),new(6.89f,1.18f,15.89f),new(6.89f,3.92f,15.89f),new(-6.89f,3.92f,15.89f));
            for(int i=0;i<t.Count;i+=3){int tmp=t[i+1];t[i+1]=t[i+2];t[i+2]=tmp;}
        });
        // Upper wall faces are reversed to face into the occupied room.
        FinishGeometry("tenant painted plaster finish",walls,paint);
        foreach(int side in new[]{-1,1}){
            Decor("washable lower wall lining",PrimitiveType.Cube,new Vector3(side*6.88f,.60f,8),new Vector3(.025f,1.18f,15.8f),lower);
            Decor("tenant skirting board",PrimitiveType.Cube,new Vector3(side*6.85f,.065f,8),new Vector3(.04f,.13f,15.8f),shelf);
            Decor("wall lining top trim",PrimitiveType.Cube,new Vector3(side*6.85f,1.20f,8),new Vector3(.04f,.035f,15.8f),PrepSteel());
            for(float z=1.4f;z<16;z+=1.2f)Decor("wall lining vertical joint",PrimitiveType.Cube,new Vector3(side*6.861f,.66f,z),new Vector3(.006f,1.04f,.006f),shelf);
        }
        Decor("back wall lower lining",PrimitiveType.Cube,new Vector3(0,.60f,15.88f),new Vector3(13.8f,1.18f,.025f),lower);
        Decor("back wall skirting",PrimitiveType.Cube,new Vector3(0,.065f,15.85f),new Vector3(13.8f,.13f,.04f),shelf);
        foreach(float z in new[]{5,11})CeilingVent(new Vector3(0,3.90f,z));
        // The existing two lights remain the only local illumination; casings add no lights.
        foreach(float x in new[]{-3,3}){
            Decor("practical ceiling mounting plate",PrimitiveType.Cube,new Vector3(x,3.895f,8),new Vector3(.52f,.06f,.52f),shelf);
            RailSegment("practical ceiling short suspension",new Vector3(x,3.76f,8),new Vector3(x,3.94f,8),.025f,PrepSteel(),root);
        }
        var conduit=MallFinish("enamel",new Color(.63f,.65f,.61f),.15f,.24f);
        RailSegment("surface electrical conduit",new Vector3(6.82f,.38f,14.3f),new Vector3(6.82f,3.82f,14.3f),.022f,conduit,root);
        RailSegment("ceiling service branch",new Vector3(6.82f,3.82f,14.3f),new Vector3(1.2f,3.82f,14.3f),.022f,conduit,root);
        foreach(float y in new[]{.38f,1.5f,3.55f})Decor("conduit saddle clip",PrimitiveType.Cube,new Vector3(6.825f,y,14.3f),new Vector3(.035f,.035f,.055f),PrepSteel());
        MerchBox("service electrical socket",new Vector3(6.81f,.38f,14.3f),new Vector3(.05f,.10f,.08f),conduit);
        foreach(float z in new[]{14.28f,14.32f})Decor("socket pin recess",PrimitiveType.Cube,new Vector3(6.782f,.38f,z),new Vector3(.005f,.02f,.009f),dark);
        foreach(float z in new[]{4.4f,10.4f}){
            Decor("sprinkler escutcheon",PrimitiveType.Cylinder,new Vector3(1.8f,3.90f,z),new Vector3(.075f,.012f,.075f),PrepSteel());
            RailSegment("sprinkler drop head",new Vector3(1.8f,3.89f,z),new Vector3(1.8f,3.82f,z),.018f,PrepSteel(),root);
        }
        // Small evidence of maintenance rather than uniform dirt on every surface.
        if(kind==4 || kind==5){
            var patch=MallFinish("paint",new Color(.64f,.62f,.54f),0,.12f);
            Decor("patched old wall fixing",PrimitiveType.Cube,new Vector3(6.873f,2.65f,14.28f),new Vector3(.006f,.43f,.62f),patch);
            foreach(float z in new[]{14.03f,14.53f})Decor("old fixing screw",PrimitiveType.Cylinder,new Vector3(6.865f,2.70f,z),new Vector3(.012f,.003f,.012f),shelf).transform.rotation=Quaternion.Euler(0,0,90);
        }
    }
    static void BasementConcourseFinish()
    {
        var old=root;root=new GameObject("B1 fixed concourse finish").transform;root.SetParent(old);
        // Seven bounded ceiling sectors: no new surfaces over stairs or escalator apertures.
        for(int i=0;i<7;i++){
            var parent=root;root=new GameObject("concourse ceiling sector "+i).transform;root.SetParent(parent);
            SuspendedCeiling("concourse",18,12,4.38f,0,false);
            foreach(float x in new[]{-5.4f,5.4f})CeilingVent(new Vector3(x,4.34f,6));
            root.position=new Vector3(0,0,-42+i*12);root=parent;
        }
        var metal=MallFinish("enamel",new Color(.35f,.36f,.33f),.15f,.24f);
        foreach(int side in new[]{-1,1}){
            // Visible service corridor ducts, segmented for culling; above 3.7 m clearance.
            for(int i=0;i<6;i++){
                float z=-26+i*11;
                Decor("B1 rectangular supply duct",PrimitiveType.Cube,new Vector3(side*30,4.02f,z),new Vector3(.85f,.34f,10.9f),metal);
                foreach(float dz in new[]{-5.1f,-1.7f,1.7f,5.1f}){
                    Decor("duct flange joint",PrimitiveType.Cube,new Vector3(side*30,4.02f,z+dz),new Vector3(.89f,.38f,.035f),PrepSteel());
                    RailSegment("duct ceiling hanger",new Vector3(side*30-.48f,3.79f,z+dz),new Vector3(side*30-.48f,4.57f,z+dz),.012f,PrepSteel(),root);
                    RailSegment("duct hanger crossbar",new Vector3(side*30-.5f,3.79f,z+dz),new Vector3(side*30+.5f,3.79f,z+dz),.018f,PrepSteel(),root);
                }
            }
        }
        root.position=Vector3.down*5;root=old;
    }
}
