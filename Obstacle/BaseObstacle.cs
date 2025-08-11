using UnityEngine;

public class BaseObstacle : MonoBehaviour
{
    public enum SenseMode { Collision, Trigger }
    [Header("플레이어 감지 옵션")]
    [Tooltip("플레이어 감지 방식을 선택하세요 (Collision/Trigger)")]
    [SerializeField] protected SenseMode _senseMode = SenseMode.Collision;
    [Tooltip("플레이어 감지 시 장애물 동작에 반영할지 여부")]
    [SerializeField] protected bool enablePlayerCarry = true;

    protected Transform _playerOnPlatform;
    protected Rigidbody _playerRigidbody;

    // --- Collision 방식 감지 ---
    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (_senseMode != SenseMode.Collision) return;
        if (!enablePlayerCarry) return;
        if (IsPlayerObject(collision.gameObject))
        {
            _playerOnPlatform = collision.transform;
            _playerRigidbody = collision.gameObject.GetComponent<Rigidbody>();
        }
    }

    protected virtual void OnCollisionExit(Collision collision)
    {
        if (_senseMode != SenseMode.Collision) return;
        if (!enablePlayerCarry) return;
        if (IsPlayerObject(collision.gameObject))
        {
            if (_playerOnPlatform == collision.transform)
            {
                _playerOnPlatform = null;
                _playerRigidbody = null;
            }
        }
    }

    // --- Trigger 방식 감지 ---
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (_senseMode != SenseMode.Trigger) return;
        if (!enablePlayerCarry) return;
        if (IsPlayerObject(other.gameObject))
        {
            _playerOnPlatform = other.transform;
            _playerRigidbody = other.GetComponent<Rigidbody>();
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (_senseMode != SenseMode.Trigger) return;
        if (!enablePlayerCarry) return;
        if (IsPlayerObject(other.gameObject))
        {
            if (_playerOnPlatform == other.transform)
            {
                _playerOnPlatform = null;
                _playerRigidbody = null;
            }
        }
    }

    // 플레이어 오브젝트 감별 (태그/구조 모두 대응)
    protected virtual bool IsPlayerObject(GameObject obj)
    {
        // "Player" 태그 + 본체/자식 모두 허용
        if (obj.CompareTag("Player")) return true;
        if (obj.transform.parent != null && obj.transform.parent.CompareTag("Player")) return true;
        return false;
    }

    /// <summary>
    /// 플레이어가 장애물 위에 올라와있는지 판정
    /// </summary>
    protected bool IsPlayerOnPlatform()
    {
        if (!enablePlayerCarry) return false;
        return _playerOnPlatform != null && _playerRigidbody != null;
    }

    protected Transform GetPlayerOnPlatform() => enablePlayerCarry ? _playerOnPlatform : null;
    protected Rigidbody GetPlayerRigidbody() => enablePlayerCarry ? _playerRigidbody : null;
}
