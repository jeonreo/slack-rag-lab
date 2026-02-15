# Slack RAG Lab

AI 기능이 포함된 서비스 설계 역량 강화를 위한 실습 프로젝트입니다.

이 프로젝트는 Slack 데이터를 활용한 RAG 기반 Q&A 시스템을 설계하고,
Batch 기반 데이터 수집 구조를 구현하는 것을 목표로 합니다.

---

## 🎯 Project Goal

- RAG 구조 이해
- pgvector 기반 유사도 검색 구현
- Slack 이벤트 및 Web API 연동
- Batch 기반 데이터 수집 설계
- API 모드와 Batch 모드 분리 실행

---

## 🏗 Architecture Overview

### API Mode
Slack → nginx → .NET API  
→ Embedding 생성 → Top-K 검색 → 답변 생성

### Batch Mode
ECS / Docker RunTask  
→ Slack Web API 조회  
→ 메시지 수집  
→ Knowledge Card 저장

---

## 🔑 Core Concepts

- Top-K는 후보 선택
- threshold는 답변 허용 기준
- 실시간 이벤트는 신호
- 데이터 생성은 Batch 기반이 안전
- API와 Batch는 동일 이미지에서 분리 실행

---

## 🚀 Run Locally

### API Mode

```bash
dotnet run

Batch Mode (Dry Run)
dotnet run -- batch ingest --channel <CHANNEL_ID> --windowHours 24 --dryRun true

