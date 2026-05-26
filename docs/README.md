# 프로젝트 운영 지침 및 개발 가이드

### 문서 목록

<!-- DOC_LIST_START -->

- [cli-options-guide.md](./cli-options-guide.md): CLI 옵션 사용 가이드
- [cocona-guide.md](./cocona-guide.md): Cocona 라이브러리 가이드
- [csharp-convention.md](./csharp-convention.md): C# 코딩 컨벤션 가이드
- [docker-guide.md](./docker-guide.md): Docker 설치 및 구동 가이드
- [dotnet-guide.md](./dotnet-guide.md): C# 개발을 위한 .NET 설치 및 프로젝트 구성 가이드
- [git-guide.md](./git-guide.md): git 기초
- [github-token-guide.md](./github-token-guide.md): GitHub Personal Access Token 가이드
- [issue-pr-guide.md](./issue-pr-guide.md): 이슈 작업 선점 관련 규정 가이드
- [octokit-guide.md](./octokit-guide.md): Octokit.NET 활용 가이드
- [score-guide.md](./score-guide.md): 오픈소스 기여 점수 산정 가이드
- [vscode-extensions.md](./vscode-extensions.md): C# 개발을 위한 VSCode 확장 가이드
<!-- DOC_LIST_END -->

---

> ⚠️**아래 상황이 발생하면 반드시 스크립트를 통해 목록을 자동 갱신해줘야 합니다.**
>
> - docs/README.md 파일을 업데이트하기 위해 docs/README-template.md.j2 파일을 수정한 경우
> - docs/\*.md 경로에 새로운 문서를 생성한 경우
> - 기존 문서를 삭제한 경우
> - 파일 이름을 변경하거나, 문서 내부의 최상위 제목(# 제목)을 수정한 경우
>
> ```실행 방법 (Root 디렉터리에서 실행시)
> 1.Codespaces 실행
> 2.하단의 Terminal 창을 확인합니다. (안 보인다면 Ctrl + ` 입력)
>  아래 명령어를 입력하여 스크립트를 실행합니다.
> ```
>
> ```bash
> python tools/update-doclist.py
> python tools/j2render.py docs/README-template.md.j2 vars/doclist.json -o docs/README.md
> ```
>
> 스크립트 위치: `tools/update-doclist.py`, `tools/j2render.py`
>
> ⚠️ **수작업으로 문서 목록 갱신하지 마세요.**
> ⚠️ **docs/README.md 파일을 직접 수정하지 마세요.**

---

## docs/README.md 파일 수정 가이드

> ⚠️ `docs/README.md`를 직접 수정하지 마세요.

`docs/README.md`를 수정하려면 먼저 `docs/README-template.md.j2`를 업데이트한 후, 루트 디렉터리에서 아래 명령어를 실행하여 `docs/README.md`를 자동으로 갱신하세요.

### 수정 방법

1. `docs/README-template.md.j2`를 업데이트합니다.
2. 루트 디렉터리에서 아래 명령어를 실행합니다.

   ```bash
   python tools/update-doclist.py
   python tools/j2render.py docs/README-template.md.j2 vars/doclist.json -o docs/README.md
   ```

3. 커밋하기 전에 `docs/README.md`가 자동으로 갱신되었는지 확인합니다.

## 문서 파일 생성 규칙

프로젝트 내 문서 파일의 일관성을 유지하기 위해 다음과 같은 파일 이름 생성 규칙을 따릅니다.

### 기본 규칙

- 모든 파일 이름은 소문자(lowercase)를 사용합니다.
- 단어 구분은 공백 대신 하이픈(-)을 사용합니다.
- 파일 확장자는 `.md`를 사용합니다.

### 예시

- `setup-guide.md`
- `api-reference.md`
- `contributing-guide.md`
- `error-handling.md`

### 추가 규칙

- 파일 이름은 간결하고 의미를 명확하게 전달해야 합니다.
- 불필요한 특수문자는 사용하지 않습니다.
- 동일한 의미의 단어는 통일하여 사용합니다 (예: guide, doc 등)

---

## 주의사항

문서 관련 이슈 작성 시 반드시 아래를 명시해주세요:

- 대상 문서의 정확한 경로 (예: docs/cli-lib-guide.md)
- 수정 또는 추가할 내용

새 md 문서 작성 시:

- #하나로 시작하는 문서의 최상위 제목을 반드시 작성(예: # 문서 제목)
