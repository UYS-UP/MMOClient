
using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class DamageUI : MonoBehaviour, IPooledObject
{

    private UnityEngine.CanvasGroup CanvasGroup;
    private TMPro.TextMeshProUGUI DamageText;

    private void BindComponent()
    {
        var root = this.transform;

        CanvasGroup = GetComponent<UnityEngine.CanvasGroup>();

        DamageText = root.Find("DamageText")?.GetComponent<TMPro.TextMeshProUGUI>();

    }

    private void Awake()
    {
        BindComponent();
    }


    public void Initialize(float damage, Vector3 worldPosition, Color color, float fontSize = 25f,
        float duration = 1.5f)
    {
        DamageText.text = damage.ToString("F0");
        DamageText.color = color;
        DamageText.fontSize = Mathf.RoundToInt(fontSize);
        var screenPosition = GameContext.Instance.MainCamera.WorldToScreenPoint(worldPosition);
        transform.position = screenPosition;

        transform.localPosition += new Vector3(
            Random.Range(-20f, 20f),
            Random.Range(-20f, 20f),
            0
            );

        PlayAnimation(duration);

    }

    private void PlayAnimation(float duration)
    {
        Sequence sequence = DOTween.Sequence();
        
        sequence.Append(transform.DOLocalMoveY(
            transform.localPosition.y + 100f, 
            duration
        ).SetEase(Ease.OutCubic));
        
        sequence.Join(CanvasGroup.DOFade(0, duration).SetEase(Ease.InQuad));
        
        sequence.Join(transform.DOScale(1.2f, duration * 0.3f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => {
                transform.DOScale(0.8f, duration * 0.7f)
                    .SetEase(Ease.InBack);
            }));
        
        sequence.OnComplete(() =>
        {
            PoolService.Instance.Despawn(this.gameObject, false);
        });
    }

    public void OnObjectSpawn()
    {
        
    }

    public void OnObjectDespawn()
    {
        CanvasGroup.alpha = 1;
        transform.localScale = Vector3.one;
    }
}
