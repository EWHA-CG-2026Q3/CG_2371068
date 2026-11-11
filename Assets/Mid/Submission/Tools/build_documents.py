"""Generate Korean submission HTML, checklist and Git instructions from actual project artifacts.
Print the HTML with Chrome headless; PDF validation uses pypdf from the local tool cache.
"""
from pathlib import Path
import html, json, shutil, subprocess

ROOT = Path(__file__).resolve().parents[4]
OUT = ROOT / 'Assets/Mid/Submission'
MID = ROOT / 'Assets/Mid'
OUT.mkdir(parents=True, exist_ok=True)
SCREENS = OUT / 'Screenshots'
SCREENS.mkdir(exist_ok=True)
for source, dest in [('Preview_Approach.png', '01_Exterior.png'), ('Preview_Interior.png', '02_Creature.png')]:
    shutil.copy2(MID / 'Documentation' / source, SCREENS / dest)

def esc(s): return html.escape(str(s))
def table(headers, rows, cls=''):
    return '<table class="'+cls+'"><thead><tr>'+''.join('<th>'+esc(h)+'</th>' for h in headers)+'</tr></thead><tbody>'+''.join('<tr>'+''.join('<td>'+esc(c)+'</td>' for c in row)+'</tr>' for row in rows)+'</tbody></table>'

