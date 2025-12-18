using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EntityFrameUI : MonoBehaviour
{
    private Image portraitImage;
    private Slider hpSlider;
    private Slider mpSlider;
    private TMP_Text nameText;
    private TMP_Text levelText;
    private TMP_Text hpText;
    private TMP_Text mpText;

    private void Awake()
    {
        portraitImage = transform.Find("Portrait/Back/PortraitImage").GetComponent<Image>();
        hpSlider = transform.Find("StatusFrame/HpSlider").GetComponent<Slider>();
        mpSlider = transform.Find("StatusFrame/MpSlider").GetComponent<Slider>();
        nameText = transform.Find("StatusFrame/NameText").GetComponent<TMP_Text>();
        levelText = transform.Find("Level/LevelText").GetComponent<TMP_Text>();
        hpText = hpSlider.transform.Find("HpText").GetComponent<TMP_Text>();
        mpText = mpSlider.transform.Find("MpText").GetComponent<TMP_Text>();
        
        mpSlider.onValueChanged.AddListener((value) =>
        {
            mpSlider.DOValue(value, 0.5f);
        });
        
        hpSlider.onValueChanged.AddListener((value) =>
        {
            hpSlider.DOValue(value, 0.5f);
        });
    }

    public void UpdateFrame(string name, int level, int maxHp, int hp, int maxMp, int mp, string portrait)
    {
        if (hp == 0 || maxHp == 0) hpSlider.value = 0;
        if (mp == 0 || maxMp == 0) mpSlider.value = 0;
        hpSlider.value = (float)maxHp / hp;
        mpSlider.value = (float)maxMp / hp;
        nameText.text = name;
        levelText.text = level.ToString();
        mpText.text = $"{mp}/{maxMp}";
        hpText.text = $"{hp}/{maxHp}";

    }
    
    
}
