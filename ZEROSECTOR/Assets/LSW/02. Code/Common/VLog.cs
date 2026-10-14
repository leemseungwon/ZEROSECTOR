using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

public static class VLog
{
#if UNITY_EDITOR
    private const bool IsLogEnabled = true;
#else
    private static bool IsLogEnabled => UnityEngine.Debug.isDebugBuild;
#endif

    private static void SendLogMessage(string description, LogType logType)
    {
        switch (logType)
        {
            case LogType.Log:
                UnityEngine.Debug.Log(description);
                break;

            case LogType.Warning:
                UnityEngine.Debug.LogWarning(description);
                break;

            case LogType.Error:
                UnityEngine.Debug.LogError(description);
                break;

            case LogType.Assert:
                UnityEngine.Debug.LogAssertion(description);
                break;

            case LogType.Exception:
                UnityEngine.Debug.LogException(new Exception(description));
                break;

            default:
                UnityEngine.Debug.Log($"{logType} is not supported");
                break;
        }
    }

    public static void Log(
        string description,
        LogType logType = LogType.Log,
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (!IsLogEnabled)
            return;

        SendLogMessage(
            $"{description}\nCaller from Line : {sourceLineNumber} in {sourceFilePath}",
            logType);
    }

    public static void LogStruct<TS>(
        string description,
        TS value,
        LogType logType = LogType.Log,
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        where TS : struct
    {
        if (!IsLogEnabled)
            return;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"[<b>{description}</b>]");

        FieldInfo[] fields = typeof(TS).GetFields( BindingFlags.Public |
                BindingFlags.Instance);

        foreach (var field in fields)
        {
            object fieldValue = field.GetValue(value);
            sb.AppendLine($"{field.Name} : {fieldValue}");
        }

        sb.AppendLine($"Caller from Line : {sourceLineNumber} in {sourceFilePath}");

        SendLogMessage(sb.ToString(), logType);
    }

    public static bool LogTerm(
        bool term,
        LogType logType = LogType.Log,
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (term)
            return true;

        if (IsLogEnabled)
        {
            SendLogMessage(
                $"term is false\nCaller from Line : {sourceLineNumber} in {sourceFilePath}",
                logType);
        }

        return false;
    }

    public static bool LogNotNull(
        object target,
        LogType logType = LogType.Log,
        string targetName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        bool isNull;

        if (target is UnityEngine.Object unityObj)
        {
            isNull = unityObj == null;
        }
        else
        {
            isNull = target == null;
        }

        if (!isNull)
            return true;

        if (IsLogEnabled)
        {
            string name = targetName == "" ? "target" : targetName;

            SendLogMessage(
                $"{name} is null\nCaller from Line : {sourceLineNumber} in {sourceFilePath}",
                logType);
        }

        return false;
    }

    public static bool LogEqualsValue(
        object target1,
        object target2,
        LogType logType = LogType.Log,
        string target1Name = "",
        string target2Name = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        string name1 = target1Name == "" ? "target1" : target1Name;

        string name2 = target2Name == "" ? "target2" : target2Name;

        if (!LogEqualsType(target1, target2, logType,
                name1, name2, sourceFilePath, sourceLineNumber))
            return false;

        if (!LogNotNull(target1, LogType.Error, name1, sourceFilePath, sourceLineNumber)
            || !LogNotNull(target2, LogType.Error, name2, sourceFilePath, sourceLineNumber))
            return false;

        bool equals = target1.Equals(target2);

        if (equals)
            return true;

        if (IsLogEnabled)
        {
            SendLogMessage(
                $"{name1}({target1}) is not equal with {name2}({target2})\n" +
                $"Caller from Line : {sourceLineNumber} in {sourceFilePath} is not equal value",
                logType);
        }

        return false;
    }

    public static bool LogEqualsType(
        object target1,
        object target2,
        LogType logType = LogType.Log,
        string target1Name = "",
        string target2Name = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        string name1 = target1Name == "" ? "target1" : target1Name;

        string name2 = target2Name == "" ? "target2" : target2Name;

        if (!LogNotNull(target1, LogType.Error, name1, sourceFilePath, sourceLineNumber)
            || !LogNotNull(target2, LogType.Error, name2, sourceFilePath, sourceLineNumber))
            return false;

        bool equals = target1.GetType() == target2.GetType();

        if (equals)
            return true;

        if (IsLogEnabled)
        {
            SendLogMessage(
                $"{name1}({target1.GetType()}) is not equal type with {name2}({target2.GetType()})" +
                $"Caller from Line : {sourceLineNumber} in {sourceFilePath} is not equal type",
                logType);
        }

        return false;
    }
}

public enum LogType
{
    Log,
    Warning,
    Error,
    Assert,
    Exception
}