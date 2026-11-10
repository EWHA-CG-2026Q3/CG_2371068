"""Build two presentation documents using the existing report and actual screenshots."""
from pathlib import Path
import html, json, re, sys

ROOT=Path(__file__).resolve().parents[4]
OUT=ROOT/'Assets/Mid/Submission/Presentation'
OUT.mkdir(exist_ok=True)
sys.path.insert(0,str(ROOT/'Logs/MidTools/pypdf.whl'))
from pypdf import PdfReader
report=PdfReader(ROOT/'Assets/Mid/Submission/The_Silent_Pavilion_Midterm.pdf')
report_text='\n'.join(p.extract_text() for p in report.pages)
assert 'MidMeshBuilder' in report_text and '기말' in report_text

# Speaker-only stage directions are excluded from the timed spoken text.
sections=[
('01','00:00–00:25','25초','침묵의 정자',
 '첫 장을 보여주고, 정자와 크리처를 손으로 가리킵니다.',
 ['안녕하세요. 컴퓨터그래픽스 중간 프로젝트, 침묵의 정자를 소개하겠습니다. 깊은 밤 숲속의 정자에 다가가면, 벤치에 앉은 나무 같은 크리처가 서서히 드러나는 장면입니다. 갑자기 놀라게 하기보다, 멈춰 있는 것처럼 보이는 존재가 조금씩 움직일 때 느끼는 불안함을 표현하고 싶었습니다.']),
('02','00:25–00:55','30초','장면 구성과 구현 범위',
 '왼쪽은 외부, 오른쪽은 크리처입니다. 아래의 직접 구현/재사용 구분을 짚습니다.',
 ['장면은 크리처, 뒤틀린 나무 숲, 떠다니는 불빛, 정자와 랜턴으로 구성했습니다. 정자와 기존 벤치는 외부 에셋을 재사용했고, 크리처는 별도 의자가 아니라 원래 정자의 왼쪽 벤치에 앉혔습니다. 반면 크리처와 숲의 메시, 크리처의 호흡 변환, 불빛의 움직임은 코드로 구현했습니다. 따라서 외부 모델을 단순히 배치한 부분과 과제에서 설명할 직접 구현 부분을 구분했습니다.']),
('03','00:55–01:50','55초','반복문으로 생성한 가지 Mesh',
 '그림의 중심선 → 단면 → 삼각형 순서로 설명합니다. 수식 전체를 읽을 필요는 없습니다.',
 ['첫 번째 핵심은 반복문으로 정점을 생성하는 메시입니다. 먼저 제어점을 통과하는 캣멀 롬 곡선으로 가지의 중심선을 정합니다. 중심선을 따라가는 바깥 반복문과 단면 둘레를 도는 안쪽 반복문을 사용해, 각 지점에 원형 또는 납작한 단면의 정점을 만듭니다.',
  '슬라이드의 식에서 씨는 중심선의 위치이고, 엔과 비는 단면 방향, 알은 반지름입니다. 각도에 따라 단면의 정점 위치를 계산하고, 단면 회전과 반지름 변화를 더해 가지가 비틀리고 가늘어지도록 했습니다. 그다음 이웃한 두 단면 사이의 네 정점을 삼각형 두 개로 연결합니다.',
  '이 방식으로 크리처의 몸과 가지를 구성했고, 별도의 나무 형태에도 적용했습니다. 메시 생성 후 법선과 경계를 다시 계산하며, 정점과 삼각형 인덱스가 유효한지도 검사했습니다.']),
('04','01:50–02:40','50초','직접 만든 행렬로 호흡 표현',
 '오른쪽부터 적용되는 순서를 짚습니다. 골반 피벗과 상체의 관계를 강조합니다.',
 ['두 번째 핵심은 직접 구현한 행렬 변환입니다. 크리처의 상체가 작게 팽창하고 기울어지도록, 사인 함수로 시간에 따른 크기와 회전 각도를 계산했습니다. 행렬 곱과 정점 변환은 사 곱하기 사 배열을 이용해 직접 계산합니다.',
  '열벡터 기준으로 식의 오른쪽부터 적용합니다. 먼저 골반 근처의 피벗을 원점으로 옮기고, 크기를 바꾼 뒤 엑스축과 제트축으로 회전시킵니다. 마지막에 피벗을 원래 위치로 돌려놓습니다. 이렇게 하면 하체의 착석을 유지하면서 상체가 움직입니다.',
  '매 프레임 이전 결과가 아니라 원본 정점에 새 행렬을 적용하므로 변형이 계속 누적되지 않습니다. 눈의 정점에도 같은 행렬을 적용해 얼굴에서 분리되지 않도록 했습니다.']),
('05','02:40–03:15','35초','추가 선택 요소 2개',
 '필수 구현과 구분된 두 대상, 숲과 불빛을 각각 가리킵니다.',
 ['추가 선택 요소는 두 가지입니다. 첫째는 별도의 뒤틀린 나무 숲입니다. 크리처와 다른 분기와 반지름을 가진 나무를 코드로 만들었고, 기하 모델링 요소로 제시했습니다. 둘째는 네 개의 독립된 불빛 운동입니다. 코사인과 사인으로 수평 타원 궤도를 만들고, 다른 주기의 사인 함수로 높이를 바꿉니다. 이를 부모 기준의 로컬 위치에 적용합니다. 크리처의 정점 변형과는 대상과 운동 방식이 다른 추가 변환입니다.']),
('06','03:15–03:50','35초','탐색 연출과 실제 검증',
 '여기서는 영상을 재생하지 않고 설명합니다. 발표 후 질문 시간에 실제 장면을 보여줄 수 있습니다.',
 ['탐색 과정에서는 가까워질수록 크리처가 보이고, 짧은 화면 잡음이 간헐적으로 나타납니다. 걷기는 실제 이동 거리에 맞춰 시점 흔들림과 합성 발소리를 동기화했습니다. 랜턴은 갓과 전구, 실제 광원 위치를 맞춰 정자 내부가 읽히도록 조정했습니다.',
  '실제 유니티 플레이 검사에서 크리처 변위, 보행 주기, 불빛 운동과 네 번의 잡음 발생을 확인했습니다. 실제 장면을 녹화한 약 오십팔 초 영상도 만들었습니다. 다만 수피의 색과 홈은 직접 계산했지만, 빛 반응 자체는 유알피의 피비알 함수를 사용합니다.']),
('07','03:50–04:20','30초','기말에는 무엇을 확장할까',
 '모든 항목이 계획임을 분명히 말합니다. 지금 구현된 기능처럼 설명하지 않습니다.',
 ['기말에는 새 오브젝트로 부적과 풍경, 제단과 촛불을 추가할 계획입니다. 곡선과 곡면으로 부적을 만들고, 풍경에는 부모 자식 계층의 진자 운동을 연결하려고 합니다. 또한 조명식을 직접 계산하는 셰이더와 깊이 기반 안개를 구현하고, 공간 음향과 상호작용을 보강하겠습니다. 현재 화면 잡음은 유아이 오버레이이므로, 렌더러 피처 기반 효과는 앞으로의 확장 항목입니다.']),
('08','04:20–04:40','20초','핵심 정리와 질의응답',
 '마지막 장에서 멈춥니다. 질문을 받은 뒤 필요한 경우 Unity 창으로 전환합니다.',
 ['정리하면, 이번 중간 프로젝트에서는 반복문으로 만든 유기적인 메시와 직접 계산한 행렬 변환을 장면의 핵심으로 사용했습니다. 여기에 나무 숲의 모델링과 독립된 불빛 운동을 더해 과제 조건을 구성했습니다. 기말에는 이 그래픽스 기법들을 탐색과 상호작용으로 확장하겠습니다. 감사합니다.'])]

