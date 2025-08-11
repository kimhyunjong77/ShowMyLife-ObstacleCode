using UnityEngine;
using DG.Tweening;

public enum RotationAxis
{
    X, Y, Z
}

public class RotatingObstacle : BaseObstacle
{
    [Header("회전 설정")]
    [SerializeField] private RotationAxis _rotationAxis = RotationAxis.Y;
    [Tooltip("초당 회전 각도")]
    [SerializeField] private float _rotationSpeed = 90f;
    [Tooltip("시계 방향 회전 여부")]
    [SerializeField] private bool _clockwise = true;

    private Quaternion _lastRotation;
    private float _currentAngle = 0f;

    private void Start()
    {
        _lastRotation = transform.rotation;
        StartRotating();
    }

    private void FixedUpdate()
    {
        Quaternion currentRotation = transform.rotation;
        Quaternion deltaRotation = currentRotation * Quaternion.Inverse(_lastRotation);

        if (IsPlayerOnPlatform())
        {
            Transform player = GetPlayerOnPlatform();
            Rigidbody rb = GetPlayerRigidbody();
            if (player != null && rb != null)
            {
                Vector3 dir = player.position - transform.position;

                // 중심에 딱 올라간 경우를 방지(아주 약간의 오프셋)
                if (dir.magnitude < 0.01f)
                {
                    dir = Vector3.forward * 0.01f; // ※ 축이 Y축일 때 자연스러운 오프셋, 필요시 더 정교화 가능
                }

                Vector3 newPos = transform.position + deltaRotation * dir;
                rb.MovePosition(newPos);
            }
        }

        _lastRotation = currentRotation;
    }

    private Vector3 GetRotationAxis()
    {
        switch (_rotationAxis)
        {
            case RotationAxis.X: return Vector3.right;
            case RotationAxis.Y: return Vector3.up;
            case RotationAxis.Z: return Vector3.forward;
            default: return Vector3.up;
        }
    }

    private void StartRotating()
    {
        float direction = _clockwise ? 1f : -1f;
        Vector3 axis = GetRotationAxis() * direction;

        DOTween.To(() => _currentAngle, x =>
        {
            _currentAngle = x % 360f;
            transform.localRotation = Quaternion.AngleAxis(_currentAngle, axis.normalized);
        },
        360f,
        360f / _rotationSpeed)
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Incremental)
        .SetUpdate(UpdateType.Fixed);
    }
}
