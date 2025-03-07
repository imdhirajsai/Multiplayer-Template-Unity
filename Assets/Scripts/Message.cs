using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Message : MonoBehaviour
{
    public TMP_Text MyMessage;
    void Start()
    {
        GetComponent<RectTransform>().SetAsFirstSibling();  
    }
}
