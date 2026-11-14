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
        this.RemoveInactiveReceivers();
        if (this.keyboardReceivers.Count == 0) { return; }
        var current = this.keyboardReceivers[this.keyboardReceivers.Count - 1];
        // F1 is also available while an editor side panel owns keyboard focus.
        if (Input.GetKeyDown(KeyCode.F1) && (current is EditorMenu || current is BaseSidePanel) &&
            EditorMenu.Instance != null && EditorMenu.Instance.isActiveAndEnabled)
        {
            var help = EditorMenu.Instance.GetComponentInChildren<ControlsPanel>(true);
            if (help != null) { this.SwitchToMe(help); return; }
        }
        this.keyboardReceivers[this.keyboardReceivers.Count - 1].CoreHandleInputs();

    }

    public void SwitchToMe(InputReceiver me)
    {
        this.RemoveInactiveReceivers();
        if (keyboardReceivers.Count > 0 && keyboardReceivers[keyboardReceivers.Count - 1] == me) { return; }
        if (keyboardReceivers.Count > 0)
        {
            var lastReceiver = keyboardReceivers[keyboardReceivers.Count - 1];
            lastReceiver.OnStopReceiving();
        }
        me.OnStartReceiving();
        this.RemoveInactiveReceivers();

        //print($"Switching to {me.GetType()}");
        while (keyboardReceivers.Remove(me)) { }

        keyboardReceivers.Add(me);
    }

    public void TurnMeOff(InputReceiver me)
    {
        //print($"Turning off {me.GetType()}");
        //print($"Before removing: {me.GetType()}: {keyboardReceivers.Count}");
        //keyboardReceivers.Remove(me);
        if (keyboardReceivers.Count > 0)
        {
            bool iWasReceivingAlready = keyboardReceivers[keyboardReceivers.Count - 1] == me;
            if (iWasReceivingAlready)
            {
                me.OnStopReceiving();
            }

            while (keyboardReceivers.Remove(me)) { }

            // if I was receiving, that means I'm not last anymore and a new guy is last now
            if (iWasReceivingAlready && keyboardReceivers.Count > 0)
            {
                keyboardReceivers[keyboardReceivers.Count - 1].OnStartReceiving();
            }
        }
        //print($"After removing: {me.GetType()}: {keyboardReceivers.Count}");
    }

    void RemoveInactiveReceivers()
    {
        this.keyboardReceivers.RemoveAll(receiver => receiver is MonoBehaviour component &&
            (component == null || !component.isActiveAndEnabled));
    }

}
