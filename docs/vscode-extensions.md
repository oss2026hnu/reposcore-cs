# C# 개발을 위한 VSCode 확장 가이드

## 📌 개요

VSCode에서 C# 개발을 시작할 때 필요한 확장 프로그램과 설치 방법을 안내하는 가이드입니다.  
**GitHub Codespaces(클라우드 환경)** 와 **Local(로컬 환경)** 을 모두 기준으로 설명합니다.

---

## 🧩 공통 확장 (핵심 필수 설치 ⭐)

쾌적한 C# 개발 환경 구축을 위해 다음 3가지 핵심 확장을 설치해야 합니다.  
**C# Dev Kit** 하나만 설치해도 아래 두 항목이 자동으로 함께 설치됩니다.

| 확장 이름 | 게시자 | 역할 |
|---|---|---|
| **C# Dev Kit** | Microsoft | 솔루션 탐색기, 프로젝트 관리, 통합 테스트 환경 제공 |
| **C#** | Microsoft | IntelliSense(자동 완성), 실시간 오류 검사, 디버깅 기능 |
| **.NET Install Tool** | Microsoft | .NET SDK 및 런타임 자동 설치·관리 |

### C# Dev Kit의 주요 기능

- **솔루션 탐색기 지원**: Visual Studio처럼 솔루션(`.sln`) 및 프로젝트(`.csproj`) 단위로 파일과 참조를 직관적으로 관리  
- **고급 언어 서비스**: 향상된 IntelliSense, 코드 분석, AI 기반 코드 추천(IntelliCode) 제공  
- **통합 테스트 환경**: Test Explorer를 통해 xUnit, NUnit, MSTest 기반 단위 테스트를 코드 위에서 바로 실행 및 디버깅 가능  

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

### 방법 1 — UI에서 수동 설치

1. 확장 메뉴 열기 (`Ctrl + Shift + X`)  
2. `C# Dev Kit` 검색  
3. Microsoft 확장 설치  
4. 자동 설치 확인  

---

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

| 항목 | Codespaces | Local |
|---|---|---|
| 실행 위치 | 클라우드 | 로컬 |
| 설치 | 자동 | 수동 |
| 환경 공유 | 가능 | 제한적 |
| 성능 | 서버 기반 | PC 의존 |
| 인터넷 | 필수 | 선택 |

---

## ⚙️ 자동 설정 (devcontainer.json)

```json
{
    "name": "C#/.NET Environment",
    "customizations": {
        "vscode": {
            "extensions": [
                "ms-dotnettools.csdevkit"
            ]
        }
    }
}
