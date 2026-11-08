using UnityEngine;

namespace SilentPavilion
{
    public static class MidCreatureMesh
    {
        // 의자 좌판 y=.72, 골반 y=.87. 무릎은 전방(-Z), 발은 바닥에 고정.
        public static Mesh BuildLowerBody()
        {
            var m = new MidMeshBuilder();
            m.AddEllipsoid(new Vector3(0,.88f,.05f), new Vector3(.24f,.20f,.18f));
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * .18f;
                m.AddTube(new[] { new Vector3(x,.9f,.04f), new Vector3(x*1.2f,.85f,-.33f),
                    new Vector3(x*1.5f,.77f,-.70f), new Vector3(x*1.65f,.39f,-.78f),
                    new Vector3(x*1.8f,.09f,-.91f) }, new[] {.115f,.105f,.077f,.048f,.038f}, 36, 14);
                m.AddEllipsoid(new Vector3(x*1.8f,.085f,-1.02f), new Vector3(.06f,.065f,.21f));
            }
            return m.Build("Mid_Creature_SeatedLowerBody");
        }

        // 팔/목/몸통을 단면 링들로 생성하고 손가락까지 가늘어지는 곡면으로 만든다.
        public static Mesh BuildUpperBody()
        {
            var m = new MidMeshBuilder();
            m.AddTube(new[] {new Vector3(0,.89f,.04f), new Vector3(0,1.12f,.10f),
                new Vector3(.025f,1.41f,.06f), new Vector3(0,1.62f,-.08f)},
                new[] {.16f,.135f,.23f,.18f}, 30, 22, .65f, .065f);
            m.AddTube(new[] {new Vector3(0,1.57f,-.08f), new Vector3(.045f,1.77f,-.20f),
                new Vector3(.07f,1.96f,-.26f)}, new[] {.085f,.065f,.072f}, 20, 14, 1, .07f);
            m.AddEllipsoid(new Vector3(.065f,2.095f,-.285f), new Vector3(.15f,.245f,.145f), 22, 28, .11f);
            for (int side = -1; side <= 1; side += 2)
            {
                m.AddTube(new[] {new Vector3(0,1.52f,-.01f), new Vector3(side*.28f,1.56f,-.03f),
                    new Vector3(side*.41f,1.35f,-.18f), new Vector3(side*.47f,1.04f,-.44f),
                    new Vector3(side*.49f,.72f,-.58f), new Vector3(side*.48f,.40f,-.64f)},
                    new[] {.13f,.105f,.068f,.061f,.04f,.026f}, 46, 14, .85f, .05f);
                m.AddEllipsoid(new Vector3(side*.48f,.36f,-.65f), new Vector3(.048f,.095f,.032f));
                for (int finger = 0; finger < 5; finger++)
                {
                    float x = side*.48f + (finger-2)*.018f;
                    float length = .15f + .035f * Mathf.Sin(finger*.8f);
                    m.AddTube(new[] {new Vector3(x,.32f,-.655f), new Vector3(x+side*.018f,.23f,-.68f),
                        new Vector3(x+side*.025f,.32f-length,-.72f)},
                        new[] {.012f,.009f,.003f}, 12, 7, .8f);
                }
                // 돌출된 갈비뼈: 공포 실루엣의 세부 형태. 몸통과 같은 메시로 합친다.
                for (int rib=0; rib<5; rib++)
                {
                    float y=1.19f+rib*.065f, width=.12f+rib*.014f;
                    m.AddTube(new[] {new Vector3(side*.025f,y,-.055f),new Vector3(side*width,y+.025f,-.09f),
                        new Vector3(side*(width+.025f),y+.01f,.015f)}, new[] {.016f,.018f,.009f}, 12, 7);
                }
            }
            return m.Build("Mid_Creature_LoopedOrganicUpperBody");
        }

        public static Mesh BuildEyes()
        {
            var m = new MidMeshBuilder();
            m.AddEllipsoid(new Vector3(.005f,2.125f,-.407f), new Vector3(.016f,.009f,.009f), 8, 12);
            m.AddEllipsoid(new Vector3(.112f,2.12f,-.405f), new Vector3(.014f,.007f,.009f), 8, 12);
            return m.Build("Mid_Creature_Eyes");
        }
    }
}
