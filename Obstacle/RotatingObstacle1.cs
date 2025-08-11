using UnityEngine;
using DG.Tweening;

public enum RotationAxis1
{
    X, Y, Z
}

public class RotatingObstacle1 : BaseObstacle
{
    [Header("회전 설정")]
    [SerializeField] private RotationAxis _rotationAxis = RotationAxis.Y;
    [Tooltip("초당 회전 각도")]
    [SerializeField] private float _rotationSpeed = 90f;
    [Tooltip("시계 방향 회전 여부")]
    [SerializeField] private bool _clockwise = true;

    private float _currentAngle = 0f;

    private void Start()
    {
        StartRotating();
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