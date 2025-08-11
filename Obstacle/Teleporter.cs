using System.Collections;
using UnityEngine;

public class Teleporter : BaseObstacle
{
    [Header("텔레포트 설정")]
    [Tooltip("플레이어를 위치시킬 피벗 (캐비넷 내부 기준)")]
    [SerializeField] private Transform _pivot;
    [Tooltip("텔레포트 도착 캐비넷 (A만 지정, B는 null)")]
    [SerializeField] private Teleporter _targetCabinet;
    [Tooltip("문 오브젝트(회전 연출용, A만 할당)")]
    [SerializeField] private Transform _door;
    [Tooltip("문 열림(시작) 각도"), SerializeField] private float _doorOpenAngle = -90f;
    [Tooltip("문 닫힘 각도"), SerializeField] private float _doorCloseAngle = 0f;
    [Tooltip("문 애니메이션 속도"), SerializeField] private float _doorSpeed = 3f;
    [Tooltip("문 닫힘 후 대기 시간"), SerializeField] private float _holdDelay = 0.8f;

    [Header("텔레포트 쿨타임(초)")]
    [SerializeField] private float _cooldown = 1.0f;

    private float _lastTeleportTime = -999f;
    private bool _isBusy = false;

    protected override void OnTriggerEnter(Collider other)
    {
        if (_senseMode != SenseMode.Trigger) return;
        if (_isBusy) return;
        if (!IsPlayerObject(other.gameObject)) return;
        if (_targetCabinet == null) return; // B는 동작하지 않음

        // 쿨타임 체크 (텔레포트 완료 후 일정 시간 동안 무시)
        if (Time.time < _lastTeleportTime + _cooldown) return;

        // 플레이어 찾기
        var player = other.GetComponent<Player>() ?? other.GetComponentInParent<Player>();
        if (player == null) return;

        _isBusy = true;
        _lastTeleportTime = Time.time;
        StartCoroutine(TeleportSequence(player));
    }

    /// <summary>
    /// 플레이어를 고정, 문 연출, 텔레포트, 문 다시 열기
    /// </summary>
    private IEnumerator TeleportSequence(Player player)
    {
        // 1. 입력 막고, 위치 고정
        if (player.InputReader != null) player.InputReader.DisableInput();
        if (player.MovementController != null) player.MovementController.ResetMovement();
        player.transform.SetPositionAndRotation(_pivot.position, _pivot.rotation);

        // 2. 문 닫기 애니메이션
        yield return RotateDoorCoroutine(_doorOpenAngle, _doorCloseAngle);
        yield return new WaitForSeconds(_holdDelay);

        // 3. 텔레포트(도착 캐비넷에서 연출 없음)
        _targetCabinet.ReceiveTeleport(player);

        // 4. 문 다시 열기 애니메이션
        yield return RotateDoorCoroutine(_doorCloseAngle, _doorOpenAngle);

        // 5. 쿨타임 동안 재진입 방지
        yield return new WaitForSeconds(_cooldown);

        _isBusy = false;
    }

    /// <summary>
    /// 도착 캐비넷에서는 연출 없이 플레이어 위치만 고정
    /// </summary>
    public void ReceiveTeleport(Player player)
    {
        //Debug.Log("ReceiveTeleport 실행, 위치 이동 시도");

        if (player.InputReader != null) player.InputReader.DisableInput();
        if (player.MovementController != null) player.MovementController.ResetMovement();

        // 1. CharacterController 우선 비활성화
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 2. Rigidbody 있는 경우에는 MovePosition + 속도 초기화
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.MovePosition(_pivot.position);
            player.transform.rotation = _pivot.rotation;
        }
        else
        {
            player.transform.SetPositionAndRotation(_pivot.position, _pivot.rotation);
        }

        // 3. CharacterController 재활성화 (있었던 경우)
        if (cc != null) cc.enabled = true;

        // 4. (권장) 트리거 안에 남지 않도록 약간 앞으로 이동
        // player.transform.position += player.transform.forward * 0.5f;
        // 필요 시 활성화해서 쓸 것!

        if (player.InputReader != null) player.InputReader.EnableInput();

        //Debug.Log($"텔레포트 완료. 현재좌표: {player.transform.position}");
    }

    /// <summary>
    /// 문 회전 애니메이션 코루틴 (A 캐비넷만)
    /// </summary>
    private IEnumerator RotateDoorCoroutine(float fromAngle, float toAngle)
    {
        if (_door == null) yield break;
        float t = 0f;
        float duration = Mathf.Max(0.01f, Mathf.Abs(toAngle - fromAngle) / (_doorSpeed * 90f));
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float angle = Mathf.Lerp(fromAngle, toAngle, t);
            _door.localRotation = Quaternion.Euler(0, angle, 0);
            yield return null;
        }
        _door.localRotation = Quaternion.Euler(0, toAngle, 0);
    }
}
