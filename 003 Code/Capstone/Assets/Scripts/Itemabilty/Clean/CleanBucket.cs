using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 양동이 아이템
/// 청소 도구 중 하나로, 물을 담아 사용하는 도구입니다.
/// </summary>
public class CleanBucket : CleanItem
{
    /*
     * 양동이 효과 (2턴)
     * - 스태미나 2 소모
     * - 물을 뿌려 적을 적시킵니다
     * - 받는 데미지 -20% 3턴 동안 지속됩니다 (소모)
     */

    /// <summary>
    /// 아이템 사용
    /// 물을 뿌려 방어력을 증가시킵니다.
    /// </summary>
    /// 
    protected override void Awake()
    {
        _particlePath = "Effect/Effect_49_IceAge";
        _particleScale = 0.5f;
        _particleTarget = ParticleTarget.Enemy;
        base.Awake();
    }

    private void OnEnable()
    {
        _stamina = 2;
        _interactionType = InteractionType.Consume;
    }
    public override void Use()
    {
        base.Use();
        if (NetworkManager.Singleton.IsHost)
            GameManager.GetInstance().SetBuff(GameManager.BUFTYPE.INCDEFENSE, 0.2f, 3, NetworkManager.Singleton.IsHost);
        else
            GameManager.GetInstance().SetBuffServerRpc(GameManager.BUFTYPE.INCDEFENSE, 0.2f, 3, NetworkManager.Singleton.IsHost);

    }
}
