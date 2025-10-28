using UnityEngine;
using UnityEngine.UI;

public class KeyboardManager : MonoBehaviour
{
    public InputField textInputField;
    public Shift shift;
    public Button shiftButton;
    public int _shiftPressed = 0;
    public bool _shiftFixed = false;
    public UIInteractor _interactor;
    public void Start()
    {
        _interactor = FindAnyObjectByType<UIInteractor>();
        shift = GetComponentInChildren<Shift>();
        if (shift != null )
            shiftButton = shift.GetButton();
    }

    private void Update()
    {
        if (_interactor != null)
            textInputField = _interactor.GetLastInputField();
        else
            _interactor = FindAnyObjectByType<UIInteractor>();

        if (shiftButton != null)
        {
            if (_shiftPressed == 1 && !_shiftFixed)
            {
                shiftButton.image.color = Color.yellow;
            }
            else if (_shiftPressed == 1 && _shiftFixed)
            {
                shiftButton.image.color = Color.red;
            }
            else
            {
                shiftButton.image.color = Color.grey;
            }
        }
        

    }

    public void ShiftPressed()
    {
        if (textInputField == null)
            return;

        if (_shiftPressed == 0)
            _shiftPressed = 1;
        else if (_shiftPressed == 1 && !_shiftFixed)
            _shiftFixed = true;
        else if (_shiftPressed == 1 && _shiftFixed)
        {
            _shiftFixed = false;
            _shiftPressed = 0;
        }
    }

    public void SendText(string message)
    {
        if (textInputField == null)
            return;

        textInputField.text += message[_shiftPressed].ToString();
        if (!_shiftFixed)
            _shiftPressed = 0;
    }

    public void Cancel()
    {
        if (textInputField == null)
            return;

        if (textInputField.text.Length > 0)
        {
            textInputField.text = textInputField.text[..^1];
        }
    }
}
