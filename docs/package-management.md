# NuGet 패키지 관리 가이드

이 문서는 프로젝트에서 외부 라이브러리(NuGet 패키지)를 추가하고 관리하는 표준 절차를 안내합니다.

## 1. NuGet이란?

**NuGet**은 .NET 환경의 패키지 관리자입니다. 외부 개발자가 공유한 라이브러리를 프로젝트에 손쉽게 통합하고, 의존성을 자동으로 관리해주어 개발 생산성을 높여줍니다.

### 프로젝트에서 NuGet의 역할
- **통합 관리**: 필요한 라이브러리를 중앙 저장소(NuGet.org)에서 가져와 프로젝트에 연결합니다.
- **의존성 해결**: 선택한 패키지가 의존하는 다른 패키지들을 자동으로 찾아 함께 설치합니다.
- **환경 공유**: `.csproj` 파일에 패키지 정보를 기록하여, 다른 팀원도 동일한 패키지 환경을 재현할 수 있게 합니다.

---

## 2. 패키지 설치 방법 (CLI)

터미널에서 `dotnet` 명령어를 사용하여 패키지를 추가할 수 있습니다.

### 기본 명령어
```bash
dotnet add package [패키지명]
```

### 특정 버전 설치
특정 버전이 필요한 경우 `--version` 옵션을 사용합니다.
```bash
dotnet add package Octokit --version 13.0.1
```

---

## 3. `.csproj` 파일 확인

패키지를 설치하면 프로젝트 설정 파일인 `.csproj`에 `<PackageReference>` 항목이 자동으로 추가됩니다.

**예시 (`reposcore-cs.csproj`):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  ...
  <ItemGroup>
    <PackageReference Include="Octokit" Version="13.0.1" />
    <PackageReference Include="Cocona" Version="2.5.0" />
  </ItemGroup>
</Project>
```

계획된 패키지 정보가 `.csproj`에 포함되어 있어야 다른 환경(예: CI/CD, 다른 팀원의 PC)에서 `dotnet restore`나 `dotnet build` 수행 시 자동으로 설치됩니다.

---

## 4. 패키지 업데이트 및 삭제

### 패키지 업데이트
설치된 패키지를 최신 버전으로 업데이트하려면 다시 설치 명령어를 실행하거나 버전을 수정합니다.
```bash
dotnet add package [패키지명]
```

### 패키지 삭제
명령어를 통해 프로젝트에서 패키지를 제거할 수 있습니다.
```bash
dotnet remove package [패키지명]
```

---

## 5. 주요 라이브러리 설치 예시

우리 프로젝트에서 자주 사용하거나 사용할 계획인 라이브러리 설치 방법입니다.

### GitHub API 연동 (Octokit.NET)
GitHub 정보(이슈, PR 등)를 가져올 때 사용합니다.
```bash
dotnet add package Octokit
```

### CLI 프레임워크 (Cocona)
간결하고 강력한 명령줄 인터페이스(CLI)를 구축할 때 사용합니다.
```bash
dotnet add package Cocona
```

---

## 참고 자료
- [NuGet 공식 문서](https://learn.microsoft.com/ko-kr/nuget/)
- [NuGet 저장소 (NuGet.org)](https://www.nuget.org/)
