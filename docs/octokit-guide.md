# Octokit.NET 활용 가이드

[Octokit 라이브러리 공식 홈페이지](https://github.com/octokit)

> C# 환경에서 GitHub API를 사용하기 위한 라이브러리인 Octokit.NET의 기본 사용법입니다.  
> 저장소 이슈 조회, Pull Request 조회 등 GitHub 정보 연동 기능 구현 전에 기본 구조를 이해하는 데 목적이 있습니다.
> 본 프로젝트에서는 GitHub GraphQL API 기반으로 데이터를 조회합니다.
> 다만 Octokit.NET은 GitHub REST API를 .NET 환경에서 사용하기 위한 클라이언트 라이브러리이므로, 아래 REST 관련 내용은 기본 이해 및 참고용으로 다룹니다.

## 1. Octokit.NET 설치 방법 (GitHub REST API 참고용)

Octokit.NET은 GitHub 공식 .NET API 라이브러리입니다.

### 특징

* GitHub API를 C#에서 쉽게 호출 가능
* 이슈, Pull Request, 커밋, 저장소 정보 조회 가능
* 인증 토큰 연동 지원
* 비동기 API 기반

### 설치 방법

아래 명령어를 터미널에 입력합니다.

```bash
dotnet add package Octokit
```

---

## 2. GitHub REST API 접근을 위한 기본 설정 (Octokit.NET 참고용)

GitHub API를 사용하려면 먼저 `GitHubClient` 객체를 생성해야 합니다.

### 기본 설정

```csharp
using Octokit;

var client = new GitHubClient(new ProductHeaderValue("reposcore-app"));
```

### 설명

* `ProductHeaderValue`에는 프로그램 이름을 지정합니다.
* 공개 저장소 조회는 인증 없이 가능합니다.

### 인증 설정

GitHub Personal Access Token이 필요한 경우 다음과 같이 설정합니다.

```csharp
client.Credentials = new Credentials("YOUR_GITHUB_TOKEN");
```

---

## 추가 정보

### Rate Limit 확인

GitHub API는 요청 횟수 제한이 있으므로 현재 제한 상태를 확인할 수 있습니다.

```csharp
var rateLimits = await client.Miscellaneous.GetRateLimits();
Console.WriteLine(rateLimits.Resources.Core.Remaining);
```

### 참고 사항

* 공개 저장소는 인증 없이 일부 조회 가능
* 비공개 저장소는 토큰 인증 필요
* 많은 요청 시 API 제한 발생 가능

---

## GraphQL 활용 가이드 (Octokit 환경)


[Octokit.GraphQL.NET 공식 저장소](https://github.com/octokit/octokit.graphql.net)

> C# 환경에서 Octokit 기반으로 GraphQL API를 사용하는 방법입니다.
> GraphQL의 기본 문법 및 개념은 공식 문서를 참고하고, 여기서는 설치 및 사용 방법 중심으로 다룹니다.

---

## 1. 안내

GraphQL의 개념, 문법, 쿼리 작성 방법은 아래 공식 문서를 참고합니다.

* GraphQL 기본 튜토리얼: https://graphql.org/learn/
* GitHub GraphQL API 문서: https://docs.github.com/en/graphql

이 문서에서는 **Octokit 환경에서 GraphQL을 사용하는 방법만 설명합니다.**

---

## 2. 설치 방법

GraphQL을 사용하려면 `Octokit.GraphQL` 패키지를 설치해야 합니다.

```bash
dotnet add package Octokit.GraphQL --prerelease
```

### 설명

* `Octokit.GraphQL`은 GitHub GraphQL API를 위한 .NET 라이브러리입니다.
* 현재 베타(pre-release) 버전으로 제공됩니다.

---

## 3. 기본 설정

```csharp
using Octokit;
using Octokit.GraphQL;

var connection = new Connection(
    new ProductHeaderValue("reposcore-app"),
    "YOUR_GITHUB_TOKEN"
);
```

### 설명

* `ProductHeaderValue` : 어플리케이션 이름
* `YOUR_GITHUB_TOKEN` : GitHub Personal Access Token

-> GitHub 토큰 필요

---

## 4. 사용 예시

### 4.1 저장소 이슈 조회

```csharp
using Octokit;
using Octokit.GraphQL;

var connection = new Connection(
    new ProductHeaderValue("reposcore-app"),
    "YOUR_GITHUB_TOKEN"
);

var query = new Query()
    .Repository("owner", "repo-name")
    .Issues(first: 5)
    .Nodes
    .Select(issue => new
    {
        issue.Number,
        issue.Title
    });

var issues = await connection.Run(query);

foreach (var issue in issues)
{
    Console.WriteLine($"이슈 번호: {issue.Number} - {issue.Title}");
}
```

---

### 4.2 Pull Request 조회

```csharp
using Octokit;
using Octokit.GraphQL;

var connection = new Connection(
    new ProductHeaderValue("reposcore-app"),
    "YOUR_GITHUB_TOKEN"
);

var query = new Query()
    .Repository("owner", "repo-name")
    .PullRequests(first: 5)
    .Nodes
    .Select(pr => new
    {
        pr.Number,
        pr.Title
    });

var pullRequests = await connection.Run(query);

foreach (var pr in pullRequests)
{
    Console.WriteLine($"PR 번호: {pr.Number} - {pr.Title}");
}
```

---

## 5. 참고 사항

* `Octokit.GraphQL`은 기존 `Octokit`과 별도의 패키지입니다.
* GraphQL은 문자열이 아닌 **C# 코드 형태로 쿼리를 작성**합니다.
* 필요한 데이터만 선택해서 가져올 수 있습니다.

---

## 병렬 비동기 처리 (Parallel Async Processing)

### 순차 실행 vs 병렬 실행

GraphQL 쿼리를 여러 개 실행할 때, 순차 실행은 각 요청이 완료된 후 다음 요청을 보내므로 느릴 수 있습니다.
`Task.WhenAll`을 사용하면 여러 쿼리를 동시에 실행하여 성능을 높일 수 있습니다.

```csharp
// 순차 실행 (느림)
var issues = await connection.Run(issueQuery);
var pullRequests = await connection.Run(prQuery);

// 병렬 실행 (빠름)
var issueTask = connection.Run(issueQuery);
var prTask = connection.Run(prQuery);
var (issues, pullRequests) = await Task.WhenAll(issueTask, prTask);
```

### Task.WhenAll 사용 예시

```csharp
using Octokit.GraphQL;

var connection = new Connection(
    new ProductHeaderValue("reposcore-app"),
    "YOUR_GITHUB_TOKEN"
);

var issueQuery = new Query()
    .Repository("owner", "repo-name")
    .Issues(first: 5)
    .Nodes
    .Select(issue => new { issue.Number, issue.Title });

var prQuery = new Query()
    .Repository("owner", "repo-name")
    .PullRequests(first: 5)
    .Nodes
    .Select(pr => new { pr.Number, pr.Title });

// 두 쿼리를 동시에 실행
var results = await Task.WhenAll(
    connection.Run(issueQuery),
    connection.Run(prQuery)
);

var issues = results[0];
var pullRequests = results[1];
```

### 주의사항

- **예외 처리:** `Task.WhenAll`은 모든 Task가 완료될 때까지 기다립니다. 하나라도 실패하면 예외가 발생하므로 `try-catch`로 감싸야 합니다.
- **API Rate Limit:** 동시에 너무 많은 요청을 보내면 GitHub API의 Rate Limit에 걸릴 수 있습니다. 요청 수를 적절히 조절하세요.
- **응답 순서:** `Task.WhenAll`에 전달한 순서대로 결과가 반환됩니다.
