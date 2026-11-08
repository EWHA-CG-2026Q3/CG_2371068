using UnityEngine;

namespace SilentPavilion
{
    /// <summary>S09의 원리 확장: Unity TRS/MultiplyPoint를 호출하지 않는 직접 계산.</summary>
    public static class MidMatrixMath
    {
        public static float[,] Identity() { var m=new float[4,4]; for(int i=0;i<4;i++) m[i,i]=1; return m; }
        public static float[,] Translation(Vector3 t)
        { var m=Identity(); m[0,3]=t.x; m[1,3]=t.y; m[2,3]=t.z; return m; }
        public static float[,] Scale(Vector3 s)
        { var m=Identity(); m[0,0]=s.x; m[1,1]=s.y; m[2,2]=s.z; return m; }
        public static float[,] RotationX(float radians)
        {
            var m=Identity(); float c=Mathf.Cos(radians),s=Mathf.Sin(radians);
            m[1,1]=c;m[1,2]=-s;m[2,1]=s;m[2,2]=c;return m;
        }
        public static float[,] Multiply(float[,] a,float[,] b)
        {
            var c=new float[4,4];
            // 행·열 내적. 순서를 바꾸면 일반적으로 결과도 달라진다.
            for(int row=0;row<4;row++) for(int col=0;col<4;col++)
                for(int k=0;k<4;k++) c[row,col]+=a[row,k]*b[k,col];
            return c;
        }
        public static Vector3 TransformPoint(float[,] m, Vector3 p)
        {
            // 동차좌표 w=1이므로 네 번째 열의 이동 항이 더해진다. affine 변환에서 결과 w=1.
            return new Vector3(m[0,0]*p.x+m[0,1]*p.y+m[0,2]*p.z+m[0,3],
                m[1,0]*p.x+m[1,1]*p.y+m[1,2]*p.z+m[1,3],
                m[2,0]*p.x+m[2,1]*p.y+m[2,2]*p.z+m[2,3]);
        }
        public static float[,] RotationZ(float radians)
        {
            var m=Identity();float c=Mathf.Cos(radians),s=Mathf.Sin(radians);
            m[0,0]=c;m[0,1]=-s;m[1,0]=s;m[1,1]=c;return m;
        }
    }
}
