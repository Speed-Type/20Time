using UnityEngine;

public class GladiatorAnimManager : MonoBehaviour
{
    public Token token;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        token.onAttack.AddListener((CollisionAction) => {PlayAnim(AnimClips.Attack);});
        token.onFriendlyBump.AddListener((CollisionAction) => {PlayAnim(AnimClips.FriendlyBump);});
    }

    public void PlayAnim(AnimClips anim)
    {
        if(anim == AnimClips.Die) {animator.applyRootMotion = true;}
        animator.SetTrigger(anim.ToString());
    }
}

public enum AnimClips
{
    Attack,
    TakeDamage,
    FriendlyBump,
    Die
}
