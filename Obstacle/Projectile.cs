using UnityEngine;
using System.Collections;
using DG.Tweening;

public class Projectile : MonoBehaviour
{
    [Header("투사체 밀림 설정")]
    [Tooltip("플레이어에게 가할 힘의 크기")]
    [SerializeField] private float pushForce = 10f;

    [Tooltip("Y축(수직) 추가 힘")]
    [SerializeField] private float upwardForce = 2f;

    [Tooltip("밀림 지속 시간")]
    [SerializeField] private float pushDuration = 0.4f;

    [Tooltip("입력 저감(1=완전 불가, 0=완전 가능)")]
    [SerializeField] private float inputReduction = 0.8f;

    [Tooltip("밀림 종료 후 투사체 삭제까지의 딜레이(초)")]
    [SerializeField] private float destroyDelayAfterPush = 0.0f;

    [Tooltip("자동 삭제 시간(초)")]
    [SerializeField] private float lifeTime = 3f;

    [Tooltip("힘 감소 커브(없으면 기본 커브 세팅)")]
    [SerializeField] private AnimationCurve pushCurve;

    [Tooltip("플레이어 입력 비활성화")]
    [SerializeField] private bool disablePlayerInput = true;

    [Tooltip("입력 비활성화 시간")]
    [SerializeField] private float inputDisableDuration = 0.3f;

    [Header("사운드 설정")]
    [Tooltip("투사체 충돌 사운드 재생 여부")]
    [SerializeField] private bool playProjectileSound = true;

    [Tooltip("투사체 충돌 사운드 볼륨 (0~1)")]
    [SerializeField] private float projectileSoundVolume = 1.0f;

    [Tooltip("투사체 충돌 사운드 쿨타임 (초)")]
    [SerializeField] private float soundCooldownTime = 0.2f;

    private Tween _currentPushTween;
    private bool _hasPushed = false; // 1회만 발동
    private Coroutine _autoDestroyCoroutine;
    private PlayerMovementController _pushedPlayerController;

    // 마지막 사운드 재생 시간
    private float _lastSoundTime = -10f;

    // SoundManager 참조(없으면 사운드 스킵)
    private SoundManager _soundManager;

    // 오브젝트가 활성화될 때 초기화
    private void OnEnable()
    {
        _hasPushed = false;
        _pushedPlayerController = null;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 자동 삭제 타이머 시작
        _autoDestroyCoroutine = StartCoroutine(AutoDestroyTimer());
    }

    private void Start()
    {
        // 감속이 자연스러운 커브 기본값 제공 (인스펙터에서 없을 때만 세팅)
        if (pushCurve == null || pushCurve.keys.Length == 0)
        {
            pushCurve = new AnimationCurve(
                new Keyframe(0f, 1f),     // 시작(100%)
                new Keyframe(0.1f, 0.6f), // 아주 빠르게 감소
                new Keyframe(0.3f, 0.25f),
                new Keyframe(0.6f, 0.08f),
                new Keyframe(1f, 0f)      // 끝(멈춤)
            );
        }

        // SoundManager 참조 가져오기(없으면 null)
        if (GameManager.Instance != null)
        {
            _soundManager = GameManager.Instance.SoundManager;
        }
    }

    /// <summary>
    /// 자동 삭제용 타이머 시작
    /// </summary>
    private IEnumerator AutoDestroyTimer()
    {
        yield return new WaitForSeconds(lifeTime);
        ReturnToPool();
    }

    /// <summary>
    /// 플레이어와 충돌 시 밀어내기 + 입력 제한
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (_hasPushed) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        Player player = collision.gameObject.GetComponent<Player>()
                     ?? collision.gameObject.GetComponentInParent<Player>();
        if (player == null) return;

        _hasPushed = true;

        // 사운드 재생(쿨타임 적용)
        PlayProjectileSound();

        // 기존 자동 삭제 해제
        if (_autoDestroyCoroutine != null)
        {
            StopCoroutine(_autoDestroyCoroutine);
            _autoDestroyCoroutine = null;
        }

        _pushedPlayerController = player.MovementController;

        // 투사체 진행 방향(velocity)로 밀기 (y는 upwardForce 적용)
        var selfRb = GetComponent<Rigidbody>();
        Vector3 pushDir = (selfRb != null) ? selfRb.velocity : transform.forward;
        pushDir.y = 0f;
        if (pushDir.sqrMagnitude < 0.01f)
            pushDir = transform.forward;

        pushDir.Normalize();
        Vector3 finalPushDir = (pushDir + Vector3.up * upwardForce).normalized;

        // 이전 트윈 제거
        _currentPushTween?.Kill();

        // 플레이어 속도 초기화
        if (player.MovementController != null && player.MovementController.Rigidbody != null)
            player.MovementController.Rigidbody.velocity = Vector3.zero;

        // 입력 잠시 제한 (선택)
        if (disablePlayerInput && player.InputReader != null)
            StartCoroutine(DisablePlayerInputTemporarily(player.InputReader));

        // 밀림 효과 시작
        _pushedPlayerController?.ActivateObstacleSlide(finalPushDir, pushForce, pushDuration, inputReduction);

        // DOTween을 통한 점진적 힘 감소
        _currentPushTween = DOVirtual.Float(pushForce, 0f, pushDuration, (force) =>
        {
            _pushedPlayerController?.UpdateObstacleSlideSpeed(force);
        })
        .SetEase(pushCurve)
        .OnComplete(() =>
        {
            _pushedPlayerController?.DeactivateObstacleSlide();
            StartCoroutine(DestroyAfterDelay(destroyDelayAfterPush)); // <- 인스펙터 값 사용
        });
    }

    /// <summary>
    /// 밀림 종료 후 일정 시간 후 삭제
    /// </summary>
    private IEnumerator DestroyAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        ReturnToPool();
    }

    /// <summary>
    /// 플레이어 입력 일시 제한 코루틴
    /// </summary>
    private IEnumerator DisablePlayerInputTemporarily(InputReader inputReader)
    {
        inputReader.DisableInput();
        yield return new WaitForSeconds(inputDisableDuration);
        inputReader.EnableInput();
    }

    /// <summary>
    /// 오브젝트 풀로 반환 (삭제 대신 재사용)
    /// </summary>
    private void ReturnToPool()
    {
        _currentPushTween?.Kill();
        _currentPushTween = null;

        _pushedPlayerController?.DeactivateObstacleSlide();
        _pushedPlayerController = null;

        ObjectPool.Return("Projectile", gameObject);
    }

    /// <summary>
    /// 투사체 충돌 사운드 재생(쿨타임 적용)
    /// </summary>
    private void PlayProjectileSound()
    {
        if (!playProjectileSound) return;

        // 쿨타임 체크
        if (Time.time - _lastSoundTime < soundCooldownTime) return;
        _lastSoundTime = Time.time;

        if (_soundManager != null)
        {
            _soundManager.PlaySFX(SfxType.Projectile, projectileSoundVolume);
        }
    }
}
