using UnityEngine;
using UnityEngine.Events;

public class Token : MonoBehaviour
{
    //Identity
    [Header("Identity")]
    [Tooltip("0 = Player 1, 1 = Player 2. Friendly-fire knockback still applies; " +
             "damage is only dealt to the opposing team.")]
    public int ownerIndex = 0;

    public MovementType movementType;

    //Physics
    [Header("Physics")]
    [Tooltip("Collision radius in world units (XZ plane).")]
    public float radius = 0.5f;

    [Tooltip("Mass used for elastic collision impulse exchange.")]
    public float mass = 1f;

    [Tooltip("Velocity lost per second to friction. 0 = frictionless, 1 = stops in ~1 s.")]
    [Range(0f, 10f)]
    public float drag = 2f;

    [Tooltip("Energy kept after bouncing off a wall. 1 = perfect reflection, 0 = dead stop.")]
    [Range(0f, 1f)]
    public float wallRestitution = 0.85f;

    [Tooltip("Energy kept after a token-token collision (applied to both tokens).")]
    [Range(0f, 1f)]
    public float tokenRestitution = 0.9f;

    //State (written by CollisionSystem)
    [HideInInspector] public Vector2 velocity; //XZ velocity
    [HideInInspector] public bool isLaunched; //true while in active play
    [HideInInspector] public bool isOriginalLaunched; //true if this was the one that the player originally launched
    
    //When this token is a friendly knock-on, the chain traces back to the
    //original launcher so damage attribution is correct.
    //Reset to null when the token comes to rest.
    [HideInInspector] public Token launchInstigator;

    //Events
    [Header("Events")]

    //Fires on ANY collision (wall or token)
    //CollisionAction carries context you can extend for custom effects.
    public UnityEvent<CollisionAction> onCollision;

    //============================================================================

    private BaseCombatEntity baseCombatEntity;

    //============================================================================
    
    void Awake()
    {
        //Initialize references
        baseCombatEntity = GetComponent<BaseCombatEntity>();

        //Immediately register
        CollisionSystem.instance.Register(this);
    }

    void OnDestroy()
    {
        //Clean up from the collision system if the GameObject is removed mid-round.
        CollisionSystem.instance.Unregister(this);
    }

    //============================================================================

    //Call this from your pullback-and-release system to launch the token.
    //direction should already be normalised (or will be normalised here)
    public void Launch(Vector2 direction, float speed)
    {
        velocity = direction.normalized * speed;
        Debug.Log(velocity);
        isOriginalLaunched = true;
        isLaunched = true;
        launchInstigator = this; //Initialize instigator to self
    }

    //Called by TokenCollisionSystem when an enemy hit lands
    public void TakeDamage(float amount, Token source)
    {
        baseCombatEntity.TakeDamage(amount, source);
    }

    public float GetDamagePerSpeedUnit() {return baseCombatEntity.damagePerSpeedUnit;}

    public float GetMinDamageSpeed() {return baseCombatEntity.minDamageSpeed;}

    //Snaps position on the XZ plane from our 2-D representation
    public Vector2 Position2D
    {
        get => new Vector2(transform.position.x, transform.position.z);
        set => transform.position = new Vector3(value.x, transform.position.y, value.y);
    }

    public override string ToString()
    {
        return gameObject.name;
    }
}

//============================================================================

//Data carrier for the collision event

[System.Serializable]
public struct CollisionAction
{
    public enum CollisionKind { Wall, FriendlyToken, EnemyToken }

    public CollisionKind kind;
    //The other token involved (null for wall collisions)
    public Token other;
    //Relative speed at moment of impact
    public float impactSpeed;
    //World-space normal of the collision surface (XZ, length 1)
    public Vector2 normal;
}

public enum MovementType
{
    Piercer,
    Bouncer,
    Pusher
}