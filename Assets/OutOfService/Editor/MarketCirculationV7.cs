using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    // The user's floor-plan photo is a circulation reference, not a surveyed plan.
    // Keep the agreed storeys; use perimeter departments, central blocks and a loop.
    static void BuildRetailCirculation()
    {
        var soffit=Mat("rough concrete slab soffit",new Color(.19f,.20f,.19f),0,.09f);
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>()){
            if(!renderer.sharedMaterial || !renderer.sharedMaterial.name.StartsWith("600 mm porcelain tiles"))continue;
            var b=renderer.bounds;
            if(b.size.y<.39f || b.min.y<-.5f || b.max.y>5.01f)continue;
            Decor("plain slab soffit",PrimitiveType.Cube,new Vector3(b.center.x,b.min.y-.003f,b.center.z),new Vector3(b.size.x,.004f,b.size.z),soffit);
        }
        AislePaving("1F front customer concourse",-17,31,-24.2f,-18.8f,0);
        AislePaving("1F west main loop",-17,-13,-18.8f,28.8f,0);
        AislePaving("1F east main loop",28,30.5f,-18.8f,28.8f,0);
        AislePaving("1F rear main loop",-13,28,27,29.8f,0);
        AislePaving("1F centre cross aisle",-13,28,-2.25f,3.25f,0);
        AislePaving("2F front fashion concourse",-43,43,-36.2f,-32.8f,5);
        AislePaving("2F west fashion promenade",-46,-43,-32.8f,29.5f,5);
        AislePaving("2F east fashion promenade",28,31,-32.8f,29.5f,5);
        AislePaving("2F rear fashion cross aisle",-43,28,27.8f,30.4f,5);
        AislePaving("2F front atrium connection",-43,28,-14,-12,5);
        AislePaving("2F rear atrium connection",-43,28,12,14,5);
        AislePaving("B1 central mall concourse",-8,8,-28,38,-5);
        // The original posts/handrails had human-sized gaps. Glass guards close them.
        foreach(int side in new[]{-1,1}){
            Box("atrium laminated glass guard",new Vector3(side*7,5.63f,0),new Vector3(.045f,1.22f,19.9f),cabinetGlass);
            Box("atrium laminated end guard",new Vector3(0,5.63f,side*10),new Vector3(13.9f,1.22f,.045f),cabinetGlass);
        }
        foreach(float z in new[]{-20f,.5f,28f}){
            LocalLamp("main aisle practical",new Vector3(-15,3.85f,z),new Vector3(-15,0,z),new Color(.83f,.87f,.79f),2.1f,9);
            LocalLamp("east circulation practical",new Vector3(29,3.85f,z),new Vector3(29,0,z),new Color(.83f,.87f,.79f),2.1f,9);
        }
    }

    static void AislePaving(string name,float x0,float x1,float z0,float z1,float y)
    {
        var size=new Vector3(x1-x0,.003f,z1-z0);
        var finish=new Material(SizedFloor(size)){name="circulation porcelain - "+name};
        finish.color=new Color(.69f,.69f,.66f);
        Decor(name,PrimitiveType.Cube,new Vector3((x0+x1)*.5f,y+.002f,(z0+z1)*.5f),size,finish);
    }
}
