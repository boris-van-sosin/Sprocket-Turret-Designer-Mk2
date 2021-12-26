using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class JavascripAdapter
{
    [DllImport("__Internal")]
    private static extern void DownloadStringAsFile(string text, string fileType, string fileName);

    public static void DownloadData(string data, string fileType, string fileName)
    {
        DownloadStringAsFile(data, fileType, fileName);
    }
}
