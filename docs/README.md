# 프로젝트 운영 지침 및 개발 가이드

### 문서 목록

<!-- DOC_LIST_START -->
- [cli-lib-guide.md](./cli-lib-guide.md): C# CLI 프로그램 개발을 위한 추천 라이브러리
- [csharp-convention.md](./csharp-convention.md): C# 코딩 컨벤션 가이드
- [docker-guide.md](./docker-guide.md): Docker 설치 및 구동 가이드
- [dotnet-guide.md](./dotnet-guide.md): C# 개발을 위한 .NET 설치 및 프로젝트 구성 가이드
- [git-guide.md](./git-guide.md): git 기초
- [issue-guide.md](./issue-guide.md): 이슈 작업 선점 관련 규정 가이드
- [octokit-graphql-guide.md](./octokit-graphql-guide.md): Octokit.NET 과 GraphQL 활용 가이드
- [score-guide.md](./score-guide.md): 오픈소스 기여 점수 산정 가이드
- [vscode-extensions.md](./vscode-extensions.md): C# 개발을 위한 VSCode 확장 가이드
<!-- DOC_LIST_END -->
---
> ⚠️ **문서 목록은 수작업으로 갱신하지 마세요.**
> `docs/*.md` 문서를 생성·삭제하거나 제목을 변경할 경우, 반드시 아래 스크립트를 실행하여 목록을 자동 갱신하세요.
> ```bash
> python update-docs-readme.py
> ```
>
> 스크립트 위치: `update-docs-readme.py` (레포 루트)
---
프로젝트 내 문서 파일의 일관성을 유지하기 위해 다음과 같은 파일 이름 생성 규칙을 따릅니다.
---

### 1. 기본 규칙

- 모든 파일 이름은 소문자(lowercase)를 사용합니다.
- 단어 구분은 공백 대신 하이픈(-)을 사용합니다.
- 파일 확장자는 `.md`를 사용합니다.

### 2. 예시

- `setup-guide.md`
- `api-reference.md`
- `contributing-guide.md`
- `error-handling.md`

### 3. 추가 규칙

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
