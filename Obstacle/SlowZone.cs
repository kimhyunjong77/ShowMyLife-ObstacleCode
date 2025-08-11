using UnityEngine;

public class SlowZone : BaseObstacle
{
    [Header("슬로우존 옵션")]
    [Range(0.1f, 1f)] public float moveSpeedMultiplier = 0.7f;
    [Tooltip("존 이탈 후 서서히 복구 시간(초, 0이면 즉시 복구)")]
    public float restoreDuration = 2.0f;

    // 인스펙터에서 항상 Trigger만 선택되도록 강제
    private void Reset()
    {
        _senseMode = SenseMode.Trigger;
    }

    /// <summary>
    /// 플레이어가 슬로우존에 진입하면 감속 적용 (중첩/복구 관리)
    /// </summary>
    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (_senseMode != SenseMode.Trigger) return;
        if (!enablePlayerCarry) return;

        if (IsPlayerObject(other.gameObject))
        {
            var controller = other.GetComponent<PlayerMovementController>();
            if (controller != null)
                controller.EnterSlowZone(moveSpeedMultiplier, restoreDuration);
        }
    }

    /// <summary>
    /// 플레이어가 슬로우존에서 나가면 복구 시작
    /// </summary>
    protected override void OnTriggerExit(Collider other)
    {
        base.OnTriggerExit(other);

        if (_senseMode != SenseMode.Trigger) return;
        if (!enablePlayerCarry) return;

        if (IsPlayerObject(other.gameObject))
        {
            var controller = other.GetComponent<PlayerMovementController>();
            if (controller != null)
                controller.ExitSlowZone();
        }
    }
}
