using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SavePanel : BaseScrollMenu
{
    protected override void Start()
    {
        base.Start();

        this.itemScroll.SetupItemList(FillInput, Screen.height / 70, IOHelpers.GetAllModels());

        this.btns[0].btn.onClick.AddListener(() => SaveModel(this.input.input.text));

        this.btns[1].btn.onClick.AddListener(this.DeactivatePanel);

        this.scrollbar = this.itemScroll.Siblings<BaseScrollbar>(false)[0];

        this.scrollbar.scrollbar.onValueChanged.AddListener(this.itemScroll.Scroll);
        this.itemScroll.Scroll(0f);
        this.scrollbar.scrollbar.value = 0f;

        this.input.placeholder.SetText("Enter model name");

    }

    public override void OnEnable()
    {
        if(this.itemScroll is not null) this.itemScroll.virtualContainer.UpdateLabels(IOHelpers.GetAllModels());
    }

    void FillInput(string modelName)
    {
        this.input.input.SetTextWithoutNotify(modelName);

        if (IOHelpers.ModelExists(modelName))
        {
            DisplaySingleton.Instance.DisplayText(
                x =>
                {
                    DisplaySingleton.WarnMsgModification(x);
                    x.SetText("Model exists, do you want to overwrite?");
                },
                3f
            );
            UIStrings.ModelExists(modelName);
        }
    }

    void SaveTmp(string state)
    {
        string msg = null;
        try
        {
            msg = CommonChip.ClientCore.VirtualModel.SaveThisModelToFile("tmp");
        }
        catch
        {
            UnityEngine.Debug.LogWarning($"Model could not be saved.");
            DisplaySingleton.Instance.DisplayText(this.ModelError, 3f);
        }
    }

    void SaveModel(string modelName)
    {
        string modelNameWithExtension = IOHelpers.EnsureFileExtension(modelName);
        print($"Saving model: {modelName}, length: {modelName.Length} => {modelNameWithExtension}");

        string msg = null;
        try
        {
            msg = CommonChip.ClientCore.VirtualModel.SaveThisModelToFile(modelNameWithExtension);
        }
        catch
        {
            UnityEngine.Debug.LogWarning($"Model could not be saved.");
            DisplaySingleton.Instance.DisplayText(this.ModelError, 3f);
        }
        this.DeactivatePanel();
    }

    void ModelError(TMP_Text txt)
    {
        DisplaySingleton.ErrorMsgModification(txt);
        txt.SetText("Error while saving model.");
    }

    Action<TMP_Text> ModelExists(string txtStr)
    {
        return x =>
        {
            DisplaySingleton.ErrorMsgModification(x);
            x.SetText(txtStr);
        };
    }
}
