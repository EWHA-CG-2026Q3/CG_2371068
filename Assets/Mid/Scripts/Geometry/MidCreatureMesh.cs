using UnityEngine;

namespace SilentPavilion
{
    /// <summary>기존 벤치에 앉은 가지 크리처. 갈라진 몸통과 머리를 직접 표면으로 생성.</summary>
    public static class MidCreatureMesh
    {
        public static readonly Vector3 LeftEye=new Vector3(-.047f,2.17f,-.363f);
        public static readonly Vector3 RightEye=new Vector3(.073f,2.167f,-.365f);
        static void Branch(MidMeshBuilder m,Vector3[] p,float[] r,int steps=26,int sides=14,float flat=1)
        {m.AddTube(p,r,steps,sides,flat,.19f,.16f,6);}
        // 골반 최저점 .68 / 발 최저점 .02: 기존 좌판과 바닥에 접촉하도록 보존한다.
        public static Mesh BuildLowerBody()
        {
            var m=new MidMeshBuilder();
            Branch(m,new[] {new Vector3(0,.68f,.05f),new Vector3(0,.87f,.05f),new Vector3(.025f,1.01f,.02f)},
                new[] {.12f,.20f,.10f},22,18,.82f);
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*.18f;
                Branch(m,new[] {new Vector3(x,.9f,.04f),new Vector3(x*1.25f,.86f,-.32f),new Vector3(x*1.55f,.76f,-.69f),
                    new Vector3(x*1.70f,.40f,-.79f),new Vector3(x*1.82f,.09f,-.93f)},new[] {.093f,.077f,.065f,.038f,.027f},38);
                Branch(m,new[] {new Vector3(x-.035f,.93f,.06f),new Vector3(x*1.25f+.045f,.87f,-.31f),
                    new Vector3(x*1.55f-.025f,.80f,-.67f),new Vector3(x*1.70f+.03f,.43f,-.77f),new Vector3(x*1.82f,.10f,-.94f)},
                    new[] {.035f,.028f,.03f,.023f,.012f},32,9);
                for(int toe=0;toe<3;toe++)
                {
                    float tx=x*1.82f+(toe-1)*.032f;
                    Branch(m,new[] {new Vector3(x*1.82f,.10f,-.93f),new Vector3(tx,.033f,-1.03f),
                        new Vector3(tx+(toe-1)*.025f,.035f,-1.17f-.025f*toe)},new[] {.025f,.019f,.004f},14,9);
                }
                Branch(m,new[] {new Vector3(x*1.55f,.78f,-.68f),new Vector3(x*2.05f,.88f,-.63f),
                    new Vector3(x*2.25f,.99f,-.67f)},new[] {.04f,.022f,.003f},15,9);
            }
            Mesh mesh=m.Build("Mid_Creature_SeatedLowerBody");
            var v=mesh.vertices;for(int i=0;i<v.Length;i++) v[i].y=Mathf.Max(.02f,v[i].y);
            mesh.vertices=v;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static Mesh BuildUpperBody()
        {
            var m=new MidMeshBuilder();
            Branch(m,new[] {new Vector3(0,.88f,.09f),new Vector3(-.05f,1.14f,.15f),
                new Vector3(.07f,1.40f,.03f),new Vector3(0,1.63f,-.11f)},new[] {.105f,.075f,.09f,.065f},34,18);
            for(int side=-1;side<=1;side+=2)
            {
                // 가운데가 빈 흉곽을 둘러싸는 두 줄기와 작은 분기.
                Branch(m,new[] {new Vector3(side*.07f,.90f,.03f),new Vector3(side*.15f,1.13f,-.04f),
                    new Vector3(side*.20f,1.43f,-.08f),new Vector3(side*.12f,1.59f,-.13f)},new[] {.065f,.055f,.064f,.032f},28,12);
                for(int fork=0;fork<3;fork++)
                {
                    float y=1.17f+fork*.11f;
                    Branch(m,new[] {new Vector3(0,y+.015f,.10f),new Vector3(side*.14f,y,-.015f),
                        new Vector3(side*.19f,y+.075f,-.105f)},new[] {.026f,.024f,.009f},16,9);
                }
                float uneven=side<0?.025f:-.025f;
                Branch(m,new[] {new Vector3(side*.10f,1.57f,-.10f),new Vector3(side*.29f,1.55f+uneven,-.04f),
                    new Vector3(side*.38f,1.29f,-.19f),new Vector3(side*.45f,.97f,-.44f),
                    new Vector3(side*.46f,.63f,-.59f),new Vector3(side*.47f,.37f,-.68f)},
                    new[] {.075f,.078f,.051f,.045f,.029f,.018f},46,14,.85f);
                for(int finger=0;finger<4;finger++)
                {
                    float x=side*.47f+(finger-1.5f)*.022f;
                    Branch(m,new[] {new Vector3(side*.47f,.39f,-.67f),new Vector3(x,.29f,-.70f),
                        new Vector3(x+side*.025f,.20f-(finger%2)*.025f,-.76f),new Vector3(x+side*.04f,.18f,-.81f)},
                        new[] {.016f,.012f,.008f,.002f},18,8);
                }
                for(int twig=0;twig<2;twig++)
                {
                    Vector3 p=new Vector3(side*(.23f+twig*.10f),1.55f-twig*.12f,.015f);
                    Branch(m,new[] {p,p+new Vector3(side*.11f,.16f,.035f),p+new Vector3(side*.16f,.30f,.08f)},
                        new[] {.032f,.017f,.0025f},15,9);
                }
            }
            Branch(m,new[] {new Vector3(0,1.57f,-.10f),new Vector3(.06f,1.78f,-.21f),new Vector3(.015f,1.96f,-.27f)},
                new[] {.068f,.049f,.058f},24,14);
            // 머리: 달걀형 구 대신 두 갈래 가지가 둘러싼 빈 얼굴과 짧은 수관.
            for(int side=-1;side<=1;side+=2)
            {
                Branch(m,new[] {new Vector3(.01f,1.92f,-.27f),new Vector3(side*.10f,2.04f,-.30f),
                    new Vector3(side*.13f+.01f,2.22f,-.28f),new Vector3(.04f+side*.035f,2.37f,-.22f)},
                    new[] {.05f,.069f,.054f,.014f},28,14,.8f);
                Branch(m,new[] {new Vector3(side*.12f,2.20f,-.26f),new Vector3(side*.21f,2.34f,-.19f),
                    new Vector3(side*.25f,2.46f,-.17f)},new[] {.033f,.020f,.002f},16,9);
                Branch(m,new[] {new Vector3(side*.13f,2.30f,-.23f),new Vector3(side*.23f,2.38f,-.26f),
                    new Vector3(side*.29f,2.40f,-.32f)},new[] {.025f,.012f,.002f},14,9);
            }
            Branch(m,new[] {new Vector3(-.10f,2.21f,-.335f),new Vector3(.01f,2.22f,-.36f),new Vector3(.13f,2.19f,-.33f)},
                new[] {.027f,.03f,.023f},18,10);
            return m.Build("Mid_Creature_LoopedOrganicUpperBody");
        }
        public static Mesh BuildEyes()
        {
            var m=new MidMeshBuilder();
            m.AddEllipsoid(LeftEye,new Vector3(.012f,.006f,.008f),8,12);
            m.AddEllipsoid(RightEye,new Vector3(.010f,.006f,.008f),8,12);
            return m.Build("Mid_Creature_Eyes");
        }
    }
}
