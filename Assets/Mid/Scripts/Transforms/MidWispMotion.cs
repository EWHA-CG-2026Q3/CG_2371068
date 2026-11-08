using UnityEngine;

namespace SilentPavilion
{
    public sealed class MidWispMotion : MonoBehaviour
    {
        public Vector3 orbitCenter;
        public float radius=.65f;
        public float angularSpeed=.18f;
        public float phase;
        // 선택 B: 인체 변형과 독립적인 계층(local 좌표) 운동. 부모 아래 타원 궤도를 직접 계산.
        public Vector3 PositionAt(float time)
        {
            float theta=time*angularSpeed+phase;
            return orbitCenter+new Vector3(Mathf.Cos(theta)*radius,
                Mathf.Sin(theta*1.7f)*.18f,Mathf.Sin(theta)*radius*.55f);
        }
        void Update() { transform.localPosition=PositionAt(Time.time); }
        void OnDrawGizmosSelected()
        {
            Gizmos.color=new Color(.2f,.7f,.8f,.6f);
            Vector3 previous=PositionAt(0);
            for(int i=1;i<=80;i++)
            {
                Vector3 next=PositionAt(i*2*Mathf.PI/(80*Mathf.Max(.01f,angularSpeed)));
                Gizmos.DrawLine(transform.parent!=null?transform.parent.TransformPoint(previous):previous,
                    transform.parent!=null?transform.parent.TransformPoint(next):next); previous=next;
            }
        }
    }
}
