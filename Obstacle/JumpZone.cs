using UnityEngine;

/// <summary>
/// 플레이어의 점프력을 증가/감소시키는 존 (트리거 방식)
/// </summary>
public class JumpZone : BaseObstacle
{
    public enum JumpMode
    {
        JumpUp,     // 점프력 증가
        JumpDown    // 점프력 감소
    }

    [Header("점프력 변화 설정")]
    [Tooltip("점프력 증가 또는 감소 모드")]
    [SerializeField] private JumpMode jumpMode = JumpMode.JumpUp;

    [Tooltip("점프 배율 (JumpUp=1.5f, JumpDown=0.5f 등)")]
    [SerializeField, Range(0.1f, 3f)] private float jumpMultiplier = 1.5f;

    [Tooltip("존을 나간 후에도 효과가 유지되는 시간 (초)")]
    [SerializeField] private float effectRemainDuration = 2f;

    private void Reset()
    {
        _senseMode = SenseMode.Trigger; // 기본은 트리거 방식
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        if (_senseMode != SenseMode.Trigger || !enablePlayerCarry || !IsPlayerObject(other.gameObject))
            return;

        var controller = other.GetComponent<PlayerMovementController>();
        if (controller != null)
        {
            float appliedMultiplier = (jumpMode == JumpMode.JumpUp)
                ? Mathf.Max(1f, jumpMultiplier)
                : Mathf.Min(1f, jumpMultiplier);

            controller.EnterJumpModifierZone(appliedMultiplier, effectRemainDuration);
        }
    }

    protected override void OnTriggerExit(Collider other)
    {
        base.OnTriggerExit(other);
        if (_senseMode != SenseMode.Trigger || !enablePlayerCarry || !IsPlayerObject(other.gameObject))
            return;

        var controller = other.GetComponent<PlayerMovementController>();
        if (controller != null)
        {
            controller.ExitJumpModifierZone();
        }
    }

     private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box && box.isTrigger)
        {
            Gizmos.color = new Color(0.64f, 0.64f, 0.64f, 0.45f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.64f, 0.64f, 0.64f, 1f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
