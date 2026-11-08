using UnityEngine;
using UnityEngine.InputSystem;

namespace SilentPavilion
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MidFirstPersonController : MonoBehaviour
    {
        public Transform view;
        public float moveSpeed=1.65f;
        public float sensitivity=.085f;
        public float gravity=-18;
        CharacterController controller;
        float pitch,verticalSpeed,walkPhase;
        Vector3 eyeRest;
        void Awake() { controller=GetComponent<CharacterController>(); if(view!=null) eyeRest=view.localPosition; }
        void Update()
        {
            var keyboard=Keyboard.current; var mouse=Mouse.current;
            if(keyboard==null) return;
            if(mouse!=null && mouse.leftButton.wasPressedThisFrame) SetCursor(true);
            if(keyboard.escapeKey.wasPressedThisFrame) SetCursor(false);
            if(Cursor.lockState==CursorLockMode.Locked && mouse!=null)
            {
                Vector2 delta=mouse.delta.ReadValue()*sensitivity;
                transform.Rotate(0,delta.x,0); pitch=Mathf.Clamp(pitch-delta.y,-75,75);
                if(view!=null) view.localRotation=Quaternion.Euler(pitch,0,0);
            }
            Vector2 input=Vector2.zero;
            // Esc로 UI를 다룰 때 이동도 멈춘다. 마우스 장치 부재 시에도 키보드 입력은 독립적.
            if(Cursor.lockState==CursorLockMode.Locked)
            {
                if(keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y++;
                if(keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y--;
                if(keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x++;
                if(keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x--;
            }
            input=Vector2.ClampMagnitude(input,1);
            Vector3 movement=(transform.right*input.x+transform.forward*input.y)*moveSpeed;
            if(controller.isGrounded && verticalSpeed<0) verticalSpeed=-2;
            verticalSpeed+=gravity*Time.deltaTime; movement.y=verticalSpeed;
            controller.Move(movement*Time.deltaTime);
            if(view!=null)
            {
                walkPhase+=input.magnitude*Time.deltaTime*6;
                view.localPosition=eyeRest+Vector3.up*Mathf.Sin(walkPhase)*.008f*input.magnitude;
            }
        }
        static void SetCursor(bool locked) { Cursor.lockState=locked?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!locked; }
        void OnApplicationFocus(bool focus) { if(!focus) SetCursor(false); }
        void OnDisable() { SetCursor(false); }
    }
}
