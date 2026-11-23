using UnityEngine;

public static partial class MarketPrototypeBuilderV7
{
    // Horizontal flow wrap: two end crimps and a longitudinal rear fin.
    // Front/rear lettering has separate positive reading directions.
    static Mesh ChocolateBarFilm(int kind)
    {
        return MallMesh("chocolate_flow_wrap_v1_"+kind,(v,u,t)=>{
            float[] xs={0,.016f,.036f,.055f,.075f,.12f,.24f,.40f,.60f,.76f,.88f,.925f,.945f,.97f,1};
            float[] ds={.025f,.03f,.045f,.06f,.18f,.39f,.47f,.50f,.50f,.47f,.39f,.18f,.06f,.03f,.025f};
            const int rows=8;int columns=xs.Length,plane=columns*(rows+1);
            for(int face=0;face<2;face++)for(int r=0;r<=rows;r++)for(int c=0;c<columns;c++){
                float f=r/(float)rows,x=xs[c],edge=Mathf.Abs(f*2-1);
                bool seal=c<4||c>=columns-4;
                float depth=ds[c]*(1-.83f*Mathf.Pow(edge,6));
                depth+=seal?.008f*(r%2):.015f*Mathf.Sin(x*(17+kind)+f*13)*(1-edge);
                if(face==1&&!seal)depth+=.055f*Mathf.Pow(1-edge,14);
                v.Add(new Vector3(x-.5f,f,(face==0?-1:1)*depth));
                u.Add(new Vector2(face==0?x:1-x,f));
                if(r==0||c==0)continue;int d=v.Count-1,b=d-columns;
                if(face==0)t.AddRange(new[]{b-1,d-1,b,b,d-1,d});
                else t.AddRange(new[]{b-1,b,d-1,b,d,d-1});
            }
            void Join(int a,int b){t.AddRange(new[]{a,b+plane,b,a,a+plane,b+plane});}
            for(int r=0;r<rows;r++){int a=r*columns,b=a+columns;Join(a,b);Join(b+columns-1,a+columns-1);}
            for(int c=0;c<columns-1;c++){Join(c+1,c);Join(plane-columns+c,plane-columns+c+1);}
        });
    }
}
