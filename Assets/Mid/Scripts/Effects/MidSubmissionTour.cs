using UnityEngine;
using UnityEngine.UI;

namespace SilentPavilion
{
    // 제출 영상 촬영 중에만 생성하며 저장 장면에는 붙이지 않는다.
    public sealed class MidSubmissionTour : MonoBehaviour
    {
        public const int Frames=1740;
        public int Frame {get;private set;}
        public bool Running;
        MidFirstPersonController walker;
        Transform view,creature;
        MidWispMotion wisp;
        Text caption;
        float floor;
        public void Initialize()
        {
            walker=FindFirstObjectByType<MidFirstPersonController>();walker.enabled=false;walker.moveSpeed=1.02f;
            view=walker.view;creature=FindFirstObjectByType<MidCreatureBreathing>().transform;
            foreach(var candidate in FindObjectsByType<MidWispMotion>(FindObjectsSortMode.None))if(candidate.name=="Wisp 0")wisp=candidate;
            floor=.6515f;
            var root=new GameObject("Recording captions only",typeof(RectTransform),typeof(Canvas));root.transform.SetParent(transform,false);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=200;
            var band=new GameObject("Caption backdrop",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));band.transform.SetParent(root.transform,false);
            var rect=band.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=new Vector2(1,.145f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            band.GetComponent<Image>().color=new Color(.015f,.025f,.03f,.88f);band.GetComponent<Image>().raycastTarget=false;
            var label=new GameObject("Evaluation captions",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));label.transform.SetParent(root.transform,false);
            rect=label.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.035f,.015f);rect.anchorMax=new Vector2(.965f,.135f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            caption=label.GetComponent<Text>();caption.font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",24);caption.fontSize=24;
            caption.color=new Color(.88f,.93f,.90f);caption.alignment=TextAnchor.MiddleLeft;caption.raycastTarget=false;
            caption.horizontalOverflow=HorizontalWrapMode.Wrap;caption.verticalOverflow=VerticalWrapMode.Overflow;
        }
        void PlacePlayer(Vector3 p)
        {var c=walker.GetComponent<CharacterController>();c.enabled=false;walker.transform.position=p;c.enabled=true;}
        void LateUpdate()
        {
            if(!Running)return;
            float t=Frame/30f;Vector3 position,target;
            Vector3 chest=creature.TransformPoint(new Vector3(0,1.25f,0));
            if(t<10)
            {
                position=new Vector3(0,1.74f,-12);target=Vector3.Lerp(new Vector3(-3,2.4f,1.8f),new Vector3(0,1.8f,2),Mathf.SmoothStep(0,1,t/10));
                caption.text="THE SILENT PAVILION  ·  침묵의 정자\n어두운 숲, 기존 정자와 벤치, 절차적으로 만든 나무형 크리처";
            }
            else if(t<25)
            {
                walker.SimulateMovement(walker.transform.position.z<.75f?Vector2.up:Vector2.zero,1/30f);
                position=view.position;target=Vector3.Lerp(new Vector3(0,1.9f,3),chest,Mathf.SmoothStep(0,1,(t-10)/15));
                caption.text="1인칭 탐색  ·  실제 CharacterController로 정자 진입\n이동 거리에 동기화한 발걸음 / 가까워지면 짧게 나타나는 화면 잡음";
            }
            else if(t<40)
            {
                float u=Mathf.SmoothStep(0,1,(t-25)/15);
                position=Vector3.Lerp(new Vector3(.7f,floor+1.64f,.15f),new Vector3(.45f,floor+1.68f,.55f),u);target=chest+Vector3.up*.10f;
                caption.text="필수 ①  ·  C# 반복문으로 생성한 새로운 Mesh\n곡선의 단면 링, 삼각형 인덱스, 꼬인 가지와 갈라진 몸통";
            }
            else if(t<46)
            {
                position=new Vector3(.25f,2.45f,.9f);target=creature.TransformPoint(new Vector3(0,1.85f,-.10f));
                caption.text="필수 ④  ·  직접 구현한 4×4 행렬 변환\n골반 고정 / 상체와 눈에 Rz·Rx·Scale 적용 / 원본 정점에서 매 프레임 계산";
            }
            else if(t<51)
            {
                PlacePlayer(new Vector3(0,.1f,-8));position=new Vector3(0,1.7f,-10);target=new Vector3(-5,4.5f,3);
                caption.text="선택 ①  ·  추가 기하 모델링: 뒤틀린 숲\nMidTwistedTree.Build — 줄기·분기·뿌리를 가진 별도 Mesh";
            }
            else if(t<55)
            {
                // 정자 옆의 첫 불빛은 숲보다 안쪽에 있어 나무에 가리지 않는다.
                // 카메라는 고정하고 궤도 중심을 바라봐 불빛 자체의 움직임이 읽히게 한다.
                Vector3 center=wisp.transform.parent.TransformPoint(wisp.orbitCenter);position=center+new Vector3(1.1f,.45f,-1.4f);target=center;
                caption.text="선택 ④  ·  추가 변환: 떠다니는 불빛\nMidWispMotion.PositionAt — 독립된 타원 궤도와 수직 운동";
            }
            else
            {
                position=new Vector3(0,1.8f,-8);target=new Vector3(0,1.9f,2);
                caption.text="기말 확장  ·  부적/풍경 + 제단/촛불\n직접 조명 셰이더, 곡선·곡면, 깊이 기반 URP 안개, 공간 음향과 상호작용";
            }
            view.position=position;view.rotation=Quaternion.LookRotation(target-position);
            Frame++;
        }
    }
}
