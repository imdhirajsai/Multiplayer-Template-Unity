using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonClickCaller : MonoBehaviour
{
    public Button maleOutfitButton; // The button you want to invoke
    public Button maleOutfit1Button; // The button you want to invoke
    public Button femaleOutfitButton; // The button you want to invoke
    public Button femaleOutfit1Button; // The button you want to invoke
    public Button femaleOutfit2Button; // The button you want to invoke
    public Button femaleOutfit3Button; // The button you want to invoke

 

    // This method will be called when the first button is clicked
    public void CallMaleTradtionalButtonOnClick()
    {
        if (maleOutfitButton != null)
        {
            // Invoke the onClick event of the target button
            maleOutfitButton.onClick.Invoke();
        }
        else
        {
            Debug.LogWarning("Target button is not assigned!");
        }
    }
    public void CallMaleTradtional1ButtonOnClick()
    {
        if (maleOutfit1Button != null)
        {
            // Invoke the onClick event of the target button
            maleOutfit1Button.onClick.Invoke();
        }
        else
        {
            Debug.LogWarning("Target button is not assigned!");
        }
    }

    public void CallFemaleTradtionalButtonOnClick()
    {
        if (femaleOutfitButton != null)
        {
            // Invoke the onClick event of the target button
            femaleOutfitButton.onClick.Invoke();
        }
        else
        {
            Debug.LogWarning("Target button is not assigned!");
        }
    }

    public void CallFemaleTradtional1ButtonOnClick()
    {
        if (femaleOutfit1Button != null)
        {
            // Invoke the onClick event of the target button
            femaleOutfit1Button.onClick.Invoke();
        }
        else
        {
            Debug.LogWarning("Target button is not assigned!");
        }
    } 
    
    public void CallFemaleTradtional2ButtonOnClick()
    {
        if (femaleOutfit2Button != null)
        {
            // Invoke the onClick event of the target button
            femaleOutfit2Button.onClick.Invoke();
        }
        else
        {
            Debug.LogWarning("Target button is not assigned!");
        }
    } 
    
    public void CallFemaleTradtional3ButtonOnClick()
    {
        if (femaleOutfit3Button != null)
        {
            // Invoke the onClick event of the target button
            femaleOutfit3Button.onClick.Invoke();
        }
        else
        {
            Debug.LogWarning("Target button is not assigned!");
        }
    }
    
    
}

