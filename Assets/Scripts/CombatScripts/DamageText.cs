using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    [Header("References")]
    public TMP_Text text;

    //============================================================================

    private string descriptor;
    private float damage;
    private float shrinkTime;
    
    //============================================================================

    public void Initialize(string pDescriptor, float pDamage, float pShrinkTime)
    {
        descriptor = pDescriptor;
        damage = pDamage;
        shrinkTime = pShrinkTime;

        text.text = descriptor + " x" + damage.ToString("F2");
    }

    public float Merge()
    {
        //Pop out
        LeanTween.scale(this.gameObject, Vector3.zero, shrinkTime)
        .setEaseOutQuad()
        .setOnComplete(() =>
        {
            Destroy(this.gameObject);
        });
        
        return damage;
    }
}
