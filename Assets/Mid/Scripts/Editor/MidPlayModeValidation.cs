using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SilentPavilion.Editor
{
    // 실제 Unity Play 모드 검사. SessionState로 도메인 리로드를 넘어 배치 검사 상태를 보관한다.
    [InitializeOnLoad]
    public static class MidPlayModeValidation
    {
        const string Active="Mid.PlayReview.Active", Stage="Mid.PlayReview.Stage";
        static readonly List<string> failures=new List<string>();
        static double started,previousTick;
        static int frames,headIndex;
        static Vector3 originalHead,originalWisp;
        static MidCreatureBreathing breathing;
        static MidWispMotion wisp;
        static MidFirstPersonController walker;
        static MidAtmosphere atmosphere;
        static MidProximityStatic interference;
        static float maxHeadDisplacement,maxBob,maxStatic;
        static int clearNearFrames;

        static MidPlayModeValidation()
        {
            if(!SessionState.GetBool(Active,false)) return;
            RegisterCallbacks();
        }
        static void RegisterCallbacks()
        {
            Application.logMessageReceived-=OnLog;
            Application.logMessageReceived+=OnLog;
            EditorApplication.update-=Tick;
            EditorApplication.update+=Tick;
            started=EditorApplication.timeSinceStartup;
        }
        public static void RunBatch()
        {
            EditorSceneManager.OpenScene(MidSceneBuilder.ScenePath);
            MidSceneValidation.Validate();
            SessionState.SetBool(Active,true);SessionState.SetInt(Stage,0);
            // 이 프로젝트처럼 Domain Reload가 꺼져 있어도 검사 콜백이 실행되게 한다.
            RegisterCallbacks();
            EditorApplication.EnterPlaymode();
        }
        static void OnLog(string message,string stack,LogType type)
        {
            if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)
                failures.Add(message);
        }
        static void Check(bool ok,string message) {if(!ok) failures.Add(message);}
        static void Tick()
        {
            try
            {
                int stage=SessionState.GetInt(Stage,0);
                if(stage==3 && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    MidSceneValidation.CapturePreviews();
                    SessionState.SetBool(Active,false);EditorApplication.Exit(SessionState.GetInt("Mid.PlayReview.Exit",1));return;
                }
                if(!EditorApplication.isPlaying)
                {
                    if(EditorApplication.timeSinceStartup-started>90) throw new TimeoutException("Play mode entry timeout");
                    return;
                }
                if(stage==0)
                {
                    if(Time.timeSinceLevelLoad<.5f) return;
                    breathing=Object.FindFirstObjectByType<MidCreatureBreathing>();
                    walker=Object.FindFirstObjectByType<MidFirstPersonController>();
                    wisp=Object.FindFirstObjectByType<MidWispMotion>();
                    atmosphere=Object.FindFirstObjectByType<MidAtmosphere>();
                    interference=Object.FindFirstObjectByType<MidProximityStatic>();
                    maxHeadDisplacement=0;maxBob=0;maxStatic=0;clearNearFrames=0;
                    var v=breathing.upperBody.sharedMesh.vertices;
                    for(int i=1;i<v.Length;i++) if(v[i].y>v[headIndex].y) headIndex=i;
                    originalHead=v[headIndex];originalWisp=wisp.transform.localPosition;
                    started=EditorApplication.timeSinceStartup;previousTick=started;
                    SessionState.SetInt(Stage,1);return;
                }
                double now=EditorApplication.timeSinceStartup;
                if(now-previousTick<.015) return;
                previousTick=now;frames++;
                maxHeadDisplacement=Mathf.Max(maxHeadDisplacement,Vector3.Distance(originalHead,breathing.upperBody.sharedMesh.vertices[headIndex]));
                maxStatic=Mathf.Max(maxStatic,interference.CurrentIntensity);
                if(stage==1 && now-started>2)
                {
                    Check(maxHeadDisplacement>.03f,"호흡 움직임이 충분히 보이지 않음: "+maxHeadDisplacement);
                    Check(Vector3.Distance(originalWisp,wisp.transform.localPosition)>.001f,"불빛이 움직이지 않음");
                    Check(interference.BurstCount==0,"멀리서도 화면 잡음이 발생함");
                    walker.enabled=false;
                    SessionState.SetInt(Stage,2);started=now;frames=0;
                }
                else if(stage==2)
                {
                    walker.SimulateMovement(walker.transform.position.z<1.8f?Vector2.up:Vector2.zero,.06f);
                    maxBob=Mathf.Max(maxBob,walker.CameraBobAmount);
                    if(Vector3.Distance(walker.transform.position,atmosphere.creature.position)<4 && interference.CurrentIntensity==0) clearNearFrames++;
                    if(frames<220 || now-started<8) return;
                    Check(walker.transform.position.z>1.6f,"Play 모드 기존 의자 앞 진입 실패: "+walker.transform.position);
                    Check(walker.transform.position.y>-.2f,"Play 모드 지면 추락");
                    Check(atmosphere.reveal>.6f,"접근 후 크리처 드러남 연출 실패: "+atmosphere.reveal);
                    Check(walker.StepCount>=10 && maxBob>.014f,"보행 주기/카메라 움직임 실패");
                    Check(interference.BurstCount>0 && maxStatic>.03f,"접근 잡음이 발생하지 않음");
                    Check(clearNearFrames>20,"잡음이 짧은 효과로 끝나지 않음");
                    string result=failures.Count==0?"PASS":"FAIL";
                    string report="The Silent Pavilion — actual Unity Play mode\n"+Application.unityVersion+"\n"+
                        result+": readable breathing, independent wisp, walking controller, camera gait, footsteps, short proximity static, reveal.\n"+
                        "Endpoint: "+walker.transform.position+" / reveal="+atmosphere.reveal+"\n"+
                        "Head displacement (local m): "+maxHeadDisplacement+" / max camera bob (m): "+maxBob+"\n"+
                        "Steps: "+walker.StepCount+" / static bursts: "+interference.BurstCount+" / max intensity: "+maxStatic+
                        " / clear near frames: "+clearNearFrames+"\n"+string.Join("\n",failures);
                    File.WriteAllText(MidSceneBuilder.Root+"/Documentation/PlayModeValidation.txt",report);
                    SessionState.SetInt("Mid.PlayReview.Exit",failures.Count==0?0:1);
                    SessionState.SetInt(Stage,3);EditorApplication.ExitPlaymode();
                }
            }
            catch(Exception e)
            {
                SessionState.SetBool(Active,false);Debug.LogException(e);EditorApplication.Exit(1);
            }
        }
    }
}
