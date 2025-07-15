using UnityEngine;
using UnityEngine.Events;

public class XRButtonHandler : MonoBehaviour
{
    public UnityEvent OnPressed;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("XRController"))
        {
            OnPressed.Invoke();
        }
    }
}

    