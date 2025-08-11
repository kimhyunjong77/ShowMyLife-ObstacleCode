using UnityEngine;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 플레이어가 부딪히면 반대 방향으로 튕겨내는 장애물
/// </summary>
public class BouncyObstacle : BaseObstacle
{
    [Header("튕겨내기 설정")]
    [Tooltip("튕겨내는 힘의 크기")]
    [SerializeField] private float bounceForce = 10f;

    [Tooltip("튕겨낼 때 Y축 방향으로 추가되는 힘 (양수면 위로, 음수면 아래로)")]
    [SerializeField] private float upwardForce = 2f;

    [Tooltip("튕겨내는 효과가 지속되는 시간 (초)")]
    [SerializeField] private float bounceDuration = 0.5f;

    [Tooltip("튕겨내는 동안 플레이어 입력 무시 정도 (0: 완전 제어 가능, 1: 완전 제어 불가)")]
    [SerializeField] private float inputReduction = 0.8f;

    [Tooltip("튕겨내기 효과 종료 시 감속 시간 (초)")]
    [SerializeField] private float decelerationDuration = 0.3f;

    [Tooltip("튕겨내기 쿨타임 (초)")]
    [SerializeField] private float cooldownTime = 0.5f;

    [Tooltip("튕겨내기 효과 곡선 (비어있으면 기본 OutQuad 사용)")]
    [SerializeField] private AnimationCurve bounceCurve;

    [Header("플레이어 입력 제어")]
    [Tooltip("플레이어 입력을 비활성화할지 여부")]
    [SerializeField] private bool disablePlayerInput = true;

    [Tooltip("플레이어 입력을 비활성화하는 시간 (초)")]
    [SerializeField] private float inputDisableDuration = 0.5f;

    [Header("사운드 설정")]
    [Tooltip("튕김 사운드 재생 여부")]
    [SerializeField] private bool playBounceSound = true;

    [Tooltip("튕김 사운드 볼륨 (0~1)")]
    [SerializeField] private float bounceSoundVolume = 1.0f;

    [Tooltip("튕김 사운드 쿨타임 (초) - 너무 자주 재생되지 않도록")]
    [SerializeField] private float soundCooldownTime = 3.0f;

    // 현재 활성화된 트윈 저장용
    private Tween _currentBounceTween;
    // 마지막 튕겨내기 시간
    private float _lastBounceTime = -10f;
    // 마지막 사운드 재생 시간
    private float _lastSoundTime = -10f;
    // SoundManager 참조
    private SoundManager _soundManager;

    private void Start()
    {
        // 효과 곡선이 비어있으면 기본 곡선 생성
        if (bounceCurve == null || bounceCurve.keys.Length == 0)
        {
            bounceCurve = new AnimationCurve(
                new Keyframe(0, 1, 0, -2),
                new Keyframe(1, 0, 0, 0)
            );
        }

        // SoundManager 참조 가져오기
        if (GameManager.Instance != null)
        {
            _soundManager = GameManager.Instance.SoundManager;
        }
    }

