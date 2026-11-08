# The Silent Pavilion — 최종 검수

검수일: 2026-11-08. Unity 6000.3.23f1 / URP 17.3.0. 랜턴 예외 수정의 원인·좌표·120초 제한 재실행 결과는 `LANTERN_FIX_VERIFICATION.md`에 기록했습니다. `MidFinalSubmission.Prepare` 재실행 종료 코드 0 (`Logs/MidFinalPrepareVerified.log`).
`Documentation/중간과제설명2.pdf`의 최신 지침을 우선 적용했습니다. 중간 필수 ①·④ 외에 **추가 요소 2개**를 별개의 대상과 코드로 제시합니다.

| 번호 | 항목 | 상태 | 구현 및 확인 근거 |
|---|---|---|---|
| 1 | 오브젝트 3개 이상 | PASS | `Scenes/TheSilentPavilion.unity`: 정자·기존 벤치, 가지 크리처, 절차적 나무 숲, 독립 불빛, 랜턴. `Documentation/Validation.txt` 실제 장면 검사. |
| 2 | 직접 생성한 Mesh | PASS | `Scripts/Geometry/MidCreatureMesh.cs`, `MidMeshBuilder.Build`: 정점·삼각형 배열로 Mesh 생성. 생성 결과는 `Prefabs/GeneratedMeshes`. |
| 3 | 반복문으로 만든 새 형태 | PASS | `MidMeshBuilder.AddTube`: Catmull–Rom 중심선의 구간 및 단면 각도 이중 반복, 비틀림·반지름 변화로 가지 형태 생성. `GEOMETRY_AUDIT.txt` 면 방향 검사, `Documentation/Validation.txt` 실제 정점/인덱스/퇴화 삼각형 검사. |
| 4 | 직접 작성한 변환 | PASS | `MidMatrixMath`의 직접 행렬 곱, `MidCreatureBreathing.ApplyAt`: 피벗 이동·회전·크기 변환을 정점에 적용. `Documentation/PlayModeValidation.txt` 머리 변위 약 0.140m. |
| 5 | 추가 선택 요소 1: 기하 모델링 | PASS | `MidTwistedTree`: 크리처와 다른 나무 형태를 반복 생성. 숲 스크린샷 및 영상 약 46~51초. 단순 외부 모델 배치는 인정 대상으로 세지 않음. |
| 6 | 추가 선택 요소 2: 변환 | PASS | `MidWispMotion.PositionAt`: 독립 불빛의 타원 궤도·수직 진동. 실제 Play 검사와 영상 약 51~55초. 크리처 행렬 운동과 별도 대상/운동. |
| 7 | Unity 컴파일 | PASS | 설치된 Unity 6000.3.23f1에서 batch 실행 종료 코드 0. `Logs/MidSubmissionRecord.log`, `Logs/MidFinalPlayValidation.log`. C# 및 실제 URP 렌더 실행. |
| 8 | 장면 실행 | PASS | 실제 Play 모드 검사와 Unity Recorder MP4 제작. `VIDEO_CAPTURE.txt`, `Documentation/PlayModeValidation.txt`. |
| 9 | 1인칭 이동 | PASS | 실제 `MidFirstPersonController.SimulateMovement`로 이동, 끝점 z≈1.92, 19회 발걸음, 시점 bob≈0.0276m 검증. 자동 검사는 동일 이동 함수를 호출함. 사람의 물리 키보드·마우스 조작과 발소리 청취는 별도 확인 필요. |
| 10 | 정자·크리처·랜턴 표시 | PASS | 실제 URP `Screenshots/01_Exterior.png`, `02_Creature.png`. 기존 벤치 착석/발 바닥 검사 및 `Documentation/LanternPlacement.txt` 갓 지붕 아래, 전구와 실제 광원 위치 일치 검사. 발광 키워드 수정 후 재녹화. |
| 11 | PDF | PASS | `The_Silent_Pavilion_Midterm.pdf`: 한국어 3쪽, 실제 이미지 2장, 오브젝트와 구현/계획 표. `PDF_VERIFICATION.json`. |
| 12 | 실제 스크린샷 1~2장 | PASS | `Screenshots/01_Exterior.png`, `02_Creature.png`: Unity URP 실제 렌더 출력. 합성 이미지 아님. |
| 13 | MP4 및 60초 이내 | PASS | `MEDIA_VERIFICATION.json`: 57.97초, 1280×720, 30fps, 1739프레임 전체 디코딩, 오류 0. 실제 Unity Recorder 출력이며 설명 자막은 녹화에만 추가. |
| 14 | 기말 확장 계획 | PASS | PDF 3쪽: 항목 ①~⑥/URP/심화의 현재 상태·근거·확장 계획, 풍경/부적과 제단/촛불 등 새 오브젝트 2종 계획. 아직 구현하지 않은 조명식/곡면/Renderer Feature는 계획으로만 표기. |
| 15 | GitHub 제출 준비 | PARTIAL | 로컬 장면·코드·재질·셰이더 및 `.meta` 검사는 `REPOSITORY_AUDIT.json`, 소스 목록은 `GIT_FILES.txt`. 원격 Mid 브랜치 연결 확인. 이번 변경의 commit/push는 사용자 지시에 따라 수행하지 않았으므로 최종 원격 게시 완료는 아님. 명령은 `GIT_SUBMISSION.md`. |

## 수정 및 보존 범위

- 랜턴 갓을 지붕 아래로 정렬하고 짧은 매달림 줄을 생성했습니다. 전구와 Point Light를 같은 월드 위치에 맞추고 랜턴 아래에 함께 배치했습니다. 기존 FBX는 수정하지 않았습니다.
- 불빛·눈·랜턴의 `_EMISSION` 키워드와 발광 플래그를 실제로 활성화했습니다.
- 기존 실습 코드와 장면은 수정하지 않았습니다. 녹화용 카메라 투어/자막은 실행 중에만 생성되며 저장된 플레이 장면에는 없습니다.
- 직접 조명식 전체 구현, 직접 래스터라이저, GL 코드, 볼류메트릭 안개/SSR는 현재 완료 항목으로 주장하지 않습니다.
- 화면·스피커의 주관적 체감과 실제 마우스/키보드 입력은 사용자가 마지막 확인해야 합니다.

## 제출 구분

코드 제출: `Assets/Mid`의 소스·장면·재질·셰이더·메시와 `.meta` 및 기존 외부 의존 에셋. PDF·MP4·unitypackage는 Git 필수 파일이 아닙니다.
사이버캠퍼스: PDF, 1분 이내 MP4, 필요한 실제 스크린샷과 저장소 링크. 최신 안내의 11월 8일 23:59 기한과 11월 12일 갤러리워크 준비 사항을 확인하세요.
