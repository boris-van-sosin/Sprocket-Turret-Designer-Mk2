using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class JavascripAdapter
{
    [DllImport("__Internal")]
    private static extern void DownloadStringAsFile(string text, string fileType, string fileName);

    [DllImport("__Internal")]
    private static extern void GetFileFromBrowser(string targetObjectName, string callbackFuncName, string taskId);

    public static void DownloadData(string data, string fileType, string fileName)
    {
        DownloadStringAsFile(data, fileType, fileName);
    }

    public static void RequestUploadFile(string targetObjectName, string callbackFuncName, string taskId)
    {
        GetFileFromBrowser(targetObjectName, callbackFuncName, taskId);
    }
}
