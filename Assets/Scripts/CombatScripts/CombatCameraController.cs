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

    //Following
    [Header("Follow — Side-on Geometry")]
    [Tooltip("How far to the side of the token the camera sits.")]
    public float sideOffset = 6f;

    [Tooltip("How high above the token the camera sits.")]
    public float heightOffset = 1.8f;

    [Tooltip("How far behind the token (along its travel direction) the camera sits.")]
    public float behindOffset = 2.5f;

    [Tooltip("The camera looks at this point relative to the token — raise it slightly " +
             "so the token sits in the lower third of the frame.")]
    public float lookTargetHeightBias = 0.4f;

    //FOV Control
    [Header("Follow — FOV")]
    public float baseFOV = 55f;
    public float maxSpeedFOV = 70f;
    [Tooltip("Token speed at which FOV reaches maxSpeedFOV.")]
    public float fovMaxSpeed = 20f;
    public float fovSmoothTime = 0.25f;

    //Transitions
    [Header("Transition")]
    [Tooltip("How long the blend from top-down → follow takes.")]
    public float transitionInDuration  = 0.6f;
    [Tooltip("How long the blend from follow → top-down takes.")]
    public float transitionOutDuration = 0.9f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    //Pause State
    [Header("Linger")]
    [Tooltip("How long the camera holds on the resting token before returning.")]
    public float lingerDuration = 1.6f;

    //Slow Mo Controls
    [Header("Slow Motion")]
    [Tooltip("Time scale during a collision slow-mo hit.")]
    [Range(0.05f, 1f)]
    public float slowMoScale       = 0.2f;
    [Tooltip("How long the slow-mo dip lasts (real seconds).")]
    public float slowMoDuration    = 0.18f;
    [Tooltip("How long to ease back to normal time after the dip (real seconds)")]
    public float slowMoRecoverTime = 0.35f;
    [Tooltip("Minimum impact speed (from CollisionAction.impactSpeed) to trigger slow-mo")]
    public float slowMoMinImpact   = 3f;

    //Collision Zoom
    [Header("Collision Zoom Punch")]
    [Tooltip("How far the camera pulls back on impact.")]
    public float punchPullback   = 1.5f;
    [Tooltip("How quickly the punch lerps in and out.")]
    public float punchLerpSpeed  = 12f;

    //Smoothing
    [Header("Smoothing")]
    public float positionSmoothTime  = 0.08f;
    public float rotationSmoothSpeed = 8f;

    //============================================================================

    private enum CamState { TopDown, TransitionIn, Following, Lingering, TransitionOut }

    private Camera cam;
    private CamState state = CamState.TopDown;
    private Token followTarget;

    //Smooth follow
    private Vector3 posVelocity;
    private float fovVelocity;
    private float currentFOV;

    //Transition
    private float transitionT; //0 to 1
    private Vector3 transitionFromPos;
    private Quaternion transitionFromRot;
    private float transitionFromFOV;

    //Linger
    private float lingerTimer;

    //Slow mo
    private bool inSlowMo;
    private float slowMoTimer;
    private float followTargetTimeScale = 1f;

    //Collision punch
    private float punchOffset; //Extra behind-offset applied during punch
    private float punchTarget;

    //Desired follow transform (computed each frame in Following state)
    private Vector3 desiredPos;
    private Quaternion desiredRot;
    private float desiredFOV;

    //============================================================================
    void Awake()
    {
        cam = GetComponent<Camera>();
        currentFOV = cam.fieldOfView;
    }

    void OnEnable() //?????????????????
    {
        //Hook into Token collision events when we start following (done in EnterFollowMode)
    }

    void Update()
    {
        UpdateSlowMo();

        switch(state)
        {
            case CamState.TopDown:       break;
            case CamState.TransitionIn:  UpdateTransitionIn();  break;
            case CamState.Following:     UpdateFollowing();      break;
            case CamState.Lingering:     UpdateLingering();      break;
            case CamState.TransitionOut: UpdateTransitionOut();  break;
        }
    }

    //=============================Public Methods===============================================

    //Call this when a token is launched. The camera will blend from the
    //current top-down view to the side-on follow view.
    public void EnterFollowMode(Token token, Action onCompleteAction = null)
    {
        if(token == null) return;

        //Unsubscribe from any previous target
        if(followTarget != null)
            followTarget.onCollision.RemoveListener(OnTargetCollision);

        followTarget = token;
        followTarget.onCollision.AddListener(OnTargetCollision);

        //Snapshot current camera state as the "from" for the transition
        transitionFromPos = transform.position;
        transitionFromRot = transform.rotation;
        transitionFromFOV = cam.fieldOfView;
        transitionT = 0f;

        punchOffset = 0f;
        punchTarget = 0f;

        state = CamState.TransitionIn;

        if(onCompleteAction != null) afterTransitionToFollow = onCompleteAction;
    }

    //Manually force an exit from follow mode (e.g. turn ends early)
    //Normally called automatically when the token comes to rest
    public void ExitFollowMode(Action onCompleteAction = null)
    {
        if(state == CamState.TopDown) return;
        BeginTransitionOut();

        if(onCompleteAction != null) afterTransitionToTopdown = onCompleteAction;
    }

    //============================State Updates================================================
    private Action afterTransitionToFollow;
    void UpdateTransitionIn()
    {
        transitionT += Time.deltaTime / transitionInDuration;
        transitionT = Mathf.Clamp01(transitionT);

        float t = transitionCurve.Evaluate(transitionT);

        //Compute where the follow camera wants to be right now
        ComputeDesiredFollowTransform();

        transform.position = Vector3.Lerp(transitionFromPos, desiredPos, t);
        transform.rotation = Quaternion.Slerp(transitionFromRot, desiredRot, t);
        cam.fieldOfView = Mathf.Lerp(transitionFromFOV, desiredFOV, t);

        if(transitionT >= 1f)
        {
            state = CamState.Following;
            if(afterTransitionToFollow != null) afterTransitionToFollow();
        }
    }

    void UpdateFollowing()
    {
        if(followTarget == null)
        {
            BeginLinger(); 
            return;
        }

        //If token came to rest, start lingering
        if(!followTarget.isMoving)
        {
            BeginLinger();
            return;
        }

        ComputeDesiredFollowTransform();

        //Smooth punch offset back toward zero
        punchOffset = Mathf.Lerp(punchOffset, punchTarget, Time.deltaTime * punchLerpSpeed);
        punchTarget = Mathf.Lerp(punchTarget, 0f, Time.deltaTime * punchLerpSpeed);

        //Apply punch to desired position (pull camera back along its own forward)
        Vector3 punchedPos = desiredPos - transform.forward * punchOffset;

        transform.position = Vector3.SmoothDamp(transform.position, punchedPos, ref posVelocity, positionSmoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * rotationSmoothSpeed);
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, desiredFOV, ref fovVelocity, fovSmoothTime);
    }

    void UpdateLingering()
    {
        //Keep loosely tracking the resting token position
        if(followTarget != null)
        {
            ComputeDesiredFollowTransform();
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref posVelocity, positionSmoothTime * 3f);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * rotationSmoothSpeed * 0.5f);
        }

        lingerTimer -= Time.deltaTime;
        if(lingerTimer <= 0f)
            BeginTransitionOut();
    }

    private Action afterTransitionToTopdown;
    void UpdateTransitionOut()
    {
        transitionT += Time.deltaTime / transitionOutDuration;
        transitionT  = Mathf.Clamp01(transitionT);

        float t = transitionCurve.Evaluate(transitionT);

        //Blend toward wherever the top-down anchor currently is
        Vector3 tdPos = topDownAnchor != null ? topDownAnchor.position : transitionFromPos;
        Quaternion tdRot = topDownAnchor != null ? topDownAnchor.rotation : transitionFromRot;
        float tdFOV = topDownAnchor != null ? transitionFromFOV : baseFOV;

        transform.position = Vector3.Lerp(transitionFromPos, tdPos, t);
        transform.rotation = Quaternion.Slerp(transitionFromRot, tdRot, t);
        cam.fieldOfView = Mathf.Lerp(transitionFromFOV, tdFOV, t);

        if(transitionT >= 1f)
        {
            state = CamState.TopDown;
            if(followTarget != null)
            {
                followTarget.onCollision.RemoveListener(OnTargetCollision);
                followTarget = null;
            }

            if(afterTransitionToTopdown != null) afterTransitionToTopdown();
        }
    }

    //=============================Helper Methods===============================================

    void ComputeDesiredFollowTransform()
    {
        if(followTarget == null) return;

        Vector3 tokenPos = followTarget.transform.position;
        Vector2 vel2D = followTarget.velocity;

        //Travel direction on XZ; fall back to camera's current right if nearly stopped
        Vector3 travelDir = vel2D.sqrMagnitude > 0.01f
            ? new Vector3(vel2D.x, 0f, vel2D.y).normalized
            : transform.right;

        //Side-on: camera sits to the right of the travel direction (cross with up)
        Vector3 sideDir = Vector3.Cross(travelDir, Vector3.up).normalized;

        desiredPos = tokenPos
                    + sideDir   * sideOffset
                    + Vector3.up * heightOffset
                    - travelDir  * behindOffset;

        Vector3 lookAt = tokenPos + Vector3.up * lookTargetHeightBias;
        desiredRot = Quaternion.LookRotation(lookAt - desiredPos, Vector3.up);

        //FOV breathes with speed
        float speed  = vel2D.magnitude;
        float speedT = Mathf.Clamp01(speed / fovMaxSpeed);
        desiredFOV = Mathf.Lerp(baseFOV, maxSpeedFOV, speedT);
    }

    void BeginLinger()
    {
        lingerTimer = lingerDuration;
        state = CamState.Lingering;
    }

    void BeginTransitionOut()
    {
        transitionFromPos = transform.position;
        transitionFromRot = transform.rotation;
        transitionFromFOV = cam.fieldOfView;
        transitionT = 0f;
        state = CamState.TransitionOut;
    }

    //==========================Collision Response==================================================

    void OnTargetCollision(CollisionAction action)
    {
        if(action.impactSpeed < slowMoMinImpact) return;

        //Trigger slow mo
        if(!inSlowMo)
            StartCoroutine(SlowMoRoutine());

        //Trigger zoom punch; scale with impact speed
        float punchScale = Mathf.Clamp01(action.impactSpeed / (slowMoMinImpact * 3f));
        punchTarget = punchPullback * punchScale;
    }

    //===============================Slow Motion=============================================

    void UpdateSlowMo()
    {
        //Smoothly move timeScale toward target (recover from slow mo)
        if(Mathf.Abs(Time.timeScale - followTargetTimeScale) > 0.001f)
        {
            Time.timeScale = Mathf.MoveTowards(Time.timeScale, followTargetTimeScale, Time.unscaledDeltaTime / slowMoRecoverTime);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
    }

    IEnumerator SlowMoRoutine()
    {
        inSlowMo = true;
        followTargetTimeScale = slowMoScale;
        Time.timeScale = slowMoScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(slowMoDuration);

        followTargetTimeScale = 1f;
        inSlowMo = false;
        //UpdateSlowMo() handles the smooth recovery each frame
    }

    //===============================Gizmo=============================================

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (followTarget == null || topDownAnchor == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, followTarget.transform.position);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(topDownAnchor.position, 0.3f);
    }
#endif
}
