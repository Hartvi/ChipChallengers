using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    // if an InputReceiver is turned on:
    //   lastKeyboardReceiver = currentKeyboardReceiver
    //   switch currentKeyboardReceiver to the last turned on InputReceiver
    // if the currentKeyboardReceiver is turned off:
    //   switch currentKeyboardReceiver to the lastKeyboardReceiver

    public static UIManager instance;

    private List<InputReceiver> keyboardReceivers = new List<InputReceiver>();

    void Awake()
    {
        UIManager.instance = this;
    }

    void Update()
    {
        keyboardReceivers[keyboardReceivers.Count - 1].HandleInputs();
    }

    public void SwitchToMe(InputReceiver me)
    {
        //print($"Switching to {me.GetType()}");
        while (keyboardReceivers.Remove(me)) { }
        keyboardReceivers.Add(me);
    }

    public void TurnMeOff(InputReceiver me)
    {
        //print($"Turning off {me.GetType()}");
        //print($"Before removing: {me.GetType()}: {keyboardReceivers.Count}");
        //keyboardReceivers.Remove(me);
        while (keyboardReceivers.Remove(me)) { }
        //print($"After removing: {me.GetType()}: {keyboardReceivers.Count}");
    }

}
