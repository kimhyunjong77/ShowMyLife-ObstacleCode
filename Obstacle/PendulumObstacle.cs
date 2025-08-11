using UnityEngine;
using DG.Tweening;

public class PendulumObstacle : BaseObstacle
{
    [System.Flags]
    public enum RotationAxis
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4
    }

    [Header("회전 설정")]
    [Tooltip("회전할 축 (여러 축 동시 선택 가능)")]
    [SerializeField] private RotationAxis _rotationAxis = RotationAxis.Z;
    [Tooltip("X축 회전 각도")]
    [SerializeField] private float xSwingAngle = 70f;
    [Tooltip("Y축 회전 각도")]
    [SerializeField] private float ySwingAngle = 70f;
    [Tooltip("Z축 회전 각도")]
    [SerializeField] private float zSwingAngle = 70f;
    [Tooltip("한 쪽 끝에서 반대쪽까지 왕복하는 데 걸리는 시간")]
    [SerializeField] private float swingDuration = 1.5f;

    private void Start()
    {
        StartSwing();
    }

    private void StartSwing()
    {
        Vector3 targetAngle = Vector3.zero;

        // 선택된 각 축에 대해 회전 각도 설정
        if ((_rotationAxis & RotationAxis.X) != 0)
            targetAngle.x = xSwingAngle;
        
        if ((_rotationAxis & RotationAxis.Y) != 0)
            targetAngle.y = ySwingAngle;
        
        if ((_rotationAxis & RotationAxis.Z) != 0)
            targetAngle.z = zSwingAngle;

        // 왕복 진자운동 (끝에서 느려졌다가 다시 빨라짐)
        transform.DORotate(targetAngle, swingDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }
}
