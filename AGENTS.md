# AGENTS.md

이 파일은 이 저장소에서 AI 에이전트(Codex, Copilot 등)가
코드를 수정하거나 생성할 때 반드시 따라야 할 규칙을 정의한다.

AI는 아래 규칙을 반드시 준수해야 한다.

---

# 1. 프로젝트 개요

이 프로젝트는 Slack 기반 RAG (검색 증강 생성) 시스템이다.

전체 데이터 흐름:

Slack → 수집(Ingestion) → PII 마스킹 → 지식카드 생성 → 임베딩 생성 → pgvector(PostgreSQL) 저장 → 벡터 검색 → LLM 답변 생성 → 사용자 응답

기술 스택:
- .NET (ASP.NET Core)
- PostgreSQL + pgvector
- OpenAI API (Embedding + Chat)
- AWS (운영 환경)
- Slack API

---

# 2. 보안 규칙 (필수)

## 2.1 비밀 정보 하드코딩 금지

AI는 다음을 절대 코드에 직접 작성해서는 안 된다:

- OpenAI API Key
- Slack Bot Token
- DB 비밀번호
- AWS 접근 키

모든 비밀 정보는:
- 환경 변수
- AWS Secrets Manager
에서만 가져와야 한다.

---

## 2.2 개인정보(PII) 처리 규칙

Slack 데이터에는 다음 정보가 포함될 수 있다:

- 이메일
- 회사명
- 주소
- 전화번호
- 토큰 / API 키

AI는 반드시:

1. PiiRedactor.Redact()를 다음 위치에서 적용해야 한다:
   - Slack 데이터 저장 전
   - OpenAI API 호출 전
   - LLM 답변 반환 전

2. 마스킹 로직을 제거하거나 우회해서는 안 된다.
3. Slack 원문을 로그에 남겨서는 안 된다.

---

## 2.3 데이터 저장 원칙

- Slack 원문 전체를 DB에 저장하지 않는다.
- 마스킹된 지식카드만 저장한다.
- Slack thread URL만 저장한다.
- 운영 환경에서는 RDS 암호화 활성화가 전제다.

---

# 3. RAG 로직 규칙

## 3.1 벡터 검색

- distance 기준으로 컨텍스트 필터링해야 한다.
- distance <= 설정값인 카드만 LLM에 전달한다.
- 디버깅을 위해 API 응답에는 전체 hits를 반환해도 된다.

## 3.2 Fallback 규칙

다음 상황에서는 절대 추측 답변을 생성하지 않는다:

- 유효한 컨텍스트가 없는 경우
- contextChunks.Count == 0 인 경우

이때는 반드시 추가 질문을 반환해야 한다.

---

# 4. 코드 구조 규칙

## 4.1 Program.cs

- 엔드포인트 정의만 담당한다.
- 비즈니스 로직은 Helper 클래스에 위임한다.

## 4.2 OpenAiHelper.cs

- Embedding 생성
- Chat Completion 호출
- 재시도 로직
- 보안 처리

## 4.3 PiiRedactor.cs

- 정규식 기반 마스킹
- 개인정보 탐지
- 절대 삭제하거나 비활성화하지 않는다.

---

# 5. AI 코드 수정 규칙

AI는 코드 수정 시 반드시:

- diff 또는 patch 형식으로 변경 사항을 먼저 제시한다.
- 대규모 리팩토링은 사용자 승인 없이 수행하지 않는다.
- 보안 관련 코드는 수정 전 반드시 확인을 요청한다.

---

# 6. 로깅 규칙

- Slack 원문 로그 금지
- API 키 노출 금지
- 오류 메시지에 민감정보 포함 금지

---

# 7. AWS 운영 전제

운영 환경 가정:

- RDS는 Private Subnet
- Public Access 비활성화
- IAM 최소 권한 원칙 적용
- HTTPS 통신만 허용
- Secret은 Secrets Manager 사용

AI는 이 아키텍처를 위반하는 코드를 제안해서는 안 된다.

---

# 8. 답변 포맷 규칙

AI가 생성하는 최종 답변은:

- 간결해야 한다.
- 구조화되어야 한다.
- 가능한 경우 Sources 섹션을 포함해야 한다.
- 추측/환각을 최소화해야 한다.
- 민감 정보가 포함되지 않아야 한다.

---

# 9. 절대 금지 사항

AI는 절대 다음을 수행해서는 안 된다:

- 마스킹 로직 제거
- Slack 원문을 OpenAI로 직접 전송
- 토큰을 DB에 저장
- 보안 검증 로직 비활성화
- 데이터 노출 위험 증가시키는 변경

위 규칙을 위반하는 요청이 있을 경우,
AI는 거부하고 그 이유를 설명해야 한다.

---


# 10. Deployment Rules

- Production deployment must use AWS ECS and RDS in Private Subnet.
- No direct public database exposure.
- Secrets must be loaded from AWS Secrets Manager.
- Slack production tokens must not be stored in code.
- Logging must not contain Slack raw message content.

---

END OF FILE
