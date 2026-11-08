using UnityEngine;

namespace SilentPavilion
{
    public sealed class MidCreatureBreathing : MonoBehaviour
    {
        public MeshFilter upperBody;
        public Transform eyes;
        [Range(.05f, .5f)] public float breathsPerSecond=.17f;
        [Range(0, .04f)] public float expansion=.012f;
        public float angleDegrees=.65f;
        public Vector3 pivot=new Vector3(0,.88f,.04f);
        Mesh animatedMesh;
        Vector3[] original, deformed;
        Vector3 eyesPosition;

        void OnEnable()
        {
            if(upperBody==null || upperBody.sharedMesh==null) return;
            // 저장된 메시 에셋을 변형하지 않는다. Play 종료 시 복제본만 폐기.
            animatedMesh=Instantiate(upperBody.sharedMesh); animatedMesh.name="Mid_Breathing_Runtime";
            upperBody.sharedMesh=animatedMesh; animatedMesh.MarkDynamic();
            original=animatedMesh.vertices; deformed=new Vector3[original.Length];
            if(eyes!=null) eyesPosition=eyes.localPosition;
        }
        // 필수 ④: M=T(pivot) R_x(angle) S(scale) T(-pivot).
        // 골반 기준으로 미세하게 팽창/회전하며 하체는 움직이지 않는다.
        public float[,] MatrixAt(float time)
        {
            float wave=Mathf.Sin(time*2*Mathf.PI*breathsPerSecond);
            return MidMatrixMath.Multiply(MidMatrixMath.Translation(pivot),
                MidMatrixMath.Multiply(MidMatrixMath.RotationX(wave*angleDegrees*Mathf.Deg2Rad),
                MidMatrixMath.Multiply(MidMatrixMath.Scale(new Vector3(1+wave*expansion,1+wave*expansion*.3f,1+wave*expansion)),
                MidMatrixMath.Translation(-pivot))));
        }
        void Update()
        {
            if(animatedMesh==null) return;
            float[,] matrix=MatrixAt(Time.time);
            for(int i=0;i<original.Length;i++) deformed[i]=MidMatrixMath.TransformPoint(matrix,original[i]);
            animatedMesh.vertices=deformed; animatedMesh.RecalculateNormals(); animatedMesh.RecalculateBounds();
            if(eyes!=null) eyes.localPosition=MidMatrixMath.TransformPoint(matrix,new Vector3(.06f,2.12f,-.405f))
                -new Vector3(.06f,2.12f,-.405f)+eyesPosition;
        }
        void OnDestroy() { if(animatedMesh!=null) Destroy(animatedMesh); }
    }
}
