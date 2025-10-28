using Unity.Netcode;
using UnityEngine;

public class WriteInstCrayon : WriteInstItem
{
    /*
     
     크레용 (2) : 상태 랜덤 설치 아이템 2개 비활성화
     
     */
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_49_IceBlockCrash";
        _particleScale = 0.3f;
        _particleTarget = ParticleTarget.Enemy;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 2;
        _interactionType = InteractionType.Throw;
    }

    public override void Use()
    {
        base.Use();
        Shoot();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().ShootClientRpc(_oid);
        else
            GameManager.GetInstance().ShootServerRpc(_oid);
        GameManager.GetInstance().RandomDestroyItem(2, !NetworkManager.Singleton.IsHost);
    }
}
