using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeyPad : MonoBehaviour
{
    private KeyboardManager _keyboardManager;
    public string keyValue;
    public Button _key;

    void Awake()
    {
        keyValue = transform.gameObject.name;
        _keyboardManager = GetComponentInParent<KeyboardManager>();
        _key = GetComponent<Button>();

        _key.onClick.AddListener(Send);
    }

    public void Send()
    {
        _keyboardManager.SendText(keyValue);
    }

    public void Cancel()
    {
        _keyboardManager.Cancel();
    }
}
