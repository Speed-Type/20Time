using UnityEngine;
using System.Collections.Generic;

public class CollisionSystem : MonoBehaviour
{
    //Static instance
    public static CollisionSystem instance;

    //============================================================================

    [Header("References")]
    [Tooltip("Drag the ArenaWalls component here, or leave empty to auto-find on Awake")]
    public ArenaWalls arena;

    [Header("Tuning")]
    [Tooltip("An originally launched token is considered 'at rest' when its speed falls below this value")]
    public float originalTokenRestSpeedThreshold = 0.5f;
    [Tooltip("A chain-hit moved token is considered 'at rest' when its speed falls below this value")]
    public float chainTokenRestSpeedThreshold = 0.05f;

    [Tooltip("How many sub-steps to run per FixedUpdate for better tunnelling resistance; 1 is fine for most cases; raise to 2-4 for very fast tokens")]
    [Range(1, 8)]
    public int subSteps = 1;

    [Tooltip("Fraction of overlap to correct each step (0-1). Lower = softer separation, higher = stiffer but can jitter")]
    [Range(0.1f, 1f)]
    public float depenetrationFactor = 0.5f;

    [Tooltip("Multiplier for elasticity of knockback on collision")]
    public float knockbackElasticityMultiplier = 1f;

    //============================================================================

    [HideInInspector] public bool tokensAreMoving;

    readonly List<Token> allTokens = new List<Token>();

    //Pair set prevents double-processing the same pair in one frame
    readonly HashSet<long> resolvedPairs = new HashSet<long>();

    //Tracks piercing overlaps that are *currently ongoing* so damage fires only on
    //the first frame of contact, not every frame the piercer is passing through.
    //A pair is added when overlap begins and removed when the tokens separate.
    readonly HashSet<long> piercingActivePairs = new HashSet<long>();

    //============================================================================

