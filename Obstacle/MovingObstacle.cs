using UnityEngine;
using DG.Tweening;

public class MovingObstacle : BaseObstacle
{
    [Header("이동 설정")]
    [Tooltip("이동할 거리")]
    [SerializeField] protected Vector3 _moveTo = Vector3.zero;

    [Tooltip("전체 이동 경로를 완료하는 데 걸리는 시간")]
    [SerializeField] protected float _totalPathTime = 4.2f;
    
    [Tooltip("플레이어 이동 시 적용할 힘 배율")]
    [SerializeField] protected float _forceMultiplier = 1.0f;

    protected Vector3 _lastPosition;
    protected Vector3 _startPosition;

    protected virtual void Start()
    {
        _startPosition = transform.position;
        _lastPosition = transform.position;
        StartMoving();
    }

    protected virtual void FixedUpdate()
    {
        Vector3 currentPosition = transform.position;
        Vector3 delta = currentPosition - _lastPosition;

        if (delta != Vector3.zero)
        {
            MovePlayerIfOnTop(delta);
        }

        _lastPosition = currentPosition;
    }

    protected virtual void StartMoving()
    {
        // 왕복 경로 정의 (시작점 -> 목표점 -> 시작점)
        Vector3[] path = new Vector3[]
        {
            _startPosition + _moveTo,  // 목표 위치
            _startPosition             // 시작 위치로 돌아옴
        };

        // 경로를 따라 이동
        transform.DOPath(path, _totalPathTime, PathType.Linear)
            .SetEase(Ease.Linear)  // 일정한 속도로 이동
            .SetLoops(-1, LoopType.Restart)
            .SetUpdate(UpdateType.Fixed);
    }

    // OnCollision 방식으로 변경!
    protected void MovePlayerIfOnTop(Vector3 delta)
    {
        if (IsPlayerOnPlatform())
        {
            Rigidbody rb = GetPlayerRigidbody();
            if (rb != null)
            {
                rb.MovePosition(rb.position + delta * _forceMultiplier);
            }
        }
    }
    
    protected virtual void OnDestroy()
    {
        DOTween.Kill(transform);
    }
}
