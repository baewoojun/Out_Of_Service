using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    static Mesh ProjectingTissue()
    {
        return MallMesh("facial_tissue_sheet_v1",(v,u,t)=>{
            const int rows=9,cols=14;
            for(int r=0;r<=rows;r++)for(int c=0;c<=cols;c++){
                float x=c/(float)cols,y=r/(float)rows;
                float width=Mathf.Lerp(.035f,.10f,Mathf.SmoothStep(0,1,y));
                v.Add(new Vector3((x-.5f)*width,.066f*y,.007f*Mathf.Sin(x*5*Mathf.PI+y*2)*y+.009f*y*y));
                u.Add(new Vector2(x,y));if(r==0||c==0)continue;int d=v.Count-1,b=d-cols-1;
                t.AddRange(new[]{b-1,d-1,b,b,d-1,d});
            }
            MallTwoSides(v,u,t);
        });
    }
    static void PaperGoodsFacing(Vector3 p,int kind,Transform parent)
    {
        var paper=RetailSurface("soft paper fibres v2",new Color(.85f,.835f,.79f),0,.055f);
        FineRelief(paper,"PaperFineFibresV2",(u,v)=>Mathf.PerlinNoise(u*210,v*170)*.022f,.20f);
        var card=RetailSurface("unbleached paper core",new Color(.49f,.36f,.23f),0,.08f);
        var art=PackMaterial(28,kind);
        if(kind==1||kind==3){
            Vector3 size=kind==1?new Vector3(.235f,.095f,.115f):new Vector3(.105f,.055f,.065f);
            var cardboard=RetailSurface("facial tissue pale carton",new Color(.73f,.70f,.64f),0,.10f);
            HandmadeProp("folded tissue dispenser carton",SoftShopEdge(size),p+Vector3.up*size.y*.5f,Vector3.one,cardboard,parent);
            Decor("tissue carton front print",PrimitiveType.Quad,p+new Vector3(0,size.y*.5f,-size.z*.505f),new Vector3(size.x*.97f,size.y*.91f,1),art,parent);
            if(kind==1){
                var seal=RetailSurface("sealed tissue carton top",new Color(.59f,.53f,.52f),0,.12f);
                // Unopened sale stock: the perforated panel is still attached.
                Decor("unopened tissue perforated dispensing panel",PrimitiveType.Cube,p+Vector3.up*(size.y+.0003f),new Vector3(.115f,.0005f,.036f),seal,parent);
                for(int edge=0;edge<9;edge++)foreach(int side in new[]{-1,1})
                    Decor("tissue tear perforation",PrimitiveType.Cube,p+new Vector3(-.05f+edge*.0125f,size.y+.0006f,side*.018f),new Vector3(.006f,.0002f,.0006f),cardboard,parent);
            }else{
                Decor("pocket tissue resealable strip",PrimitiveType.Cube,p+Vector3.up*(size.y+.0004f),new Vector3(.054f,.0005f,.034f),priceWhite,parent);
            }
            return;
        }
        bool towel=kind==2;int across=towel?2:3,depth=towel?1:2;
        float diameter=kind==5?.113f:.104f,h=towel?.228f:kind==5?.108f:.103f,step=diameter+.004f;
        for(int a=0;a<across;a++)for(int d=0;d<depth;d++){
            Vector3 center=p+new Vector3((a-(across-1)*.5f)*step,h*.5f,(d-(depth-1)*.5f)*step);
            Decor("wrapped individual paper roll",PrimitiveType.Cylinder,center,new Vector3(diameter,h*.5f,diameter),paper,parent);
            Decor("cardboard roll core",PrimitiveType.Cylinder,center+Vector3.up*(h*.5f+.0003f),new Vector3(.031f,.0004f,.031f),card,parent);
            Decor("recessed roll core opening",PrimitiveType.Cylinder,center+Vector3.up*(h*.5f+.0008f),new Vector3(.025f,.0003f,.025f),dark,parent);
        }
        float width=across*step,thickness=depth*step;
        Decor("clear six roll packaging film",PrimitiveType.Cube,p+Vector3.up*h*.5f,new Vector3(width+.003f,h+.004f,thickness+.003f),ChilledWrap(),parent);
        float labelH=towel?.145f:h*.80f;
        Decor("paper roll original retail label",PrimitiveType.Quad,p+new Vector3(0,h*.49f,-thickness*.5f-.002f),new Vector3(width*.97f,labelH,1),art,parent);
    }
}
