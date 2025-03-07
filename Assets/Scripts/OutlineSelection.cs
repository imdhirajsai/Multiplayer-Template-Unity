using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlineSelection : MonoBehaviour
{
    private Transform highlightedObject;
    private RaycastHit raycastHit;
    private Outline currentOutline;

    [Header("Outline Settings")]
    public Color outlineColor = Color.red;
    public float outlineWidth = 5.0f;

    void Update()
    {
        // Remove outline from previously highlighted object
        if (highlightedObject != null)
        {
            RemoveOutline();
            highlightedObject = null;
        }

        // Perform raycast from camera to the mouse position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out raycastHit))
        {
            highlightedObject = raycastHit.transform;

            // Check if the object has the "Player" tag
            if (highlightedObject.CompareTag("PlayerMale") || highlightedObject.CompareTag("PlayerFemale"))
            {
                AddOutline();
            }
        }
    }

    private void AddOutline()
    {
        Outline outline = highlightedObject.GetComponent<Outline>();

        if (outline == null)
        {
            outline = highlightedObject.gameObject.AddComponent<Outline>();
        }

        // Set outline properties
        outline.OutlineColor = outlineColor;
        outline.OutlineWidth = outlineWidth;
        outline.enabled = true;

        currentOutline = outline;
    }

    private void RemoveOutline()
    {
        if (currentOutline != null)
        {
            currentOutline.enabled = false;
        }
    }

    private void OnDisable()
    {
        if (currentOutline != null)
        {
            currentOutline.enabled = false;
        }
        this.enabled = false;
    }
}
