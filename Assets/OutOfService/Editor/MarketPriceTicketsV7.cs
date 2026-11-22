using UnityEngine;
using UnityEditor;
public static partial class MarketPrototypeBuilderV7
{
    static void ShelfPriceTicket(int category,Vector3 position,int side,Transform parent)
    {
        string key="shelf price "+category;
        if(!retailSurfaces.TryGetValue(key,out var material))
        {
            material=Mat(key,Color.white,0,.12f);
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/OutOfService/Art/Prices/price_"+category.ToString("D2")+".png"));
            retailSurfaces.Add(key,material);
        }
        Decor("readable shelf price ticket",PrimitiveType.Quad,position,new Vector3(.17f,.056f,1),material,parent).transform.eulerAngles=new Vector3(0,-side*90,0);
    }
}
