using UnityEngine;
public static partial class MarketPrototypeBuilderV7
{
    static void ShoeDisplay(float x,float z,Transform group)
    {
        Box("shoe display back",new Vector3(x,5.85f,z),new Vector3(3,.05f,1),shelf,group);
        foreach(float dx in new[]{-1.45f,1.45f})Box("shoe display upright",new Vector3(x+dx,5.85f,z),new Vector3(.045f,1.55f,.7f),shelfTop,group);
        for(int tier=0;tier<3;tier++)
        {
            float y=5.30f+tier*.48f;
            Box("shoe slat shelf",new Vector3(x,y,z),new Vector3(3,.045f,.85f),cream,group);
            for(int pair=0;pair<4;pair++)for(int foot=0;foot<2;foot++)
            {
                Vector3 p=new Vector3(x-1.13f+pair*.72f+foot*.18f,y+.037f,z-.12f);
                Material upper=pair%3==0?dark:pair%3==1?blue:cream;
                Decor("shaped rubber outsole",PrimitiveType.Sphere,p+Vector3.up*.02f,new Vector3(.145f,.047f,.32f),priceWhite,group);
                Decor("rounded toe box",PrimitiveType.Sphere,p+new Vector3(0,.066f,-.055f),new Vector3(.14f,.095f,.21f),upper,group);
                Decor("heel counter",PrimitiveType.Sphere,p+new Vector3(0,.09f,.075f),new Vector3(.125f,.145f,.14f),upper,group);
                Decor("dark ankle opening",PrimitiveType.Sphere,p+new Vector3(0,.155f,.075f),new Vector3(.079f,.013f,.075f),dark,group);
                if(tier==2)
                {
                    Decor("ankle boot shaft",PrimitiveType.Capsule,p+new Vector3(0,.16f,.07f),new Vector3(.135f,.11f,.14f),upper,group);
                    Decor("boot collar lining",PrimitiveType.Sphere,p+new Vector3(0,.26f,.07f),new Vector3(.092f,.016f,.09f),dark,group);
                }
                if(tier==1)
                    Decor("loafer saddle strap",PrimitiveType.Cube,p+new Vector3(0,.106f,-.018f),new Vector3(.132f,.021f,.038f),upper,group);
                else for(int lace=0;lace<4;lace++)RailSegment("crossed shoelace",p+new Vector3(-.04f,.113f+lace*.006f,-.035f+lace*.025f),p+new Vector3(.04f,.113f+lace*.006f,-.01f+lace*.025f),.005f,priceWhite,group);
            }
        }
        for(int box=0;box<5;box++)
        {
            Vector3 p=new Vector3(x-1.1f+box*.55f,5.14f,z+.08f);
            Decor("stock shoebox",PrimitiveType.Cube,p,new Vector3(.47f,.18f,.32f),box%2==0?red:cream,group);
            Decor("shoebox lid lip",PrimitiveType.Cube,p+Vector3.up*.085f,new Vector3(.48f,.025f,.33f),shelfTop,group);
            Decor("shoe size sticker",PrimitiveType.Cube,p+new Vector3(0,0,-.163f),new Vector3(.12f,.07f,.002f),priceWhite,group);
        }
        Box("fitting bench cushion",new Vector3(x,5.43f,z-1.75f),new Vector3(1.8f,.14f,.52f),fabric,group);
        foreach(float dx in new[]{-.7f,.7f})Box("fitting bench foot",new Vector3(x+dx,5.2f,z-1.75f),new Vector3(.06f,.4f,.45f),shelfTop,group);
    }
}
