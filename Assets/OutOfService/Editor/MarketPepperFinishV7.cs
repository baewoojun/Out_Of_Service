using UnityEngine;
using UnityEditor;

public static partial class MarketPrototypeBuilderV7
{
    // Original surface and geometry based on the supplied produce photographs.
    // No source photograph is projected onto the fruit.
    static Mesh RetailPepperBody(int variation)
    {
        const int rings=20,sides=32;
        var mesh=MallMesh("retail_pepper_body_v3_"+variation,(v,u,t)=>{
            int lobes=variation==1||variation==4?3:4;
            float phase=.27f*variation;
            float[] levels={0,.025f,.08f,.18f,.37f,.60f,.78f,.88f,.94f,.98f,1};
            float[] widths={0,.18f,.32f,.38f,.42f,.47f,.50f,.47f,.36f,.21f,0};
            for(int row=0;row<=rings;row++)for(int col=0;col<=sides;col++){
                float f=row/(float)rings,a=col*2*Mathf.PI/sides,s=Mathf.Sin(f*Mathf.PI);
                float lobe=Mathf.Cos(lobes*a+phase+.12f*Mathf.Sin(f*3));
                int segment=0;while(segment<levels.Length-2&&f>levels[segment+1])segment++;
                float blend=Mathf.InverseLerp(levels[segment],levels[segment+1],f);
                float radius=Mathf.Lerp(widths[segment],widths[segment+1],blend);
                radius*=.96f*(1+.095f*lobe+.025f*Mathf.Sin(a*2+phase+f*2));
                float y=-.48f+1.04f*f;
                // Broad four-shouldered crown and tapered, separated basal lobes.
                y-=.22f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.94f,1,f));
                y+=.045f*Mathf.Pow(Mathf.Clamp01((.14f-f)/.14f),2);
                y-=.032f*lobe*Mathf.Sin(Mathf.PI*Mathf.Clamp01(f/.23f));
                y+=.025f*lobe*Mathf.Sin(Mathf.PI*Mathf.Clamp01((f-.7f)/.3f));
                float lean=.030f*s*Mathf.Sin(f*2+variation);
                v.Add(new Vector3(Mathf.Cos(a)*radius+lean,y,Mathf.Sin(a)*radius*(.97f+.018f*variation)));
                u.Add(new Vector2(col/(float)sides,f));
                if(row==rings||col==sides)continue;
                int k=row*(sides+1)+col;t.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});
            }
        });
        if(preparedHarvestNormals.Add(mesh)){
            var normals=mesh.normals;
            for(int row=0;row<=rings;row++){
                int first=row*(sides+1),last=first+sides;
                var n=(normals[first]+normals[last]).normalized;normals[first]=normals[last]=n;
                if(row==0||row==rings)for(int col=0;col<=sides;col++)normals[first+col]=row==0?Vector3.down:Vector3.up;
            }
            mesh.normals=normals;mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);
        }
        return mesh;
    }

    static Material RetailPepperSkin(int colour,int variation)
    {
        var mat=RetailSurface("retail paprika wax v1 "+colour+" "+variation,Color.white,0,.57f);
        if(mat.mainTexture)return mat;
        mat.mainTexture=FinishTexture("RetailPaprikaColourV1_"+colour+"_"+variation,(u,v)=>{
            float cloud=VegetableMottle(u+.137f*variation,v,4),fine=VegetableMottle(u,v,110);
            Color darkColour=colour==0?new Color(.63f,.055f,.018f):new Color(.83f,.47f,.018f);
            Color lightColour=colour==0?new Color(.80f,.10f,.027f):new Color(.99f,.67f,.030f);
            Color skin=Color.Lerp(darkColour,lightColour,Mathf.Clamp01(.17f+cloud*.77f+variation*.045f));
            // Low-amplitude pores, a slightly duller crown, no painted stripes.
            skin*=.98f+.035f*fine;
            float crown=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.91f,1,v))*(.10f+.06f*cloud);
            skin=Color.Lerp(skin,colour==0?new Color(.37f,.11f,.035f):new Color(.52f,.42f,.06f),crown);
            return skin.linear;
        });
        mat.SetTexture("_MetallicGlossMap",FinishTexture("RetailPaprikaWaxV1_"+colour,(u,v)=>{
            float cloud=VegetableMottle(u,v,7),pores=VegetableMottle(u,v,100);
            return new Color(0,0,0,.87f-.18f*cloud-.025f*pores-.15f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.91f,1,v)));
        }));
        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        FineRelief(mat,"RetailPaprikaPoresV1",(u,v)=>VegetableMottle(u,v,95)*.025f,.38f);
        return mat;
    }

    static Material RetailPepperStem()
    {
        var mat=RetailSurface("paprika cut stem v1",Color.white,0,.29f);
        if(mat.mainTexture)return mat;
        mat.mainTexture=FinishTexture("RetailPaprikaStemV1",(u,v)=>{
            float n=Mathf.PerlinNoise(u*13,v*11),rib=Mathf.Sin(u*10*Mathf.PI)*.018f;
            var c=Color.Lerp(new Color(.15f,.25f,.045f),new Color(.33f,.42f,.09f),n)+new Color(rib,rib,rib);
            return Color.Lerp(c,new Color(.56f,.55f,.29f),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.93f,1,v))).linear;
        });
        return mat;
    }
}
