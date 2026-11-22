using UnityEditor;
using UnityEngine;
using System.IO;

public static partial class MarketPrototypeBuilderV7
{
    static Mesh orchardApple;
    static Material orchardSkin;
    static void PrepareOrchardApple()
    {
        if (orchardApple && orchardSkin) return;
        const string folder="Assets/OutOfService/Generated/";
        const string meshPath=folder+"AppleScanShell.asset", imagePath=folder+"AppleScanPeel.png";
        orchardApple=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (!orchardApple || !File.Exists(imagePath))
        {
            const string prefix="Assets/OutOfService/Art/Models/food_apple_01/food_apple_01";
            var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefix+".fbx"));
            var photo=new Texture2D(2,2);
            var peel=new Texture2D(256,160,TextureFormat.RGB24,false);
            try
            {
                photo.LoadImage(File.ReadAllBytes(prefix+"_diff_1k.jpg"));
                source.transform.position=Vector3.up*1000;
                var filter=source.GetComponentInChildren<MeshFilter>();
                var hitMesh=filter.gameObject.AddComponent<MeshCollider>();
                hitMesh.sharedMesh=filter.sharedMesh;
                Physics.SyncTransforms();
                Bounds bounds=hitMesh.bounds;
                float reach=bounds.size.magnitude;
                Vector3 Sample(float u,float v,out Color color)
                {
                    float a=u*2*Mathf.PI,h=(v-.5f)*Mathf.PI;
                    var dir=new Vector3(Mathf.Cos(a)*Mathf.Cos(h),Mathf.Sin(h),Mathf.Sin(a)*Mathf.Cos(h));
                    if(hitMesh.Raycast(new Ray(bounds.center+dir*reach,-dir),out RaycastHit hit,2*reach))
                    {
                        color=photo.GetPixelBilinear(hit.textureCoord.x,hit.textureCoord.y);
                        return (hit.point-bounds.center)/bounds.size.y;
                    }
                    color=new Color(.52f,.18f,.08f); return dir*.46f;
                }
                for(int y=0;y<peel.height;y++)for(int x=0;x<peel.width;x++){
                    Sample((x+.5f)/peel.width,(y+.5f)/peel.height,out Color c);peel.SetPixel(x,y,c);
                }
                File.WriteAllBytes(imagePath,peel.EncodeToPNG());AssetDatabase.ImportAsset(imagePath);
                const int cols=24,rows=16;
                var pos=new Vector3[(rows+1)*(cols+1)];var uv=new Vector2[pos.Length];var tris=new int[rows*cols*6];
                for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++){
                    int n=y*(cols+1)+x;uv[n]=new Vector2((float)x/cols,(float)y/rows);
                    pos[n]=Sample(uv[n].x,uv[n].y,out _);
                    if(x==cols||y==rows)continue;
                    int t=(y*cols+x)*6;tris[t]=n;tris[t+1]=n+cols+1;tris[t+2]=n+1;
                    tris[t+3]=n+1;tris[t+4]=n+cols+1;tris[t+5]=n+cols+2;
                }
                orchardApple=new Mesh{name="Apple scan radial shell",vertices=pos,uv=uv,triangles=tris};
                orchardApple.RecalculateNormals();orchardApple.RecalculateBounds();AssetDatabase.CreateAsset(orchardApple,meshPath);
            }
            finally {Object.DestroyImmediate(source);Object.DestroyImmediate(photo);Object.DestroyImmediate(peel);}
        }
        orchardSkin=RetailSurface("natural scanned orchard apple",Color.white,0,.26f);
        orchardSkin.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
    }
}
