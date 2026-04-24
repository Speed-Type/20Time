using UnityEngine;

public class Projectile : CollisionEffect
{
    public int maxCharges;
    public GameObject projectile;

    private int currentCharges;

    protected override void Start()
    {
        base.Start();
        ResetEffect();
    }

    protected override void OnFriendlyBump(CollisionAction collisionAction)
    {
        if(currentCharges <= 0) return;

        Instantiate(projectile);

        currentCharges--;
    }

    protected override void ResetEffect()
    {
        currentCharges = maxCharges;
    }
}
