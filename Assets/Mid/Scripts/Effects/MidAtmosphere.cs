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
            if(revealLight!=null) revealLight.intensity=Mathf.Lerp(.12f,2.6f,reveal);
            if(lantern!=null) lantern.intensity=lanternBase*(.9f+.075f*Mathf.Sin(Time.time*2.1f)
                +.025f*Mathf.Sin(Time.time*13.7f));
            if(eyes!=null)
            {
                eyes.GetPropertyBlock(block);
                block.SetColor("_EmissionColor",new Color(.25f,.50f,.43f)*Mathf.Lerp(.1f,1.8f,reveal));
                eyes.SetPropertyBlock(block);
            }
        }
        void OnGUI()
        {
            if(!Application.isPlaying) return;
            float scale=Mathf.Clamp(Screen.height/900f,.65f,1.5f);
            GUI.color=new Color(.74f,.8f,.8f,.7f);
            var label=new GUIStyle(GUI.skin.label) {fontSize=Mathf.RoundToInt(12*scale)};
            GUI.Label(new Rect(28,Screen.height-44*scale,Screen.width-40,30*scale),
                Cursor.lockState==CursorLockMode.Locked?"WASD  /  WALK       MOUSE  /  LOOK       ESC  /  RELEASE":"CLICK TO ENTER       WASD  /  WALK       MOUSE  /  LOOK",label);
            if(Time.timeSinceLevelLoad<8)
            {
                GUI.color=new Color(.78f,.84f,.82f,Mathf.Clamp01(1-(Time.timeSinceLevelLoad-4)/4));
                var title=new GUIStyle(GUI.skin.label) {fontSize=Mathf.RoundToInt(26*scale),alignment=TextAnchor.MiddleCenter};
                GUI.Label(new Rect(0,Screen.height*.15f,Screen.width,50*scale),"THE SILENT PAVILION",title);
            }
        }
    }
}
