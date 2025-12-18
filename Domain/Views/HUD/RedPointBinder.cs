
using System;
using TMPro;
using UnityEngine;

public class RedPointBinder : MonoBehaviour
{
    public string Path;

    public GameObject DotRoot;
    public TMP_Text CountText;
    
    public bool HideWhenZero = true;
    public int MaxDisplay = 99;

    private IDisposable sub;
    
    private void OnEnable()
    {
        if (RedPointService.Instance == null) return;
        sub = RedPointService.Instance.Subscribe(Path, OnNodeChanged);
    }

    private void OnDisable()
    {
        sub?.Dispose();
        sub = null;
    }
    
    void OnNodeChanged(RedPointNode node)
    {
        int c = node.TotalCount;
        if (DotRoot != null)
            DotRoot.SetActive(!HideWhenZero || c > 0);

        if (CountText != null)
        {
            if (c <= 0) CountText.text = "";
            else CountText.text = c > MaxDisplay ? $"{MaxDisplay}+" : c.ToString();
        }
    }
}
