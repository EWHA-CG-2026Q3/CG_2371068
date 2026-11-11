using UnityEngine;

namespace SilentPavilion
{
    public sealed class MidCreatureBreathing : MonoBehaviour
    {
        public MeshFilter upperBody;
        public Transform eyes;
        [Range(.05f,.5f)] public float breathsPerSecond=.22f;
        [Range(0,.08f)] public float expansion=.028f;
        public float angleDegrees=3.2f;
        public float swayDegrees=.9f;
        public Vector3 pivot=new Vector3(0,.88f,.04f);
        Mesh animatedMesh,sourceMesh,eyeMesh,sourceEyeMesh;
        MeshFilter eyeFilter;
        Vector3[] original,deformed,originalEyes,deformedEyes;
        void OnEnable()
        {
            if(upperBody==null || upperBody.sharedMesh==null) return;
            sourceMesh=upperBody.sharedMesh;animatedMesh=Instantiate(sourceMesh);
            animatedMesh.name="Mid_Breathing_Runtime";animatedMesh.MarkDynamic();upperBody.sharedMesh=animatedMesh;
            original=sourceMesh.vertices;deformed=new Vector3[original.Length];
            eyeFilter=eyes!=null?eyes.GetComponent<MeshFilter>():null;
            if(eyeFilter!=null)
            {
                sourceEyeMesh=eyeFilter.sharedMesh;eyeMesh=Instantiate(sourceEyeMesh);eyeFilter.sharedMesh=eyeMesh;
                originalEyes=sourceEyeMesh.vertices;deformedEyes=new Vector3[originalEyes.Length];
            }
        }
        // 필수 ④: T(pivot) Rz(sway) Rx(breath) S T(-pivot). 모든 행렬 곱은 직접 구현.
        public float[,] MatrixAt(float time)
        {
            float phase=time*2*Mathf.PI*breathsPerSecond,wave=Mathf.Sin(phase);
            var rotation=MidMatrixMath.Multiply(MidMatrixMath.RotationZ(Mathf.Sin(phase*.57f)*swayDegrees*Mathf.Deg2Rad),
                MidMatrixMath.RotationX(wave*angleDegrees*Mathf.Deg2Rad));
            return MidMatrixMath.Multiply(MidMatrixMath.Translation(pivot),MidMatrixMath.Multiply(rotation,
                MidMatrixMath.Multiply(MidMatrixMath.Scale(new Vector3(1+wave*expansion,1+wave*expansion*.25f,1+wave*expansion)),
                MidMatrixMath.Translation(-pivot))));
        }
        public void ApplyAt(float time)
        {
            if(animatedMesh==null) return;
            float[,] matrix=MatrixAt(time);
            for(int i=0;i<original.Length;i++) deformed[i]=MidMatrixMath.TransformPoint(matrix,original[i]);
            animatedMesh.vertices=deformed;animatedMesh.RecalculateNormals();animatedMesh.RecalculateBounds();
            if(eyeMesh!=null)
            {
                for(int i=0;i<originalEyes.Length;i++) deformedEyes[i]=MidMatrixMath.TransformPoint(matrix,originalEyes[i]);
                eyeMesh.vertices=deformedEyes;eyeMesh.RecalculateBounds();
            }
        }
        void Update() {ApplyAt(Time.time);}
        void OnDisable()
        {
            // Scene Reload가 꺼져 있어도 다음 Play가 원본 정점에서 시작하도록 복원한다.
            if(upperBody!=null && upperBody.sharedMesh==animatedMesh) upperBody.sharedMesh=sourceMesh;
            if(eyeFilter!=null && eyeFilter.sharedMesh==eyeMesh) eyeFilter.sharedMesh=sourceEyeMesh;
            Release(animatedMesh);Release(eyeMesh);animatedMesh=null;eyeMesh=null;
        }
        static void Release(Object o) {if(o==null)return;if(Application.isPlaying) Destroy(o);else DestroyImmediate(o);}
    }
}
