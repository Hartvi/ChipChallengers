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
    private int previousReceiverLength = 0;
    private InputReceiver previousReceiver;

    void Awake()
    {
        UIManager.instance = this;
    }

    void Update()
    {
        this.keyboardReceivers[this.keyboardReceivers.Count - 1].CoreHandleInputs();

        if (this.keyboardReceivers.Count != this.previousReceiverLength) {
            this.previousReceiver = keyboardReceivers[keyboardReceivers.Count - 1];
            this.previousReceiverLength = this.keyboardReceivers.Count;
        }
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
