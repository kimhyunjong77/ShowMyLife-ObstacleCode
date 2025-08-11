using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildCollider : MonoBehaviour
{
    private BaseObstacle _parentObstacle;

    private void Awake()
    {
        // 부모에서 BaseObstacle 찾기 (최상위까지)
        _parentObstacle = GetComponentInParent<BaseObstacle>();
        //if (_parentObstacle == null)
        //    Debug.LogWarning($"{gameObject.name}: 부모에 BaseObstacle이 없습니다.");
    }

    // 콜리전 이벤트 전달
    private void OnCollisionEnter(Collision col)
    {
        _parentObstacle?.SendMessage("OnCollisionEnter", col, SendMessageOptions.DontRequireReceiver);
    }
    private void OnCollisionExit(Collision col)
    {
        _parentObstacle?.SendMessage("OnCollisionExit", col, SendMessageOptions.DontRequireReceiver);
    }
    // 트리거 이벤트 전달
    private void OnTriggerEnter(Collider other)
    {
        _parentObstacle?.SendMessage("OnTriggerEnter", other, SendMessageOptions.DontRequireReceiver);
    }
    private void OnTriggerExit(Collider other)
    {
        _parentObstacle?.SendMessage("OnTriggerExit", other, SendMessageOptions.DontRequireReceiver);
    }
}