def e(text):return html.escape(text)
def foot(i,label):return f'<footer><span>THE SILENT PAVILION · 2371068</span><span>{label}</span><b>{i:02} / 08</b></footer>'
def slide(i,kicker,title,body,cls=''):
    return f'<section class="slide {cls}"><div class="kicker">{kicker}</div><h1>{title}</h1>{body}{foot(i,sections[i-1][1])}</section>'
def card(n,title,body):return f'<div class="card"><div class="num">{n}</div><h3>{title}</h3><p>{body}</p></div>'

css='''
@page{size:320mm 180mm;margin:0}*{box-sizing:border-box}body{margin:0;background:#0c1b20;color:#f5f4ec;font-family:"Malgun Gothic",sans-serif;-webkit-print-color-adjust:exact;print-color-adjust:exact}
.slide{width:320mm;height:180mm;padding:13mm 17mm 15mm;position:relative;overflow:hidden;break-after:page;background:#102329}.slide:last-child{break-after:auto}
.kicker{font-size:10pt;color:#8ec7bd;letter-spacing:2px;margin-bottom:4mm}h1{font-size:30pt;letter-spacing:-.7px;line-height:1.23;margin:0 0 9mm}h2{font-size:22pt;margin:0 0 6mm}h3{font-size:17pt;line-height:1.35;margin:3mm 0}p{font-size:15pt;line-height:1.65;margin:2mm 0;color:#d3dedb}b{color:#f8f4e9}.accent{color:#9ee2cc}.muted{color:#a7bab8}.small{font-size:11pt;line-height:1.5}.cols{display:grid;grid-template-columns:1fr 1fr;gap:9mm}.three{display:grid;grid-template-columns:1fr 1fr 1fr;gap:6mm}.card{padding:6mm;border:1px solid #3a565b;background:#173137;border-radius:3mm}.card p{font-size:13.5pt}.num{font-size:11pt;color:#91d7bd;letter-spacing:1px}.strip{padding:4mm 6mm;margin-top:6mm;border-left:1mm solid #91d7bd;background:#1a363b;font-size:15pt;line-height:1.5}.code{font-family:Consolas,"Malgun Gothic",monospace;font-size:11pt;color:#9ab8b5;overflow-wrap:anywhere}footer{position:absolute;bottom:6mm;left:17mm;right:17mm;border-top:1px solid #365055;padding-top:3mm;display:flex;justify-content:space-between;font-size:9pt;color:#95aeaa}footer b{color:#95aeaa}.shot{width:100%;height:77mm;object-fit:cover;border:1px solid #365055;border-radius:2mm}.label{margin:2mm 0;font-size:12pt;color:#bdd2cc}.hero{background:#0c1b20}.hero img{position:absolute;right:0;top:0;width:204mm;height:180mm;object-fit:cover;opacity:.85}.hero .veil{position:absolute;inset:0;background:linear-gradient(90deg,#0b1b20 9%,rgba(11,27,32,.97) 25%,rgba(11,27,32,.15) 77%)}.hero .front{position:relative;width:173mm}.hero h1{font-size:43pt;margin:11mm 0 2mm}.hero h2{font-size:25pt;color:#b4d1c7}.hero p{font-size:17pt;max-width:140mm}.pill{display:inline-block;border:1px solid #6c9e91;padding:2mm 4mm;margin-top:6mm;color:#b4e1ca;font-size:12pt}.formula{font-family:Consolas,"Malgun Gothic",monospace;font-size:20pt;color:#b9e7cd;background:#0c1d23;border:1px solid #395852;padding:5mm;margin:0 0 5mm;text-align:center}.steps{display:flex;gap:3mm;align-items:stretch;margin-top:7mm}.step{flex:1;background:#1a353a;border-top:1mm solid #7ab6a7;padding:4mm 3mm;text-align:center}.step b{font-size:17pt}.step p{font-size:12pt}.arrow{align-self:center;color:#8ec7bd;font-size:18pt}.metric{font-size:26pt;color:#a5deca;margin:2mm 0}.check{font-size:18pt;line-height:1.8;color:#d3dedb}.last h1{font-size:34pt}.last .takeaway{font-size:25pt;line-height:1.65;margin-top:10mm}.qa{position:absolute;right:18mm;bottom:25mm;font-size:32pt;color:#95c8b8}
'''

