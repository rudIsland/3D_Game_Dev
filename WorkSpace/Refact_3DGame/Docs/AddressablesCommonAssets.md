# Addressables Common 그룹 조사와 적용

Ground와 UnderGround 번들에 실제로 중복 포함되던 에셋 603개를 `Common` 그룹에 등록했다. 원본 파일을 이동하거나 복제하지 않았고, 씬 배치와 연결된 참조도 변경하지 않았다.

전체 에셋 경로, GUID, 종류, 참조 출처, 등록 제외 사유와 Start 중복 목록은 [조사 원본](AddressablesCommonAssets.json)에 기록했다. 기록 시각은 JSON의 `generatedUtc`에 있다.

## 조사 방법과 결과

- Unity 6000.3.9f1, Addressables 2.8.1, StandaloneWindows64에서 조사했다.
- Unity AssetDatabase의 재귀 의존성은 Ground 1,578개, UnderGround 971개, Start 134개였다. 두 맵의 교집합은 905개였다.
- 파일 이름이나 폴더 위치로 공용 여부를 판단하지 않았다. `CheckBundleDupeDependencies`의 빌드 의존성 분석으로 두 맵 번들에 중복 포함되는 파일을 확인했다.
- 재귀 의존성의 교집합 중 601개와 빌드 과정에서 추가되는 패키지 대체 셰이더 2개를 합쳐 603개를 등록했다.
- 교집합의 나머지 304개는 실제 번들 중복으로 확인되지 않아 개별 등록하지 않았다. 배치용 프리팹 원본 216개, 스크립트 16개 등이 포함된다. 등록하지 않았다고 씬에서 제거한 것은 아니다.
- 적용 후 같은 중복 분석을 다시 실행한 결과 `No issues found`를 확인했다. 번들 사이 중복 파일 수는 603개에서 0개로 줄었다.

| Common 등록 대상 | 개수 |
| --- | ---: |
| 텍스처 | 285 |
| FBX 모델 | 173 |
| 머티리얼 | 125 |
| 메시 에셋 | 9 |
| 셰이더·Shader Graph | 9 |
| Volume Profile | 1 |
| Lighting Settings | 1 |
| 합계 | 603 |

텍스처에는 공용 라이트맵도 포함된다. 패키지 셰이더는 기존 패키지 에셋을 참조하며 Assets 폴더로 복사하지 않았다. 603은 파일 단위 등록 개수이며 메시 하위 에셋 수, GameObject 수, 메모리 사용량이 아니다.

## 그룹 설정

| 그룹 | 등록 내용 | Address |
| --- | --- | --- |
| Ground | Ground.unity 한 개 | Ground |
| UnderGround | UnderGround.unity 한 개 | UnderGround |
| Common | 검증된 공용 에셋 603개 | 각 에셋의 전체 프로젝트 경로 |

Common은 `Local.BuildPath`, `Local.LoadPath`, `Pack Together`, `LZ4`, `Include in Build` 켜짐으로 구성했다. 기본 그룹은 기존 Ground를 유지했다. 콘텐츠 업데이트 설정도 Ground와 동일하게 유지했다. Common은 그룹 이름이며 새 씬이 아니다.

## 맵 안의 에셋은 어떻게 되는가

입력은 기존 맵 씬과 에셋 참조다. 콘텐츠 빌드 시 공용 원본은 Common 번들에 배치되고, 맵 번들은 그 의존성을 갖는다. Addressables로 맵을 로딩하면 필요한 공용 번들도 함께 로딩된다.

예를 들어 같은 바위를 지상과 지하에 배치했으면 지상 바위 GameObject와 지하 바위 GameObject는 각 씬에 그대로 남는다. 좌표, 회전, Collider, 머티리얼 연결도 그대로다. 공유 모델·머티리얼·텍스처의 번들 소속만 바뀐다. 씬에 배치된 모든 프리팹을 별도 Addressables 인스턴스 생성 코드로 바꾸지 않는다.

한 맵을 해제해도 다른 맵이 공용 번들을 사용하면 유지된다. 모든 Addressables 사용자가 참조를 해제한 뒤 해당 번들을 해제할 수 있다. `Pack Together`는 번들 단위로 수명이 묶이므로 개별 텍스처 하나가 더 이상 필요하지 않다고 즉시 메모리에서 사라지는 것은 아니다. Common을 전부 수동으로 미리 로딩할 필요는 없다.

## Start와의 중복은 별도 문제

`CheckSceneDupeDependencies`로 일반 빌드 씬 Start와 Addressables 번들도 비교했다. 중복 경고 18건이 남아 있다.

- Common 번들과 6건: 조명 베이크 설정 1개, URP Lit·Unlit 및 대체 셰이더 5개.
- Ground 번들과 11건: ScrollItem·BookItem 정의와 아이콘, FleshHitEffect 프리팹, 피격 효과 머티리얼·셰이더·텍스처.
- Unity 내장 셰이더 번들과 1건: `Resources/unity_builtin_extra`.

일반 시작 씬의 직접 참조는 그룹에 등록하는 것만으로 번들 참조로 바뀌지 않는다. 따라서 이번 결과를 프로젝트 전체의 중복이 0이라고 해석하면 안 된다. 해당 중복을 없애려면 Start의 리소스 소유권과 로딩 방법을 별도로 설계해야 한다. 단순히 Start에 공용 에셋 목록을 추가하거나 기존 참조를 제거하지 않았다.

## 검증 범위와 다음 작업

- Common의 603개 GUID, Address, 그룹 소속과 저장된 설정을 Unity API로 재확인했다.
- 기존 씬·프리팹·머티리얼·에셋·메타데이터 2,385개의 SHA-256을 비교했고 변경은 0개였다.
- 빌드 의존성 분석 중 바뀐 열린 씬은 원래 UnderGround로 복원했다. 씬을 저장하거나 Play 모드에 진입하지 않았다.
- 분석 중 기존 TMP 샘플의 구형 API 경고와 비어 있는 PlayMode 테스트 어셈블리 경고가 나왔지만, 두 분석 모두 결과를 생성했다.
- 전체 Addressables 콘텐츠 빌드, 플레이어 빌드, 실제 로딩·해제 및 메모리 측정은 수행하지 않았다. 이번 검증은 빌드 파이프라인의 의존성·번들 배치 분석까지다.
- 위 조사 당시 `WorldObjectContainer`는 SceneManager 기반이었다. 이후 자동 맵 교체 구조를 거쳤으나 현재는 제거하고 `04.Loading/MapContainer`의 씬 캐시·refCount 관리만 남겼다. 현재 사용 방법은 [맵 로딩 안내](../Assets/_Project/04.Loading/README.md)를 따른다.
- 위 검증 수치는 Common 그룹 분리 당시의 결과다. 로더 구현 후의 검증은 별도 기록하며, 콘텐츠 빌드와 플레이어 빌드의 검증 범위를 구분한다.
- 맵이나 패키지, 플랫폼 설정을 바꾸거나 동적 로딩 대상을 추가하면 다시 의존성을 조사한다. 현재 목록은 이후 추가되는 공용 에셋을 자동으로 등록하지 않는다.

## 참고

- [Addressables 2.8 씬 로딩과 의존성](https://docs.unity3d.com/Packages/com.unity.addressables@2.8/manual/LoadingScenes.html)
- [Addressables 메모리 관리](https://docs.unity3d.com/Packages/com.unity.addressables@2.8/manual/MemoryManagement.html)
