using UnityEngine;

public class WriteInstGlue : WriteInstItem
{
    /*
     
     풀 (1) : (스테미나, 설치) 설치...
     
     */
    protected override void Awake()
    {
        _interactionType = InteractionType.Throw;
        base.Awake();
    }
    public override void Use()
    {
        
    }
}
