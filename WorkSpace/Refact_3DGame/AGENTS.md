# 프로젝트 에이전트 규칙

공통 규칙 원문은 [Docs/Rules](Docs/Rules/README.md)에 보관한다. 모든 작업 전에 규칙 목록의 읽는 순서와 [마지막 세션 기록](Docs/Work/last-session.md)을 확인한다.

## 코드 수정

수정 전 설명·변경 범위·적용 절차는 [작업 규칙](Docs/Rules/work-rules.md)을 따른다.

## AI 작업 시작

[공통 작업 순서](Docs/Rules/work-rules.md#공통-작업-순서)에 따라 요청 범위와 실제 호출자를 확인한다. 시작·생성·반환 흐름은 [진입점 안내](Assets/_Project/01.Boot/README.md), 책임 배치는 [구조와 객체 소유 규칙](Docs/Rules/architecture.md)을 기준으로 한다. 과거 세션의 미구현 설명을 현재 코드보다 우선하지 않는다.

## 기능 변경과 문서 갱신

현재 구현·연결·검증·남은 작업은 각 기능 폴더의 README.md에 둔다. 기능을 변경한 단계에서 관련 설명을 즉시 수정하며 다음 작업이나 커밋까지 미루지 않는다. [기능 폴더의 현재 상태 갱신](Docs/Rules/work-rules.md#기능-폴더의-현재-상태-갱신)을 따른다.

## 검증

EditMode·PlayMode 테스트 코드 금지와 Cheat 창을 통한 검증 기준은 [작업 규칙](Docs/Rules/work-rules.md)을 따른다.

## Unity C#과 GC

이름·주석·상수·할당·캐시 기준은 [코드 작성 규칙](Docs/Rules/coding-style.md)을 따른다.

## 전역 접근과 객체 소유

전역 접근·컨테이너·현재 맵 관리 책임은 [구조와 객체 소유 규칙](Docs/Rules/architecture.md)을 따른다.

## 이름공간

이름공간은 [코드 작성 규칙](Docs/Rules/coding-style.md), 번호·파일 위치·어셈블리는 [폴더 규칙](Docs/Rules/folder-rules.md)을 따른다.

## 생명주기 커스터마이징

생성·활성화·갱신·비활성화·해제 기준은 [코드 작성 규칙](Docs/Rules/coding-style.md)을 따른다.

## 실습을 위한 답변

입력 → 처리 → 출력 설명과 실습 안내 기준은 [작업 규칙](Docs/Rules/work-rules.md)을 따른다.

## 커밋과 세션 기록

커밋 범위·메시지·포함 파일은 [커밋 규칙](Docs/Rules/commit-rules.md)을 따른다. 작업 마무리 시 [세션 기록](Docs/Work/last-session.md)을 갱신한다. 기록 형식은 [작업 규칙](Docs/Rules/work-rules.md)에 있다.
