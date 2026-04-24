using UnityEngine;

public class Retaliate : CollisionEffect
{
    public float retaliationDamagePerSpeedUnit;

    protected override void OnFriendlyBump(CollisionAction collisionAction)
    {
        collisionAction.other.TakeDamage(collisionAction.impactSpeed * retaliationDamagePerSpeedUnit, token);
    }
}
