using Photon.Pun;
using Photon.Voice.PUN;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceManager : MonoBehaviour
{
    private UIManagement UI;
    private GameObject UIObject;
    private void Awake()
    {
        UIObject = GameObject.FindGameObjectWithTag("UI"); // Find UI object by tag
        if (UIObject != null)
        {
            UI = UIObject.GetComponent<UIManagement>();
           
        }
        else
        {
            Debug.LogError("UIObject not found in the scene. Ensure it is tagged as 'UI'.");
        }

    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

   

  
  
}
