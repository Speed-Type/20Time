using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(Camera))]
public class CombatCameraController : MonoBehaviour
{
    //============================================================================

    //References
    [Header("References")]
    [Tooltip("Empty Transform that represents the top-down camera anchor. " +
             "Should match where your existing top-down camera parks itself.")]
    public Transform topDownAnchor;

    //Impact threshold
    [Header("Impact Threshold")]
    [Tooltip("Minimum impact speed to trigger any camera effect.")]
    public float minImpactSpeed = 2.5f;
    [Tooltip("Impact speed considered maximum for scaling effect intensity.")]
    public float maxImpactSpeed = 12f;
 
    //Cinematic cut
    [Header("Cinematic Side-On Cut")]
    [Tooltip("How far from the impact point the side-on camera sits.")]
    public float cinematicDistance = 7f;
    [Tooltip("Height of the side-on camera above the arena floor.")]
    public float cinematicHeight = 2.5f;
    [Tooltip("The side-on camera looks at this height on the impact point.")]
    public float cinematicLookHeight = 0.5f;
    [Tooltip("FOV used during the cinematic cut.")]
    public float cinematicFOV = 50f;
    [Tooltip("How fast the camera snaps to the cinematic position (unscaled).")]
    public float cinematicSnapSpeed = 22f;
    [Tooltip("How fast the camera returns to top-down after the cut (unscaled).")]
    public float cinematicReturnSpeed = 10f;
    [Tooltip("How long to hold the cinematic view before returning (real seconds). " +
             "Should be >= slowMoDuration so the cut covers the full slow-mo window.")]
    public float cinematicHoldDuration = 0.22f;
    [Tooltip("Intensity threshold (0-1) above which the cinematic cut fires. " +
             "Below this, only the top-down zoom plays.")]
    [Range(0f, 1f)]
    public float cinematicIntensityThreshold = 0.3f;

    //Zoom
    [Header("Top-Down Zoom (minor hits)")]
    public float normalFOV        = 60f;
    public float zoomedFOV        = 45f;
    public float zoomInSpeed      = 14f;
    public float zoomOutSpeed     = 4f;
    public float zoomHoldDuration = 0.3f;
 
    [Tooltip("How far the top-down camera shifts toward the impact point (0-1).")]
    [Range(0f, 1f)]
    public float zoomPositionBlend = 0.35f;
    public float positionLerpSpeed = 7f;

    //Slow mo
    [Header("Slow Motion")]
    [Range(0.02f, 1f)]
    public float slowMoScale      = 0.25f;
    public float slowMoDuration   = 0.14f;  // real seconds at slow speed
    public float slowMoRecoverTime = 0.4f;  // real seconds to ease back to 1x

    //Ortho support
    [Header("Orthographic (optional)")]
    [Tooltip("If your top-down camera is orthographic, enable this and set the " +
             "sizes instead of FOV values above.")]
    public bool useOrthographic = false;
    public float normalOrthoSize = 8f;
    public float zoomedOrthoSize = 5f;

    //============================================================================

    private Camera cam;
 
    // Top-down zoom
    private float   zoomT;
    private Vector3 currentPos;
    private bool    hasImpact;
    private Vector3 impactWorldPos;
    private Vector3 impactNormal;
 
    // Multi-collision frame guard
    private bool    collisionThisFrame;
    private float   frameMaxIntensity;
    private Vector3 frameImpactPos;
    private Vector3 frameImpactNormal;
 
    // Cinematic
    private bool       inCinematic;
    Vector3    committedPerp;      // locked side direction for the current sequence
    bool       hasCommittedPerp;   // false until the first cut of a sequence picks a side
    private Vector3    cinematicTargetPos;
    private Quaternion cinematicTargetRot;
    private float      cinematicTargetFOV;
 
    // Slow mo
    private bool  inSlowMo;
    private float targetTimeScale = 1f;
 
    // Coroutines
    private Coroutine zoomRoutine;
    private Coroutine slowMoRoutine;
    private Coroutine cinematicRoutine;

    private Token targetToken;

    //============================================================================
    void Awake()
    {
        cam = GetComponent<Camera>();
        currentPos = topDownAnchor != null ? topDownAnchor.position : transform.position;
        
        if (useOrthographic) cam.orthographic = true;
        ApplyTopDownZoom(0f);
    }

    void Update()
    {

        //Resolve all collisions that fired this FixedUpdate into one camera event
        if (collisionThisFrame)
        {
            ProcessImpact(frameMaxIntensity, frameImpactPos, frameImpactNormal);
            collisionThisFrame = false;
            frameMaxIntensity  = 0f;
        }
 
        UpdateSlowMo();
 
        //Top-down zoom and position only when not in a cinematic cut
        if (!inCinematic)
        {
            ApplyTopDownZoom(zoomT);
            UpdateTopDownPosition();
        }
    }

    //=============================Public Methods===============================================

