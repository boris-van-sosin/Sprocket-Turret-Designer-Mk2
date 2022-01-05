using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class JavascripAdapter
{
    [DllImport("__Internal")]
    private static extern void DownloadStringAsFile(string text, string fileType, string fileName);

    [DllImport("__Internal")]
    private static extern void GetFileFromBrowser(string targetObjectName, string callbackFuncName, string taskId);

    [DllImport("__Internal")]
    private static extern void SetTurretData(string tankBlueprint, string turretData);

    public static void DownloadData(string data, string fileName)
    {
        DownloadStringAsFile(data, "text/json", fileName);
    }

    public static void RequestUploadFile(string targetObjectName, string callbackFuncName, string taskId)
    {
        GetFileFromBrowser(targetObjectName, callbackFuncName, taskId);
    }

    public static void SetTurretDataAndDownload(string tankBlueprint, string turretData)
    {
        SetTurretData(tankBlueprint, turretData);
    }
}
