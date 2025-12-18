// using System;
// using System.Collections;
// using System.Collections.Generic;
// using DG.Tweening;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
//
// public class HealthBarUI : MonoBehaviour
// {
//     private Image healthBarImage;
//     public Transform target;
//     private Camera playerCamera;
//     private Camera mainCamera;
//     private TMP_Text healthText;
//     public Vector2 offset = new Vector3(0, 120);
//
//     private void Awake()
//     {
//         healthBarImage = GetComponentInChildren<Image>();
//         healthText =  GetComponentInChildren<TMP_Text>();
//     }
//
//     public void SetTarget(Transform target, Camera playerCamera)
//     {
//         this.target = target;
//         this.playerCamera = playerCamera;
//         mainCamera = GameResourcesManager.Instance.MainCamera;
//     }
//     
//     public void UpdateHealthBar(int currentHealth, int maxHealth)
//     {
//         float healthPercent = (float)currentHealth / (float)maxHealth;
//         healthBarImage.DOFillAmount(healthPercent, 0.2f);
//         healthText.text = currentHealth + " / " + maxHealth;
//     }
//     
//     private void Update()
//     {
//         if (target != null)
//         {
//             var screenPos  = playerCamera.WorldToScreenPoint(target.position);
//             RectTransformUtility.ScreenPointToLocalPointInRectangle(
//                 UIService.Instance.Canvas.GetComponent<RectTransform>(),
//                 screenPos,
//                 mainCamera,
//                 out var localPos
//             );
//             transform.localPosition = localPos + offset;
//         }
//     }
// }