# Code-native diagrams express the actual algorithm; they are not substitute scene screenshots.
mesh_svg='''<svg viewBox="0 0 600 260" style="width:100%;height:67mm" xmlns="http://www.w3.org/2000/svg"><defs><marker id="a" markerWidth="6" markerHeight="6" refX="5" refY="3" orient="auto"><path d="M0 0L6 3L0 6" fill="#a5deca"/></marker></defs><path d="M50 210C140 215 140 90 245 110S410 190 530 50" fill="none" stroke="#91d7bd" stroke-width="4" stroke-dasharray="9 6"/><g fill="none" stroke="#c3dcd0" stroke-width="2"><ellipse cx="110" cy="184" rx="30" ry="47" transform="rotate(40 110 184)"/><ellipse cx="210" cy="111" rx="25" ry="41" transform="rotate(80 210 111)"/><ellipse cx="330" cy="137" rx="25" ry="36" transform="rotate(75 330 137)"/><ellipse cx="440" cy="130" rx="20" ry="29" transform="rotate(25 440 130)"/></g><g stroke="#6ba397" stroke-width="2"><path d="M79 191L207 69L305 128L433 101M142 174L213 152L355 146L447 159M79 191L213 152M207 69L355 146M305 128L447 159" fill="none"/></g><g fill="#e1c083"><circle cx="79" cy="191" r="5"/><circle cx="207" cy="69" r="5"/><circle cx="213" cy="152" r="5"/><circle cx="305" cy="128" r="5"/><circle cx="355" cy="146" r="5"/><circle cx="433" cy="101" r="5"/></g><g fill="#d3dedb" font-family="Malgun Gothic" font-size="18"><text x="19" y="244">중심선 C(t)</text><text x="183" y="35">반복 생성한 단면</text><text x="358" y="224">이웃 정점 → 삼각형 2개</text></g></svg>'''
wisp_svg='''<svg viewBox="0 0 510 245" style="height:66mm;width:100%" xmlns="http://www.w3.org/2000/svg"><ellipse cx="255" cy="133" rx="181" ry="64" stroke="#6d9c93" stroke-width="3" stroke-dasharray="9 6" fill="none"/><path d="M74 133Q150 10 255 93T436 133" stroke="#9fe5cc" stroke-width="4" fill="none"/><circle cx="255" cy="93" r="12" fill="#e3fff1"/><circle cx="255" cy="133" r="4" fill="#e1c083"/><path d="M255 133V93" stroke="#e1c083" stroke-width="2"/><g fill="#d3dedb" font-size="18" font-family="Malgun Gothic"><text x="127" y="229">수평 타원 궤도 + 수직 진동</text><text x="277" y="94">서로 다른 위상</text></g></svg>'''

