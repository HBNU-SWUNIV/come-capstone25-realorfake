using Unity.Netcode;
using UnityEngine;

namespace HelloWorld
{
    public class PlayerManager : MonoBehaviour
    {
        private NetworkManager m_NetworkManager;

        void Awake()
        {
            m_NetworkManager = GetComponent<NetworkManager>();
        }
    }
}
