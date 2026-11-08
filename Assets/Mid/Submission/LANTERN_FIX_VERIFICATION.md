# 랜턴 정렬 예외 수정 및 재검증

## 원인과 수정

이전 실패의 shade bounds에는 랜턴 아래의 발광 중심부 및 잘못 변환된 매달림 줄까지 포함됐습니다. 또한 가져온 램프 메시의 원점·크기와 부모 스케일을 그대로 사용한 상태에서 월드 좌표로 생성한 줄을 다시 랜턴 자식으로 변환해 위치/크기를 중복 적용하는 문제가 있었습니다. 실패 로그의 중심 y=3.47, 높이 반범위 .91은 실제 갓만의 경계가 아니었습니다.

`Scripts/Editor/MidLanternPlacement.cs`는 다음과 같이 수정되어 있습니다.

1. `core`, Point Light, rim을 일시적으로 조명 부모 아래로 분리합니다. `SetParent(..., true)`로 월드 좌표를 보존하여 램프 이동/크기 변경이 자식에 누적되지 않게 합니다.
2. 원본 FBX를 변경하지 않고 `Mid_LanternShade.asset` 복사본의 정점을 원점 중심, 높이 .34m로 정규화합니다. 랜턴 회전은 항등, 스케일은 1로 두고 월드 중심을 지붕 아래에 배치합니다.
3. 전구와 Point Light의 **월드** 위치를 동일하게 지정한 뒤 둘 다 랜턴 자식으로 되돌립니다. `SetParent(lamp,true)` 때문에 Unity가 로컬 위치를 역변환하며 실제 위치는 유지됩니다.
4. 매달림 줄 정점은 월드 좌표로 생성하므로, 항등 Transform인 조명 그룹의 자식으로 둡니다. 갓 자식으로 두어 이중 변환하지 않습니다.
5. 메시 경계의 8개 모서리를 `TransformPoint`로 월드 변환해 측정합니다. `Renderer.bounds` 갱신 시점에 의존하지 않습니다.

현재 장면의 조명 부모는 항등 Transform이고, 랜턴 월드 중심은 약 `(.55, 2.800594, 2.04)`입니다. 전구/Light의 월드 위치는 `(.55, 2.590594, 2.04)`, 랜턴 기준 로컬 위치는 둘 다 약 `(0, -.21, 0)`입니다. 줄은 랜턴과 형제이며, 갓/전구 경계를 오염시키지 않습니다.

## 검증 유지 및 실제 결과

`BoundsOf(lamp).max.y >= roofBottom` 또는 `Distance(core.position, light.position) > .001`이면 여전히 `InvalidOperationException`을 발생시킵니다. 예외를 제거하거나 catch하여 성공으로 처리하지 않았습니다.

- 실제 실행: Unity 6000.3.23f1, `-batchmode -quit -force-d3d11 -executeMethod SilentPavilion.Editor.MidFinalSubmission.Prepare`.
- 이번 작업이 시작한 프로세스에 120초 제한 적용. 제한을 초과할 때만 해당 프로세스를 종료하도록 실행했습니다.
- 실제 결과: **종료 코드 0**, `Logs/MidFinalPrepareVerified.log`.
- 지붕 밑면: `3.110594`. 갓/전구 합친 경계: 중심 `(.55,2.75,2.04)`, 반범위 `(.13,.22,.12)`. 최대 높이 약 `2.97`, 지붕 아래에 위치.
- 전구/Light: 둘 다 `(.55,2.59,2.04)`. 상세 결과: `Documentation/LanternPlacement.txt`.
- 최종 장면 실제 Play 검사 종료 코드 0: `Logs/MidFinalPlayValidation.log`, `Documentation/PlayModeValidation.txt`.
- 수정 후 실제 Unity 영상/스크린샷으로 제출 PDF·MP4 생성. 각각 `PDF_VERIFICATION.json`, `MEDIA_VERIFICATION.json` 참조.

Git commit/push는 수행하지 않았습니다. `Logs/`는 로컬 검증 로그이며 저장소 제출 대상에서 제외합니다.