slides=[]
slides.append(slide(1,'COMPUTER GRAPHICS · MIDTERM GALLERY WALK · 2026.11.12','',
 '<img src="../Screenshots/02_Creature.png"><div class="veil"></div><div class="front"><h1>The Silent<br>Pavilion</h1><h2>침묵의 정자</h2><p>가만히 앉아 있는 것처럼 보이는 존재가<br>조금씩 움직일 때의 불안함</p><span class="pill">절차적 Mesh · 직접 행렬 변환</span><p class="small" style="margin-top:7mm">2371068 · Unity 6000.3.23f1 / URP 17.3.0</p></div>','hero'))
slides.append(slide(2,'SCENE & OWNERSHIP','장면을 구성한 것, 직접 구현한 것',
 '<div class="cols"><div><img class="shot" src="../Screenshots/01_Exterior.png"><div class="label">정자 · 뒤틀린 숲 · 독립 불빛 · 랜턴</div></div><div><img class="shot" src="../Screenshots/02_Creature.png"><div class="label">기존 벤치에 앉은 가지 크리처</div></div></div><div class="strip"><b>직접 구현</b> 메시·행렬·운동 &nbsp; / &nbsp; <span class="muted">재사용</span> 정자·기존 벤치·텍스처</div>'))
slides.append(slide(3,'REQUIRED ① · PROCEDURAL GEOMETRY','중심선 × 단면 = 새로운 가지 Mesh',
 '<div class="cols" style="grid-template-columns:1.15fr 1fr"><div>'+mesh_svg+'<p class="small muted">생성 원리 도식 · 실제 모델 스크린샷은 1–2장</p></div><div><div class="formula" style="font-size:16pt">P = C + r(cos θ N + f sin θ B)</div><p>① 경로 구간 반복 → 중심선 샘플링<br>② 단면 각도 반복 → 정점 생성<br>③ 이웃 정점 연결 → 삼각형 2개</p><p class="small accent">반지름·단면 회전 변화 → 가늘어짐·꼬임</p></div></div><div class="strip">Catmull–Rom 중심선 · 정점/삼각형 직접 생성 · 법선 재계산</div><p class="code">MidMeshBuilder.AddTube → MidCreatureMesh / MidTwistedTree</p>'))
