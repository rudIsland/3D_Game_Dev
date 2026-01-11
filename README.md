# 🚀 3D Game System Refactoring & Optimization

> **AI 기반 아키텍처 개편 및 데이터 중심 설계를 통한 성능 최적화**
> **프로젝트 기간:** 2025.12.09 ~ 2026.01.11

---

## 📌 1. Project Overview
기존의 복잡한 로직을 AI를 활용한 유지보수성 중심의 아키텍처로 개편하고, 오브젝트 풀링 및 데이터 최적화를 통해 저사양 환경에서도 안정적인 프레임을 확보한 리팩토링 프로젝트입니다.

---

## 📊 2. Refactoring Summary

| 항목 | 기존 방식 (Before) | 개선 방식 (After) | 개선 효과 |
| :--- | :--- | :--- | :--- |
| **AI 구조** | Behavior Tree (BH 트리) | **FSM (State 패턴)** | 로직 직관성 확보, 공격 패턴 확장성 강화 |
| **데미지 로직** | 단순 뺄셈 (Atk - Def) | **방어력 효율 공식** | 최소 데미지 보장, 밸런스 붕괴 방지 |
| **데이터 관리** | 하드코딩 변수 | **ScriptableObject (SO)** | 데이터 중심 설계, 메모리 효율 극대화 |
| **메모리 최적화** | 실시간 생성/파괴 | **Object Pooling** | 가비지 컬렉션 부하 감소 및 렉 방지 |
| **저장 시스템** | 단순 변수 기록 | **JSON + Build Index** | 저장 안정성 확보 및 스마트 이어하기 구현 |
| **성능 (FPS)** | 45 ~ 49 FPS | **Avg 70 FPS** | **약 50% 성능 향상 성공** |

---

## 🛠 3. Key Technical Highlights

### 🧠 3.1 AI Architecture: FSM (Finite State Machine)

* **구현 내용**: 몬스터의 상태(Idle, Move, Attack, Hit, Dead)를 클래스로 분리하여 관리하는 State 패턴 적용.
* **이유**: 액션 게임 특성상 애니메이션 타이밍과 로직이 정밀하게 맞물려야 하므로, 상태 전이가 명확한 FSM이 유지보수에 유리함.
* **성과**: 부위별 타격 판정 및 세분화된 공격 패턴을 코드 수정 없이 유연하게 추가 가능.

### ⚔️ 3.2 Advanced Damage System
방어력이 공격력보다 높을 때 데미지가 0이 되는 현상을 방지하고, 합리적인 밸런스를 위해 비율 감쇄 공식을 도입했습니다.
* **Damage Formula**: 
  $$Final Damage = Atk \times \left( \frac{100}{100 + Def} \right)$$
* **Body-part Damage**: 몬스터의 부위별(Head, Body, Leg) 콜라이더 분리 및 데미지 배율 차등 적용.
* **Level Scaling**: 플레이어 성장에 맞춰 새로 스폰되는 적의 스탯을 동적으로 상향하여 긴장감 유지.

### 💾 3.3 Data-Driven Design (SO & JSON)

* **ScriptableObject (SO)**: 스탯 프리셋, 스테이지 정보, 저장 데이터 등을 에셋화하여 데이터 관리 편의성 증대.
* **JSON Serialization**: SO 데이터를 물리 파일(`.json`)로 저장하여 게임 종료 후에도 영구적으로 데이터 보존.
* **Smart Continue**: 저장된 `Build Index`를 검증하여 유효한 전투 스테이지에서만 게임 재개 가능하도록 설계.

### ⚡ 3.4 Performance Optimization (Object Pooling)

* **Object Pooling**: 빈번하게 생성/파괴되는 몬스터와 이펙트를 풀(Pool)에서 재사용하여 CPU 부하 최적화.
* **Event System**: 매 프레임 수치를 확인하던 `Update` 로직을 `Action` 기반 이벤트 시스템으로 변경하여 UI 연산 낭비 제거.

---

## 📈 4. Performance Metrics
리팩토링 후 유니티 프로파일러 측정 결과:

* **Frame Rate**: 기존 45 FPS 대역에서 **평균 70 FPS**로 비약적 향상.
* **Memory Usage**: SO 기반 데이터 공유를 통해 메모리 점유율 최적화.
* **Maintainability**: 구조화된 FSM 덕분에 신규 몬스터 추가 시간이 기존 대비 약 50% 단축.

---

## 🛠 5. Tech Stack
* **Engine**: Unity 2022.3+
* **Language**: C#
* **Main Patterns**: State Pattern, Object Pooling, Observer Pattern, Singleton
* **Data Format**: JSON, ScriptableObjects
