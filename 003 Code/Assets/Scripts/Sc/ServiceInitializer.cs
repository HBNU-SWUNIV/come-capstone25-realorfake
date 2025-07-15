using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class ServiceInitializer : MonoBehaviour
{
    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log($"�÷��̾� ID: {AuthenticationService.Instance.PlayerId}");
    }
}