slides.append(slide(4,'REQUIRED ④ · DIRECT MATRIX TRANSFORMATION','골반은 고정하고, 상체에 호흡을 준다',
 '<div class="formula">M = T(p) · Rz · Rx · S · T(−p)</div><p class="small muted" style="text-align:center">열벡터 기준 · 오른쪽부터 적용 · p = 골반 근처 피벗</p><div class="steps"><div class="step"><b>1. 원점으로</b><p>T(−p)</p></div><div class="arrow">→</div><div class="step"><b>2. 크기 변화</b><p>사인파 기반 S</p></div><div class="arrow">→</div><div class="step"><b>3. 회전</b><p>Rx 후 Rz</p></div><div class="arrow">→</div><div class="step"><b>4. 되돌리기</b><p>T(p)</p></div></div><div class="strip">float[4,4] 직접 곱셈 · 매 프레임 <b>원본 정점</b> 사용 · 눈도 같은 변환</div><p class="code">MidMatrixMath.Multiply / TransformPoint · MidCreatureBreathing.MatrixAt / ApplyAt</p>'))
slides.append(slide(5,'TWO ADDITIONAL ELEMENTS','별도 숲 모델링 + 독립된 불빛 운동',
 '<div class="cols"><div class="card"><div class="num">추가 선택 ① · 기하 모델링</div><h3>뒤틀린 나무 숲</h3><p>크리처와 다른 분기·반지름<br>반복 생성한 나무 Mesh</p><p class="code">MidTwistedTree.Build</p><div class="strip small" style="margin-top:3mm">새로운 대상에 적용한 기하 모델링</div></div><div><div class="num">추가 선택 ④ · 변환</div>'+wisp_svg+'<p class="small">x = r cos θ &nbsp; z = 0.55r sin θ &nbsp; y = 0.18 sin(1.7θ)</p><p class="code">MidWispMotion.PositionAt → transform.localPosition</p></div></div>'))
slides.append(slide(6,'EXPERIENCE & VALIDATION','연출은 조용하게, 검증은 실제 장면으로',
 '<div class="three">'+card('EXPLORATION','접근과 보행','거리 기반 드러남<br>이동 거리와 동기화한<br>시점 흔들림·합성 발소리')+card('ATMOSPHERE','짧은 이상 신호','간헐적인 화면 잡음<br>랜턴·전구·광원 정렬<br>따뜻한 내부 조명')+card('ACTUAL PLAY TEST','실행 검사 완료','컴파일·Play 검사 통과<br>머리 변위 약 0.140m<br>실제 녹화 57.97초')+'</div><div class="strip"><b>구현 범위:</b> 수피 색·홈 계산은 직접 작성, 빛 반응은 URP PBR 사용</div><p class="small muted">근거: Documentation/PlayModeValidation.txt · Submission/MEDIA_VERIFICATION.json<br>화면 잡음은 UI 오버레이이며, Renderer Feature 구현으로 세지 않습니다.</p>'))
slides.append(slide(7,'FINAL PROJECT · PLANNED','새 오브젝트와 그래픽스 기법을 연결',
 '<div class="cols"><div class="card"><div class="num">새 오브젝트 계획 A</div><h3>부적 · 풍경</h3><p>곡선·곡면으로 형상 제작<br>부모–자식 계층의 진자 운동</p></div><div class="card"><div class="num">새 오브젝트 계획 B</div><h3>제단 · 촛불</h3><p>새로운 Mesh와 직접 조명식<br>탐색 목표·상호작용 연결</p></div></div><div class="strip">직접 조명 셰이더 → 깊이 기반 URP 안개 → 공간 음향·상호작용</div><p class="small muted">위 내용은 기말 계획입니다. 직접 조명식·Renderer Feature·직접 래스터라이저/GL은 현재 완료 항목으로 주장하지 않습니다.</p>'))
