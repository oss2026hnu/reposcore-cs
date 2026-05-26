# C# 개발을 위한 VSCode 확장 가이드

## 📌 개요

VSCode에서 C# 개발을 시작할 때 필요한 확장 프로그램과 설치 방법을 안내하는 가이드입니다.  
**GitHub Codespaces(클라우드 환경)** 와 **Local(로컬 환경)** 을 모두 기준으로 설명합니다.

---

## 🧩 공통 확장 (핵심 필수 설치 ⭐)

쾌적한 C# 개발 환경 구축을 위해 다음 3가지 핵심 확장을 설치해야 합니다.  
**C# Dev Kit** 하나만 설치해도 아래 두 항목이 자동으로 함께 설치됩니다.

| 확장 이름             | 게시자    | 역할                                                   |
| --------------------- | --------- | ------------------------------------------------------ |
| **C# Dev Kit**        | Microsoft | 솔루션 탐색기, 프로젝트 관리, 통합 테스트 환경 제공    |
| **C#**                | Microsoft | IntelliSense(자동 완성), 실시간 오류 검사, 디버깅 기능 |
| **.NET Install Tool** | Microsoft | .NET SDK 및 런타임 자동 설치·관리                      |

### C# Dev Kit의 주요 기능

- **솔루션 탐색기 지원**: Visual Studio처럼 솔루션(`.sln`) 및 프로젝트(`.csproj`) 단위로 파일과 참조를 직관적으로 관리
- **고급 언어 서비스**: 향상된 IntelliSense, 코드 분석, AI 기반 코드 추천(IntelliCode) 제공
- **통합 테스트 환경**: Test Explorer를 통해 xUnit, NUnit, MSTest 기반 단위 테스트를 코드 위에서 바로 실행 및 디버깅 가능
- **XML 문서 주석(Standard XML Documentation) 자동 생성**: 메서드, 클래스, 인터페이스 직관 위에 `///`을 입력하면 `<summary>`, `<param>`, `<returns>` 등 XML 주석 템플릿이 자동으로 생성

> **XML 문서 주석 사용 예시**
>
> ```csharp
> /// <summary>
> /// 사용자의 기여도 데이터를 기반으로 최종 점수를 산출합니다.
> /// </summary>
> /// <param name="repoData">저장소별 상세 기여 데이터</param>
> /// <returns>산출된 최종 집계 점수</returns>
> public int CalculateUserScores(DetailedRepoData repoData)
> {
>     // 로직 구현부
> }
> ```

---

## 📄 HTML 결과 파일 미리보기 (Live Server)

HTML 결과를 확인하려면 아래 확장을 설치해야 합니다.

| 확장 이름       | 게시자      | 역할                                                                         |
| --------------- | ----------- | ---------------------------------------------------------------------------- |
| **Live Server** | Ritwick Dey | 로컬 웹 서버를 띄워 외부 브라우저에서 HTML 문서를 실시간으로 확인하도록 지원 |

본 프로젝트에서는 `--format html` 옵션을 사용하면 `results/results.html` 파일이 생성됩니다.  
예를 들어 아래 명령을 실행하면 HTML 리포트를 만들 수 있습니다.

```bash
dotnet run -- oss2026hnu/reposcore-cs -t YOUR_GITHUB_TOKEN --format html
```

> 이 문서는 Codespaces와 Local에서 각각 다른 방식으로 확인하는 방법을 설명합니다.

### Codespaces 기준 사용 방법

1. VSCode에서 **프로젝트 루트 폴더**를 엽니다.
2. 우측 하단의 **`Go Live`** 버튼을 클릭하거나, 폴더를 **우클릭 → `Open with Live Server`** 를 선택합니다.
3. 브라우저가 열리면 **파일 목록**이 보일 수 있습니다. 이때 `results/results.html` 경로를 찾아 선택하거나 브라우저 주소창에 `/results/results.html`을 입력해 주세요.
4. 브라우저에서 URL이 `results/results.html`이 제대로 표시되는지 확인합니다.

> Codespaces에서는 Live Server가 열린 포트가 브라우저로 포워딩되지 않으면 접근이 되지 않을 수 있습니다.

### Local 기준 사용 방법

1. VSCode에서 **프로젝트 루트 폴더**를 엽니다.
2. 탐색기에서 `results/results.html`을 찾은 뒤, **우클릭 → `Open with Live Server`** 를 선택합니다.  
   또는 우측 하단의 **`Go Live`** 버튼을 클릭합니다.
3. 브라우저에서 URL이 `results/results.html`이 제대로 표시되는지 확인합니다.

### 문제 해결 방법

#### 1. `[Codespaces]` `Open with Live Server`를 눌렀는데 루트 폴더 파일 목록만 보일 때
- **원인:** Live Server가 선택한 파일이 아니라 **프로젝트 루트 폴더**를 기준으로 서버를 띄운 경우입니다.
- **해결:** 브라우저 주소창에 `/results/results.html`을 직접 입력하거나, 우측 하단의 **Open in Browser**를 클릭하세요.

