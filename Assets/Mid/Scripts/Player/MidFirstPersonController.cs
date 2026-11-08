using UnityEngine;
using UnityEngine.InputSystem;

namespace SilentPavilion
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MidFirstPersonController : MonoBehaviour
    {
        public Transform view;
        public float moveSpeed=1.55f,sensitivity=.085f,gravity=-18;
        public float acceleration=10,deceleration=20,strideLength=1.45f;
        public float headBobHeight=.027f,headBobSide=.013f;
        public bool footstepAudio=true;
        public int StepCount {get;private set;}
        public float CameraBobAmount {get;private set;}
        CharacterController controller;
        float pitch,verticalSpeed,walkPhase,bobWeight;
        Vector3 eyeRest,planarVelocity,smoothedBob;
        AudioSource footSource;
        AudioClip[] footsteps;
        void Awake()
        {
            controller=GetComponent<CharacterController>();if(view!=null) eyeRest=view.localPosition;
            footSource=GetComponent<AudioSource>();if(footSource==null) footSource=gameObject.AddComponent<AudioSource>();
            footSource.playOnAwake=false;footSource.spatialBlend=0;footSource.volume=.18f;
            footsteps=new[] {MakeFootstep(73),MakeFootstep(134)};
        }
        void Update()
        {
            var keyboard=Keyboard.current;var mouse=Mouse.current;
            if(mouse!=null && mouse.leftButton.wasPressedThisFrame) SetCursor(true);
            if(keyboard!=null && keyboard.escapeKey.wasPressedThisFrame) SetCursor(false);
            if(Cursor.lockState==CursorLockMode.Locked && mouse!=null)
            {
                Vector2 delta=mouse.delta.ReadValue()*sensitivity;transform.Rotate(0,delta.x,0);
                pitch=Mathf.Clamp(pitch-delta.y,-75,75);
            }
            Vector2 input=Vector2.zero;
            if(keyboard!=null && Cursor.lockState==CursorLockMode.Locked)
            {
                if(keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y++;
                if(keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y--;
                if(keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x++;
                if(keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x--;
            }
            SimulateMovement(input,Mathf.Min(Time.deltaTime,.06f));
        }
        public void SimulateMovement(Vector2 input,float dt)
        {
            if(controller==null || dt<=0) return;
            input=Vector2.ClampMagnitude(input,1);
            Vector3 wish=(transform.right*input.x+transform.forward*input.y)*moveSpeed;
            planarVelocity=Vector3.MoveTowards(planarVelocity,wish,(input.sqrMagnitude>0?acceleration:deceleration)*dt);
            if(input.sqrMagnitude==0 && planarVelocity.sqrMagnitude<.0004f) planarVelocity=Vector3.zero;
            if(controller.isGrounded && verticalSpeed<0) verticalSpeed=-2;
            verticalSpeed+=gravity*dt;
            Vector3 before=transform.position;
            float gait=1+.055f*Mathf.Cos(walkPhase*2);
            controller.Move((planarVelocity*gait+Vector3.up*verticalSpeed)*dt);
            Vector3 traveled=transform.position-before;traveled.y=0;
            bool walking=controller.isGrounded && traveled.magnitude/dt>.1f;
            int previousHalf=Mathf.FloorToInt(walkPhase/Mathf.PI);
            // 실제 이동 거리로 발걸음 위상을 누적. 벽에 막히면 제자리 보행/발소리가 나지 않는다.
            if(walking) walkPhase+=traveled.magnitude/strideLength*2*Mathf.PI;
            if(walking && Mathf.FloorToInt(walkPhase/Mathf.PI)!=previousHalf)
            {
                StepCount++;
                if(footstepAudio)
                {
                    footSource.pitch=transform.position.y>.4f?1.05f:.88f;
                    footSource.PlayOneShot(footsteps[StepCount%2],.65f);
                }
            }
            bobWeight=Mathf.MoveTowards(bobWeight,walking?Mathf.Clamp01(planarVelocity.magnitude/moveSpeed):0,dt*7);
            Vector3 bob=new Vector3(Mathf.Sin(walkPhase)*headBobSide,-(1-Mathf.Cos(walkPhase*2))*.5f*headBobHeight,0)*bobWeight;
            smoothedBob=Vector3.Lerp(smoothedBob,bob,1-Mathf.Exp(-dt*18));CameraBobAmount=smoothedBob.magnitude;
            if(view!=null)
            {
                view.localPosition=eyeRest+smoothedBob;
                view.localRotation=Quaternion.Euler(pitch+Mathf.Sin(walkPhase*2)*.28f*bobWeight,0,Mathf.Sin(walkPhase)*.45f*bobWeight);
            }
        }
        static AudioClip MakeFootstep(int seed)
        {
            const int rate=22050,count=3307;var samples=new float[count];var rng=new System.Random(seed);float low=0;
            for(int i=0;i<count;i++)
            {
                float t=(float)i/rate,noise=(float)rng.NextDouble()*2-1;low=Mathf.Lerp(low,noise,.16f);
                float envelope=Mathf.Min(1,t*250)*Mathf.Exp(-t*32);
                samples[i]=(low*.65f+Mathf.Sin(2*Mathf.PI*(72-t*150)*t)*.18f)*envelope;
            }
            var clip=AudioClip.Create("Mid_Footstep_"+seed,count,1,rate,false);clip.SetData(samples,0);return clip;
        }
        static void SetCursor(bool locked) {Cursor.lockState=locked?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!locked;}
        void OnApplicationFocus(bool focus) {if(!focus)SetCursor(false);}
        void OnDisable()
        {
            SetCursor(false);planarVelocity=Vector3.zero;
            if(view!=null){view.localPosition=eyeRest;view.localRotation=Quaternion.Euler(pitch,0,0);}
        }
        void OnDestroy() {if(footsteps!=null) foreach(var clip in footsteps) if(clip!=null) Destroy(clip);}
    }
}
