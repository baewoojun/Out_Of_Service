using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    // Stable merchandising variation, baked into the existing shared sectors.
    // Coordinates, not Random.state, keep the same shelf through regeneration.
    static uint StockSeed(Vector3 p,int family,int variant)
    {
        unchecked {
            uint h=(uint)(Mathf.RoundToInt(p.x*1000)*73856093 ^ Mathf.RoundToInt(p.y*1000)*19349663 ^ Mathf.RoundToInt(p.z*1000)*83492791 ^ family*971 ^ variant*67);
            h^=h>>16;h*=0x7feb352d;h^=h>>15;h*=0x846ca68b;return h^(h>>16);
        }
    }
    static void SetShoppedFacing(Transform unit,Vector3 foot,float yaw,int family,int variant)
    {
        uint seed=StockSeed(foot,family,variant);
        float Signed(int shift)=>((seed>>shift)&255)/127.5f-1;
        bool round=family>=10&&family<=14||family==4&&variant!=1;
        bool soft=family==0||family==3||family==5||family==9||family>=20&&family<=23;
        // Most fronts stay readable; a few packs were handled and put back.
        float angle=Signed(0)*(seed%9==0?(round?11:soft?7:3.5f):1.6f);
        float setback=seed%5==0?.013f+.013f*Mathf.Abs(Signed(8)):.004f*Signed(8);
        var rotation=Quaternion.Euler(0,yaw,0);
        unit.rotation=Quaternion.Euler(0,yaw+angle,0);
        unit.position=foot+rotation*new Vector3(Signed(16)*.0025f,0,setback);
    }
    static bool ShoppedFrontGap(int category,int tier,int item,int side,int depth)
    {
        if(depth!=0||item<2)return false; // Keep the interactive first facing.
        int bay=item/3;
        int seed=Mathf.Abs(category*31+tier*17+bay*13+(side+1)*19);
        return seed%29==0 && item%3!=2; // A partly bought-out bay, with rear stock.
    }
}
