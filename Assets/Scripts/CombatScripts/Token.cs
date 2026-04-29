using UnityEngine;
using UnityEngine.Events;

public class Token : MonoBehaviour
{
    //References
    [Header("References")]
    public GameObject gladiatorModel;

    //Identity
    [Header("Identity")]
    [Tooltip("0 = Player 1, 1 = Player 2. Friendly-fire knockback still applies; " +
             "damage is only dealt to the opposing team.")]
    public int ownerIndex = 0;

    public MovementType movementType;

    //Physics
    [Header("Physics")]
    [Tooltip("Collision radius in world units (XZ plane)")]
    public float radius = 0.5f;

    [Tooltip("Mass used for elastic collision impulse exchange")]
    public float mass = 1f;

    [Tooltip("Velocity lost per second to friction. 0 = frictionless, 1 = stops in ~1 s")]
    [Range(0f, 10f)]
    public float drag = 2f;

    [Tooltip("Energy kept after bouncing off a wall. 1 = perfect reflection, 0 = dead stop")]
    [Range(0f, 1f)]
    public float wallRestitution = 0.85f;

    [Tooltip("Energy kept after a token-token collision (applied to both tokens)")]
    [Range(0f, 1f)]
    public float tokenRestitution = 0.9f;

    //State (written by CollisionSystem)
    [HideInInspector] public Vector2 velocity; //XZ velocity
    [HideInInspector] public bool isMoving; //true while not at rest
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
    public UnityEvent<CollisionAction> onAttack; //Called when this token collides with an enemy token
    public UnityEvent<CollisionAction> onFriendlyBump; //Called when this token is bumped by a friendly token
    public UnityEvent<CollisionAction> onUnfriendlyBump; //Called when this token is bumped by an enemy token
    public UnityEvent onTurnEnd; //Called when a turn ends

    //============================================================================

    private BaseCombatEntity baseCombatEntity;

    //============================================================================
    
    private void Awake()
    {
        //Initialize references
        baseCombatEntity = GetComponent<BaseCombatEntity>();

        //Immediately register
        CollisionSystem.instance.Register(this);
    }
    
    private void Start()
    {
        onCollision.AddListener(SetModelDirection);
    }

    private void OnDestroy()
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
        isMoving = true;
        launchInstigator = this; //Initialize instigator to self

        SetModelDirection();
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

    //============================================================================

    private int modelRotationTween;
    public void SetModelDirection(CollisionAction collisionAction = new CollisionAction())
    {
        //Cancel any currently running tweens
        LeanTween.cancel(modelRotationTween);

        if(isOriginalLaunched)
        {
            if(collisionAction.kind == CollisionAction.CollisionKind.EnemyToken) //Case 1: Colliding into enemy
            {
                //Face the thing that bumped you (immediate)
                gladiatorModel.transform.forward = (collisionAction.other.transform.position - gameObject.transform.position).normalized;
                //gladiatorModel.transform.forward = new Vector3(-collisionAction.normal.x, 0, -collisionAction.normal.y);

                //Wait for cinematic to be over
                modelRotationTween = LeanTween.delayedCall(CombatCameraController.instance.cinematicHoldDuration, () =>
                {
                    //Tween back to rotate to face velocity again
                    Vector3 startVector = gladiatorModel.transform.forward;
                    Vector3 endVector = new Vector3(velocity.x, 0, velocity.y);
                    
                    modelRotationTween = LeanTween.value(0, 1, 0.15f)
                    .setOnUpdate((float val) =>
                    {
                        gladiatorModel.transform.forward = (1 - val) * startVector + (val) * endVector;
                    })
                    .setIgnoreTimeScale(true)
                    .id;
                })
                .setIgnoreTimeScale(true)
                .id;
            }
            else //Case 2: Colliding into wall or friendly token
            {
                //Face the direction of velocity (tweened)
                Vector3 startVector = gladiatorModel.transform.forward;
                Vector3 endVector = new Vector3(velocity.x, 0, velocity.y);
                
                modelRotationTween = LeanTween.value(0, 1, 0.15f)
                .setOnUpdate((float val) =>
                {
                    gladiatorModel.transform.forward = (1 - val) * startVector + (val) * endVector;
                })
                .setIgnoreTimeScale(true)
                .id;
            }
        }
        else
        {
            if(collisionAction.kind != CollisionAction.CollisionKind.Wall)
            {
                //Face the thing that bumped you (immediate)
                gladiatorModel.transform.forward = (collisionAction.other.transform.position - gameObject.transform.position).normalized;
                //gladiatorModel.transform.forward = new Vector3(-collisionAction.normal.x, 0, -collisionAction.normal.y);
            }
        }
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