using UnityEngine;

namespace SilentPavilion
{
    public static class MidTwistedTree
    {
        // 선택 A: 착석 인체와 다른 분기형 기하. 지역 난수로 생성해 Unity 전역 난수에 영향 없음.
        public static Mesh Build(int seed, float height)
        {
            var rng = new System.Random(seed);
            var m = new MidMeshBuilder();
            float bend = (float)rng.NextDouble() * .9f - .45f;
            m.AddTube(new[] {Vector3.zero, new Vector3(.12f,height*.26f,0),
                new Vector3(bend,height*.55f,.18f),new Vector3(bend-.3f,height*.8f,.2f),
                new Vector3(bend-.1f,height,0)}, new[] {.37f,.24f,.17f,.11f,.012f}, 34, 11, 1, .15f);
            for (int branch=0; branch<9; branch++)
            {
                float a=(float)rng.NextDouble()*Mathf.PI*2, y=height*(.35f+branch*.055f);
                Vector3 start=new Vector3(bend*y/height,y,.1f);
                Vector3 dir=new Vector3(Mathf.Cos(a),.65f,Mathf.Sin(a));
                float length=height*(.19f+(float)rng.NextDouble()*.15f);
                Vector3 elbow=start+dir*length*.55f, tip=start+dir*length+Vector3.up*.35f;
                m.AddTube(new[] {start,elbow,tip},new[] {.095f,.055f,.006f},18,8,1,.13f);
                m.AddTube(new[] {elbow,elbow+new Vector3(-dir.z,.4f,dir.x)*.55f,
                    tip+new Vector3(-dir.z,.8f,dir.x)*.65f},new[] {.04f,.023f,.003f},12,7);
            }
            for(int root=0;root<5;root++)
            {
                float a=root*2*Mathf.PI/5;
                Vector3 end=new Vector3(Mathf.Cos(a),.015f,Mathf.Sin(a))*.9f;
                m.AddTube(new[] {new Vector3(0,.25f,0),end*.5f+Vector3.up*.05f,end},new[] {.16f,.07f,.008f},12,8);
            }
            return m.Build("Mid_TwistedTree_"+seed);
        }
    }
}
