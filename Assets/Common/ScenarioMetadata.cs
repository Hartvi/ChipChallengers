using System;

// ScenarioMetadata describes a scenario's identity, author, completion type, rules and expected processor version.
[Serializable]
public class ScenarioMetadata
{
    public string scenarioId = "";
    public string title = "";
    public string author = "";
    public string completionType = "timeTrial";
    public ScenarioRuleSnapshot rules = new ScenarioRuleSnapshot();
    public int recordVersion = 1;

    // scenarioLogicVersion identifies the processor/API version required by the scenario.
    public int scenarioLogicVersion = 1;
}
