using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputObstacle : BaseObstacle
{
    [Header("구름연기 이펙트(필수)")]
    [Tooltip("플레이어가 밟으면 나오는 구름 이펙트 오브젝트")]
    [SerializeField] private GameObject _smokeEffect;
    [Tooltip("구름연기 유지 시간(초)")]
    [SerializeField] private float _smokeDuration = 1.5f;

    private Coroutine _smokeCoroutine;

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
        if (IsPlayerObject(collision.gameObject))
        {
            ShowSmokeEffect();
        }
    }

    private void ShowSmokeEffect()
    {
        if (_smokeEffect == null) return;

        // 이전 타이머가 돌고 있으면 취소 (남은 시간 상관없이 새로 연출)
        if (_smokeCoroutine != null)
        {
            StopCoroutine(_smokeCoroutine);
        }
        _smokeEffect.SetActive(true);
        _smokeCoroutine = StartCoroutine(HideSmokeAfterDelay());
    }

    private IEnumerator HideSmokeAfterDelay()
    {
        yield return new WaitForSeconds(_smokeDuration);
        _smokeEffect.SetActive(false);
        _smokeCoroutine = null;
    }
}