    void Awake()
    {
        //Set singleton
        if(instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        if(arena == null) arena = FindFirstObjectByType<ArenaWalls>();
        if(arena == null) Debug.LogWarning("[TokenCollisionSystem] No ArenaWalls found in scene!");
    }

    void FixedUpdate()
    {
        if(!tokensAreMoving) return;

        bool restCheck = true;
        foreach(Token t in allTokens)
        {
            if(t.isMoving)
            {
                restCheck = false;
            }
        }

        if(tokensAreMoving && restCheck)
        {
            tokensAreMoving = false;

            //End the current player's turn
            CombatManager.instance.GetCurrentPlayer().EndTurn();

            return;
        }

        float dt = Time.fixedDeltaTime / subSteps;
        resolvedPairs.Clear();

        //Remove any piercing pairs whose tokens are no longer overlapping, so the next contact registers as a fresh hit
        piercingActivePairs.RemoveWhere(key =>
        {
            //Decode the pair indices back out of the key
            int i = (int)(key >> 32);
            int j = (int)(key & 0xFFFFFFFFL);
            if (i >= allTokens.Count || j >= allTokens.Count) return true; // stale
            Token ta = allTokens[i], tb = allTokens[j];
            float minDist = ta.radius + tb.radius;
            return (tb.Position2D - ta.Position2D).sqrMagnitude >= minDist * minDist;
        });

        for(int step = 0; step < subSteps; step++)
        {
            // 1. Drag + integrate
            foreach(Token t in allTokens)
                if(t.isMoving) StepToken(t, dt);

            // 2. Token–token collisions
            for(int i = 0; i < allTokens.Count; i++)
            for(int j = i + 1; j < allTokens.Count; j++)
            {
                long pairKey = PairKey(i, j);
                if(resolvedPairs.Contains(pairKey)) continue;
                if(ResolveTokenPair(allTokens[i], allTokens[j]))
                    resolvedPairs.Add(pairKey);
            }

            // 3. Wall collisions
            foreach (Token t in allTokens)
                ResolveWalls(t);
        }

        // 4. Mark resting tokens
        foreach(Token t in allTokens)
        {
            if (!t.isMoving) continue;
            //TODO: on rest for a piercer, make sure that there's no issues with overlap
            if ((t.isOriginalLaunched && t.velocity.sqrMagnitude < originalTokenRestSpeedThreshold * originalTokenRestSpeedThreshold) || (!t.isOriginalLaunched && t.velocity.sqrMagnitude < chainTokenRestSpeedThreshold * chainTokenRestSpeedThreshold))
            {
                t.velocity = Vector2.zero;
                t.isMoving = false;
                t.isOriginalLaunched = false;
                t.launchInstigator = null;
                Debug.Log("Token named [" + t + "] is now resting");
            }
        }
    }

    //============================================================================

    //Register a token so the system starts simulating it.
    //Token.Launch() calls this automatically; you shouldn't need to call it directly.
    public void Register(Token token)
    {
        if (!allTokens.Contains(token))
            allTokens.Add(token);
    }

    //Immediately remove a token from simulation (e.g. when it's destroyed)
    public void Unregister(Token token) => allTokens.Remove(token);

    //============================================================================
    
    static void StepToken(Token t, float dt)
    {
        //Friction (exponential decay approximated as linear step — stable for small dt)
        float speedBefore = t.velocity.magnitude;
        float drag = Mathf.Clamp(t.drag * dt, 0f, 1f);
        t.velocity *= (1f - drag);

        //Integrate position on XZ plane
        t.Position2D += t.velocity * dt;
    }

    bool ResolveTokenPair(Token a, Token b)
    {
        Vector2 delta = b.Position2D - a.Position2D;
        float distSq = delta.sqrMagnitude;
        float minDist = a.radius + b.radius;
        float impactSpeed;

        if(distSq >= minDist * minDist) return false; //no overlap

        float dist = Mathf.Sqrt(distSq);
        Vector2 norm = dist > 1e-6f ? delta / dist : Vector2.right; //safe normal

        //=====================Piercing pass through==================
        //If either token is piercing: no depenetration, no impulse exchange
        //Damage fires only on the first frame of each discrete contact
        if ((a.isOriginalLaunched && a.movementType == MovementType.Piercer) || (b.isOriginalLaunched && b.movementType == MovementType.Piercer))
        {
            long pairKey = PairKey(
                allTokens.IndexOf(a),
                allTokens.IndexOf(b));

            if(!piercingActivePairs.Contains(pairKey))
            {
                piercingActivePairs.Add(pairKey);
                float relVelN = Vector2.Dot(a.velocity - b.velocity, norm);
                impactSpeed = Mathf.Abs(relVelN);
                FireTokenCollisionEvents(a, b, norm, impactSpeed);
            }
            return true; //Mark pair handled for resolvedPairs this frame
        }
        
        //================================================================

        //Positional depenetration
        float overlap = minDist - dist;
        float totalMass = a.mass + b.mass;
        // massRatioA = how much of B's mass vs total; approaches 1 as B → ∞
        float massRatioA  = b.mass / totalMass;  // blend toward full reflect as target gets heavier
        float massRatioB  = a.mass / totalMass;

        //OPTION 1:

        float pushA = (b.mass / totalMass) * overlap * depenetrationFactor;
        float pushB = (a.mass / totalMass) * overlap * depenetrationFactor;

        a.Position2D -= norm * pushA;
        b.Position2D += norm * pushB;

        //OPTION 2:

        // float pushA = (a.isOriginalLaunched && a.movementType == MovementType.Pusher) ? 0f : (b.mass / totalMass) * overlap * depenetrationFactor;
        // float pushB = (b.isOriginalLaunched && b.movementType == MovementType.Pusher) ? 0f : (a.mass / totalMass) * overlap * depenetrationFactor;

        // //If both are immovable just separate them evenly (shouldn't normally happen).
        // if (a.isOriginalLaunched && a.movementType == MovementType.Pusher && b.isOriginalLaunched && b.movementType == MovementType.Pusher) { pushA = pushB = overlap * 0.5f * depenetrationFactor; }

        // a.Position2D -= norm * pushA;
        // b.Position2D += norm * pushB;

        //=====================Velocity handling==================

        float velNA = Vector2.Dot(a.velocity, norm);  // A's normal-axis speed
        float velNB = Vector2.Dot(b.velocity, norm);  // B's normal-axis speed

        // Only resolve if tokens are closing (relative normal speed < 0 from A's POV)
        float closingSpeed = velNA - velNB;
        if (closingSpeed <= 0f) return false;

        Debug.Log("Token named [" + a.gameObject.name + "] collided with token named [" + b.gameObject.name + "]");

        float restitution = Mathf.Min(a.tokenRestitution, b.tokenRestitution);

        if(a.isOriginalLaunched)
        {
            Debug.Log("token named [" + a + "] is the original");

            //If a is a bouncer token, then do full bounce
            if(a.movementType == MovementType.Bouncer)
            {
                Debug.Log("token named [" + a + "] is movement type bouncer");
                a.velocity += norm * (-velNA * (restitution + 1f));
            }
            //Otherwise, nothing changes for a's velocity

            //Unless a is a piercer, knock b back
            if(a.movementType != MovementType.Piercer)
            {
                Debug.Log("applying knockback to [" + b + "]");
                float kickB = velNA * massRatioB * restitution;
                b.velocity += norm * kickB * knockbackElasticityMultiplier;
            }
        }
        else if(b.isOriginalLaunched)
        {
            Debug.Log("token named [" + b + "] is the original");

            //If b is a bouncer token, then do full bounce
            if(b.movementType == MovementType.Bouncer)
            {
                Debug.Log("token named [" + b + "] is movement type bouncer");
                b.velocity += norm * (-velNB * (restitution + 1f));
            }
            //Otherwise, nothing changes for a's velocity

            //Unless b is a piercer, knock a back
            if(b.movementType != MovementType.Piercer)
            {
                Debug.Log("applying knockback to [" + a + "]");
                float kickA = velNB * massRatioA * restitution;
                a.velocity += norm * kickA * knockbackElasticityMultiplier;
            }
        }
        else //This is a chained collision
        {
            //Kick both a and b back, in OPPOSITE DIRECTIONS
            float kickA = velNB * massRatioA * restitution;
            a.velocity += norm * kickA * knockbackElasticityMultiplier;

            float kickB = velNA * massRatioB * restitution;
            b.velocity += norm * kickB * knockbackElasticityMultiplier;
        }

        // if (!a.isOriginalLaunched || a.movementType == MovementType.Bouncer)
        // {
        //     // Reflect A's normal component, scaled by how heavy B is relative to total.
        //     // At equal mass: 0.5 blend → partial reflect + partial pass-through.
        //     // As B → ∞ mass (or isImmovable): full reflect (wall bounce).
        //     float newVelNA = Mathf.Lerp(velNA, -velNA * restitution, 1);
        //     a.velocity += norm * (newVelNA - velNA);

        //     float kickA = velNB * massRatioB * (1f + restitution);
        //     a.velocity -= norm * kickA;
        // }

        // if (!b.isOriginalLaunched || b.movementType == MovementType.Bouncer)
        // {
        //     float newVelNB = Mathf.Lerp(velNB, -velNB * restitution, 1);
        //     b.velocity += norm * (newVelNB - velNB);
        // }

        impactSpeed = closingSpeed;

        //===================================================

        //Damage & collision events
        FireTokenCollisionEvents(a, b, norm, impactSpeed);

        return true;
    }

    void FireTokenCollisionEvents(Token a, Token b, Vector2 norm, float impactSpeed)
    {
        //Mark both tokens as moving
        a.isMoving = true;
        b.isMoving = true;

        //Determine instigators (might be null if token is resting/unowned)
        Token instigatorA = a.launchInstigator;
        Token instigatorB = b.launchInstigator;

        //Build collision actions for both sides
        CollisionAction actionForA = new CollisionAction
        {
            kind = GetCollisionKind(a, b),
            other = b,
            impactSpeed = impactSpeed,
            normal = -norm
        };
        CollisionAction actionForB = new CollisionAction
        {
            kind = GetCollisionKind(b, a),
            other = a,
            impactSpeed = impactSpeed,
            normal = norm
        };

        a.onCollision?.Invoke(actionForA);
        b.onCollision?.Invoke(actionForB);

        //Damage: only deal damage across team boundaries
        //Damage is determined only by the most directly collided token
        if(instigatorA != null && instigatorA.ownerIndex != b.ownerIndex)
            ApplyDamage(b, a, a, impactSpeed);

        if(instigatorB != null && instigatorB.ownerIndex != a.ownerIndex)
            ApplyDamage(a, b, b, impactSpeed);

        //Propagate launch instigator for friendly knock-ons:
        //If A was launched and hits friendly B (at rest), B inherits A's instigator.
        PropagateInstigator(a, b);
        PropagateInstigator(b, a);
    }

    static CollisionAction.CollisionKind GetCollisionKind(Token self, Token other)
    {
        return self.ownerIndex == other.ownerIndex
            ? CollisionAction.CollisionKind.FriendlyToken
            : CollisionAction.CollisionKind.EnemyToken;
    }

    static void ApplyDamage(Token victim, Token striker, Token instigator, float impactSpeed)
    {
        if(impactSpeed < striker.GetMinDamageSpeed()) return;
        float damage = impactSpeed * striker.GetDamagePerSpeedUnit();
        victim.TakeDamage(damage, instigator);
    }

    //If mover is actively launched and hits resting mover on the same team, target
    //inherits the instigator so it can deal damage when it subsequently hits an enemy
    static void PropagateInstigator(Token mover, Token target)
    {
        if(mover.launchInstigator == null) return;
        if(mover.ownerIndex != target.ownerIndex) return; // not friendly
        if(target.launchInstigator != null) return; // already attributed

        target.launchInstigator = mover.launchInstigator;
        target.isMoving = true;
    }

    void ResolveWalls(Token t)
    {
        if(arena == null) return;

        Vector2 pos = t.Position2D;
        bool hit = false;
        Vector2 hitNormal = Vector2.zero;

        // Left wall
        if (pos.x - t.radius < arena.left)
        {
            pos.x       = arena.left + t.radius;
            t.velocity.x = Mathf.Abs(t.velocity.x) * t.wallRestitution;
            hitNormal   = Vector2.right;
            hit         = true;
        }
        // Right wall
        else if (pos.x + t.radius > arena.right)
        {
            pos.x       = arena.right - t.radius;
            t.velocity.x = -Mathf.Abs(t.velocity.x) * t.wallRestitution;
            hitNormal   = Vector2.left;
            hit         = true;
        }

        // Bottom wall (min Y)
        if (pos.y - t.radius < arena.bottom)
        {
            pos.y       = arena.bottom + t.radius;
            t.velocity.y = Mathf.Abs(t.velocity.y) * t.wallRestitution;
            hitNormal   = Vector2.up;
            hit         = true;
        }
        // Top wall (max Y)
        else if (pos.y + t.radius > arena.top)
        {
            pos.y       = arena.top - t.radius;
            t.velocity.y = -Mathf.Abs(t.velocity.y) * t.wallRestitution;
            hitNormal   = Vector2.down;
            hit         = true;
        }

        t.Position2D = pos;

        if (hit)
        {
            float wallImpactSpeed = t.velocity.magnitude;   // post-restitution speed
            t.onCollision?.Invoke(new CollisionAction
            {
                kind        = CollisionAction.CollisionKind.Wall,
                other       = null,
                impactSpeed = wallImpactSpeed,
                normal      = hitNormal
            });
        }
    }

    //Stable unique key for an unordered pair of list indices
    static long PairKey(int i, int j) => ((long)Mathf.Min(i,j) << 32) | (uint)Mathf.Max(i,j);

    //============================================================================

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.yellow;
        foreach (Token t in allTokens)
        {
            Vector3 center = t.transform.position;
            // Draw radius circle approximation (16 segments on XZ)
            for (int i = 0; i < 16; i++)
            {
                float a1 = i       / 16f * Mathf.PI * 2f;
                float a2 = (i + 1) / 16f * Mathf.PI * 2f;
                Gizmos.DrawLine(
                    center + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * t.radius,
                    center + new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2)) * t.radius);
            }
        }
    }
#endif
}
