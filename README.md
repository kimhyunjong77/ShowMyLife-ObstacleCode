# ShowMyLife-ObstacleCode
ShowMyLife-ObstacleCode
플레이어와 상호작용하는 다양한 장애물 기믹 모음입니다.
이동·회전 플랫폼, 튕겨내기, 속도/점프력 변화, 발사체, 텔레포트, 사라지는 발판 등 모든 장애물이 공통 BaseObstacle 구조를 기반으로 동작하며, PlayerMovementController와 연동됩니다.

포함된 스크립트
장애물 시스템
파일명	설명
BaseObstacle.cs	공통 감지(Collision/Trigger), 플레이어 캐리, 플레이어 판별
ChildCollider.cs	자식 콜라이더 이벤트를 부모(BaseObstacle)로 전달
MovingObstacle.cs	시작점↔목표점 왕복 이동 플랫폼
CrossMovingObstacle.cs	십자형 경로 이동 플랫폼
RandomMovingObstacle.cs	왕복 이동 + 랜덤 대기 플랫폼
RotatingObstacle.cs	회전 플랫폼, 플레이어 위치 보정
RotatingObstacle1.cs	단순 회전 오브젝트
PendulumObstacle.cs	시계추/길로틴 회전
BouncyObstacle.cs	충돌 시 방향 계산 후 플레이어 튕겨내기
JumpPad.cs	위에서 밟으면 점프력 부여
JumpZone.cs	점프력 Up/Down 존
SlowZone.cs	속도 감소 존
SpeedObstacle.cs	속도 증가/감소 존
DisappearingObstacle.cs	일정 시간 후 사라졌다 재등장 (투명/SetActive)
InputObstacle.cs	밟으면 이펙트 표시
ShooterObstacle.cs	투사체 발사 장애물
Projectile.cs	발사체 동작 및 플레이어 밀어내기
Teleporter.cs	트리거형 텔레포터(캐비넷 등)
ObjectPool.cs	발사체 풀링 시스템 (Resources 기반, 씬 전환 유지)

주요 기능 설명
1. 감지 & 공통 구조
SenseMode로 Collision / Trigger 방식 선택

플레이어 여부 판별 (IsPlayerObject)

이동/회전 플랫폼 위 캐리 기능 (enablePlayerCarry)

ChildCollider로 서브 콜라이더 감지도 부모로 전달

2. 이동/회전 플랫폼
MovingObstacle, CrossMovingObstacle, RandomMovingObstacle: DOTween 경로 이동, FixedUpdate 동기화

RotatingObstacle: 플레이어 중심 회전 반영

PendulumObstacle: X/Y/Z 축 지정 진자 회전

3. 플레이어 상태 변화
BouncyObstacle / Projectile: 밀어내기 + 입력 제한

JumpPad, JumpZone: 점프력 변화

SlowZone, SpeedObstacle: 속도 변화 (중첩 방지)

4. 특수 효과
DisappearingObstacle: 일정 시간 후 사라지고 재등장

InputObstacle: 밟으면 이펙트 표시

ShooterObstacle: 주기적 투사체 발사

Teleporter: 트리거 진입 시 다른 위치로 이동

5. Object Pool
ObjectPool로 발사체를 사전 생성/반환

씬 전환 시에도 유지 (DontDestroyOnLoad)

죽은 참조 정리(PurgeDestroyed) 지원

코드 아키텍처
모듈화: 모든 장애물은 BaseObstacle 상속, 고유 동작만 개별 구현

PlayerMovementController 연동: API 호출로 속도/점프력/튕김 상태 변경

DOTween 기반: 이동·회전·페이드·흔들림 등 애니메이션 제어

Object Pool 연계: ShooterObstacle → Projectile → ObjectPool

이벤트 기반 캐리 처리: OnCollision/OnTriggerEnter/Exit에서 캐리 정보 저장·해제

기술 스택
Unity 2022.3.17f1

C#

DG.Tweening

URP

Object Pooling (Resources 기반)
