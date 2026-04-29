using UnityEngine;

public class MainMenuManager : MonoBehaviour
{

    //============================================================================

    private void Start()
    {
        
    }

    //============================Popup System================================================

    public void OpenPopup(GameObject target)
    {
        target.SetActive(true);
    }

    public void ClosePopup(GameObject target)
    {
        target.SetActive(false);
    }
}
