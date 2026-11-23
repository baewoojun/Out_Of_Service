using UnityEditor;
using UnityEngine;

static class MarketBeverageInstancingStudy
{
    // Opt-in comparison material; baseline scene and global SRP Batcher stay unchanged.
    public static void BuildStudyPlayer()
    {
        const string folder="Assets/OutOfService/Resources";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/OutOfService","Resources");
        var shader=Shader.Find("OutOfService/Thin PET Instanced");
        if(!shader)throw new System.InvalidOperationException("Missing PET instancing study shader");
        const string path=folder+"/MarketPETInstancing.asset";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}else mat.shader=shader;
        mat.name="candidate instanced clear PET";mat.enableInstancing=true;mat.renderQueue=3000;
        mat.SetColor("_BaseColor",new Color(.985f,.995f,1,1));mat.SetFloat("_Smoothness",.88f);
        mat.SetFloat("_RefractionPixels",8);mat.SetFloat("_EdgeOpacity",.26f);mat.SetFloat("_EdgeLoss",.55f);
        mat.SetVector("_InstanceTransmission",Vector4.one);EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();
        MarketPlayerBuildReview.BuildPlayer();
    }
}
