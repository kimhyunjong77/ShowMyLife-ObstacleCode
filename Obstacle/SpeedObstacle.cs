using UnityEngine;

/// <summary>
/// 플레이어 이속 증가/감소 발판 (충돌 방식, 한 스크립트에서 토글)
/// </summary>
public class SpeedObstacle : BaseObstacle
{
    public enum SpeedMode
    {
        Fast, // 이속 증가
        Slow  // 이속 감소
    }

    [Header("이속 변화 설정")]
    public SpeedMode speedMode = SpeedMode.Fast;
    [Range(0.1f, 2f)] public float speedMultiplier = 1.3f; // Fast=1.3(30% 증가), Slow=0.7(30% 감소) 등
    [Tooltip("존을 나가도 효과가 남아있는 시간(초)")]
    public float effectRemainDuration = 2f;

    private void Reset()
    {
        _senseMode = SenseMode.Trigger;
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);
        if (_senseMode != SenseMode.Trigger) return;
        if (!enablePlayerCarry) return;
        if (!IsPlayerObject(other.gameObject)) return;

        var controller = other.GetComponent<PlayerMovementController>();
        if (controller != null)
        {
            float appliedMultiplier = (speedMode == SpeedMode.Fast)
                ? Mathf.Max(1f, speedMultiplier)     // 1보다 크면 빠름
                : Mathf.Min(1f, speedMultiplier);    // 1보다 작으면 느림
            controller.EnterSlowZone(appliedMultiplier, effectRemainDuration);
        }
    }

    protected override void OnTriggerExit(Collider other)
    {
        base.OnTriggerExit(other);
        if (_senseMode != SenseMode.Trigger) return;
        if (!enablePlayerCarry) return;
        if (!IsPlayerObject(other.gameObject)) return;

        var controller = other.GetComponent<PlayerMovementController>();
        if (controller != null)
            controller.ExitSlowZone();
    }

     private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box && box.isTrigger)
        {
            Gizmos.color = new Color(0.45f, 0.45f, 0.45f, 0.45f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.45f, 0.45f, 0.45f, 1f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
