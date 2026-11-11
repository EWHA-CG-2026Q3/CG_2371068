# 검토 후 사용자가 실행할 Git 제출 명령

현재 브랜치: `Mid`. 원격: https://github.com/EWHA-CG-2026Q3/CG_2371068
검수 시작 HEAD 및 확인한 원격 Mid: `9073f9dc93e8c23a7d80456d4f4b9ea1511dd9fa`.
자동 commit/push/merge는 수행하지 않았습니다. 원격은 확인 이후 변경될 수 있습니다.

## 코드 중심 제출 (PowerShell)

프로젝트 루트에서 아래 순서로 실행합니다. PDF·MP4·unitypackage와 그 `.meta`는 제외하고 나머지 Mid 파일을 함께 추가합니다. 실제 소스 목록은 `GIT_FILES.txt`, 변경/참조 검사는 `REPOSITORY_AUDIT.json`에 있습니다.

```powershell
git branch --show-current
git status --short
git diff --stat
git add -- Assets/Mid.meta Assets/Mid ':(exclude)Assets/Mid/Submission/*.mp4' ':(exclude)Assets/Mid/Submission/*.mp4.meta' ':(exclude)Assets/Mid/Submission/*.pdf' ':(exclude)Assets/Mid/Submission/*.pdf.meta' ':(exclude)Assets/Mid/Submission/*.unitypackage' ':(exclude)Assets/Mid/Submission/*.unitypackage.meta'
git diff --cached --stat
git diff --cached --check
git commit -m "Finalize The Silent Pavilion midterm submission"
git push -u origin Mid
```

`Mid`가 아닌 브랜치가 표시되면 그대로 커밋하지 말고 브랜치 상태부터 확인합니다. push 충돌이 발생하면 강제 push하지 말고 원격 변경을 확인합니다. `.csproj`는 Unity가 자동 생성하므로 제출용 변경으로 추가할 필요가 없습니다.

PDF/MP4를 Git에도 보관하고 싶으면 commit 전에 해당 결과물과 `.meta`를 함께 `git add` 할 수 있습니다. 필수는 아니며 사이버캠퍼스에 직접 업로드하는 편이 간단합니다.

## 제외 및 의존성

- `Library`, `Temp`, `Logs`, `UserSettings`, `Obj`, 빌드 출력은 `.gitignore`에 따라 제외합니다. 검증 로그의 `Logs/`는 로컬 증거이며 Git 제출 대상이 아닙니다.
- 정자·벤치·램프는 이미 프로젝트의 `Assets/ThirdParty/Pavilion`에 있는 에셋을 참조합니다. 기존 외부 에셋과 `Packages/manifest.json`, `packages-lock.json`, `ProjectSettings`는 기존 저장소 상태를 유지합니다.
- `Scenes/TheSilentPavilion.unity`, `Scripts`, `Materials`, `Shaders`, `Prefabs/GeneratedMeshes`와 각 `.meta`를 함께 유지해야 합니다. `.unitypackage`는 외부 의존성을 포함한 별도 백업입니다.

## 사이버캠퍼스

1. PDF 3쪽과 MP4 58초를 직접 열어 최종 확인합니다.
2. 소개 게시물에 PDF, MP4, 필요 시 스크린샷 1~2장과 GitHub `Mid` 브랜치 링크를 첨부합니다.
3. 최신 안내 기준 2026-11-08 23:59까지 제출합니다. 11월 12일 갤러리워크 때 실행 가능한 노트북/장면과 당일 좌석 정보를 준비하고, 공지의 동료 소개글 댓글 요구도 확인합니다.
