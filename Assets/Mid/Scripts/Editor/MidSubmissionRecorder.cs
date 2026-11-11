using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Recorder.Encoder;
using UnityEngine;

namespace SilentPavilion.Editor
{
    [InitializeOnLoad]
    public static class MidSubmissionRecorder
    {
        const string Active="Mid.Submission.Recording";
        static RecorderController recorder;
        static MidSubmissionTour tour;
        static double started;
        static bool recording;
        static MidSubmissionRecorder(){if(SessionState.GetBool(Active,false))Hook();}
        static void Hook(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;started=EditorApplication.timeSinceStartup;}
        [MenuItem("Tools/Mid/The Silent Pavilion/08 Record Submission Video (58 seconds)")]
        public static void RunBatch()
        {
            MidFinalSubmission.Prepare();SessionState.SetBool(Active,true);Hook();EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            try
            {
                if(!EditorApplication.isPlaying)
                {
                    if(EditorApplication.timeSinceStartup-started>120)throw new TimeoutException("Recording did not enter Play mode");return;
                }
                if(!recording)
                {
                    if(Time.timeSinceLevelLoad<.5f)return;
                    tour=new GameObject("Submission camera tour (temporary)").AddComponent<MidSubmissionTour>();tour.Initialize();
                    var settings=ScriptableObject.CreateInstance<RecorderControllerSettings>();
                    settings.FrameRate=30;settings.CapFrameRate=false;settings.ExitPlayMode=false;settings.SetRecordModeToManual();
                    var movie=ScriptableObject.CreateInstance<MovieRecorderSettings>();movie.name="Midterm submission";movie.Enabled=true;
                    movie.EncoderSettings=new CoreEncoderSettings{Codec=CoreEncoderSettings.OutputCodec.MP4,EncodingQuality=CoreEncoderSettings.VideoEncodingQuality.Medium};
                    movie.ImageInputSettings=new GameViewInputSettings{OutputWidth=1280,OutputHeight=720};movie.CaptureAudio=true;movie.CaptureAlpha=false;
                    movie.OutputFile=Path.GetFullPath(MidFinalSubmission.Folder+"/The_Silent_Pavilion_Midterm");
                    settings.AddRecorderSettings(movie);recorder=new RecorderController(settings);
                    recorder.PrepareRecording();if(!recorder.StartRecording())throw new InvalidOperationException("Recorder StartRecording failed");
                    recording=true;tour.Running=true;started=EditorApplication.timeSinceStartup;
                }
                else if(tour.Frame>=MidSubmissionTour.Frames)
                {
                    tour.Running=false;recorder.StopRecording();
                    string file=MidFinalSubmission.Folder+"/The_Silent_Pavilion_Midterm.mp4";
                    if(!File.Exists(file) || new FileInfo(file).Length<100000)throw new IOException("Movie output empty");
                    File.WriteAllText(MidFinalSubmission.Folder+"/VIDEO_CAPTURE.txt","Actual Unity Recorder Game View capture\nUnity "+Application.unityVersion+
                        "\n1280x720 / 30 fps / requested 1740 frames (58 seconds)\nRuntime camera tour + actual CharacterController walking\nTemporary recording captions; no captions saved into gameplay scene.\n");
                    SessionState.SetBool(Active,false);EditorApplication.ExitPlaymode();EditorApplication.update-=Tick;
                    EditorApplication.delayCall+=Finish;
                }
                else if(EditorApplication.timeSinceStartup-started>900)throw new TimeoutException("Recorder timeout");
            }
            catch(Exception e)
            {
                SessionState.SetBool(Active,false);if(recorder!=null&&recorder.IsRecording())recorder.StopRecording();
                Debug.LogException(e);EditorApplication.Exit(1);
            }
        }
        static void Finish()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Finish;return;}
            MidSceneValidation.CapturePreviews();MidFinalSubmission.ExportPackage();
            if(Application.isBatchMode)EditorApplication.Exit(0);
            else Debug.Log("[Mid] Submission video, screenshots and package saved.");
        }
    }
}
