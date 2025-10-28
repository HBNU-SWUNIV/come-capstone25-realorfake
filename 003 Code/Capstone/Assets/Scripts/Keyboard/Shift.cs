using UnityEngine;
using UnityEngine.UI;

public class Shift : MonoBehaviour
{
    private KeyboardManager _keyboardManager;
    private Button _key;

    void Awake()
    {
        _keyboardManager = GetComponentInParent<KeyboardManager>();
        _key = GetComponent<Button>();

        _key.onClick.AddListener(Send);
    }

    public void Send()
    {
        _keyboardManager.ShiftPressed();
    }

    public Button GetButton()
    {
        return _key;
    }
}
