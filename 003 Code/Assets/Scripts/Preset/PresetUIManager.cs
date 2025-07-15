using UnityEngine;
using UnityEngine.UI;
using System;

public class PresetUIManager : MonoBehaviour
{
    [SerializeField] private GameObject presetUIPrefab;
    private GameObject currentUI;

    public event Action OnConfirmClicked;
    public event Action OnCancelClicked;

    public void ShowPresetUI()
    {
        if (currentUI != null) return;

        Camera cam = Camera.main;
        Vector3 uiPos = cam.transform.position + cam.transform.forward * 0.5f + Vector3.up * -0.2f;

        currentUI = Instantiate(presetUIPrefab, uiPos, Quaternion.identity);

        Canvas canvas = currentUI.GetComponentInChildren<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        currentUI.transform.LookAt(cam.transform);
        currentUI.transform.Rotate(0, 180f, 0);

        SetupButtons();
    }

    private void SetupButtons()
    {
        var buttons = currentUI.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.CompareTag("ConfirmButton"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnConfirmClicked?.Invoke());
            }
            else if (btn.CompareTag("CancelButton"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnCancelClicked?.Invoke());
            }
        }
    }

    public void HidePresetUI()
    {
        if (currentUI != null)
        {
            Destroy(currentUI);
            currentUI = null;
        }
    }
} 