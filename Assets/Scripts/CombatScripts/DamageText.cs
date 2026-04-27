using UnityEngine;

public class DamageText : MonoBehaviour
{
    private string descriptor;
    private float damage;

    public void Initialize(string pDescriptor, float pDamage)
    {
        descriptor = pDescriptor;
        damage = pDamage;
    }

    public float Merge()
    {
        //TODO: Delete in a cool way/animation
        
        
        return damage;
    }
}
