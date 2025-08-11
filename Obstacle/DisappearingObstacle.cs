using System.Collections;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class DisappearingObstacle : BaseObstacle
{
    public enum DisappearMode
    {
        Transparency, // 투명도 변화로 사라짐 (모든 Renderer 자동 감지)
        Deactivate    // 자식 오브젝트 SetActive + 메인 콜라이더 enable로 사라짐
    }

    [Header("사라짐 연출 방식")]
    [Tooltip("Transparency: 투명도 변화\nDeactivate: 자식 오브젝트를 비활성화 + 메인 콜라이더 끄기")]
    [SerializeField] private DisappearMode disappearMode = DisappearMode.Transparency;

    [Header("셋액티브 방식(Deactivate) 선택시만 사용")]
    [Tooltip("Deactivate 모드에서 사라질 자식(들) Drag & Drop (Transparency일 땐 무시)")]
    [SerializeField] private List<GameObject> targetChildren;

    [Header("작동 방식 설정")]
    [SerializeField] private bool useAutoLoop = false;

    [Header("시간 설정")]
    [Tooltip("사라지기 전 대기 시간")]
    [SerializeField] private float delayBeforeDisappear = 1f;
    [Tooltip("나타나기 전 대기 시간")]
    [SerializeField] private float delayBeforeReappear = 2f;
    [Tooltip("애니메이션 시간")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("흔들림 옵션")]
    [Tooltip("흔들릴 모델(자식) 오브젝트")]
    [SerializeField] private Transform shakeModel;
    [Tooltip("흔들림 XYZ 범위 (local)")]
    [SerializeField] private Vector3 shakeStrength = new Vector3(0.05f, 0.05f, 0.05f);
    [Tooltip("흔들림 속도")]
    [SerializeField] private float shakeVibrato = 20f;

    private Collider col;
    private bool isProcessing = false;
    private bool _wasPlayerOnPlatform = false;
    private Tween _shakeTween;
    private Vector3 _modelOriginPos;

    // 모든 Renderer, 원본 알파값 저장용 (Transparency 방식 전용)
    private Renderer[] _allRenderers;
    private Dictionary<Renderer, float> _originalAlphas = new Dictionary<Renderer, float>();

    private void Awake()
    {
        // Transparency 방식 전용: 모든 Renderer를 자식까지 다 찾고 알파값 저장, 머티리얼 분리
        _allRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in _allRenderers)
        {
            r.material = new Material(r.material); // 인스턴스화(분리)
            SetMaterialToOpaque(r.material);       // 시작은 Opaque
            _originalAlphas[r] = r.material.color.a;
        }

        col = GetComponent<Collider>();

        if (shakeModel != null)
            _modelOriginPos = shakeModel.localPosition;
    }

    private void Start()
    {
        if (useAutoLoop)
        {
            StartCoroutine(AutoLoopRoutine());
        }
    }

    private void Update()
    {
        if (!useAutoLoop && !isProcessing)
        {
            bool isOn = IsPlayerOnPlatform();

            if (isOn && !_wasPlayerOnPlatform)
            {
                StartCoroutine(DisappearRoutine());
            }
            _wasPlayerOnPlatform = isOn;
        }
    }

    private IEnumerator AutoLoopRoutine()
    {
        while (true)
        {
            if (shakeModel != null)
                StartShake();

            yield return new WaitForSeconds(2f);

            StopShake();
            yield return new WaitForSeconds(delayBeforeDisappear - 2f);

            Disappear();
            yield return new WaitForSeconds(delayBeforeReappear);

            Reappear();
        }
    }

    private IEnumerator DisappearRoutine()
    {
        isProcessing = true;

        if (shakeModel != null)
            StartShake();

        yield return new WaitForSeconds(2f);

        StopShake();

        float remain = delayBeforeDisappear - 2f;
        if (remain > 0)
            yield return new WaitForSeconds(remain);

        Disappear();

        yield return new WaitForSeconds(delayBeforeReappear);
        Reappear();

        isProcessing = false;
    }

    private void StartShake()
    {
        // DOTween의 DOShakePosition으로 XYZ축 랜덤하게 흔들림
        if (_shakeTween != null && _shakeTween.IsActive()) _shakeTween.Kill();
        if (shakeModel != null)
        {
            shakeModel.localPosition = _modelOriginPos;
            _shakeTween = shakeModel.DOShakePosition(
                2f,          // duration (2초)
                shakeStrength,
                Mathf.RoundToInt(shakeVibrato),
                90,          // randomness
                false,       // fade out
                true         // snapping
            ).SetEase(Ease.Linear);
        }
    }

    private void StopShake()
    {
        if (_shakeTween != null && _shakeTween.IsActive())
            _shakeTween.Kill();
        if (shakeModel != null)
            shakeModel.localPosition = _modelOriginPos;
    }

    private void Disappear()
    {
        if (disappearMode == DisappearMode.Transparency)
        {
            // 모든 렌더러의 알파값 0으로 (자식 포함)
            if (_allRenderers != null)
            {
                foreach (var r in _allRenderers)
                {
                    SetMaterialToTransparent(r.material);
                    r.material.DOFade(0f, fadeDuration);
                }
            }
            if (col != null)
                col.enabled = false;
        }
        else if (disappearMode == DisappearMode.Deactivate)
        {
            // targetChildren 리스트에 포함된 자식 오브젝트만 비활성화
            foreach (var obj in targetChildren)
                if (obj != null) obj.SetActive(false);

            // 메인 콜라이더도 OFF
            if (col != null)
                col.enabled = false;
        }

        // 플레이어 상태 리셋
        _playerOnPlatform = null;
        _playerRigidbody = null;
    }

    private void Reappear()
    {
        if (disappearMode == DisappearMode.Transparency)
        {
            // 모든 렌더러의 알파값 원본값으로 복원
            if (_allRenderers != null)
            {
                foreach (var r in _allRenderers)
                {
                    float originAlpha = 1f;
                    if (_originalAlphas.TryGetValue(r, out originAlpha))
                    {
                        r.material.DOFade(originAlpha, fadeDuration)
                            .OnComplete(() => SetMaterialToOpaque(r.material));
                    }
                    else
                    {
                        r.material.DOFade(1f, fadeDuration)
                            .OnComplete(() => SetMaterialToOpaque(r.material));
                    }
                }
            }
            if (col != null)
                col.enabled = true;

            StopShake();
        }
        else if (disappearMode == DisappearMode.Deactivate)
        {
            // targetChildren 리스트에 포함된 자식 오브젝트만 활성화
            foreach (var obj in targetChildren)
                if (obj != null) obj.SetActive(true);

            // 메인 콜라이더도 ON
            if (col != null)
                col.enabled = true;
        }
    }

    private void OnDestroy()
    {
        StopShake();
    }

    // === 머티리얼 렌더링모드 제어 함수 ===
    private void SetMaterialToTransparent(Material mat)
    {
        if (mat.shader.name.Contains("Universal Render Pipeline/Lit"))
        {
            mat.SetFloat("_Surface", 1); // 1=Transparent
            mat.SetFloat("_Blend", 0);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }
        else if (mat.HasProperty("_Mode")) // Standard Shader
        {
            mat.SetFloat("_Mode", 2); // Fade
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
    }

    private void SetMaterialToOpaque(Material mat)
    {
        if (mat.shader.name.Contains("Universal Render Pipeline/Lit"))
        {
            mat.SetFloat("_Surface", 0); // 0=Opaque
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = -1;
        }
        else if (mat.HasProperty("_Mode")) // Standard Shader
        {
            mat.SetFloat("_Mode", 0); // Opaque
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = -1;
        }
    }
}
