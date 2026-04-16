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
    [Tooltip("A token is considered 'at rest' when its speed falls below this value")]
    public float restSpeedThreshold = 0.05f;

    [Tooltip("How many sub-steps to run per FixedUpdate for better tunnelling resistance; 1 is fine for most cases; raise to 2-4 for very fast tokens")]
    [Range(1, 8)]
    public int subSteps = 1;

    [Tooltip("Fraction of overlap to correct each step (0-1). Lower = softer separation, higher = stiffer but can jitter")]
    [Range(0.1f, 1f)]
    public float depenetrationFactor = 0.5f;

    //============================================================================

    [HideInInspector] public bool tokensAreMoving;

    readonly List<Token> allTokensList = new List<Token>();

    //Pair set prevents double-processing the same pair in one frame
    readonly HashSet<long> resolvedPairs = new HashSet<long>();

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

        //TODO: change the check for list length to a check whether there are any tokens still moving
        bool restCheck = true;
        foreach(Token t in allTokensList)
        {
            if(t.isLaunched)
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

        for(int step = 0; step < subSteps; step++)
        {
            // 1. Drag + integrate
            foreach(Token t in allTokensList)
                if(t.isLaunched) StepToken(t, dt);

            // 2. Token–token collisions
            for(int i = 0; i < allTokensList.Count; i++)
            for(int j = i + 1; j < allTokensList.Count; j++)
            {
                long pairKey = PairKey(i, j);
                if(resolvedPairs.Contains(pairKey)) continue;
                if(ResolveTokenPair(allTokensList[i], allTokensList[j]))
                    resolvedPairs.Add(pairKey);
            }

            // 3. Wall collisions
            foreach (Token t in allTokensList)
                ResolveWalls(t);
        }

        // 4. Mark resting tokens
        foreach(Token t in allTokensList)
        {
            if (!t.isLaunched) continue;
            if (t.velocity.sqrMagnitude < restSpeedThreshold * restSpeedThreshold)
            {
                t.velocity         = Vector2.zero;
                t.isLaunched       = false;
                t.launchInstigator = null;
            }
        }
    }

    //============================================================================

    //Register a token so the system starts simulating it.
    //Token.Launch() calls this automatically; you shouldn't need to call it directly.
    public void Register(Token token)
    {
        if (!allTokensList.Contains(token))
            allTokensList.Add(token);
    }

    //Immediately remove a token from simulation (e.g. when it's destroyed)
    public void Unregister(Token token) => allTokensList.Remove(token);

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

        if(distSq >= minDist * minDist) return false; //no overlap

        float dist   = Mathf.Sqrt(distSq);
        Vector2 norm = dist > 1e-6f ? delta / dist : Vector2.right; //safe normal

        //Positional depenetration
        float overlap = minDist - dist;
        float totalMass = a.mass + b.mass;
        float pushA = (b.mass / totalMass) * overlap * depenetrationFactor;
        float pushB = (a.mass / totalMass) * overlap * depenetrationFactor;

        a.Position2D -= norm * pushA;
        b.Position2D += norm * pushB;

        //Elastic impulse
        // Relative velocity along collision normal
        Vector2 relVel = a.velocity - b.velocity;
        float relVelN = Vector2.Dot(relVel, norm);

        //Only resolve if tokens are moving toward each other
        if(relVelN > 0f) return false;

        float restitution = Mathf.Min(a.tokenRestitution, b.tokenRestitution);
        float impulseMag = -(1f + restitution) * relVelN / (1f / a.mass + 1f / b.mass);

        Vector2 impulse = impulseMag * norm;
        a.velocity += impulse / a.mass;
        b.velocity -= impulse / b.mass;

        float impactSpeed = Mathf.Abs(relVelN);

        //Damage & collision events
        FireTokenCollisionEvents(a, b, norm, impactSpeed);

        return true;
    }

    void FireTokenCollisionEvents(Token a, Token b, Vector2 norm, float impactSpeed)
    {
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
        //The instigator (original launcher) is credited for any knock-on damage.
        if(instigatorA != null && instigatorA.ownerIndex != b.ownerIndex)
            ApplyDamage(b, a, instigatorA, impactSpeed);

        if(instigatorB != null && instigatorB.ownerIndex != a.ownerIndex)
            ApplyDamage(a, b, instigatorB, impactSpeed);

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
        target.isLaunched = true;
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

        // Bottom wall (min Z)
        if (pos.y - t.radius < arena.bottom)
        {
            pos.y       = arena.bottom + t.radius;
            t.velocity.y = Mathf.Abs(t.velocity.y) * t.wallRestitution;
            hitNormal   = Vector2.up;
            hit         = true;
        }
        // Top wall (max Z)
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
        foreach (Token t in allTokensList)
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
