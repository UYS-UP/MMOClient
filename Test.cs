using System;
using System.Collections.Generic;
using DG.Tweening;
using Newtonsoft.Json;
using UnityEngine;

public class Test : MonoBehaviour
{
    

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                gameObject.transform.GetChild(i).gameObject.SetActive(true);
            }
            GetComponent<RectTransform>().localScale = new Vector3(0, 1, 1);
            GetComponent<RectTransform>().DOScaleX(1, 1f).SetEase(Ease.OutBack);

        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                if(i == 0) continue;
                gameObject.transform.GetChild(i).gameObject.SetActive(false);
            }
            // GetComponent<RectTransform>().DOScaleX(0, 1f).SetEase(Ease.InBack);

        }
    }
}