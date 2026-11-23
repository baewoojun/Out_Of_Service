using System.Collections.Generic;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    static float EscalatorRise(float z)=>Mathf.Clamp((z+5)*.5f,0,5);
    static Mesh EscalatorRailLoop()
    {
        return MallMesh("escalator_continuous_handrail_v1",(v,u,t)=>{
            var path=new List<Vector3>();
            void Arc(float z,float y,float from,float to){for(int i=0;i<=16;i++){float a=Mathf.Lerp(from,to,i/16f)*Mathf.Deg2Rad;path.Add(new Vector3(0,y+Mathf.Sin(a)*.38f,z+Mathf.Cos(a)*.38f));}}
            Arc(-6.1f,.52f,180,90);
            path.Add(new(0,.90f,-5.3f));path.Add(new(0,1.05f,-4.7f));path.Add(new(0,5.75f,4.7f));path.Add(new(0,5.90f,5.3f));
            Arc(6.1f,5.52f,90,-90);
            path.Add(new(0,5.14f,5.3f));path.Add(new(0,4.99f,4.7f));path.Add(new(0,.29f,-4.7f));path.Add(new(0,.14f,-5.3f));
            Arc(-6.1f,.52f,-90,-180);
            path.RemoveAt(path.Count-1);
            for(int i=0;i<path.Count;i++){
                var tangent=(path[(i+1)%path.Count]-path[(i+path.Count-1)%path.Count]).normalized;
                var inPlane=Vector3.Cross(Vector3.right,tangent).normalized;
                for(int s=0;s<12;s++){
                    float a=s*Mathf.PI/6;
                    v.Add(path[i]+Vector3.right*(Mathf.Cos(a)*.061f)+inPlane*(Mathf.Sin(a)*.035f));u.Add(new Vector2(i/(float)path.Count,s/12f));
                    int k=i*12+s,next=i*12+(s+1)%12,other=((i+1)%path.Count)*12+s,otherNext=((i+1)%path.Count)*12+(s+1)%12;
                    t.AddRange(new[]{k,next,other,next,otherNext,other});
                }
            }
        });
    }

    static void EscalatorAssembly(float x)
    {
        var old=root;root=new GameObject("escalator complete assembly "+x).transform;root.SetParent(old);
        var steel=PrepSteel();
        var slope=Box("escalator invisible walking support",new Vector3(x,2.45f,0),new Vector3(1.48f,.22f,11.18f),steel);
        slope.transform.rotation=Quaternion.Euler(-26.565f,0,0);
        Object.DestroyImmediate(slope.GetComponent<MeshRenderer>());Object.DestroyImmediate(slope.GetComponent<MeshFilter>());
        var stepMesh=MallMesh("escalator_ribbed_tread_v1",(v,u,t)=>{
            FinishCuboid(v,u,t,Vector3.zero,new Vector3(1.40f,.040f,.294f));
            for(int n=0;n<47;n++)FinishCuboid(v,u,t,new Vector3((n-23)*.029f,.023f,0),new Vector3(.012f,.006f,.287f));
        });
        for(int step=0;step<34;step++){
            float z=-5+(step+.5f)*10/34f,y=EscalatorRise(z)+.052f;
            HandmadeProp("escalator ribbed horizontal tread",stepMesh,new Vector3(x,y,z),Vector3.one,steel,root);
            Decor("escalator aluminium riser",PrimitiveType.Cube,new Vector3(x,y-.067f,z-.147f),new Vector3(1.40f,.147f,.020f),steel);
            foreach(int side in new[]{-1,1})Decor("escalator yellow step side",PrimitiveType.Cube,new Vector3(x+side*.684f,y+.027f,z),new Vector3(.020f,.004f,.286f),yellow);
        }
        foreach(int side in new[]{-1,1}){
            float edge=x+side*.82f;
            float[] zs={-6.1f,-5f,5f,6.1f};
            var panel=MallMesh("escalator_upright_glass_v1",(v,u,t)=>{
                for(int i=0;i<zs.Length-1;i++){
                    float a=zs[i],b=zs[i+1];
                    FinishQuad(v,u,t,new(0,EscalatorRise(a)+.17f,a),new(0,EscalatorRise(b)+.17f,b),new(0,EscalatorRise(b)+.86f,b),new(0,EscalatorRise(a)+.86f,a));
                }
                MallTwoSides(v,u,t);
            });
            HandmadeProp("escalator upright balustrade glazing",panel,new Vector3(edge,0,0),Vector3.one,glass,root);
            var rail=HandmadeProp("escalator continuous rounded rubber handrail",EscalatorRailLoop(),new Vector3(edge,0,0),Vector3.one,dark,root);
            foreach(float z in new[]{-6.1f,6.1f}){
                Decor("escalator handrail newel base",PrimitiveType.Cube,new Vector3(edge,EscalatorRise(z)+.15f,z),new Vector3(.19f,.30f,.65f),steel);
                Decor("escalator handrail entry brush",PrimitiveType.Cube,new Vector3(edge,EscalatorRise(z)+.15f,z+Mathf.Sign(z)*.27f),new Vector3(.16f,.11f,.035f),dark);
            }
            for(int section=0;section<3;section++){
                float a=zs[section],b=zs[section+1],rise=EscalatorRise(b)-EscalatorRise(a),length=Mathf.Sqrt((b-a)*(b-a)+rise*rise);
                var p=new Vector3(edge,(EscalatorRise(a)+EscalatorRise(b))*.5f,(a+b)*.5f);
                var skirt=Decor("escalator brushed steel side casing",PrimitiveType.Cube,p+Vector3.up*.04f,new Vector3(.16f,.34f,length),steel);
                skirt.transform.rotation=Quaternion.Euler(-Mathf.Atan2(rise,b-a)*Mathf.Rad2Deg,0,0);
                var guard=Box("escalator side safety collision",p+Vector3.up*.53f,new Vector3(.08f,.88f,length),steel);
                guard.transform.rotation=skirt.transform.rotation;
                Object.DestroyImmediate(guard.GetComponent<MeshRenderer>());Object.DestroyImmediate(guard.GetComponent<MeshFilter>());
            }
        }
        foreach(int end in new[]{-1,1}){
            float y=end<0?0:5;
            Decor("escalator flush service landing",PrimitiveType.Cube,new Vector3(x,y+.013f,end*5.85f),new Vector3(1.46f,.025f,1.68f),steel);
            for(int seam=0;seam<3;seam++)Decor("escalator landing inspection seam",PrimitiveType.Cube,new Vector3(x,y+.026f,end*(5.25f+seam*.5f)),new Vector3(1.44f,.002f,.008f),dark);
            for(int tooth=0;tooth<47;tooth++)Decor("escalator interlocking comb tooth",PrimitiveType.Cube,new Vector3(x+(tooth-23)*.029f,y+.029f,end*5.02f),new Vector3(.011f,.012f,.12f),yellow);
        }
        root=old;
    }
}
