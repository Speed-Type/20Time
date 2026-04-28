using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class DamageIndicator : MonoBehaviour
{
    public static DamageIndicator instance;

    [Header("References")]
    public TMP_Text damageSumDisplay;
    public Transform spawnParentTransform;
    public GameObject damageTextPrefab;

    [Header("List/Timing Settings")]
    public int maxListLength = 5;
    public float timeBeforeMergeStart;
    public float timeBetweenMerges;

    //============================================================================

    private List<DamageText> damageTextList;
    private float damageSum;
    private Coroutine mergeCoroutine;

    //============================================================================

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        damageTextList = new List<DamageText>();
        Reset();
    }

    //============================================================================

    public void Reset()
    {
        //Clear the list
        foreach(DamageText damageText in damageTextList)
        {
            Destroy(damageText.gameObject);
        }

        damageTextList = new List<DamageText>();

        damageSum = 0;

        damageSumDisplay.text = Round(damageSum).ToString();
    }

    public void AddDamage(string descriptor, float damage)
    {
        //Instantiate the actual damage text
        DamageText damageText = Instantiate(damageTextPrefab, spawnParentTransform.position, spawnParentTransform.rotation, spawnParentTransform).GetComponent<DamageText>();

        //Initialize its values
        damageText.Initialize(descriptor, damage, timeBetweenMerges);

        //Add to list
        damageTextList.Add(damageText);

        //If we're over the limit, quickly pop one
        if(damageTextList.Count == maxListLength) Pop();
        
        //End current merge coroutine (if it exists) and start the new one
        if(mergeCoroutine != null) StopCoroutine(mergeCoroutine);
        mergeCoroutine = StartCoroutine(MergeDamage());
    }

    IEnumerator MergeDamage()
    {
        yield return new WaitForSeconds(timeBeforeMergeStart);

        //Repeat until damage text list is empty
        while(damageTextList.Count > 0)
        {
            Pop();

            yield return new WaitForSeconds(timeBetweenMerges);
        }
    }

    //============================================================================

    public static float Round(float f)
    {
        return (float)Math.Round((double)f, 2, MidpointRounding.AwayFromZero);
    }

    //Method to remove the first element of the list and increment sum accordingly
    private void Pop()
    {
        damageSum += damageTextList[0].Merge();
        damageSumDisplay.text = Round(damageSum).ToString();
        damageTextList.RemoveAt(0);
    }
}
