using UnityEngine;

namespace SilentPavilion
{
    public sealed class MidAtmosphere : MonoBehaviour
    {
        public Transform player;
        public Transform creature;
        public Light revealLight;
        public Light lantern;
        public Renderer eyes;
        public float farRevealIntensity=.25f;
        public float nearRevealIntensity=5.5f;
        [Range(0,1)] public float reveal;
        MaterialPropertyBlock block;
        float lanternBase;
        void Awake() { block=new MaterialPropertyBlock(); if(lantern!=null) lanternBase=lantern.intensity; }
        public static float RevealAtDistance(float distance) { return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,8,distance)); }
        void Update()
        {
            if(player==null || creature==null) return;
            Vector3 offset=player.position-creature.position; offset.y=0;
            float target=RevealAtDistance(offset.magnitude);
            reveal=Mathf.MoveTowards(reveal,target,Time.deltaTime*.19f);
            if(revealLight!=null) revealLight.intensity=Mathf.Lerp(farRevealIntensity,nearRevealIntensity,reveal);
            if(lantern!=null) lantern.intensity=lanternBase*(.9f+.075f*Mathf.Sin(Time.time*2.1f)
                +.025f*Mathf.Sin(Time.time*13.7f));
            if(eyes!=null)
            {
                eyes.GetPropertyBlock(block);
                block.SetColor("_EmissionColor",new Color(.25f,.50f,.43f)*Mathf.Lerp(.1f,1.8f,reveal));
                eyes.SetPropertyBlock(block);
            }
        }
    }
}
