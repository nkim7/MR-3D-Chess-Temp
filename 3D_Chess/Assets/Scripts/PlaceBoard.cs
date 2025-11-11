using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceBoard : MonoBehaviour
{
    // Start is called before the first frame update
    public bool clicked;

    public void onClicked()
    {
        Debug.LogError("Clicked");

    }

    public void Update()
    {
        if (clicked)
        {
            Debug.LogError("Enabled");
        }
    }
}