    /// <summary>
    /// 플레이어가 접촉 시 튕겨내기 효과 적용
    /// </summary>
    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);

        if (!collision.gameObject.CompareTag("Player")) return;

        // 쿨타임 체크
        if (Time.time - _lastBounceTime < cooldownTime) return;
        _lastBounceTime = Time.time;

        // 튕김 사운드 재생
        PlayBounceSound();

        // 플레이어 컴포넌트 가져오기
        Player player = collision.gameObject.GetComponent<Player>();
        if (player == null)
            player = collision.gameObject.GetComponentInParent<Player>();
        if (player == null) return;

        // --- [핵심] 튕겨낼 방향 계산 ---
        // 1. 장애물과 플레이어의 중심 상대 위치 (Y축 무시)
        Vector3 delta = player.transform.position - transform.position;
        Vector2 delta2D = new Vector2(delta.x, delta.z);

        Vector3 bounceDir = Vector3.zero;
        if (Mathf.Abs(delta2D.x) > Mathf.Abs(delta2D.y))
        {
            // X축 방향(좌/우)으로 튕김
            bounceDir = delta2D.x > 0 ? Vector3.right : Vector3.left;
        }
        else
        {
            // Z축 방향(앞/뒤)으로 튕김
            bounceDir = delta2D.y > 0 ? Vector3.forward : Vector3.back;
        }

        // 위쪽 힘 추가 (upwardForce가 0이면 옆방향만)
        Vector3 finalBounceDir = (bounceDir + Vector3.up * upwardForce).normalized;

        // 기존 트윈이 있으면 중단
        if (_currentBounceTween != null && _currentBounceTween.IsActive())
            _currentBounceTween.Kill();

        // 플레이어 속도 초기화 (선택적)
        player.MovementController.Rigidbody.velocity = Vector3.zero;

        // 플레이어 입력 비활성화 (옵션에 따라)
        if (disablePlayerInput && player.InputReader != null)
            StartCoroutine(DisablePlayerInputTemporarily(player.InputReader));

        // 플레이어에게 슬라이드(튕겨나감) 효과 적용
        player.MovementController.ActivateObstacleSlide(finalBounceDir, bounceForce, bounceDuration, inputReduction);

        // DOTween을 사용하여 힘을 서서히 감소시키며 슬라이드 효과 적용
        _currentBounceTween = DOVirtual.Float(bounceForce, 0f, bounceDuration, (force) => {
            player.MovementController.UpdateObstacleSlideSpeed(force);
        }).SetEase(bounceCurve)
        .OnComplete(() => {
            player.MovementController.DeactivateObstacleSlide();
        });

        // (디버그용) 실제 튕겨낸 방향과 힘 확인
        //Debug.Log($"튕김 방향: {finalBounceDir}, 힘: {bounceForce}, Upward: {upwardForce}");
    }

    /// <summary>
    /// 튕김 사운드 재생
    /// </summary>
    private void PlayBounceSound()
    {
        // 사운드 재생이 비활성화되어 있으면 무시
        if (!playBounceSound) return;

        // 개별 쿨타임 체크 제거 (SoundManager에서 전역 관리)
        // if (Time.time - _lastSoundTime < soundCooldownTime) return;
        // _lastSoundTime = Time.time;

        // SoundManager가 있으면 사운드 재생 (쿨타임은 SoundManager에서 관리)
        if (_soundManager != null)
        {
            _soundManager.PlaySFX(SfxType.Bounce, bounceSoundVolume);
            //Debug.Log($"[BouncyObstacle] 튕김 사운드 재생 요청 (볼륨: {bounceSoundVolume})");
        }
        else
        {
            //Debug.LogWarning("[BouncyObstacle] SoundManager를 찾을 수 없어 사운드를 재생할 수 없습니다.");
        }
    }

    /// <summary>
    /// 일정 시간 동안 플레이어 입력을 비활성화합니다
    /// </summary>
    private IEnumerator DisablePlayerInputTemporarily(InputReader inputReader)
    {
        inputReader.DisableInput();
        //Debug.Log($"플레이어 입력 비활성화: {inputDisableDuration}초 동안");

        yield return new WaitForSeconds(inputDisableDuration);

        inputReader.EnableInput();
        //Debug.Log("플레이어 입력 다시 활성화됨");
    }

    private void OnDestroy()
    {
        // 안전하게 트윈 정리
        if (_currentBounceTween != null && _currentBounceTween.IsActive())
        {
            _currentBounceTween.Kill();
            _currentBounceTween = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 튕겨내기 효과 범위를 시각화
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, 0.5f);

        // 콜라이더가 있으면 그 크기도 표시
        Collider collider = GetComponent<Collider>();
        if (collider != null && collider is BoxCollider boxCollider)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.1f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        }
    }
}
