# The Silent Pavilion — 침묵의 정자

늦은 밤 숲속 정자에 접근하면 의자에 앉아 있는 길고 마른 인간형 크리처가 서서히 드러나는 1인칭 탐색 장면입니다. 점프 스케어 대신 작은 호흡, 흔들리는 불빛, 낮은 안개로 불길한 분위기를 만듭니다.

## 실행

- Unity **6000.3.23f1**, URP **17.3.0**, Input System **1.20.0** 사용. 추가 패키지나 외부 다운로드는 필요하지 않습니다.
- `Assets/Mid/Scenes/TheSilentPavilion.unity`를 열고 Play를 누릅니다.
- Game 화면을 클릭하면 마우스가 고정됩니다. WASD/방향키로 걷고 마우스로 시점을 돌립니다. Esc로 고정을 해제합니다.
- 장면이 없으면 `Tools > Mid > The Silent Pavilion > 01 Open or Create Scene`을 실행합니다.
- `02 Rebuild Generated Scene`은 Mid의 생성 장면/에셋을 재구성합니다. 장면을 직접 수정했다면 먼저 다른 이름으로 저장하세요. 기존 실습 장면/재질/프리팹은 수정하지 않습니다.
- `03 Validate Scene`: 메시·행렬·참조·정자 진입 충돌 경로 검사. 결과는 `Documentation/Validation.txt`에 기록합니다.
- `04 Capture Preview Images`: URP로 외부/내부 미리보기 이미지를 `Documentation`에 저장합니다.

## 오브젝트와 수업 요구사항

|구분|오브젝트 / 파일·함수|직접 구현 내용|
|---|---|---|
|필수 ①|크리처 / `Scripts/Geometry/MidCreatureMesh.cs`, `BuildUpperBody`, `BuildLowerBody`|굽은 몸통·긴 목·팔·다리·손가락·갈비뼈·비대칭 머리. 정점과 인덱스를 C#에서 생성|
|반복문 새 형태|`MidMeshBuilder.AddTube`, `AddEllipsoid`|경로 샘플 반복 × 단면 각도 반복으로 유기적인 표면 생성. 이웃한 네 정점을 삼각형 두 개로 연결|
|필수 ④|크리처 상체 / `MidMatrixMath`, `MidCreatureBreathing.MatrixAt`, `Update`|직접 4×4 행렬 생성·곱셈·정점 변환. 골반 기준 미세한 호흡, 하체 고정|
|선택 A: 추가 기하|뒤틀린 나무 / `MidTwistedTree.Build`|인체와 다른 분기형 줄기·가지·뿌리. 고정 seed로 재현 가능한 숲 생성|
|선택 B: 추가 변환|떠다니는 불빛 / `MidWispMotion.PositionAt`, `Update`|삼각함수로 부모 기준 타원 운동과 수직 운동을 계산. 인체 호흡과 독립된 오브젝트|

장면에는 재사용 정자, 새 의자, 크리처, 나무, 젖은 지면, 물웅덩이, 램프, 떠다니는 불빛, 안개와 먼지가 있습니다. 외부 정자와 목재 텍스처는 환경 재사용이며 직접 Mesh 구현 항목은 크리처와 나무입니다. 선택 요소의 최종 인정 범위는 수업 채점 기준에 따릅니다.

## 원리와 코드 읽는 순서

1. `MidMeshBuilder.AddTube`: Catmull–Rom 곡선 중심 C(t)에 단면 기저 N, B를 두고 `P=C+r(cosθ N+flatten·sinθ B)`를 계산합니다. 이전 단면의 법선 방향을 새 접평면으로 투영해 프레임의 갑작스러운 뒤집힘을 줄입니다.
2. `MidCreatureMesh`: 제어점과 반지름을 바꿔 착석 인체를 구성합니다. 메시는 Editor 생성 시 에셋으로 저장되어 장면을 다시 열어도 남습니다.
3. `MidMatrixMath`: 행과 열의 내적을 반복문으로 계산합니다. 동차좌표 w=1이 이동 항을 적용하게 합니다.
4. `MidCreatureBreathing`: `M=T(pivot)·Rx·S·T(-pivot)`. 매 프레임 원본 정점에 적용해 변형 누적을 막고, 런타임 메시 복제본만 수정합니다.
5. `MidWispMotion`: `x=r cosθ`, `z=.55r sinθ`, `y=.18 sin(1.7θ)`로 별도 운동을 구현합니다.
6. `MidAtmosphere`: 거리 기반 목표 밝기를 시간에 따라 천천히 따라가며 크리처를 드러냅니다. 조명 깜빡임과 눈 Emission도 여기서 제어합니다.

