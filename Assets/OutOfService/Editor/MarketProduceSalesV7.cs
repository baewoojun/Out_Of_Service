using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    static Material produceFilm;

    // Drop onto the sloping deck or the upper envelope of fruit already placed.
    // Best-of-random sampling fills pockets first and leaves the pile shoulder irregular.
    static Vector4[] SettledFruit(int count,float halfX,float halfZ,float pitch,float slope,int seed)
    {
        var rng=new System.Random(seed);
        var pile=new Vector4[count];
        for(int i=0;i<count;i++)
        {
            float radius=pitch*(.90f+.19f*(float)rng.NextDouble());
            float best=float.PositiveInfinity;
            for(int trial=0;trial<20;trial++)
            {
                float x=(2*(float)rng.NextDouble()-1)*(halfX-radius);
                float z=(2*(float)rng.NextDouble()-1)*(halfZ-radius);
                float y=z*slope+radius;
                for(int j=0;j<i;j++)
                {
                    var other=pile[j];
                    float dx=x-other.x,dz=z-other.z,r=radius+other.w;
                    float overlap=r*r-dx*dx-dz*dz;
                    if(overlap>0) y=Mathf.Max(y,other.y+Mathf.Sqrt(overlap));
                }
                float score=y-z*slope+.19f*(x*x/(halfX*halfX)+z*z/(halfZ*halfZ));
                if(score>=best) continue;
                best=score;pile[i]=new Vector4(x,y,z,radius);
            }
        }
        return pile;
    }
    static void StockProduceCrate(int kind,string title,Vector3 center,int quadrant,Transform stand)
    {
        // Pears ship in divided cartons. Apples and mandarins share their island between
        // loose fruit at the aisle and takeaway boxes at the back.
        bool boxed=kind==1 || kind==6 || ((kind==0 || kind==2) && quadrant>=2);
        if(boxed)
        {
            int count=kind==6?4:3,rows=kind==6?6:3;
            for(int row=0;row<rows;row++)
            for(int col=0;col<count;col++){
                uint stock=StockSeed(center,kind,row*17+col);
                if(col>0 && col<count-1 && stock%17==0)continue;
                Vector3 foot=center+new Vector3((col-(count-1)*.5f)*(kind==6?.43f:.50f),0,(row-(rows-1)*.5f)*(kind==6?.296f:.48f));
                var pack=new GameObject("slightly handled produce carton").transform;pack.SetParent(stand);pack.position=foot;
                ProduceSalePack(kind,title,foot,pack);
                pack.rotation=Quaternion.Euler(0,((stock&255)/127.5f-1)*(kind==6?1.3f:3.2f),0);
                pack.position+=new Vector3((((stock>>8)&255)/127.5f-1)*.008f,0,(((stock>>16)&255)/127.5f-1)*(kind==6?.005f:.022f));
            }
            return;
        }
        if(kind==5 || kind==7)
        {
            int rows=kind==7?4:3,columns=kind==7?8:9;
            for(int row=0;row<rows;row++)
            for(int col=0;col<columns;col++)
            {
                uint stock=StockSeed(center,kind,row*19+col);
                // Partly shopped bays, not random litter. Keep the boundary stock
                // within its original crate and preserve the clear picking edge.
                if(col>0 && col<columns-1 && stock%23==0)continue;
                Vector3 p=center+new Vector3((col-(columns-1)*.5f)*(kind==7?.21f:.185f),.019f,(row-(rows-1)*.5f)*(kind==7?.42f:.56f));
                p+=new Vector3((((stock>>8)&255)/127.5f-1)*(kind==7?.007f:.004f),0,(((stock>>16)&255)/127.5f-1)*(kind==7?.018f:.032f));
                if(kind==5){HarvestScallions(p,(int)(stock%60013),stand);continue;}
                var pack=new GameObject("handled cucumber sleeve").transform;pack.SetParent(stand);pack.position=p;
                int seed=(int)(stock%100003);
                for(int n=0;n<3;n++)
                {
                    float length=.268f+.025f*ProduceNoise(seed,n);
                    var cucumber=HandmadeProp("three cucumber retail bundle",OrganicProduceMesh(7,(seed+n)%5),
                        p+new Vector3((n-1)*.048f,.023f,(ProduceNoise(seed+n,row)-.5f)*.019f),new Vector3(.043f,length,.043f),HarvestSkin(7),pack);
                    cucumber.transform.rotation=Quaternion.Euler(90,(ProduceNoise(seed,n)-.5f)*5,(n-1)*1.8f);
                    var tip=cucumber.transform.position+cucumber.transform.up*length*.5f;
                    RailSegment("short cut cucumber stem",tip,tip+cucumber.transform.up*.008f,.0032f,green,pack);
                }
                HandmadeProp("clear sealed cucumber sleeve",ProduceShell(true,true),p,new Vector3(.156f,.052f,.335f),ChilledWrap(),pack);
                Decor("small cucumber sleeve information panel",PrimitiveType.Cube,p+new Vector3(.049f,.054f,-.098f),new Vector3(.045f,.001f,.055f),priceWhite,pack);
                var previousRoot=root;root=pack;
                Label("다다기오이\n3입 · 국내산",p+new Vector3(.049f,.055f,-.098f),new Vector3(90,0,0),.008f);
                root.GetChild(root.childCount-1).GetComponent<TextMesh>().color=new Color(.12f,.15f,.11f);root=previousRoot;
                pack.rotation=Quaternion.Euler(0,((stock&255)/127.5f-1)*4.5f,0);
            }
            return;
        }
        // Shallow liner keeps stock within reach; stagger the fruit, with a second course at the rear.
        float rise=kind==0 || kind==2 ? .33f : .13f;
        var liner=Decor("inclined fruit deck",PrimitiveType.Cube,center+new Vector3(0,rise*.5f,0),new Vector3(1.76f,.035f,1.78f),dark,stand);
        liner.transform.rotation=Quaternion.Euler(-Mathf.Atan(rise/1.78f)*Mathf.Rad2Deg,0,0);
        if(kind==4){
            float slope=rise/1.78f;var pile=SettledFruit(170,.83f,.83f,.066f,slope,4407+quadrant);
            for(int i=0;i<pile.Length;i++){
                var f=pile[i];
                var p=center+new Vector3(f.x,.018f+rise*.5f+(f.y-f.z*slope)*.65f+f.z*slope,f.z);
                var potato=HandmadeProp("settled loose potato",HarvestBody(4,i%5),p,new Vector3(f.w*2,f.w*1.3f,f.w*1.6f),HarvestSkin(4),stand);
                potato.transform.rotation=Quaternion.Euler(i*31%19-9,i*137.5f,i*17%17-8);
            }
            return;
        }
        if(kind==0)
        {
            var pile=SettledFruit(175,.83f,.83f,.071f,rise/1.78f,1021+quadrant);
            for(int i=0;i<pile.Length;i++)
            {
                var f=pile[i];
                Vector3 p=center+new Vector3(f.x,.018f+rise*.5f+f.y,f.z);
                PrepareOrchardApple();
                var apple=HandmadeProp("loose orchard apple",orchardApple,p,new Vector3(1.99f,1.93f,2.02f)*f.w,orchardSkin,stand);
                apple.transform.rotation=Quaternion.Euler(i*79%94-47,i*137,i*37%101-50);
                Vector3 crown=p+apple.transform.up*f.w*.81f;
                RailSegment("apple stem",crown,crown+apple.transform.up*.016f+apple.transform.right*.005f,.0028f,cream,stand);
            }
            return;
        }
        if(kind==2){
            // Similar stock count to the old staggered grid, with actual support
            // heights. Flatten the packing vertically along with the fruit body.
            float slope=rise/1.78f;
            var pile=SettledFruit(260,.83f,.83f,.049f,slope,5203+quadrant);
            for(int i=0;i<pile.Length;i++){
                var f=pile[i];
                var p=center+new Vector3(f.x,.018f+rise*.5f+f.z*slope+(f.y-f.z*slope)*.81f,f.z);
                Mandarin(p,f.w,i,stand);
            }
            return;
        }
        int lanes=kind==8?8:12;
        float pitch=1.55f/lanes;
        for(int row=0;row<11;row++)
        for(int col=0;col<lanes;col++)
        {
            int seed=quadrant*101+row*9+col;
            float h=kind==8?.085f:kind==2?.039f:kind==4?.044f:.065f;
            Vector3 p=center+new Vector3((col-(lanes-1)*.5f)*pitch+(row%2)*pitch*.35f,
                h+.018f+rise*(.5f+(row-5)*.15f/1.78f),(row-5)*.15f);
            p.x+=(ProduceNoise(seed,3)-.5f)*.038f;
            p.z+=(ProduceNoise(seed,5)-.5f)*.045f;
            ProducePiece(kind,title,p,stand);
            if(row>=3 && col<lanes-1 && seed%3!=0)
                ProducePiece(kind,title,p+new Vector3(pitch*.45f,h*1.32f,-.064f),stand);
        }
    }

    static void ProduceSalePack(int kind,string title,Vector3 basePoint,Transform stand)
    {
        if(kind==6){HarvestTomatoPunnet(basePoint,stand);return;}
        bool sealedTray=kind==6;
        Material board=sealedTray ? dark : cream;
        float w=kind==2 || sealedTray ? .365f : .44f;
        float d=kind==2 ? .33f : sealedTray ? .255f : .35f;
        Decor("retail tray base",PrimitiveType.Cube,basePoint+Vector3.up*.009f,new Vector3(w,.018f,d),board,stand);
        foreach(int side in new[]{-1,1})
        {
            Decor("short folded carton flap",PrimitiveType.Cube,basePoint+new Vector3(0,.044f,side*d*.5f),
                new Vector3(w,.083f,.012f),board,stand);
            Decor("long folded carton flap",PrimitiveType.Cube,basePoint+new Vector3(side*w*.5f,.054f,0),
                new Vector3(.012f,.10f,d),board,stand);
        }
        int cols=kind==1?2:3;
        int rows=kind==2?3:2;
        if(kind==1)
        {
            Decor("pear carton cross divider",PrimitiveType.Cube,basePoint+Vector3.up*.046f,new Vector3(.009f,.085f,d),cream,stand);
            Decor("pear carton cross divider",PrimitiveType.Cube,basePoint+Vector3.up*.046f,new Vector3(w,.085f,.009f),cream,stand);
        }
        float radius=kind==2?.04f:kind==6?.046f:kind==1?.075f:.062f;
        for(int row=0;row<rows;row++)
        for(int col=0;col<cols;col++)
            ProducePiece(kind,title,basePoint+new Vector3(((col+.5f)/cols-.5f)*w,.018f+radius,((row+.5f)/rows-.5f)*d),stand);
        if(sealedTray)
        {
            Decor("clear tomato punnet cover",PrimitiveType.Cube,basePoint+Vector3.up*.077f,new Vector3(w+.008f,.118f,d+.008f),produceFilm,stand);
            Decor("tomato punnet closure strip",PrimitiveType.Cube,basePoint+new Vector3(.10f,.138f,0),new Vector3(.028f,.003f,d),producePalette[6],stand);
        }
    }
}
