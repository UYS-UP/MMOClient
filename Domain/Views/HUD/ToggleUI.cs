using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToggleUI : MonoBehaviour, IPointerClickHandler
{
    public GameObject active;
    public bool isActive;
    public event Action OnValueChanged;

    public void OnPointerClick(PointerEventData eventData)
    {
        // 禁止点已激活的按钮时关闭
        if (isActive) return;

        SetActiveState(true);
    }

    /// <summary>
    /// 外部控制激活状态
    /// </summary>
    public void SetActiveState(bool value, bool triggerEvent = true)
    {
        isActive = value;
        if (active != null)
            active.SetActive(isActive);

        if (triggerEvent)
            OnValueChanged?.Invoke();
    }
}