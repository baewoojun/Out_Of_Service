# 토끼 괴물 (Bunny Monster) 유니티 패키지 v3

## 들어 있는 파일
- `Model/bunny_monster.fbx` : 리그(뼈 29개), 애니메이션 5개, 메시 2개(LOD)가 한 파일에 들어 있어요.
  - `Bunny_Mesh_LOD0` : 하이폴리, 삼각형 약 30만 개. 가까이서 보이는 모델이에요. 눈구멍, 코, 입, 주름, 봉제선, 털 뭉침을 형태로 깎았어요.
  - `Bunny_Mesh_LOD1` : 로우폴리, 삼각형 약 1.7만 개. 멀리서 보이는 모델이에요. 하이폴리 디테일을 노멀맵으로 구워 넣어서 실루엣 말고는 거의 같아 보여요.
- `Textures/bunny_albedo.png` : 색상 텍스처 (2048×2048). 두 LOD가 같이 써요.
- `Textures/bunny_mask.png` : 마스크맵 (R 금속성 0, G 앰비언트 오클루전, A 매끄러움). 두 LOD가 같이 써요. 털과 얼굴은 거의 무광이고, 피 묻은 살·혀·입안만 살짝 젖은 느낌이에요.
- `Textures/bunny_hp_normal.png` : LOD0용 노멀맵 (하이폴리에서 줄이고 남은 아주 잔 디테일)
- `Textures/bunny_normal.png` : LOD1용 노멀맵 (하이폴리 디테일 전체)
- `Scripts/BunnyMonster.cs` : 몬스터 AI(배회하다 플레이어를 발견하면 비명, 추격, 공격)와 키보드 테스트 모드
- `Scripts/BunnyInput.cs` : 구 Input Manager와 새 Input System을 둘 다 지원해요
- `Scripts/BunnyDemoPlayer.cs` : 데모 씬에서 쓰는 플레이어(캡슐)
- `Editor/BunnyMonsterSetup.cs` : 메뉴 한 번으로 세팅하는 스크립트

## 설치 (Unity 2021.3 LTS 이상 / Built-in, URP, HDRP 모두 지원)
1. 압축을 풀고 `BunnyMonster` 폴더를 유니티 Project 창의 `Assets` 안으로 끌어다 놓아요.
   - 예전 버전을 넣어 둔 적이 있으면 그 폴더를 먼저 지우고 넣어 주세요.
2. 컴파일이 끝나면 상단 메뉴 **Tools > Bunny Monster > 1. Setup**을 실행해요.
   - FBX 임포트 설정(Generic 리그, 클립 이름 정리, Idle·Walk·Run 루프)
   - 지금 쓰는 렌더 파이프라인에 맞는 머티리얼 2개 생성(`BunnyMonster_LOD0_Mat`, `BunnyMonster_LOD1_Mat`), 노멀맵·마스크맵 자동 연결
   - 무광 설정: 환경 반사를 꺼서 얼굴과 눈이 번들거리지 않게 해요
   - LOD Group 설정: 화면 높이의 30% 이상 크게 보이면 하이폴리(LOD0), 그보다 작으면 로우폴리(LOD1)로 자동 전환돼요
   - `BunnyMonster.controller`(Animator)와 `BunnyMonster.prefab` 생성
3. **Tools > Bunny Monster > 2. Create Demo Scene**을 실행하고 Play를 눌러요.
   - WASD나 방향키로 캡슐(플레이어)을 움직이고, Shift로 달려요.
   - 토끼가 배회하다가 플레이어를 보면 비명을 지르고 쫓아와요. 가까워지면 공격하고, 잡히면 Console에 `Caught` 로그가 떠요.
   - 선반 뒤에 숨으면 시야가 막혀서 발견되지 않아요.

## 내 게임에 넣기
- `BunnyMonster.prefab`을 씬에 놓고 Inspector의 **Target**에 플레이어를 넣으면 돼요.
- **onSpotted**(발견하는 순간)와 **onAttackHit**(공격이 닿는 순간, 0.5초) 이벤트에 점프스케어, 사운드, 게임오버를 연결하면 돼요.
- Target을 비워 두면 키보드 테스트 모드가 돼요. WASD 이동, Shift 달리기, Space 공격, R 비명이에요.
- 이동 속도는 걷기 **0.9 m/s**, 달리기 **2.6 m/s**예요. 애니메이션 발걸음에 맞춘 값이라 바꾸면 발이 미끄러져 보일 수 있어요. 바꿀 때는 Animator > Locomotion 블렌드 트리의 Walk·Run 값도 같이 바꿔 주세요.
- 다른 스크립트에서 직접 제어할 때 쓰는 Animator 파라미터: `Speed`(float, m/s), `Attack`(trigger), `Scream`(trigger)
- 실제 맵에서는 `BunnyMonster`의 이동 부분을 NavMeshAgent로 바꾸면 벽을 돌아서 쫓아와요(지금은 직선으로 다가와요).

## 성능 팁 (하이폴리)
- 하이폴리(LOD0)는 가까이 있을 때만 그려지고, 숨어 있는 LOD는 스키닝도 하지 않게 해 뒀어요.
- 토끼를 한 화면에 여러 마리 띄우거나 모바일에서 쓸 때는 프리팹의 **LOD Group**에서 LOD0 구간을 줄이거나(예: 50%), LOD0를 지우고 LOD1만 쓰면 돼요.
- 컷신이나 점프스케어처럼 얼굴을 크게 보여 주는 장면에서는 LOD Group의 **Force LOD**(코드: `GetComponentInChildren<LODGroup>().ForceLOD(0)`)로 하이폴리를 고정하면 좋아요.

## 애니메이션 클립 (30fps, 제자리 동작)
| 클립 | 길이 | 루프 | 내용 |
|---|---|---|---|
| Idle | 3.0초 | O | 구부정하게 숨 쉬다가 고개를 홱 꺾어요 |
| Walk | 1.33초 | O | 오른쪽 다리를 끌며 걸어요 |
| Run | 0.8초 | O | 팔을 앞으로 뻗고 달려들어요 |
| Attack | 1.4초 | X | 팔을 들어 올렸다가 덮쳐요(0.5초에 타격) |
| Scream | 2.0초 | X | 고개를 젖히고 팔을 벌린 채 몸을 떨어요 |

## 문제 해결
- 모델이 분홍색이에요: 렌더 파이프라인을 바꿨다면 **1. Setup**을 다시 실행해 주세요.
- 아직 번들거려요: 씬의 Reflection Probe나 Skybox가 아주 밝으면 그럴 수 있어요. 머티리얼의 Smoothness 배율(URP `Smoothness`, Built-in `Gloss Map Scale`)을 0.5로 낮춰 보세요.
- 가까이 갔는데 로우폴리로 보여요: 프리팹의 LOD Group에서 LOD0 경계(기본 30%)를 낮춰 주세요.
- 크기가 이상해요: Setup이 키를 약 2.1m로 자동 보정해요.
- 새 Input System만 쓰는 프로젝트에서도 그대로 동작해요.
- Humanoid로 쓰고 싶어요: 뼈 이름이 Mixamo 방식이라 Rig를 Humanoid로 바꾸면 자동으로 매핑돼요. 다만 귀와 꼬리는 Humanoid 리타깃 대상이 아니라서 마스크 설정이 따로 필요해요. 그래서 기본값은 Generic으로 해 뒀어요.
