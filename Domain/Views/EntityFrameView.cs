using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 实体框架视图
/// 显示实体的基本信息(名称、等级、血量、法力等)
/// </summary>
public class EntityFrameView : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text levelText;
    public Slider hpSlider;
    public Slider mpSlider;
    public Image hpFillImage;
    public Image mpFillImage;
    public TMP_Text statusText;

    /// <summary>
    /// 更新实体框架显示
    /// </summary>
    public void UpdateFrame(string entityName, int level, int maxHp, int currentHp, int maxMp, int currentMp, string status)
    {
        if (nameText != null)
        {
            nameText.text = entityName;
        }

        if (levelText != null)
        {
            levelText.text = $"Lv.{level}";
        }

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }

        if (mpSlider != null)
        {
            mpSlider.maxValue = maxMp;
            mpSlider.value = currentMp;
        }

        if (statusText != null)
        {
            statusText.text = status;
        }
    }

    /// <summary>
    /// 更新血量显示
    /// </summary>
    public void UpdateHealth(int currentHp, int maxHp)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = currentHp;
        }
    }

    /// <summary>
    /// 更新法力显示
    /// </summary>
    public void UpdateMana(int currentMp, int maxMp)
    {
        if (mpSlider != null)
        {
            mpSlider.maxValue = maxMp;
            mpSlider.value = currentMp;
        }
    }
}