## 환경과 재사용

- 원본: `Assets/ThirdParty/Pavilion/Prefabs/Pavilion.prefab`, `Lamp.prefab`, 기존 목재/지붕 텍스처, 암석 텍스처.
- 참고 원리: S03–S04 Mesh/와인딩, S09 직접 행렬 계산, S10 변환 합성, S13 이후 URP 재질/조명. 기존 실습 코드는 변경하지 않습니다.
- URP Lit 재질, 차가운 달빛/림라이트, 약한 따뜻한 램프, 거리 기반 보조 조명, 전역 지수 안개를 사용합니다.
- `Shaders/Mid_GroundMist.shader`: 월드 좌표 노이즈를 시간에 따라 이동시키는 반투명 수평 안개 층입니다. 볼류메트릭 레이마칭은 아닙니다.
- 별도 VolumeProfile에 Bloom, ACES, 대비/채도, Vignette를 저장합니다. 프로젝트의 공용 URP 설정을 덮어쓰지 않습니다.

## 검증 범위와 확인할 부분

- 2026-11-08: 실제 Unity 6000.3.23f1에서 자동 배치·장면 저장과 검사 실행 완료. `Documentation/Validation.txt`에 직접 행렬 검산, 메시 인덱스/면적 검사, 원본 정자 크기, 바깥→내부 CharacterController 이동 결과를 기록했습니다.
- 실제 Play 모드에서 호흡 메시 변화·불빛 이동·충돌 이동·접근 연출 검사를 통과했습니다. 결과는 `Documentation/PlayModeValidation.txt`입니다. 키보드/마우스를 사람이 직접 조작한 검사는 아니며, 이동 경로는 CharacterController를 코드로 구동해 검사했습니다.
- 그래픽 출력을 켠 URP 렌더 이미지: `Documentation/Preview_Approach.png`, `Preview_Interior.png`. PNG는 sRGB로 저장합니다. 내부 이미지는 접근 후 보조 조명이 켜진 상태를 재현합니다.
- 실제 검증 결과는 `Documentation`의 보고서와 실행 로그를 기준으로 확인합니다. 코드 컴파일과 실제 렌더링/플레이 검증은 서로 다릅니다.
- 최종 화면 밝기는 디스플레이/해상도에 따라 확인이 필요합니다. 너무 어두우면 Mid 전용 Volume의 Post Exposure 또는 보조 조명 밝기를 조절합니다.
- 물웅덩이는 부드러운 반사 재질이며 평면 반사 카메라/SSR을 구현하지 않았습니다.
- 인체는 절차적 표면 여러 개가 겹치는 구조이며, 단일 스킨드 메시나 리깅 모델은 아닙니다.
- 안개 평면에는 투명 정렬의 한계가 있습니다. 진짜 볼류메트릭 조명, 오디오, 퀘스트는 이번 범위에 포함되지 않습니다.

## 기말 확장 계획

- 추가 오브젝트: 바람에 흔들리는 부적/풍경, 제단과 촛불, 숲길 안내석 중 2개 이상.
- 조명 셰이더: S19 Phong을 확장한 피부/젖은 목재용 직접 조명 계산과 림 조명.
- 곡선·곡면: 곡선 제어점을 편집하는 목/손가락 생성 도구와 곡면 법선 계산 개선.
- 확장 URP: Renderer Feature를 이용한 깊이 기반 안개·화면 효과, 품질별 성능 비교.
- 심화 콘텐츠: 공간 음향, 가까이 볼 때의 시선 반응, 조용한 탐색 목표와 상호작용.

## 1분 이내 소개 영상

- 0–10초: 제목과 숲속 정자 외부 실루엣.
- 10–28초: WASD로 천천히 정자에 진입하며 크리처가 드러나는 모습.
- 28–40초: 의자 착석, 긴 팔과 손가락, 미세한 호흡 근접 촬영.
- 40–50초: 뒤틀린 나무와 떠다니는 불빛의 독립 운동.
- 50–60초: 반복문 Mesh 생성과 직접 행렬 변환 함수 및 요구사항 표를 짧게 제시.
