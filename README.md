# SlackRagBot

Slack 메시지에서 승인된 내용을 Knowledge Card로 적재하고, RAG 검색으로 질문에 답하는 Web API

## 구성 요약
- Api: HTTP 라우팅, DI, Slack Events 수신, batch 모드 엔트리
- Application: CQRS 유스케이스, MediatR Handler, PipelineBehavior
- Domain: 포트와 모델
- Infrastructure: Postgres Npgsql, Slack Web API, OpenAI 어댑터

## 주요 기능
- Ask: DB에 저장된 카드에서 벡터 검색 후 답변 생성
- Reindex: embedding 없는 카드에 embedding 생성 후 업데이트
- Batch ingest: 최근 N시간 메시지 수집 후 카드로 적재
- Slack approval: 특정 리액션이 달리면 해당 메시지를 카드로 적재
- Slack signature verification: Slack 서명 검증으로 요청 위조 방지
- 중복 방지: source_url unique 인덱스 + ON CONFLICT

## 사전 준비
- .NET SDK
- Docker Desktop
- Slack App
  - Bot Token
  - Signing Secret
  - Event Subscriptions 설정

## 환경변수
필수
- SLACK_BOT_TOKEN
- SLACK_SIGNING_SECRET
- ConnectionStrings__RagDb

예시
- ConnectionStrings__RagDb
  - Host=localhost;Port=5432;Database=ragdb;Username=rag;Password=ragpw

## 로컬 실행
### 1) DB 실행
docker compose를 사용하는 경우
- docker compose up -d

DB 접속정보 예시
- POSTGRES_USER=rag
- POSTGRES_PASSWORD=ragpw
- POSTGRES_DB=ragdb

### 2) API 실행
PowerShell 예시

- $env:SLACK_BOT_TOKEN="xoxb-..."
- $env:SLACK_SIGNING_SECRET="..."
- $env:ConnectionStrings__RagDb="Host=localhost;Port=5432;Database=ragdb;Username=rag;Password=ragpw"
- dotnet run --project src/SlackRag.Api

Swagger
- /swagger

## Batch 실행
최근 N시간 메시지 ingest

PowerShell 예시
- dotnet run --no-launch-profile --project src/SlackRag.Api -- batch ingest --channel C0HFT4M0D --windowHours 6 --dryRun true
- dotnet run --no-launch-profile --project src/SlackRag.Api -- batch ingest --channel C0HFT4M0D --windowHours 6 --dryRun false

중복 방지 검증 예시
- dotnet run --no-launch-profile --project src/SlackRag.Api -- batch ingest --channel C0HFT4M0D --windowHours 1 --dryRun false
- dotnet run --no-launch-profile --project src/SlackRag.Api -- batch ingest --channel C0HFT4M0D --windowHours 1 --dryRun false

기대 결과
- 첫 번째 inserted가 0 이상
- 두 번째 inserted는 0

## API 엔드포인트
- POST /ask
- POST /admin/reindex
- POST /slack/events
- GET /checkkey

## Slack Events 설정
1. Event Subscriptions 활성화
2. Request URL을 /slack/events 로 설정
3. URL verification 통과 확인
4. 승인 리액션 allowlist
   - appsettings.json SlackApproval:ApprovedReactions
5. 채널에 봇 초대

## 테스트
- dotnet test SlackRag.sln

## 트러블슈팅
- Slack API missing_scope
  - 채널 타입에 맞는 scope 추가 후 reinstall
- Slack API channel_not_found
  - 채널 ID 확인
  - 봇이 채널에 초대되어 있는지 확인
- Postgres connection refused
  - docker compose up -d
  - 포트 매핑 확인
- ON CONFLICT 오류 42P10
  - source_url unique 인덱스 생성 경로가 batch에서도 실행되는지 확인

## 보안
- /slack/events는 Slack signature verification 적용
- 운영에서는 SLACK_SIGNING_SECRET을 환경변수로 관리

## 운영 메모
- 승인 이벤트는 카드 insert만 수행
- embedding 생성은 /admin/reindex로 별도 수행
