# C# 개발을 위한 .NET 설치 및 프로젝트 구성 가이드

## 1. .NET SDK 설치
- **다운로드:** [.NET 공식 다운로드 페이지](https://dotnet.microsoft.com/download)
- **권장 버전:** .NET 10.0 SDK (LTS) — 2028년 11월까지 지원

### Codespaces (Linux) 환경

우리 프로젝트는 주로 Codespaces(리눅스) 환경에서 작업하므로, 리눅스 기준으로 .NET SDK 설치 방법을 정리합니다.

먼저 패키지 목록을 최신 상태로 갱신합니다.
```bash
sudo apt update
```
그 다음 최신버전 .NET 10.0 SDK를 설치합니다.
```bash
sudo apt install -y dotnet-sdk-10.0
```
### 설치 확인
설치가 완료되면 아래 명령어로 정상 설치 여부를 확인할 수 있습니다.
```bash
dotnet --version
```
정상적으로 설치되었다면 `10.x.x` 형태의 버전 번호가 출력됩니다.

---

## 2. 신규 프로젝트 생성 (CLI 방식)

터미널 명령어를 사용하여 프로젝트의 기초 뼈대를 생성할 수 있습니다.

콘솔 애플리케이션을 생성합니다.
```bash
dotnet new console -n [프로젝트명]
```
(예: `dotnet new console -n MyConsoleApp`)

생성된 프로젝트 폴더로 이동합니다.
```bash
cd [프로젝트명]
```

---
## 3. `.csproj` 파일로 프로젝트 구성하기

`.csproj` 파일은 C# 프로젝트의 설정 정보를 담는 파일입니다.
대상 프레임워크, 출력 형식 등 기본 설정을 포함합니다.

예시:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

## 4. 기본 디렉토리 구조 예시
일반적인 C# 프로젝트는 아래와 같은 구조로 구성될 수 있습니다.
``` text
MyApp/
├─ MyApp.csproj
├─ Program.cs
├─ bin/
└─ obj/
```
- `Program.cs` : 프로그램 시작 파일
- `.csproj` : 프로젝트 설정 파일
- `bin/` : 빌드 결과물 폴더
- `obj/` : 빌드 중간 파일 폴더

4.1 실제 개발 시 확장된 디렉토리 구조
앞서 살펴본 기본 디렉토리 구조는 프로젝트 생성 직후의 최소 구성입니다.  
하지만 실제 개발에서는 하나의 `Program.cs` 파일에 모든 기능을 작성하기보다,  
기능별로 파일을 분리하여 관리하는 것이 유지보수와 협업에 유리합니다.

> ⚠️ **아래 내용은 참고용 예시일 뿐, 현재 프로젝트의 실제 구조를 나타내지 않습니다.**  
> 실제 프로젝트 구조는 시간이 지남에 따라 변경될 수 있으므로, 현재 저장소의 파일을 직접 확인하세요.

예를 들어 기능이 추가되면 다음과 같은 구조로 확장할 수 있습니다.
```bash
src/
├─ Program.cs
├─ Services/
│ └─ GitHubService.cs
├─ Data/
│ └─ ScoreCalculator.cs
├─ Utils/
│ └─ OutputFormatter.cs
```
위의 구조에 따른 분리된 디렉토리는 다음과 같은 기능들을 포함합니다.
- Services/ : GitHub API 통신, 외부 데이터 수집 관련 클래스
- Data/ : 점수 계산, 데이터 처리 관련 클래스
- Utils/ : 공통 보조 함수 및 출력 포맷 관련 클래스

이 프로젝트에서 향후 구현될 기능을 기준으로 보면 다음과 같이 분리할 수 있습니다.

- GitHub API 통신 → `Services/GitHubService.cs`
- 점수 계산 로직 → `Data/ScoreCalculator.cs`
- CLI 출력 처리 → `Utils/OutputFormatter.cs`
- 전체 실행 흐름 → `Program.cs`

위 구조에 따른 예시 흐름
1. `Program.cs`에서 사용자 입력을 받는다
2. `GitHubService`를 통해 데이터를 가져온다
3. `ScoreCalculator`로 점수를 계산한다
4. `OutputFormatter`로 결과를 출력한다

## 5. 빌드 및 실행 방법
프로젝트 폴더에서 아래 명령어를 사용합니다.

코드를 빌드(오류 체크)합니다.
```bash
dotnet build
```
프로그램을 실행합니다.
```bash
dotnet run
```

## 6. NuGet 기반 패키지 관리
6.1 NuGet이란?
NuGet은 .NET에서 사용하는 패키지(라이브러리) 관리 시스템입니다.
외부 라이브러리를 프로젝트에 쉽게 추가할 수 있으며, 버전 및 의존성 관리를 자동으로 처리합니다.
또한 `dotnet add package` 명령어는 내부적으로 NuGet을 사용하여 패키지를 다운로드하고 프로젝트에 등록합니다.
더 자세하게 설명을 한다면, .NET CLI에서 사용하는 `dotnet add package` 명령어는 내부적으로 NuGet을 사용하여 패키지를 다운로드하고  
`.csproj` 파일에 자동으로 의존성을 추가합니다.

6.2 패키지 설치 방법
터미널에서 다음 명령어를 사용하여 패키지를 추가할 수 있습니다.

```bash
dotnet add package <패키지명>
```

예시:
```bash
dotnet add package Octokit
dotnet add package Cocona
```

6.3 .csproj 파일 반영 방식
패키지를 설치하면 .csproj 파일에 다음과 같이 자동으로 추가됩니다.
```xml
<ItemGroup>
  <PackageReference Include="Octokit" Version="x.x.x" />
</ItemGroup>
```
.csproj 파일은 프로젝트의 의존성을 관리하는 핵심 파일입니다.

6.4 패키지 버전 관리

특정 버전을 설치하려면:

```bash
dotnet add package Octokit --version 0.50.0
```

패키지를 최신 버전으로 업데이트하려면:

```bash
dotnet add package Octokit
```

또는 .csproj 파일에서 직접 버전을 수정할 수도 있습니다.


6.5 패키지 복원 (Restore)

```bash
dotnet restore
```
- .csproj 파일을 기준으로 필요한 패키지를 다시 다운로드합니다.
- 팀원이 동일한 환경을 구성할 때 사용됩니다.


6.6 현재 프로젝트 예시

```bash
dotnet add package Octokit.GraphQL --prerelease
dotnet add package Cocona
```

> **참고:** 현재 프로젝트는 GitHub GraphQL API를 주력으로 사용하므로 `Octokit.GraphQL`을 설치합니다.  

6.7 정리
- 패키지는 dotnet add package로 설치합니다.
- 의존성은 .csproj 파일에서 관리됩니다.
- dotnet restore로 환경을 재현할 수 있습니다.
- 팀원 간 동일한 개발 환경 유지가 가능합니다.

## 7. 자동화 테스트 도구

CLI 프로그램은 옵션 조합에 따라 실행 경로가 달라질 수 있으므로, 실제 실행까지 검사할 필요가 있습니다.  
아래는 기능 추가/변경 후 테스트 시 테스트를 좀 더 쉽게 할 수 있도록 도와주는 프레임워크들에 대한 가이드입니다.

---

### 7.1 테스트 도구 후보

| 도구 | 설명 |
|---|---|
| xUnit | .NET에서 많이 사용되는 오픈소스 테스트 프레임워크 |
| NUnit | 다양한 테스트 기능을 제공하는 오래된 테스트 프레임워크 |
| MSTest | Microsoft에서 제공하는 테스트 프레임워크 |

세 도구 모두 테스트 프로젝트를 만들고 `dotnet test` 명령으로 실행할 수 있습니다.

---

### 7.2 테스트 프로젝트 생성

테스트 도구에 따라 다음 명령어 중 하나를 사용하여 테스트 프로젝트를 생성할 수 있습니다.

xUnit:

```bash
dotnet new xunit -n RepoScore.Tests
```

NUnit:

```bash
dotnet new nunit -n RepoScore.Tests
```

MSTest:

```bash
dotnet new mstest -n RepoScore.Tests
```

테스트 프로젝트에서 실제 프로젝트 코드를 사용할 수 있도록 참조를 추가합니다.

```bash
dotnet add RepoScore.Tests/RepoScore.Tests.csproj reference reposcore-cs.csproj
```

현재 디렉토리에 `.csproj` 파일이 하나만 있는 경우에는 다음 명령어로 테스트를 실행할 수 있습니다.
테스트 실행:

```bash
dotnet test
```

단, 현재 디렉토리에 여러 .csproj 파일이 존재하는 경우에는 실행할 테스트 프로젝트를 명시해야 합니다.
특정 테스트 프로젝트만 실행:

```bash
dotnet test RepoScore.Tests/RepoScore.Tests.csproj
```

---

### 7.3 기본 테스트 코드 예시

xUnit 기준 기본 테스트 코드는 다음과 같습니다.

```csharp
using Xunit;

namespace RepoScore.Tests;

public class BasicTests
{
    [Fact]
    public void SampleTest()
    {
        int result = 1 + 1;

        Assert.Equal(2, result);
    }
}
```

`[Fact]`는 하나의 테스트를 의미하고,  
`Assert.Equal(2, result)`는 `result` 값이 `2`인지 확인하는 코드입니다.

NUnit이나 MSTest를 선택한 경우에는 테스트 속성 이름이 조금 다릅니다.

| 도구 | 단일 테스트 | 여러 입력값 테스트 |
|---|---|---|
| xUnit | `[Fact]` | `[Theory]`, `[InlineData]` |
| NUnit | `[Test]` | `[TestCase]` |
| MSTest | `[TestMethod]` | `[DataTestMethod]`, `[DataRow]` |

---

### 7.4 CLI 실행 테스트 예시

CLI 프로그램은 테스트 코드에서 실제 명령어를 실행해 볼 수 있습니다.

```csharp
using System.Diagnostics;
using Xunit;

namespace RepoScore.Tests;

public class CliExecutionTests
{
    [Theory]
    [InlineData("--format=txt")]
    [InlineData("--format=csv")]
    [InlineData("--format=txt --claims=Issue")]
    public async Task Cli_Should_Run_Without_Error(string options)
    {
        var repo = "oss2026hnu/reposcore-cs";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run -- {repo} {options}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        Assert.True(process.ExitCode == 0, error);
    }
}
```

이 테스트는 옵션 조합별로 프로그램을 실행하고, 종료 코드가 `0`인지 확인합니다.

프로젝트 경로를 직접 지정해야 한다면 다음처럼 `--project`를 사용할 수 있습니다.

```csharp
Arguments = $"run --project reposcore-cs.csproj -- {repo} {options}";
```

---

### 7.5 Codespaces에서 xUnit 오류가 나는 경우

Codespaces에서 다음 오류가 발생할 수 있습니다.

```text
The type or namespace name 'Xunit' could not be found
The type or namespace name 'Fact' could not be found
```

`RepoScore.Tests.csproj`에 xUnit 패키지가 있고 테스트 파일 위치도 맞는데 오류가 난다면,  
메인 프로젝트가 테스트 폴더의 `.cs` 파일까지 같이 컴파일하고 있을 수 있습니다.

이 경우 메인 프로젝트의 `.csproj` 파일에 테스트 폴더 제외 설정을 추가합니다.

```xml
<ItemGroup>
  <Compile Remove="RepoScore.Tests/**/*.cs" />
  <EmbeddedResource Remove="RepoScore.Tests/**" />
  <None Remove="RepoScore.Tests/**" />
</ItemGroup>
```

수정 후 다시 실행합니다.

```bash
dotnet clean
dotnet restore
dotnet test RepoScore.Tests/RepoScore.Tests.csproj
```

---

### 7.6 현재 프로젝트 테스트 실행 방법

현재 프로젝트는 `RepoScore.Tests` 테스트 프로젝트를 통해 주요 로직을 검증합니다.

테스트 대상은 다음과 같습니다.

- `Data/ScoreCalculator.cs`: 점수 계산 로직
- `Data/CacheManager.cs`: 캐시 저장, 불러오기, 키워드 비교 로직
- `Services/GitHubService.cs`: 라벨 파싱, 문서 작업 판별, 이슈 종료 사유 파싱 로직

현재 저장소에는 루트 기준으로 다음 프로젝트가 존재합니다.
- `./reposcore-cs.csproj`: 메인 프로젝트
- `./RepoScore.Tests/RepoScore.Tests.csproj`: 테스트 프로젝트

따라서 루트 디렉토리에서 테스트를 실행할 때는 테스트 프로젝트 경로를 명시해야 합니다.

```bash
dotnet test RepoScore.Tests/RepoScore.Tests.csproj
```

테스트 코드는 GitHub API 호출 없이 실행되도록 구성합니다.
GitHub 토큰이나 네트워크 연결이 필요한 기능은 단위 테스트 대상에서 제외하고, 외부 의존성이 없는 데이터 처리 로직을 우선 검증합니다.

`dotnet test RepoScore.Tests/RepoScore.Tests.csproj`명령어를 실행한 테스트 결과는 다음과 같이 나올 수 있습니다.
```bash
Test summary: total: 36, failed: 0, succeeded: 36, skipped: 0, duration: 2.4s
```
해당 결과는 다음을 의미합니다. 
```text
테스트 결과 요약 : 총 테스트: 36개, 실패: 0, 성공: 36, 스킵: 0
```
해당 결과는 모든 테스트가 정상적으로 통과되었음을 의미하며, 현재 프로젝트의 주요 로직이 의도한 대로 동작함을 검증합니다.

---

### 7.7 정리

기본 흐름은 다음과 같습니다.

```text
1. 테스트 도구 선택
2. 테스트 프로젝트 생성
3. 실제 프로젝트 참조 추가
4. 테스트 코드 작성
5. dotnet test 실행
```
