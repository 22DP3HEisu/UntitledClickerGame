using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Clicker : MonoBehaviour
{
    public Transform targetTransform;
    public ClickPopupSpawner popupSpawner;

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(mousePos);

            if (hit != null && hit.transform == targetTransform)
            {
                ClickManager.Instance.AddClicks(1);
                popupSpawner.SpawnPopup(mousePos, "+1");
            }
        }
    }
}