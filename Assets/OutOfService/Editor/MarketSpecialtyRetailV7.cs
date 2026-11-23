using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public static partial class MarketPrototypeBuilderV7
{
    static readonly Dictionary<string,Material> retailSurfaces=new Dictionary<string,Material>();
    static Material RetailSurface(string key,Color color,float metallic,float gloss)
    {if(!retailSurfaces.TryGetValue(key,out var m)){m=Mat(key,color,metallic,gloss);retailSurfaces.Add(key,m);}return m;}
    static Mesh HandleRing()
    {
        const string path="Assets/OutOfService/Generated/MoldedHandleRing.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
        var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
        for(int a=0;a<=24;a++)for(int b=0;b<=8;b++)
        {
            float t=a*Mathf.PI*2/24,q=b*Mathf.PI*2/8;
            Vector3 normal=new Vector3(Mathf.Sin(q),Mathf.Cos(t)*Mathf.Cos(q),Mathf.Sin(t)*Mathf.Cos(q));
            v.Add(new Vector3(0,Mathf.Cos(t)*.38f,Mathf.Sin(t)*.38f)+normal*.12f);n.Add(normal);uv.Add(new Vector2(a/24f,b/8f));
        }
        for(int a=0;a<24;a++)for(int b=0;b<8;b++){int k=a*9+b;tr.AddRange(new[]{k,k+9,k+1,k+1,k+9,k+10});}
        mesh=new Mesh{name="MoldedHandleRing"};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetTriangles(tr,0);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static bool SpecialtyRetailItem(string name,int category,int variant,Vector3 p,int side,Transform group,Material print)
    {
        var paper=RetailSurface("embossed uncoated paper",new Color(.88f,.86f,.80f),0,.06f);
        var resin=RetailSurface("ivory HDPE molded plastic",new Color(.8f,.81f,.74f),0,.28f);
        if(category==21)
        {
            if(variant==3)
            {
                Decor("sealed pocket tissue stack",PrimitiveType.Cube,p+Vector3.up*.038f,new Vector3(.098f,.076f,.156f),paper,group);
                RetailPrint(name,print,p,side,.098f,.076f,.156f,group);
                Decor("resealable pocket flap",PrimitiveType.Cube,p+Vector3.up*.077f,new Vector3(.049f,.002f,.086f),resin,group);
            }
            else if(variant==1)
            {
                Decor("tissue cardboard dispenser",PrimitiveType.Cube,p+Vector3.up*.09f,new Vector3(.27f,.18f,.43f),cream,group);
                RetailPrint(name,print,p,side,.27f,.18f,.43f,group);
                Decor("oval tissue opening",PrimitiveType.Sphere,p+Vector3.up*.184f,new Vector3(.14f,.008f,.27f),dark,group);
                var tissue=HandmadeProp("folded projecting facial tissue",GarmentMesh(0),p+new Vector3(0,.185f,0),new Vector3(.22f,.17f,.3f),paper,group);
                tissue.transform.Rotate(0,90,10);
            }
            else
            {
                int across=variant==2?2:3,deep=variant==2?1:2;float h=variant==2?.39f:variant==5?.25f:.29f;
                for(int a=0;a<across;a++)for(int d=0;d<deep;d++)
                {
                    Vector3 c=p+new Vector3((d-(deep-1)*.5f)*.18f,h*.5f,(a-(across-1)*.5f)*.16f);
                    Decor("individual paper roll",PrimitiveType.Cylinder,c,new Vector3(.155f,h*.5f,.155f),paper,group);
                    Decor("paper core opening",PrimitiveType.Cylinder,c+Vector3.up*(h*.5f+.002f),new Vector3(.042f,.002f,.042f),dark,group);
                    Decor("cardboard core rim",PrimitiveType.Cylinder,c+Vector3.up*(h*.5f+.001f),new Vector3(.055f,.001f,.055f),cream,group);
                }
                RetailPrint(name,print,p+Vector3.up*h*(variant==5?.26f:.08f),side,deep*.18f+.021f,h*(variant==5?.69f:.81f),across*.16f,group);
                Decor("clear retail roll wrapping",PrimitiveType.Cube,p+Vector3.up*h*.5f,new Vector3(deep*.18f+.01f,h+.008f,across*.16f+.01f),produceFilm,group);
            }
            return true;
        }
        if(category==22)
        {
            bool pump=variant==2||variant==5,handle=variant==0||variant==3;
            float h=pump?.32f:handle?.43f:.46f,d=handle?.24f:.19f,w=handle?.32f:.23f;
            Material plastic=variant==0?red:variant==3?RetailSurface("green detergent HDPE",new Color(.10f,.32f,.13f),0,.32f):variant==4?blue:resin;
            HandmadeProp("molded laundry bottle",RetailBottleProfile(handle?5:2),p,new Vector3(d,h,w),plastic,group);
            if(handle)HandmadeProp("open integral jug handle",HandleRing(),p+new Vector3(0,h*.63f,w*.43f),new Vector3(.12f,.21f,.16f),plastic,group);
            if(pump)
            {
                Decor("pump neck",PrimitiveType.Cylinder,p+Vector3.up*(h+.035f),new Vector3(.028f,.035f,.028f),priceWhite,group);
                Decor("pump head",PrimitiveType.Cube,p+new Vector3(side*.035f,h+.075f,0),new Vector3(.12f,.026f,.055f),priceWhite,group);
                Decor("downturned dispenser tip",PrimitiveType.Cylinder,p+new Vector3(side*.085f,h+.062f,0),new Vector3(.018f,.015f,.018f),priceWhite,group);
            }
            else Decor("measuring cup screw cap",PrimitiveType.Cylinder,p+Vector3.up*(h+.025f),new Vector3(.09f,.035f,.09f),variant==3?red:blue,group);
            RetailPrint(name,print,p,side,d,h*.78f,w*.85f,group);return true;
        }
        if(category==19&&(variant==1||variant==3))
        {
            Material plastic=variant==1?red:green;
            Decor("rectangular Korean paste tub",PrimitiveType.Cube,p+Vector3.up*.046f,new Vector3(.096f,.092f,.111f),plastic,group);
            Decor("overhanging sealed lid",PrimitiveType.Cube,p+Vector3.up*.096f,new Vector3(.102f,.008f,.116f),plastic,group);
            RetailPrint(name,print,p,side,.096f,.092f,.111f,group);return true;
        }
        return false;
    }
}
