using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UploadFileReceiver : MonoBehaviour
{
    public DataReceiveHandle StartUploadFile()
    {
        string id = _taskCounter.ToString();
        DataReceiveHandle handle = new DataReceiveHandle();
#if UNITY_WEBGL
        JavascripAdapter.RequestUploadFile(name, "ReceiveData", id);
#else
        Debug.Log(string.Format("Current working directory: {0}", System.IO.Directory.GetCurrentDirectory()));
        handle.ReceiveData(System.IO.File.ReadAllText("C:\\Users\\boris\\Downloads\\MyTurret (1).txt"));        
#endif
        //Debug.Log(string.Format("Requested upload with task id {0}", id));
        _tasks[id] = handle;
        ++_taskCounter;
        return handle;
    }

    public void ReceiveData(string data)
    {
        //Debug.Log("Received data");
        //Debug.Log(data);
        UploadResponseData reponse = JsonUtility.FromJson<UploadResponseData>(data);
        string taskId = reponse.Id;
        //Debug.Log(string.Format("Got upload with task id {0}", taskId));
        if (reponse.Success)
        {
            _tasks[taskId].ReceiveData(reponse.Data);
        }
        else
        {
            _tasks[taskId].ReceiveError(reponse.Data);
        }
        _tasks.Remove(taskId); 
    }

    public class DataReceiveHandle
    {
        public DataReceiveHandle()
        {
            Data = null;
            ReceivedData = false;
        }

        public void ReceiveData(string data)
        {
            Data = data;
            ReceivedData = true;
            Success = true;
            OnDataReceived?.Invoke(data, true);
        }

        public void ReceiveError(string errorMsg)
        {
            Data = null;
            ReceivedData = true;
            Success = false;
            OnDataReceived?.Invoke(errorMsg, false);
        }

        public string Data { get; private set; }
        public bool Success { get; private set; }
        public bool ReceivedData { get; private set; }

        public event System.Action<string, bool> OnDataReceived;
    }

    public class UploadResponseData
    {
        public string Id;
        public bool Success;
        public string Data;
    }

    private Dictionary<string, DataReceiveHandle> _tasks = new Dictionary<string, DataReceiveHandle>();

    private int _taskCounter = 0;
}
