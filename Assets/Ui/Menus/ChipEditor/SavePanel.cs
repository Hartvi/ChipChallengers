using System;
using UnityEngine;

public class SavePanel : BaseScrollMenu
{
    string pendingOverwrite;

    protected override void Start()
    {
        base.Start();
        this.itemScroll.SetupItemList(this.FillInput, Screen.height / 70, IOHelpers.GetAllModels());
        this.btns[0].btn.onClick.AddListener(() => this.SaveModel(this.input.input.text));
        this.btns[1].btn.onClick.AddListener(this.DeactivatePanel);
        this.scrollbar = this.itemScroll.Siblings<BaseScrollbar>(false)[0];
        this.scrollbar.scrollbar.onValueChanged.AddListener(this.itemScroll.Scroll);
        this.itemScroll.Scroll(0f);
        this.scrollbar.scrollbar.value = 0f;
        this.input.placeholder.SetText(UIStrings.EnterModelName);
        this.input.input.onValueChanged.AddListener(_ => this.ResetOverwrite());
        this.ResetOverwrite();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        this.ResetOverwrite();
        if (this.itemScroll?.virtualContainer != null)
        {
            this.itemScroll.virtualContainer.UpdateLabels(IOHelpers.GetAllModels());
            this.itemScroll.Scroll(0f);
        }
    }

    void FillInput(string name)
    {
        this.ResetOverwrite();
        this.input.input.SetTextWithoutNotify(name);
    }

    void ResetOverwrite()
    {
        this.pendingOverwrite = null;
        if (this.btns != null) { this.btns[0].text.SetText("Save"); }
    }

    void SaveModel(string name)
    {
        try
        {
            string filename = IOHelpers.EnsureFileExtension(name);
            if (IOHelpers.ModelExists(filename) && this.pendingOverwrite != filename)
            {
                this.pendingOverwrite = filename;
                this.btns[0].text.SetText("Overwrite");
                DisplaySingleton.Instance.DisplayText(text => {
                    DisplaySingleton.WarnMsgModification(text);
                    text.SetText("This model already exists. Select Overwrite to replace it.");
                }, 6f, "model-save-status");
                return;
            }
            CoreChip.ClientCoreChip.VirtualModel.SaveThisModelToFile(filename);
            GameManager.Instance.SetModel(ModelFileNames.DisplayName(filename));
            this.DeactivatePanel();
            DisplaySingleton.Instance.DisplayText(text => {
                DisplaySingleton.BasicSmallModification(text);
                DisplaySingleton.MessagePositionModification(text);
                text.SetText("Model saved.");
            }, 3f, "model-save-status");
        }
        catch (Exception error)
        {
            this.ResetOverwrite();
            DisplaySingleton.Instance.DisplayText(text => {
                DisplaySingleton.ErrorMsgModification(text);
                text.SetText("Model could not be saved: " + error.Message);
            }, 5f, "model-save-status");
        }
    }

    protected override void DeactivatePanel()
    {
        this.ResetOverwrite();
        base.DeactivatePanel();
    }

    protected override void SpecificOnStartReceiving()
    {
        this.ResetOverwrite();
        this.input.input.SetTextWithoutNotify(GameManager.Instance.GetModel());
    }
}
