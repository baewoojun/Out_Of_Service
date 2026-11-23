using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    // Authored cafe/food-court furniture. The previous footprints and .79m top
    // height are preserved so existing dishes, staff routes and doors still fit.
    static void RetailDiningSet(Vector3 p,bool restaurant)
    {
        int row=Mathf.RoundToInt((p.z-3)/3),style=restaurant?(row==1?2:1):(row==1?1:0);
        var wood=MallFinish("wood",restaurant?new Color(.32f,.23f,.14f):new Color(.46f,.35f,.22f),0,.28f);
        var frame=RetailSurface("furniture satin charcoal powder coat",new Color(.055f,.061f,.058f),.3f,.30f);
        var laminate=RetailSurface("food court ivory laminate",new Color(.56f,.57f,.51f),0,.30f);
        Vector3 dimensions=style==0?new(.86f,.036f,.86f):style==1?new(.80f,.036f,.80f):new(1.05f,.036f,.70f);
        GameObject top;
        if(style==0)top=HandmadeProp("rounded laminate cafe tabletop",CafeRoundTop(),p+Vector3.up*.754f,Vector3.one,wood,root);
        else top=HandmadeProp(style==1?"square bistro tabletop":"rectangular food court tabletop",SoftShopEdge(dimensions),p+Vector3.up*.772f,Vector3.one,restaurant?laminate:wood,root);
        var tabletop=top.AddComponent<BoxCollider>();tabletop.size=dimensions;tabletop.center=style==0?Vector3.up*.018f:Vector3.zero;
        if(style==2){
            foreach(int side in new[]{-1,1}){
                RailSegment("dining trestle upright",p+new Vector3(side*.37f,.10f,0),p+new Vector3(side*.37f,.75f,0),.045f,frame,root);
                RailSegment("dining trestle foot",p+new Vector3(side*.37f,.07f,-.28f),p+new Vector3(side*.37f,.07f,.28f),.045f,frame,root);
            }
            RailSegment("under table stretcher",p+new Vector3(-.37f,.62f,0),p+new Vector3(.37f,.62f,0),.04f,frame,root);
        }else{
            Box("cafe hollow pedestal",p+Vector3.up*.39f,new Vector3(.075f,.71f,.075f),frame);
            foreach(var d in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back}){
                RailSegment("four branch table base",p+Vector3.up*.12f,p+d*.34f+Vector3.up*.045f,.048f,frame,root);
                Decor("adjustable table glide",PrimitiveType.Cylinder,p+d*.34f+Vector3.up*.016f,new Vector3(.045f,.013f,.045f),dark);
            }
        }
        foreach(int side in new[]{-1,1}){
            // Same chairs within a set; different construction between cafe and restaurant.
            uint seed=StockSeed(p,restaurant?52:51,side);
            bool used=seed%3!=0;
            float yaw=side*90+(used?((int)(seed%23)-11):side*2);
            float pull=used?.04f+(seed%7)*.014f:-.025f;
            var offset=new Vector3(side*(.86f+pull),0,used?((int)(seed%9)-4)*.014f:0);
            RetailDiningChair(p+offset,yaw,restaurant?2:row==1?1:0,wood,frame);
        }
    }

    static Mesh CafeRoundTop()
    {
        return MallMesh("cafe_round_top_planar_v3",(v,u,t)=>{
            const int sides=48;
            float[] heights={0,0,.004f,.030f,.036f,.036f};
            float[] radii={0,.419f,.43f,.43f,.419f,0};
            for(int ring=0;ring<heights.Length;ring++)for(int side=0;side<=sides;side++){
                float a=side*Mathf.PI*2/sides;
                var p=new Vector3(Mathf.Cos(a)*radii[ring],heights[ring],Mathf.Sin(a)*radii[ring]);
                v.Add(p);
                // Veneer is a flat sheet: keep its grain straight across the cap,
                // rather than collapsing cylindrical UVs into a star at the centre.
                u.Add(new Vector2(p.x/.86f+.5f,p.z/.86f+.5f));
                if(ring<heights.Length-1 && side<sides){int k=ring*(sides+1)+side;t.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});}
            }
        });
    }

    static Mesh CurvedChairPanel(int style)
    {
        return MallMesh("cafe_curved_ply_back_v2_"+style,(v,u,t)=>{
            const int rows=4,cols=12;int plane=(rows+1)*(cols+1);
            for(int side=0;side<2;side++)for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++){
                float a=x/(float)cols*2-1,f=y/(float)rows;
                float h=style==1?.31f:.22f,w=.215f*(1-.12f*f);
                float z=.18f+.045f*f-.045f*a*a+(side==0?-.006f:.006f);
                v.Add(new Vector3(a*w,.57f+f*h-.016f*a*a,z));u.Add(new Vector2(x/(float)cols,f));
                if(y<rows && x<cols){int k=side*plane+y*(cols+1)+x;
                    if(side==0)t.AddRange(new[]{k,k+cols+1,k+1,k+1,k+cols+1,k+cols+2});
                    else t.AddRange(new[]{k,k+1,k+cols+1,k+1,k+cols+2,k+cols+1});
                }
            }
            void Edge(int a,int b){t.AddRange(new[]{a,b,a+plane,b,b+plane,a+plane});}
            for(int x=0;x<cols;x++){Edge(x+1,x);Edge(rows*(cols+1)+x,rows*(cols+1)+x+1);}
            for(int y=0;y<rows;y++){Edge(y*(cols+1),(y+1)*(cols+1));Edge((y+1)*(cols+1)+cols,y*(cols+1)+cols);}
        });
    }

    static void RetailDiningChair(Vector3 p,float yaw,int style,Material wood,Material frame)
    {
        var parent=root;root=new GameObject("dining chair construction "+style).transform;root.SetParent(parent);
        var seatMat=style==1?LaundryCotton(1):wood;
        HandmadeProp("rounded chair seat",SoftShopEdge(new Vector3(.43f,style==1?.055f:.026f,.43f)),new Vector3(0,.446f,0),Vector3.one,seatMat,root);
        if(style==2){
            // Slatted food-court back has real open gaps, rather than a solid box.
            foreach(float x in new[]{-.19f,.19f})RailSegment("chair back stile",new Vector3(x,.40f,.17f),new Vector3(x,.85f,.23f),.026f,wood,root);
            for(int n=0;n<3;n++){
                HandmadeProp("curved restaurant back slat",CurvedChairPanel(0),new Vector3(0,.43f+n*.095f,.12f),new Vector3(.95f,.30f,.48f),wood,root);
            }
        }else HandmadeProp("curved chair backrest",CurvedChairPanel(style),Vector3.zero,Vector3.one,wood,root);
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1}){
            RailSegment("splayed chair leg",new Vector3(x*.205f,.025f,z*.20f),new Vector3(x*.155f,.44f,z*.145f),style==1||style==3?.025f:.031f,style==1||style==3?frame:wood,root);
            Decor("chair floor glide",PrimitiveType.Cylinder,new Vector3(x*.205f,.013f,z*.20f),new Vector3(.032f,.012f,.032f),dark);
            if(z==1 && style!=2)RailSegment("chair back support",new Vector3(x*.155f,.40f,.145f),new Vector3(x*.165f,.67f,.20f),.022f,style==1||style==3?frame:wood,root);
        }
        RailSegment("chair lower stretcher",new Vector3(-.18f,.22f,.17f),new Vector3(.18f,.22f,.17f),.019f,style==1||style==3?frame:wood,root);
        var seat=root.gameObject.AddComponent<BoxCollider>();seat.center=new Vector3(0,.25f,0);seat.size=new Vector3(.44f,.48f,.44f);
        var back=root.gameObject.AddComponent<BoxCollider>();back.center=new Vector3(0,.68f,.20f);back.size=new Vector3(.43f,.37f,.075f);
        root.position=p;root.rotation=Quaternion.Euler(0,yaw,0);root=parent;
    }
}
