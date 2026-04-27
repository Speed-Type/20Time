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
    public int listMaxLength;
    public float timeBeforeMergeStart;
    public float timeBetweenMerges;

    //============================================================================

    private List<DamageText> damageTextList;
    private float damageSum;
    private Coroutine mergeCoroutine;

    //============================================================================

    private void Start()
    {
        damageTextList = new List<DamageText>();
        Reset();
    }

    //============================================================================

    public void Reset()
    {
        damageSum = 0;
    }

    public void AddDamage(string descriptor, float damage)
    {
        //Instantiate the actual damage text
        DamageText damageText = Instantiate(damageTextPrefab, spawnParentTransform.position, spawnParentTransform.rotation, spawnParentTransform).GetComponent<DamageText>();

        //Initialize its values
        damageText.Initialize(descriptor, damage);

        //Add to list
        damageTextList.Add(damageText);
        
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
            //Remove the first added one and add to sum
            damageSum += damageTextList[0].Merge();
            damageTextList.RemoveAt(0);

            yield return new WaitForSeconds(timeBetweenMerges);
        }
    }

    //============================================================================

    private int Round(float f)
    {
        return (int)f;
    }
}