    //Call this when a token is launched. The camera will blend from the
    //current top-down view to the side-on follow view.
    public void EnterFollowMode(Token token, Action onCompleteAction = null)
    {
        if(token == null) return;

        targetToken = token;

        //Remove first to avoid double-subscription if called twice
        targetToken.onAttack.RemoveListener(OnCollision);
        targetToken.onAttack.AddListener(OnCollision);

        //TEMP
        onCompleteAction();
    }

    //Manually force an exit from follow mode (e.g. turn ends early)
    //Normally called automatically when the token comes to rest
    public void ExitFollowMode(Action onCompleteAction = null)
    {
        targetToken.onAttack.RemoveListener(OnCollision);
        
        //TEMP
        onCompleteAction();
    }

    //============================Collisions================================================
    
    void OnCollision(CollisionAction action)
    {
        //Don't do any impact effects if we're below the speed threshold or this is a wall hit
        if (action.impactSpeed < minImpactSpeed || action.other == null) return;
 
        float intensity = Mathf.Clamp01(
            (action.impactSpeed - minImpactSpeed) / (maxImpactSpeed - minImpactSpeed));
 
        Vector3 impactPos    = action.other != null
            ? action.other.transform.position
            : Vector3.zero;
        Vector3 impactNormal = new Vector3(action.normal.x, 0f, action.normal.y);
 
        // Keep only the strongest hit per frame
        if (!collisionThisFrame || intensity > frameMaxIntensity)
        {
            frameMaxIntensity  = intensity;
            frameImpactPos     = impactPos;
            frameImpactNormal  = impactNormal;
        }
        collisionThisFrame = true;
    }

    void ProcessImpact(float intensity, Vector3 impactPos, Vector3 pImpactNormal)
    {
        impactWorldPos = impactPos;
        impactNormal   = pImpactNormal;
        hasImpact      = true;
 
        //Slow-mo always fires for any registered hit
        if (slowMoRoutine != null) StopCoroutine(slowMoRoutine);
        slowMoRoutine = StartCoroutine(SlowMoRoutine(intensity));
 
        if(intensity >= cinematicIntensityThreshold)
        {
            // Strong hit → cinematic side-on cut
            ComputeCinematicTransform(impactPos, impactNormal, intensity);
            if (cinematicRoutine != null) StopCoroutine(cinematicRoutine);
            cinematicRoutine = StartCoroutine(CinematicCutRoutine());
            if (zoomRoutine != null) StopCoroutine(zoomRoutine);
            zoomT = 0f;
        }
        else
        {
            //Weak hit → top-down zoom only, no cut
            if (zoomRoutine != null) StopCoroutine(zoomRoutine);
            zoomRoutine = StartCoroutine(ZoomRoutine(intensity));
        }
    }

    //============================Cinematic Cuts================================================

    void ComputeCinematicTransform(Vector3 impactPos, Vector3 impactNormal, float intensity)
    {
        // Only pick a side the first time in a sequence.
        // Mid-sequence hits (bounces, chains) reuse the same side so the camera
        // doesn't jump across the arena when the collision normal reverses.
        if (!hasCommittedPerp)
        {
            committedPerp    = Vector3.Cross(impactNormal.normalized, Vector3.up).normalized;
            if (committedPerp == Vector3.zero) committedPerp = Vector3.right;
            hasCommittedPerp = true;
        }
 
        cinematicTargetPos = impactPos
                            + committedPerp * cinematicDistance
                            + Vector3.up     * cinematicHeight;
 
        Vector3 lookAt      = impactPos + Vector3.up * cinematicLookHeight;
        cinematicTargetRot = Quaternion.LookRotation(lookAt - cinematicTargetPos, Vector3.up);
 
        // Tighter FOV on harder hits
        cinematicTargetFOV = Mathf.Lerp(cinematicFOV + 8f, cinematicFOV, intensity);
    }
 
    IEnumerator CinematicCutRoutine()
    {
        inCinematic = true;
 
        Vector3    fromPos = transform.position;
        Quaternion fromRot = transform.rotation;
        float      fromFOV = cam.fieldOfView;
 
        // ── Snap to cinematic ──────────────────────────────────────────────
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * cinematicSnapSpeed;
            float ease = EaseOutCubic(Mathf.Clamp01(t));
 
            // Recompute target position every frame so the camera chases the token
            Vector3 liveTarget = GetLiveTrackPoint();
            Vector3 livePos    = liveTarget + committedPerp * cinematicDistance
                                            + Vector3.up     * cinematicHeight;
            Vector3 liveLookAt = liveTarget + Vector3.up * cinematicLookHeight;
 
            transform.position = Vector3.Lerp(fromPos, livePos, ease);
            transform.rotation = Quaternion.Slerp(fromRot,
                                     Quaternion.LookRotation(liveLookAt - transform.position, Vector3.up),
                                     ease);
            cam.fieldOfView   = Mathf.Lerp(fromFOV, cinematicTargetFOV, ease);
            yield return null;
        }
 