slides.append(slide(8,'TAKEAWAY & QUESTIONS','작은 움직임이 장면의 분위기를 만든다',
 '<div class="takeaway"><span class="accent">필수 ①</span> 반복문으로 만든 유기적인 Mesh<br><span class="accent">필수 ④</span> 직접 계산한 행렬로 호흡 표현<br><span class="accent">추가 2개</span> 나무 모델링 + 독립 불빛 운동</div><p style="margin-top:9mm">기말에는 그래픽스 기법을 탐색과 상호작용으로 확장하겠습니다.</p><div class="qa">감사합니다 · Q&amp;A</div>','last'))
(OUT/'The_Silent_Pavilion_Presentation.html').write_text('<!doctype html><html lang="ko"><meta charset="utf-8"><title>The Silent Pavilion — 갤러리워크 발표</title><style>'+css+'</style><body>'+''.join(slides)+'</body></html>',encoding='utf-8')

scriptcss='''@page{size:A4;margin:0}*{box-sizing:border-box}body{font-family:"Malgun Gothic",sans-serif;color:#1b3338;background:#fff;margin:0}.page{width:210mm;height:297mm;padding:16mm 18mm;position:relative;break-after:page;overflow:hidden}.page:last-child{break-after:auto}.eyebrow{font-size:9pt;letter-spacing:1px;color:#507971;border-top:3px solid #2e675c;padding-top:4mm}h1{font-size:21pt;margin:5mm 0 3mm}h2{font-size:15pt;margin:7mm 0 2mm;color:#21594e}.timing{font-size:10pt;background:#eaf2ed;padding:3mm 4mm;margin-bottom:2mm}.cue{font-size:9pt;color:#607872;margin:2mm 0 3mm}p{font-size:11pt;line-height:1.82;margin:2mm 0 3mm;word-break:keep-all}.note{font-size:10pt;line-height:1.65;border-left:3px solid #779e8b;background:#f0f5f1;padding:3mm 4mm;margin:4mm 0}.foot{position:absolute;bottom:9mm;left:18mm;right:18mm;display:flex;justify-content:space-between;font-size:8pt;border-top:1px solid #c6d8cf;padding-top:3mm;color:#658079}.qa{font-size:10.5pt;line-height:1.7}.qa b{color:#21594e}.qa p{font-size:10.5pt;line-height:1.7}ul{font-size:10pt;line-height:1.7;padding-left:5mm}'''
pages=[]
for page_i in range(4):
    body='<div class="eyebrow">THE SILENT PAVILION · 발표자 대본 · 2026.11.12</div>'
    if page_i==0:
        body+='<h1>4분 40초 발표 대본</h1><div class="note">발표자료 PDF 8장과 번호가 일치합니다. 시간은 리허설 목표이며 실제 발화 속도에 따라 달라집니다. 본문만 읽고 회색 진행 메모는 읽지 않습니다. 영상 58초 전체 재생은 포함하지 않습니다.</div>'
    for s in sections[page_i*2:page_i*2+2]:
        n,t,d,title,cue,paras=s
        body+=f'<h2>슬라이드 {n} · {e(title)}</h2><div class="timing">{t} · 목표 {d}</div><div class="cue">[진행] {e(cue)}</div>'+''.join('<p>'+e(p)+'</p>' for p in paras)
        body+=f'<div class="cue">[넘김] {t.split("–")[1]} 부근에 다음 슬라이드로 이동'+('하지 않고 질문을 받습니다.' if n=='08' else '합니다.')+'</div>'
    pages.append('<section class="page">'+body+f'<div class="foot"><span>본문 낭독 + 짧은 호흡 포함 · 8장 / 4분 40초 목표</span><span>{page_i+1} / 5</span></div></section>')
