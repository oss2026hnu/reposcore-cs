# GitHub Personal Access Token 가이드

## Codespaces 환경에서의 토큰 사용

💡 **Codespaces 환경에서는 별도의 개인 액세스 토큰(PAT) 발급 및 입력이 필요하지 않습니다.**

* **이유:** Codespaces는 GitHub 계정과 연동되어 실행되는 클라우드 개발 환경이므로, 실행 시 내부에 이미 인증 권한을 가진 `GITHUB_TOKEN` 환경 변수가 자동으로 할당되어 있기 때문입니다.
* 따라서 Codespaces 내 터미널에서는 아래의 발급 과정을 생략하고 `--token` 옵션 없이 명령어만 실행해도 정상적으로 API 조회가 가능합니다.

---

## Personal Access Token 발급 방법
 
1. GitHub 우측 상단 프로필 클릭 → `Settings`
2. 좌측 메뉴 최하단의 `Developer settings` 클릭
3. 좌측 메뉴에서 `Personal access tokens` 선택
4. `Tokens (classic)` 또는 `Fine-grained tokens` 선택
5. `Generate new token` 클릭
6. 토큰 이름 (Name), 만료 기간 (Expiration) 설정
7. 토큰 권한 설정
8. 토큰 생성 (Generate token) 후 토큰 값 복사

---

## 토큰 권한 설정 방법
### Tokens (classic) 일 경우

* public 저장소만 조회하는 경우
  → `public_repo`

* private 저장소까지 조회하는 경우
  → `repo`

* 사용자 정보 조회 시
  → `read:user`

---

### Fine-grained 일 경우

* 저장소 읽기 권한만 필요할 시 (저장소 정보, 이슈, PR 조회 등)

  * `Public repositories` 선택 (이 옵션은 자동으로 읽기 권한 부여됨) 후 바로 생성

* 저장소 읽기 권한 이외에도 필요할 시

  * `Only select` 선택하고 접근할 저장소 설정 후 `Add permissions` 에서 필요한 권한 설정

---

## 토큰 복사 후 CLI에 아래처럼 사용

```
dotnet run -- oss2026hnu/reposcore-cs --token 토큰 값
```

---

## 주의사항

* 토큰 값은 한 번만 보여지니 안전한 곳에 복사
* Codespace 환경에서 토큰 값 저장 시에 PR 과정에서 토큰 값도 PR 되지 않게 주의