        // ── Hold ──────────────────────────────────────────────────────────
        float holdElapsed = 0f;
        while (holdElapsed < cinematicHoldDuration)
        {
            holdElapsed += Time.unscaledDeltaTime;
 
            Vector3 liveTarget = GetLiveTrackPoint();
            Vector3 livePos    = liveTarget + committedPerp * cinematicDistance
                                            + Vector3.up     * cinematicHeight;
            Vector3 liveLookAt = liveTarget + Vector3.up * cinematicLookHeight;
 
            // Smooth follow during hold so the camera doesn't snap if the token is fast
            transform.position = Vector3.Lerp(transform.position, livePos,
                                     Time.unscaledDeltaTime * cinematicSnapSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                                     Quaternion.LookRotation(liveLookAt - transform.position, Vector3.up),
                                     Time.unscaledDeltaTime * cinematicSnapSpeed);
            yield return null;
        }
 
        // ── Return to top-down ────────────────────────────────────────────
        // Re-snapshot so if we snapped imperfectly we return from where we are
        fromPos = transform.position;
        fromRot = transform.rotation;
        fromFOV = cam.fieldOfView;
        t = 0f;
 
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * cinematicReturnSpeed;
            float ease = EaseOutCubic(Mathf.Clamp01(t));
 
            // Track the anchor live so the return always lands correctly
            Vector3    toPos = topDownAnchor != null ? topDownAnchor.position : fromPos;
            Quaternion toRot = topDownAnchor != null ? topDownAnchor.rotation : fromRot;
 
            transform.position = Vector3.Lerp(fromPos, toPos, ease);
            transform.rotation = Quaternion.Slerp(fromRot, toRot, ease);
            cam.fieldOfView   = Mathf.Lerp(fromFOV, normalFOV, ease);
            yield return null;
        }
 
        inCinematic = false;
        hasImpact   = false;
        hasCommittedPerp = false;   // unlock side for next sequence
        currentPos  = transform.position;
        ApplyTopDownZoom(0f);
    }

    //============================Zoom================================================

    IEnumerator ZoomRoutine(float intensity)
    {
        float startT  = zoomT;
        float target  = intensity;
        float elapsed = 0f;
 
        while (elapsed < 1f)
        {
            elapsed += Time.unscaledDeltaTime * zoomInSpeed;
            zoomT   = Mathf.Lerp(startT, target, Mathf.Clamp01(elapsed));
            yield return null;
        }
        zoomT = target;
 
        yield return new WaitForSecondsRealtime(zoomHoldDuration);
 
        startT  = zoomT;
        elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.unscaledDeltaTime * zoomOutSpeed;
            zoomT   = Mathf.Lerp(startT, 0f, Mathf.Clamp01(elapsed));
            yield return null;
        }
 
        zoomT     = 0f;
        hasImpact = false;
    }
 
    void ApplyTopDownZoom(float t)
    {
        if (useOrthographic)
            cam.orthographicSize = Mathf.Lerp(normalOrthoSize, zoomedOrthoSize, t);
        else
            cam.fieldOfView = Mathf.Lerp(normalFOV, zoomedFOV, t);
    }
 
    void UpdateTopDownPosition()
    {
        if (topDownAnchor == null) return;
 
        Vector3 anchorPos      = topDownAnchor.position;
        transform.rotation     = topDownAnchor.rotation;
 
        Vector3 targetPos;
        if (hasImpact && zoomT > 0.01f)
        {
            Vector3 shiftedPos = new Vector3(impactWorldPos.x, anchorPos.y, impactWorldPos.z);
            targetPos = Vector3.Lerp(anchorPos, shiftedPos, zoomPositionBlend);
        }
        else
        {
            targetPos = anchorPos;
        }
 
        currentPos        = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * positionLerpSpeed);
        transform.position = currentPos;
    }

    //=============================Slow mo===============================================

    IEnumerator SlowMoRoutine(float intensity)
    {
        inSlowMo           = true;
        float scale         = Mathf.Lerp(1f, slowMoScale, intensity);
        Time.timeScale      = scale;
        Time.fixedDeltaTime = 0.02f * scale;
 
        yield return new WaitForSecondsRealtime(slowMoDuration);
 
        targetTimeScale = 1f;
        inSlowMo        = false;
    }
 
    void UpdateSlowMo()
    {
        if (inSlowMo) return;
        if (Mathf.Abs(Time.timeScale - targetTimeScale) > 0.001f)
        {
            Time.timeScale      = Mathf.MoveTowards(Time.timeScale, targetTimeScale,
                                      Time.unscaledDeltaTime / slowMoRecoverTime);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
    }

    //=============================Helpers===============================================

    static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    //Returns the world position the cinematic camera should orbit around.
    //Uses the live token position when available, falls back to the stored
    //impact point if targetToken is unassigned or has come to rest.
    Vector3 GetLiveTrackPoint()
    {
        if (targetToken != null)
            return targetToken.transform.position;
        return impactWorldPos;
    }

    
    //=============================Nice gizmo===============================================

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(impactWorldPos, 0.4f);
        if (topDownAnchor != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(topDownAnchor.position, 0.3f);
            Gizmos.DrawLine(transform.position, topDownAnchor.position);
        }
    }
#endif
}
