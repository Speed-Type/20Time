using UnityEngine;

//This collision effect speeds up any friendly tokens that bump into this token

public class BumpBoost : CollisionEffect
{
    public int maxCharges;
    public float speedIncrease;

    private int currentCharges;

    protected override void Start()
    {
        base.Start();
        ResetEffect();
    }

    protected override void OnFriendlyBump(CollisionAction collisionAction)
    {
        if(currentCharges <= 0) return;

        //Debug.Log("boosting speed");
        //Debug.Log("\toriginal velocity: " + collisionAction.other.velocity);
        collisionAction.other.velocity += collisionAction.other.velocity.normalized * speedIncrease;

        //Debug.Log("\tnew velocity: " + collisionAction.other.velocity);

        currentCharges--;
        //Debug.Log("\tRemaining charges: " + currentCharges);
    }

    protected override void ResetEffect()
    {
        currentCharges = maxCharges;
    }
}
