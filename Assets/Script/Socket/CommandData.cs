using UnityEngine;

[System.Serializable]
public class CommandData
{
    public string commandType;
    public TransformData transformData;

    public CommandData(string type, TransformData data = null)
    {
        commandType = type;
        transformData = data;
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public static CommandData FromJson(string json)
    {
        return JsonUtility.FromJson<CommandData>(json);
    }
}

public static class CommandTypes
{
    public const string DO_ONCE = "DO_ONCE";
    public const string START_REALTIME = "START_REALTIME";
    public const string STOP_REALTIME = "STOP_REALTIME";
    public const string RESET_TRANSFORM = "RESET_TRANSFORM";
    public const string TRANSFORM_DATA = "TRANSFORM_DATA";
}