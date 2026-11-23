using System;
using System.IO;
using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    struct CatalogPose
    {
        public Transform fixture;
        public Vector3 foot, outward;
        public float rank;
    }
    static CatalogPose[] catalog;

    static void CatalogView(int family,int variant,Vector3 foot,Vector3 outward,Transform fixture,float rank)
    {
        if(catalog==null || family<0 || family>30)return;
        int i=family*6+variant;
        if(catalog[i].fixture && catalog[i].rank<=rank)return;
        catalog[i]=new CatalogPose{fixture=fixture,foot=fixture.InverseTransformPoint(foot),
            outward=fixture.InverseTransformDirection(outward),rank=rank};
    }

    public static void BuildWithCatalogViews()
    {
        catalog=new CatalogPose[186];
        try {
            Build();
            Directory.CreateDirectory("Builds/MarketReview");
            using(var file=new StreamWriter("Builds/MarketReview/catalog-views.tsv"))
                for(int i=0;i<catalog.Length;i++){
                    var item=catalog[i];
                    if(!item.fixture)throw new InvalidOperationException("Missing retail facing "+i);
                    var p=item.fixture.TransformPoint(item.foot);
                    var d=item.fixture.TransformDirection(item.outward);
                    file.WriteLine(FormattableString.Invariant($"{i}\t{p.x:R}\t{p.y:R}\t{p.z:R}\t{d.x:R}\t{d.z:R}"));
                }
        } finally { catalog=null; }
    }
}
