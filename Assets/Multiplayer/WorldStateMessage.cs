using Mirror;

public struct WorldStateMessage : NetworkMessage
{
    public string mapFile;
    public string mapHash;
    public bool challengeActive;
}
