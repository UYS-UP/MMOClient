using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmojiPicker : MonoBehaviour
{
    [Header("UI References")]
    public Toggle showEmojiToggle;            // 控制是否显示表情面板
    public GameObject emojiPanel;             // 表情面板
    public TMP_InputField inputField;         // 聊天输入框
    public GameObject emojiButtonPrefab;      // 表情按钮预制体
    public Transform emojiContainer;          // 表情按钮的容器（带 GridLayoutGroup）

    // 可自定义表情集
    private string[] emojis = new string[]
    {
        "😀", "😂", "🤣", "😅", "😊", "😍", "😎", "🤔", "😭", "😡",
        "👍", "👎", "🙏", "💪", "🔥", "✨", "❤️", "💔", "💯", "🎉"
    };

    void Start()
    {
        // 初始隐藏面板
        emojiPanel.SetActive(false);
        showEmojiToggle.onValueChanged.AddListener(OnToggleChanged);

        // 动态创建表情按钮
        foreach (string emoji in emojis)
        {
            GameObject btnObj = Instantiate(emojiButtonPrefab, emojiContainer);
            TMP_Text emojiText = btnObj.GetComponentInChildren<TMP_Text>();
            emojiText.text = emoji;

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnClickEmoji(emoji));
        }
    }

    private void OnToggleChanged(bool isOn)
    {
        emojiPanel.SetActive(isOn);
    }

    private void OnClickEmoji(string emoji)
    {
        int caretPos = inputField.stringPosition;
        string currentText = inputField.text;

        // 在光标位置插入 emoji
        inputField.text = currentText.Insert(caretPos, emoji);
        inputField.caretPosition = caretPos + emoji.Length;

        // 自动关闭面板
        showEmojiToggle.isOn = false;
    }
}