#### 2. `[Codespaces]` 접근이 안 될 때
- **원인:** 컨테이너 내부에서 열린 포트가 외부 브라우저로 포워딩되지 않았기 때문입니다.
- **해결:** `Go Live` 실행 후 우측 하단의 **Open in Browser** 버튼을 클릭하거나, 하단 터미널 영역의 `포트(Ports)` 탭에서 직접 포워딩 주소를 선택하세요.

#### 3. `[Local]` 브라우저가 열리지 않을 때
- **원인:** 기본 브라우저가 설정되지 않았거나 브라우저를 찾지 못한 경우입니다.
- **해결:** `Ctrl + ,` 에서 `Live Server > Settings: Custom Browser`를 검색해 브라우저를 지정하세요.

#### 4. `[Local]` `Open with Live Server`가 보이지 않거나 동작하지 않을 때
- **원인:** `results.html` 같은 단일 파일을 직접 열거나, 프로젝트 루트가 아닌 파일만 열어 Live Server를 실행한 경우입니다.
- **해결:** **프로젝트 루트 폴더를 열고** `Open with Live Server` 또는 `Go Live`를 실행하세요.

#### 5. `[Local]` `file://`로 열면 차트나 스타일이 깨질 때
- **원인:** 브라우저에서 HTML을 직접 파일 경로로 열면 보안 정책에 의해 차트(JavaScript)나 스타일(CSS)이 차단될 수 있습니다.
- **해결:** 반드시 Live Server를 통해 열어야 합니다.

---

## 💻 Codespaces 환경

### 특징

- 로컬 PC에 별도의 개발 환경을 구축할 필요 없이, **웹 브라우저에서 바로** 실행 가능
- 컨테이너 기반 환경으로, 로컬 머신의 성능에 구애받지 않고 무거운 C# 프로젝트도 원활하게 작업 가능
- `.devcontainer/devcontainer.json` 파일 하나로 팀원 전체가 **동일한 환경을 자동으로** 공유 가능

---

### Codespaces 생성 방법

1. GitHub 저장소 이동
2. `<> Code` 버튼 클릭
3. `Codespaces` 탭 선택
4. Codespace 생성
5. 환경 로드 후 사용

---

### 방법 — UI에서 수동 설치

1. 확장 메뉴 열기 (`Ctrl + Shift + X`)
2. `C# Dev Kit` 검색
3. Microsoft 확장 설치
4. 자동 설치 확인

---

## 🔄 devcontainer 변경 후 적용 방법 (Rebuild)

### ❗ 개요

devcontainer 설정이 변경된 경우, 기존 Codespace에는  
변경 사항이 자동으로 반영되지 않습니다.  
따라서 변경된 환경을 적용하려면 **Rebuild 작업**이 필요합니다.

---

### 📌 Rebuild가 필요한 경우

- `.devcontainer/devcontainer.json` 파일이 수정된 경우
- 새로운 VSCode 확장이 devcontainer에 추가된 경우
- .NET SDK 또는 런타임 버전이 변경된 경우

---

### ⚙️ Rebuild 방법

1. 명령 팔레트 열기
    - `Ctrl + Shift + P` (macOS: `Cmd + Shift + P`)

2. 아래 명령어 중 하나 실행
    - `Codespaces: Rebuild Container`
    - `Codespaces: Full Rebuild Container`

3. Codespace가 재시작되며 변경된 설정이 적용됨

---

### ⚖️ Rebuild vs Full Rebuild

| 구분      | Rebuild            | Full Rebuild      |
| --------- | ------------------ | ----------------- |
| 캐시      | 사용               | 사용 안 함        |
| 속도      | 빠름               | 느림              |
| 사용 상황 | 일반적인 설정 변경 | 캐시 문제 발생 시 |

---

### 🌐 웹 Codespaces에서도 동일 적용

브라우저 환경에서도 동일하게  
`Ctrl + Shift + P` → `Rebuild Container` 실행

---

### ⚠️ 주의사항

- Rebuild 시 컨테이너 내부 변경 사항이 초기화될 수 있음
- 반드시 작업 내용을 **commit & push 후 진행**

---

### ✅ 정리

- devcontainer 변경 → 자동 적용 ❌
- Rebuild → 변경 적용 ✔
- 문제 발생 시 → Full Rebuild 사용

## 🖥 Local 환경

### 직접 설치 방법

1. VS Code 실행
2. 확장 메뉴 이동 (`Ctrl + Shift + X`)
3. `C# Dev Kit` 검색 후 설치
4. .NET SDK 설치 안내 진행
5. Solution Explorer 확인

---

### 참고 자료

👉 https://learn.microsoft.com/ko-kr/shows/visual-studio-code/getting-started-with-csharp-dotnet-in-vs-code-official-beginner-guide

---

## ⚖️ Codespaces vs Local 환경 비교

| 항목      | Codespaces | Local   |
| --------- | ---------- | ------- |
| 실행 위치 | 클라우드   | 로컬    |
| 설치      | 자동       | 수동    |
| 환경 공유 | 가능       | 제한적  |
| 성능      | 서버 기반  | PC 의존 |
| 인터넷    | 필수       | 선택    |

---

## ⚙️ 자동 설정 (devcontainer.json)

```json
{
    "name": "C#/.NET Environment",
    "customizations": {
        "vscode": {
            "extensions": ["ms-dotnettools.csdevkit"]
        }
    }
}
```
