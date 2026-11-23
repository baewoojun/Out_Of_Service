using UnityEditor;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    static void WarehouseCarton(Vector3 p,Vector3 size,int kind,float yaw=0)
    {
        var group=new GameObject("folded corrugated logistics carton").transform;group.SetParent(root);group.position=p;
        bool cold=kind>=2;
        var board=RetailSurface("warehouse kraft board "+kind,cold?new Color(.71f,.72f,.67f):kind==0?new Color(.51f,.38f,.23f):new Color(.62f,.48f,.31f),0,.10f);
        board.mainTexture=FinishTexture("KraftPaperFibresV1",(u,v)=>{
            float n=.83f+.10f*Mathf.PerlinNoise(u*91,v*91)+.06f*Mathf.PerlinNoise(u*12,v*155);
            return new Color(n,n,n,1);
        });
        FineRelief(board,"KraftPaperReliefV1",(u,v)=>Mathf.PerlinNoise(u*47,v*185)*.08f,.16f);
        var tape=RetailSurface("warehouse tan packing tape",new Color(.45f,.33f,.18f),0,.46f);
        var crease=RetailSurface("warehouse folded seam shadow",new Color(.22f,.18f,.13f),0,.07f);
        MerchBox("creased corrugated case",p+Vector3.up*(size.y*.5f),size,board,group);
        Decor("sealed carton top tape",PrimitiveType.Cube,p+new Vector3(0,size.y+.001f,0),new Vector3(.058f,.002f,size.z+.002f),tape,group);
        var ink=RetailSurface("warehouse original handling label "+kind,Color.white,0,.12f);
        ink.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Prints/warehouse-"+kind+".png");
        foreach(int face in new[]{-1,1}){
            float z=face*(size.z*.5f+.001f);
            Decor("carton tape folded over edge",PrimitiveType.Cube,p+new Vector3(0,size.y-.055f,z),new Vector3(.058f,.11f,.002f),tape,group);
            Decor("carton flap overlap",PrimitiveType.Cube,p+new Vector3(0,size.y-.007f,z),new Vector3(size.x*.97f,.0015f,.0015f),crease,group);
            var label=Decor("warehouse readable handling ticket",PrimitiveType.Quad,p+new Vector3(size.x*.06f,size.y*.48f,z+face*.0015f),new Vector3(.235f,.147f,1),ink,group);
            label.transform.rotation=Quaternion.Euler(0,face>0?180:0,0);
        }
        foreach(int edge in new[]{-1,1})
            Decor("carton folded edge crease",PrimitiveType.Cube,p+new Vector3(edge*(size.x*.5f-.008f),size.y+.0011f,0),new Vector3(.0018f,.001f,size.z*.96f),crease,group);
        group.rotation=Quaternion.Euler(0,yaw,0);
    }
}
