using UnityEngine;

public class CollisionEffect : MonoBehaviour
{
    protected Token token;
    
    protected virtual void Start()
    {
        Token token = GetComponent<Token>();

        //Add listeners to the events
        token.onAttack.AddListener(OnAttack);
        token.onFriendlyBump.AddListener(OnFriendlyBump);
        token.onUnfriendlyBump.AddListener(OnUnfriendlyBump);
        token.onTurnEnd.AddListener(ResetEffect);
    }

    protected virtual void OnAttack(CollisionAction collisionAction)
    {
        //To be filled by child classes
    }

    protected virtual void OnFriendlyBump(CollisionAction collisionAction)
    {
        //To be filled by child classes
    }

    protected virtual void OnUnfriendlyBump(CollisionAction collisionAction)
    {
        //To be filled by child classes
    }

    protected virtual void ResetEffect()
    {
        //To be filled by child classes
    }
}
