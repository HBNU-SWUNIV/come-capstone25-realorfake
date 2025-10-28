using UnityEngine;
using UnityEngine.UI;

public class Delete : MonoBehaviour
{
    private KeyboardManager _keyboardManager;
    private Button _key;

    void Awake()
    {
        _keyboardManager = GetComponentInParent<KeyboardManager>();
        _key = GetComponent<Button>();

        _key.onClick.AddListener(Cancel);
    }

    public void Cancel()
    {
        _keyboardManager.Cancel();
    }
}