qas=[
('왜 프리미티브 대신 Mesh를 직접 만들었나요?', '단면마다 반지름과 꼬임을 바꿔 가지처럼 이어지는 형태를 만들기 위해서입니다. 정점과 인덱스를 직접 구성해 표면이 생성되는 원리를 설명할 수 있습니다.'),
('행렬을 왜 그 순서로 곱하나요?', '열벡터 기준 오른쪽부터 적용합니다. 피벗을 원점으로 옮긴 뒤 크기와 회전을 적용하고 되돌리면 골반 근처를 중심으로 움직입니다. 순서를 바꾸면 회전 중심과 결과가 달라집니다.'),
('왜 매 프레임 원본 정점을 사용하나요?', '이전 프레임 결과에 반복 적용하면 크기나 회전이 누적될 수 있습니다. 원본에 현재 시각의 행렬을 적용해 같은 시간에는 같은 자세가 나오도록 했습니다.'),
('추가 요소 두 개가 필수 구현과 어떻게 다른가요?', '크리처 외에 별도의 나무 형태를 생성한 기하 모델링과, 독립 불빛의 로컬 좌표 운동입니다. 대상과 결과가 다른 두 요소로 제시했습니다.'),
('셰이더의 조명도 직접 만들었나요?', '현재 수피의 색과 홈, 화면 잡음은 직접 작성했지만 조명 반응은 URP PBR 함수를 호출합니다. Blinn–Phong 같은 조명식을 직접 계산하는 작업은 기말 계획입니다.'),
('어떤 부분을 재사용했고 무엇을 검증했나요?', '정자와 기존 벤치·텍스처를 재사용했습니다. 직접 생성한 메시, 행렬 변형, 불빛 운동, 보행과 잡음은 기존 실제 Play 검사 결과를 근거로 설명합니다. 하드웨어 입력과 체감 밝기는 발표 전 직접 확인합니다.')]
qa_body='<div class="eyebrow">SPEAKER ONLY · 본 발표 시간에 포함하지 않는 참고자료</div><h1>예상 질문과 발표 전 확인</h1><div class="qa">'+''.join(f'<p><b>Q. {e(q)}</b><br>A. {e(a)}</p>' for q,a in qas)+'</div><div class="note"><b>시간 조절:</b> 길어지면 3장 마지막 두 문장과 6장 검증 수치를 줄입니다. 58초 영상 전체를 본문 중 재생하면 약 5분 38초가 되므로 재생하지 않습니다. 원하면 발표 후 짧은 구간만 보여줍니다.</div><ul><li>발표자료 PDF를 전체 화면으로 열고 1→8장 넘김을 연습합니다.</li><li>Unity 장면을 미리 열어 둡니다. 필요할 때 Play → Game 클릭 → WASD/마우스, Esc로 해제합니다.</li><li>11/12 의무출석. 입장 후 지정 좌석과 당일 코멘터 그룹을 확인합니다.</li><li>발표 4분 40초 후 질의응답 1~2분, 다음 발표 준비 1분을 고려해 진행자의 시간 안내를 따릅니다.</li></ul>'
pages.append('<section class="page">'+qa_body+'<div class="foot"><span>출처: 기존 중간 제출 PDF · 검수표 · 실제 구현 코드</span><span>5 / 5</span></div></section>')
(OUT/'The_Silent_Pavilion_Script.html').write_text('<!doctype html><html lang="ko"><meta charset="utf-8"><title>The Silent Pavilion — 발표 대본</title><style>'+scriptcss+'</style><body>'+''.join(pages)+'</body></html>',encoding='utf-8')
spoken='\n\n'.join('\n'.join(s[5]) for s in sections)
timing=dict(target_seconds=280,slide_count=8,script_pages=5,spoken_characters_excluding_whitespace=len(re.sub(r'\s','',spoken)),sections=[dict(slide=s[0],time=s[1],target=s[2],title=s[3]) for s in sections],source_report_pages=len(report.pages),note='Timing is a rehearsal target, not a measured voice recording. Full 57.97-second video playback is excluded.')
(OUT/'PRESENTATION_TIMING.json').write_text(json.dumps(timing,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(timing,ensure_ascii=True))
