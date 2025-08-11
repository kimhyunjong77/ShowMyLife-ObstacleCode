using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShooterObstacle : BaseObstacle
{
    [Header("투사체 발사 설정")]
    [Tooltip("발사 위치 (Transform)")]
    [SerializeField] private Transform _shootPoint;

    [Tooltip("발사 간격(초)")]
    [SerializeField] private float _shootInterval = 2f;

    [Tooltip("투사체 속도")]
    [SerializeField] private float _projectileSpeed = 12f;

    [Tooltip("투사체 발사 방향(로컬)")]
    [SerializeField] private Vector3 _shootDirection = Vector3.forward;

    [Tooltip("정지/재시작에 사용")]
    [SerializeField] private bool _isPaused = false;

    private float _timer = 0f;

    private void Start()
    {
        _timer = 0f;
    }

    private void Update()
    {
        if (_isPaused) return;

        _timer += Time.deltaTime;
        if (_timer >= _shootInterval)
        {
            _timer = 0f;
            FireProjectile();
        }
    }

    /// <summary>
    /// 투사체 1발 발사
    /// </summary>
    private void FireProjectile()
    {
        if (_shootPoint == null)
        {
            //Debug.LogWarning("[ShooterObstacle] _shootPoint is null.");
            return;
        }

        // 풀에서 꺼내기 (없으면 Purge 후 재시도 → 그래도 없으면 로그)
        GameObject proj = ObjectPool.Get("Projectile");
        if (proj == null)
        {
            Debug.LogWarning("[ShooterObstacle] Pool returned null. Attempting purge & retry.");
            ObjectPool.PurgeDestroyed();
            proj = ObjectPool.Get("Projectile");

            if (proj == null)
            {
                //Debug.LogError("[ShooterObstacle] Still null after purge. Check prefab registration/path.");
                return;
            }
        }

        // 위치/회전 세팅
        proj.transform.SetPositionAndRotation(_shootPoint.position, _shootPoint.rotation);

        // 속도 세팅
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = _shootPoint.TransformDirection(_shootDirection.normalized) * _projectileSpeed;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            //Debug.LogWarning("[ShooterObstacle] Projectile has no Rigidbody.");
        }
    }

    /// <summary>
    /// 외부에서 일시 정지/재개
    /// </summary>
    public void SetPaused(bool paused)
    {
        _isPaused = paused;
    }
}
