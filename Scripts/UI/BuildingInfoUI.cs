using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingInfoUI : MonoBehaviour
{
    [SerializeField] private Transform iconTemplate;

    private BuildingStatus buildingStatus;

    //RESIDENT



    //VISITOR





    private void RegisterUI(Transform character, bool hasArrived)
    {
        //get color of the character
        Color skinColor= character.GetComponentInChildren<Material>().color;

        if (character.GetComponent<CharacterAgent>().IsResident())      //RESIDENT
        {

        }
        else    //VISITOR
        {

        }

        Transform newIcon= Instantiate(iconTemplate);
        newIcon.GetChild(1).GetComponent<Image>().color = skinColor;

        if(hasArrived) newIcon.gameObject.SetActive(true);
        else newIcon.gameObject.SetActive(true); newIcon.GetChild(1).gameObject.SetActive(false);

    }

    private void UpdateUI(Transform character)
    {

    }
}