objects = [
    ['가지 크리처', '기존 왼쪽 벤치에 착석한 장면의 중심', '반복문 정점·삼각형 생성 / 직접 4×4 행렬 호흡', 'MidCreatureMesh.BuildUpperBody / BuildLowerBody; MidCreatureBreathing.ApplyAt'],
    ['뒤틀린 숲', '정자 주변의 불길한 실루엣', '별도의 분기형 Mesh 생성 — 선택 ①', 'MidTwistedTree.Build'],
    ['떠다니는 불빛 4개', '정자 외부에서 느리게 움직이는 작은 빛', '타원 궤도·상하 운동 — 선택 ④', 'MidWispMotion.PositionAt / Update'],
    ['정자와 기존 벤치', '탐색 공간과 크리처의 좌석', '기존 에셋 재사용 / 좌판 실측·착석 배치', 'ThirdParty/Pavilion; MidSceneRevision.PositionOnBench'],
    ['랜턴·지면·안개', '따뜻한 내부 조명, 젖은 바닥과 낮은 안개', '재질·조명·노이즈 기반 안개 연출', 'MidLanternPlacement.Apply; Mid_GroundMist.shader'],
]
topics = [
    ['① 기하 모델링', '가지 크리처 + 별도 숲 Mesh. 경로·단면 반복문, 꼬임과 분기.', 'MidMeshBuilder.AddTube; MidCreatureMesh; MidTwistedTree.Build', '부적/풍경과 제단의 Mesh를 추가하고 면별 법선·UV 품질 개선.'],
    ['② 래스터화', '직접 CPU 래스터라이저는 현재 미구현. 중간 선택 항목으로 계산하지 않음.', '직접 구현 코드 없음', '무게중심 좌표로 만든 문양을 정자 액자/부적 텍스처로 활용.'],
    ['③ OpenGL', 'GL 즉시 모드는 현재 미구현. 중간 선택 항목으로 계산하지 않음.', 'GL 직접 호출 없음', '필요 시 GL 선으로 이동 경로·곡선 제어점 검증 도구 제작.'],
    ['④ 변환', '직접 행렬 호흡·좌우 흔들림 + 독립된 불빛 운동.', 'MidMatrixMath; MidCreatureBreathing.MatrixAt; MidWispMotion.PositionAt', '풍경/부적의 부모-자식 계층과 진자 운동, 상호작용 연계.'],
    ['⑤ 조명', '직접 수피 표면 색/홈 계산. 빛 반응은 URP PBR 함수 사용.', 'Mid_CreatureBark.shader; UniversalFragmentPBR', 'Blinn–Phong 확산·정반사·림 조명을 직접 계산하고 URP Lit과 비교.'],
    ['⑥ 곡선·곡면', 'Catmull–Rom 중심선을 샘플링해 가지 표면 생성. 중간에는 기하 모델링으로 제시.', 'MidMeshBuilder.CatmullRom / AddTube', 'Bezier 제어점 편집과 곡면 패치로 구부러진 부적·목/가지 형상 제작.'],
    ['확장 URP', '기본 Volume/Bloom 사용. 화면 잡음은 UI 오버레이이며 Renderer Feature 아님.', 'MidProximityStatic; Mid_ScreenStatic.shader', '깊이 텍스처 기반 안개를 ScriptableRendererFeature / RenderGraph 패스로 구현.'],
    ['심화 콘텐츠', '거리 기반 드러남·짧은 잡음·합성 발소리의 기초 연출.', 'MidAtmosphere; MidFirstPersonController.MakeFootstep', '공간 음향/차폐, 시선 반응과 제단 상호작용을 통합한 탐색 이벤트.'],
]
css = '''
@page{size:A4;margin:0}*{box-sizing:border-box}body{margin:0;background:#f6f5ef;color:#172d33;font-family:"Malgun Gothic",sans-serif}
.page{width:210mm;height:297mm;padding:14mm 15mm 12mm;break-after:page;position:relative;overflow:hidden}.page:last-child{break-after:auto}
.kicker{font-size:8pt;letter-spacing:1.4px;color:#57777c;border-top:3px solid #2d6668;padding-top:10px;margin-bottom:10px}
h1{font-size:27pt;font-weight:700;line-height:1.12;margin:4px 0 5px}h2{font-size:18pt;margin:5px 0 14px}h3{font-size:11pt;margin:14px 0 5px;color:#2d6668}
p{font-size:9.4pt;line-height:1.65;margin:7px 0}.subtitle{font-size:13pt;color:#587678}.concept{font-size:10pt;line-height:1.65;margin:11px 0 13px}
.shot{width:164mm;height:102.5mm;object-fit:contain;display:block;margin:auto;background:#091215}.caption{font-size:8pt;color:#587078;text-align:center;margin:4px 0 9px}
table{width:100%;border-collapse:collapse;table-layout:fixed;font-size:8.15pt;line-height:1.48;margin:9px 0}th{background:#254b50;color:white;text-align:left;padding:7px 6px;font-weight:400}
td{padding:7px 6px;border-bottom:1px solid #ccd6d3;vertical-align:top;overflow-wrap:anywhere}tr:nth-child(even){background:#eaf0ed}
.objects th:nth-child(1){width:15%}.objects th:nth-child(2){width:22%}.objects th:nth-child(3){width:28%}.objects th:nth-child(4){width:35%}
.topics{font-size:7.6pt;line-height:1.42}.topics td{padding:6px 5px}.topics th:nth-child(1){width:12%}.topics th:nth-child(2){width:30%}.topics th:nth-child(3){width:27%}.topics th:nth-child(4){width:31%}
.note{padding:9px 12px;background:#e1ebe6;border-left:3px solid #447b75;font-size:8.7pt;line-height:1.6;margin:12px 0}
.proof{font-size:8.8pt;line-height:1.55}.proof b{color:#315f60}.foot{position:absolute;left:15mm;right:15mm;bottom:8mm;font-size:7pt;color:#6a8084;display:flex;justify-content:space-between;border-top:1px solid #c5d1cb;padding-top:5px}
ul{font-size:8.8pt;line-height:1.65;padding-left:18px;margin:5px 0}
'''
cover = '''
<section class="page"><div class="kicker">COMPUTER GRAPHICS · MIDTERM PROJECT · 2371068</div>
<h1>The Silent Pavilion</h1><div class="subtitle">침묵의 정자</div>
<p class="concept">깊은 밤, 축축한 숲속 정자에 다가가면 기존 벤치에 앉은 나무형 크리처가 드러납니다.<br>조용한 호흡과 흔들리는 불빛, 짧은 화면 잡음으로 불길한 탐색 경험을 구성했습니다.</p>
<img class="shot" src="Screenshots/01_Exterior.png"><div class="caption">01 · 정자 외부와 진입로 — 실제 Unity 장면 렌더</div>
<img class="shot" src="Screenshots/02_Creature.png"><div class="caption">02 · 기존 벤치에 착석한 가지 크리처 — 실제 Unity 장면 렌더</div>
<div class="foot"><span>Unity 6000.3.23f1 · URP 17.3.0</span><span>01 / 03</span></div></section>'''
page2 = '<section class="page"><div class="kicker">CURRENT SCENE & IMPLEMENTATION</div><h2>현재 오브젝트와 구현 근거</h2>'+table(['오브젝트','장면 역할','직접 구현 / 활용','관련 코드'],objects,'objects')+'''
<div class="note">최신 공지 <b>중간과제설명2.pdf</b> 기준: 오브젝트 3개 이상, 필수 ①·④, 추가 선택 2개.<br>선택 요소는 <b>별도 숲 Mesh(①)</b>와 <b>독립된 불빛 움직임(④)</b>으로 구분했습니다.</div>
<h3>필수 ① · 반복문으로 만든 새로운 Mesh</h3><p class="proof">Catmull–Rom 경로를 따라 각 단면의 정점을 반복 생성합니다. 단면의 회전·반지름 변화를 더해 꼬인 가지를 만들고, 이웃한 네 정점을 삼각형 두 개로 연결합니다. 직접 생성한 메시를 장면의 크리처와 숲에서 사용합니다.</p>
<h3>필수 ④ · 직접 구현한 행렬 변환</h3><p class="proof"><b>M = T(pivot) · Rz · Rx · S · T(−pivot)</b><br>float[4,4]의 행·열 내적과 행렬–정점 곱을 직접 계산합니다. 매 프레임 원본 상체/눈 정점에 적용하며 골반과 하체의 착석은 유지합니다.</p>
<h3>검증 및 재사용 범위</h3><p class="proof">실제 Unity에서 컴파일, 메시/참조 검사와 Play 검사를 진행했습니다. 정자와 기존 벤치·목재 텍스처는 ThirdParty 에셋 재사용이며, 직접 만든 그래픽스 기법으로 중복 계산하지 않습니다. 기존 실습 장면과 스크립트는 보존합니다.</p>
<div class="foot"><span>Code: Assets/Mid/Scripts · Evidence: Documentation/Validation.txt</span><span>02 / 03</span></div></section>'''
page3 = '<section class="page"><div class="kicker">TOPIC MAP & FINAL PROJECT PLAN</div><h2>주제 반영과 기말 확장 계획</h2>'+table(['주제','현재 구현','구현 근거','기말까지의 계획'],topics,'topics')+'''
<h3>추가할 오브젝트 · 2개 이상</h3><ul><li><b>부적과 풍경:</b> 곡선·곡면으로 휘어진 부적과 계층 진자 운동을 가진 풍경. 바람과 접근 이벤트에 반응합니다.</li><li><b>제단과 촛불:</b> 직접 만든 제단/촛불 Mesh와 조명 셰이더, 상호작용 가능한 작은 탐색 목표를 추가합니다.</li></ul>
<div class="note">기말에는 중간 요구를 유지하면서 직접 조명 셰이더, 곡선·곡면, 깊이 기반 확장 URP, 공간 음향/상호작용을 보강합니다. 기말 작업은 공지에 따라 Assets/Final에서 이어갈 계획입니다.</div>
<p style="font-size:7.7pt">출처: 중간과제설명1.pdf / 최신 리마인드 중간과제설명2.pdf. 소개글·영상 제출: 2026.11.08 23:59. 갤러리 워크: 11.12(목), 노트북 시연 및 당일 좌석 이름 기재.</p>
<div class="foot"><span>추가 선택 2개 확보 · 현재 미구현 주제는 계획으로 명시</span><span>03 / 03</span></div></section>'''
document='<!doctype html><html lang="ko"><meta charset="utf-8"><title>The Silent Pavilion — 중간 프로젝트</title><style>'+css+'</style><body>'+cover+page2+page3+'</body></html>'
(OUT/'The_Silent_Pavilion_Midterm.html').write_text(document,encoding='utf-8')
print('Generated submission HTML and copied 2 actual Unity renders.')
