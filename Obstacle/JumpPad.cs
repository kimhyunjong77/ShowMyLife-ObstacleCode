using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPad : BaseObstacle
{
    [Header("점프 패드 옵션")]
    [SerializeField] private float jumpForce = 30f;
    [SerializeField] private Vector3 forceDirection = Vector3.up;
    [SerializeField] private float padCooldown = 0.3f;

    [Header("사운드 설정")]
    [Tooltip("점프 패드 사운드 재생 여부")]
    [SerializeField] private bool playJumpPadSound = true;

    [Tooltip("점프 패드 사운드 볼륨 (0~1)")]
    [SerializeField] private float jumpPadSoundVolume = 1.0f;

    [Tooltip("점프 패드 사운드 쿨타임 (초) - 너무 자주 재생되지 않도록")]
    [SerializeField] private float soundCooldownTime = 0.2f;

    [Tooltip("점프 패드 사용 후 착지 사운드 억제 시간 (초)")]
    [SerializeField] private float suppressLandSoundDuration = 1.0f;

    private float _lastActivateTime = -10f;
    // 마지막 사운드 재생 시간
    private float _lastSoundTime = -10f;
    // SoundManager 참조
    private SoundManager _soundManager;

    private void Start()
    {
        // SoundManager 참조 가져오기
        if (GameManager.Instance != null)
        {
            _soundManager = GameManager.Instance.SoundManager;
        }
    }

    // Collision 방식만 처리
    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);

        // 내 방식대로 처리
        if (_senseMode != SenseMode.Collision) return;
        if (!enablePlayerCarry) return;
        if (!IsPlayerObject(collision.gameObject)) return;

        // 여러 접촉점 검사
        foreach (var contact in collision.contacts)
        {
            Vector3 padUp = transform.up;
            Vector3 contactNormal = contact.normal;

            // padUp(장애물 위쪽)과 -contactNormal(플레이어 아래쪽)이 비슷해야 '위에서 밟았다'
            float dot = Vector3.Dot(padUp, -contactNormal);

            // 60도 이내, 즉 거의 위에서 밟았을 때만
            if (dot > 0.7f)
            {
                // 쿨타임 체크
                if (Time.time - _lastActivateTime < padCooldown) return;
                _lastActivateTime = Time.time;

                // 점프 패드 사운드 재생
                PlayJumpPadSound();

                // 플레이어에게 착지 사운드 억제 신호 전달
                var player = collision.gameObject.GetComponent<Player>();
                if (player != null && player.AnimationEventHandler != null)
                {
                    player.AnimationEventHandler.SuppressLandSound(suppressLandSoundDuration);
                }

                var playerMoveCtrl = collision.gameObject.GetComponent<PlayerMovementController>();
                if (playerMoveCtrl != null)
                {
                    // 오로지 위에서만 점프!
                    playerMoveCtrl.ExternalJump(forceDirection.normalized * jumpForce);
                }
                break;
            }
        }
    }

    /// <summary>
    /// 점프 패드 사운드 재생
    /// </summary>
    private void PlayJumpPadSound()
    {
        // 사운드 재생이 비활성화되어 있으면 무시
        if (!playJumpPadSound) return;

        // 개별 쿨타임 체크 제거 (SoundManager에서 전역 관리)
        // if (Time.time - _lastSoundTime < soundCooldownTime) return;
        // _lastSoundTime = Time.time;

        // SoundManager가 있으면 사운드 재생 (쿨타임은 SoundManager에서 관리)
        if (_soundManager != null)
        {
            _soundManager.PlaySFX(SfxType.JumpPad, jumpPadSoundVolume);
            //Debug.Log($"[JumpPad] 점프 패드 사운드 재생 요청 (볼륨: {jumpPadSoundVolume})");
        }
        else
        {
            //Debug.LogWarning("[JumpPad] SoundManager를 찾을 수 없어 사운드를 재생할 수 없습니다.");
        }
    }
}
