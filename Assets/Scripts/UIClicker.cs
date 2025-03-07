using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIClicker : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        // Check for mouse click
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Raycast from the camera to the mouse position
            if (Physics.Raycast(ray, out hit))
            {
                // Check if the clicked object has a specific tag or component
                if (hit.collider != null)
                {
                    // Perform an action on the clicked object
                    Debug.Log("Clicked on: " + hit.collider.name);

                    // You can execute your desired function here, for example:
                    PerformAction(hit.collider.gameObject);
                }
            }
        }
    }

    // Define what happens when the object is clicked
    void PerformAction(GameObject clickedObject)
    {
        // Example action: return the object name
        Debug.Log("Action performed on: " + clickedObject.name);

        // You can add any functionality here (e.g., return object name or trigger event)
    }
}

