using System;
using UnityEngine;
using UnityEngine.UI;

public class BaseCombatEntity : MonoBehaviour
{
    [Header("References")]
    public BaseIconInteractable iconController;
    public GladiatorAnimManager animController;
    public Player parentPlayer;
    [SerializeField] private Image healthbar;
    [SerializeField] private GameObject baseplate;

    [Header("Entity Info")]
    public String entityName;

    [Header("General Parameters")]
    [Tooltip("Whether or not a player can ever move this entity")]
    public bool isStatic = false;

    [Header("Movement")]
    [Tooltip("The max pullback (radius) for the icon interactable")]
    public float maxPullback;

    [Tooltip("Multiplier for speed")]
    public float speedMultiplier;

    //Combat
    [Header("Combat")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Tooltip("Damage dealt per unit of impact speed when hitting an enemy token.")]
    public float damagePerSpeedUnit = 5f;

    [Tooltip("Minimum relative speed required to register a damaging hit.")]
    public float minDamageSpeed = 0.5f;

    //============================================================================

    [HideInInspector] public bool isCorrectTurn;
    private WorldspaceCanvasSyncUtility syncUtility;
    private Token token;

    //============================================================================

    private void Awake()
    {
        //Call setup for icon controller
        iconController.SetBaseCombatEntity(this);
        iconController.SetOnReleaseAction(Release);

        //Initialize references
        syncUtility = iconController.gameObject.GetComponent<WorldspaceCanvasSyncUtility>();
        token = GetComponent<Token>();

        //Initialize values
        currentHealth = maxHealth;
    }

    private void Start()
    {
        //Now that at this point the parentPlayer has been set, update accordingly
        token.ownerIndex = parentPlayer.playerID;
        gameObject.name = parentPlayer.playerID + "'s " + entityName;
        baseplate.GetComponent<Renderer>().material.color = parentPlayer.playerColor;

        //Update healthbars
        UpdateHealthbars();
    }

    //============================================================================

    public void InitializeInteractable()
    {
        SetInteractable(parentPlayer.isThisPlayersTurn);
    }
    
    public void SetInteractable(bool interactable)
    {
        isCorrectTurn = interactable;
        iconController.gameObject.GetComponent<Image>().raycastTarget = interactable;
        syncUtility.SnapUIToObject();
    }

    //============================================================================

    private void Release(Vector3 launchDirection, float launchSpeed)
    {
        Debug.Log(launchDirection);
        Debug.Log(launchSpeed);

        parentPlayer.PauseTurn(); //Stop any interactability

        CombatManager.instance.SwitchCamToFollow(token, () =>
        {
            //Wait a bit, then call token launch
            CollisionSystem.instance.tokensAreMoving = true;
            token.Launch(new Vector2(launchDirection.x, launchDirection.z), launchSpeed * speedMultiplier);
        
            DamageIndicator.instance.Reset();
        });
    }

    public void TakeDamage(float amount, Token source)
    {
        Debug.Log(gameObject.name + "Took " + amount + " damage");
        if(amount <= 0f) return;

        animController.PlayAnim(AnimClips.TakeDamage);

        //Actually decrease health
        currentHealth -= amount;

        //Update healthbars
        UpdateHealthbars();
        
        //Do other damage handling here

        if(currentHealth <= 0f)
        {
            Die();
        }
    }

    private void UpdateHealthbars()
    {
        float fillPercent = Mathf.Clamp01(currentHealth / maxHealth);
        healthbar.fillAmount = fillPercent;
    }

    //Method that is called once health hits 0
    private void Die()
    {
        //First, remove the body model from this parent object
        token.gladiatorModel.transform.SetParent(CombatManager.instance.combatEnvironmentTransform);
        //Set its y position properly with a tween
        LeanTween.moveY(token.gladiatorModel, CombatManager.instance.combatEnvironmentTransform.position.y + 0.05f, 0.5f);

        //Play the animation
        animController.PlayAnim(AnimClips.Die);
        CombatCameraController.instance.DeathImpact(); //do cinematic zoom-in

        //Remove this token
        CollisionSystem.instance.Unregister(token); //This is already handled in Token, but just in case
        parentPlayer.ownedEntities.Remove(this);

        //Destroy objects
        Destroy(iconController.gameObject);
        Destroy(this.gameObject);
    }
}
