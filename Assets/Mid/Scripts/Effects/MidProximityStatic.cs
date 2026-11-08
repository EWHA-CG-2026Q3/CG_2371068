using UnityEngine;
using UnityEngine.UI;

namespace SilentPavilion
{
    /// <summary>가까워질 때 0.16초 안팎의 잡음만 나타내는 화면 효과. 텍스트와 입력 차단 없음.</summary>
    public sealed class MidProximityStatic : MonoBehaviour
    {
        public Transform player,creature;
        public RawImage overlay;
        public float startDistance=5.5f,fullDistance=1.2f;
        public float burstDuration=.16f;
        public float maxIntensity=.7f;
        public float CurrentIntensity {get;private set;}
        public int BurstCount {get;private set;}
        Material runtimeMaterial,sourceMaterial;
        float nextBurst,burstStart=-100,burstStrength;
        void OnEnable()
        {
            if(overlay==null || overlay.material==null) return;
            sourceMaterial=overlay.material;runtimeMaterial=Instantiate(sourceMaterial);overlay.material=runtimeMaterial;
            overlay.raycastTarget=false;overlay.enabled=false;nextBurst=Time.time+.4f;
        }
        public float ProximityAt(float distance) {return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(fullDistance,startDistance,distance));}
        public void TriggerBurst(float proximity)
        {
            burstStart=Time.time;burstStrength=Mathf.Lerp(.20f,maxIntensity,Mathf.Clamp01(proximity));BurstCount++;
            if(runtimeMaterial!=null) runtimeMaterial.SetFloat("_Seed",BurstCount*17.13f);
        }
        void Update()
        {
            if(player==null || creature==null || runtimeMaterial==null) return;
            Vector3 delta=player.position-creature.position;delta.y=0;
            float proximity=ProximityAt(delta.magnitude);
            if(proximity<=0) {burstStart=-100;nextBurst=Time.time+.35f;}
            else if(Time.time>=nextBurst)
            {
                TriggerBurst(proximity);
                nextBurst=Time.time+Mathf.Lerp(3.8f,1.65f,proximity);
            }
            float age=(Time.time-burstStart)/burstDuration;
            CurrentIntensity=age>=0 && age<1?Mathf.Sin(age*Mathf.PI)*burstStrength:0;
            runtimeMaterial.SetFloat("_Intensity",CurrentIntensity);overlay.enabled=CurrentIntensity>.001f;
        }
        void OnDisable()
        {
            if(overlay!=null){overlay.enabled=false;if(overlay.material==runtimeMaterial)overlay.material=sourceMaterial;}
            if(runtimeMaterial!=null){if(Application.isPlaying)Destroy(runtimeMaterial);else DestroyImmediate(runtimeMaterial);}
            runtimeMaterial=null;CurrentIntensity=0;
        }
    }
}
