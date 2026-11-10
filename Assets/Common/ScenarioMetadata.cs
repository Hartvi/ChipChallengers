using System;

// ChallengeMetadata describes a challenge's identity, author, completion type, rules and expected processor version.
[Serializable]
public class ChallengeMetadata
{
    public string challengeId = "";
    public string title = "";
    public string author = "";
    public string completionType = "timeTrial";
    public ChallengeRuleSnapshot rules = new ChallengeRuleSnapshot();
    public int recordVersion = 1;

    // challengeLogicVersion identifies the processor/API version required by the challenge.
    public int challengeLogicVersion = 1;
}
