# 매직아일랜드 야외맵 (Map3_MagicIsland)

## 여는 법
- Unity가 이 폴더의 스크립트를 처음 컴파일하면 **자동으로 한 번 생성**됩니다(열어 둔 씬은 건드리지 않음). 끝나면 창이 뜨고 "야외맵 씬 열기"를 누르면 됩니다.
- 씬: `Assets/Maps/LotteWorld/Map3_MagicIsland.unity`
- 실내와 같이 보기: `Tools > LotteWorld > Magic Island > Open Indoor + Outdoor Together` (Map3_LotteWorld + 야외맵 Additive)
- 자동 생성이 안 됐으면: `Tools > LotteWorld > Magic Island > Open Outdoor Scene`

## 실내와 연결
- 야외맵 루트 `MagicIsland` = (-49.95, 0.03, 126.9). 실내 정문(`LotteWorld_NorthGate`) 터널 바깥 끝 = Wall_02 바깥면. 0.03은 실내 바닥판과 겹치는 곳의 깜빡임 방지.
- 팀 블록아웃 `Wall_02`는 통짜 상자라 정문 자리가 막혀 있음 → 실내 씬을 열고 `Tools > LotteWorld > Magic Island > Open Main Gate In Indoor Wall_02` (폭 15.8 m, 높이 8.2 m 개구부. 원본은 꺼 두기만 하고 Undo 가능. 저장은 직접)
- 정문 위치를 바꿨으면 `MagicIsland` 루트만 옮기면 전체가 따라감(마커 `00_Markers/IndoorLink`를 정문 바깥 끝에 맞추기)

## 구조 (씬 `MagicIsland` 아래)
| 그룹 | 내용 |
|---|---|
| 00_Markers | IndoorLink, PlayerSpawn, PreviewCamera |
| 01_Terrain | Ground_Lakeside(호숫가 잔디), Island_Ground(섬 포장), 옹벽 2개, Lawns(잔디 조각) |
| 02_Water | Lake_Surface, Lagoon_Surface, Lake_Bed, 분수 2 (물은 콜라이더 없음, 난간이 막음) |
| 03_Paths | Plaza_Gate, Promenade_Lakeside, Path_*, Road_PetitCars_35, Bridge(본교 + 원뿔 탑 + 41 매점 테라스), Dock_MoonBoat_20 |
| 04_Buildings | Castle(매직캐슬, 가운데 통로 + 19 기념품 가게 방), Village(북쪽 파란 지붕 집 4 + 47 레스토랑), Cottages(49·50 빨간 지붕), Shops(21 메이플, 42 카페) |
| 05_Rides | 36 자이로드롭, 34 자이로스윙, 33 아트란티스(신전 + 트랙), 39 자이로스핀, 37 환타지드림, 38 혜성특급 건물, 35 쁘띠빵빵 차, KidsZone(40·41·42) |
| 06_Facilities | 매점 5, 화장실 3 |
| 07_Landscape | 섬·호숫가 나무, 화단, 생울타리 |
| 08_Props | 가로등, 벤치, 쓰레기통, 파라솔, 난간(Railing_*) |
| 09_Background | 호수 둘레 아파트(콜라이더 없음) |
| 10_Lighting | Sun (씬 조명·안개·환경광은 씬 Lighting 설정에) |

- 반복 요소는 전부 프리팹: `MagicIsland/Prefabs/{Props, Landscape, Facilities, Buildings, Rides}`. 프리팹을 고치면 씬의 모든 사본이 바뀜
- 들어갈 수 있는 건물: 문 `Door_*`(피벗 = 경첩, 로컬 Y 회전, 음수 = 안쪽) + 빈 방. 성 가게, 마을 집, 오두막, 레스토랑, 카페, 혜성특급 로비, 메이플 가게, 스톤 역, 아트란티스 신전 현관, 매점
- 움직이는 부품(정적 아님): `DropRing`(Y 이동), `Swing_Pivot`(로컬 Z 흔들기) → `Disc_Rotor`(로컬 Y 회전), `Spin_Disc`, `Eos_Ring`, `Train`, 쁘띠카, 문보트 배
- 가로등 `LampLight`(점광원)는 꺼져 있음 — 밤 장면에 필요한 것만 켜기

## 고치는 법
- 위치·회전: 씬에서 오브젝트를 그냥 옮기면 됨(씬 저장)
- 숫자로 다시 배치: `LotteWorldMagicIslandLayout.cs`의 `Places`(이름, x, z, 방향, 피하는 반경)·섬/호수 외곽선·길 좌표를 고친 뒤 `Rebuild Scene (keep prefabs)` (씬에서 손으로 옮긴 건 초기화됨)
- 건물 모양: `LotteWorldMagicIslandBuildings.cs` / `...Parts.cs` 고친 뒤 `Rebuild All (overwrite prefabs)` (프리팹 직접 수정분 덮어씀)
- 좌표: 모듈 원점 = 실내 정문 바깥, +X 동, +Z 북(호수 쪽). 안내도 1 px(1100 px 폭 잘라낸 그림) = 0.225 m
- 재질: `MagicIsland/Materials/MI_*.mat` (URP Lit 단색, 물은 반투명). 다른 모듈처럼 나중에 낡은 재질로 구울 수 있음
