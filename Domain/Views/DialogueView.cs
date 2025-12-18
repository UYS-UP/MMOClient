using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 对话系统视图
/// 负责显示对话内容和选项,收集用户的选项选择
/// </summary>
public class DialogueView : BaseView
{
    [Header("UI References")]
    public TMP_Text speakerText;
    public TMP_Text dialogueText;
    public Transform optionsParent;
    public GameObject optionPrefab;

    private DialogueController controller;


    protected override void Awake()
    {
        base.Awake();
        controller = new DialogueController(this);
    }

    /// <summary>
    /// 显示对话节点
    /// </summary>
    public void ShowDialogue(DialogueNode node)
    {
        if (node == null)
        {
            HideDialogue();
            return;
        }

        gameObject.SetActive(true);

        // 更新说话者和对话文本
        speakerText.text = node.Speaker;
        dialogueText.text = node.Text;

        // 清空旧选项
        ClearOptions();

        // 创建新选项
        foreach (var option in node.Options)
        {
            CreateOptionButton(option);
        }
    }

    /// <summary>
    /// 隐藏对话界面
    /// </summary>
    public void HideDialogue()
    {
        gameObject.SetActive(false);
        ClearOptions();
    }

    /// <summary>
    /// 清空所有选项按钮
    /// </summary>
    private void ClearOptions()
    {
        foreach (Transform child in optionsParent)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 创建选项按钮
    /// </summary>
    private void CreateOptionButton(DialogueOption option)
    {
        var btn = Instantiate(optionPrefab, optionsParent, false);
        btn.GetComponentInChildren<TMP_Text>().text = option.Text;
        btn.GetComponent<Button>().onClick.AddListener(() => controller.SelectOption(option));
    }
}