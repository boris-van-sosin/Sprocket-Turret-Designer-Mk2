using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerActions : MonoBehaviour
{
    private void Awake()
    {
        ElevationTextBox.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<string>(SetElevation));
    }

    public void AttachLayer(LayerPlane l)
    {
        _attachedLayer = l;
        ElevationTextBox.SetTextWithoutNotify(_attachedLayer.Elevation.ToString());
        gameObject.SetActive(true);
    }

    public void UpdateElevationFromLayer()
    {
        if (_attachedLayer != null)
        {
            ElevationTextBox.SetTextWithoutNotify(_attachedLayer.Elevation.ToString());
        }
    }

    public void Release()
    {
        _attachedLayer = null;
        gameObject.SetActive(false);
    }

    public void StartScale()
    {
        GeomObjectFactory.GetGeometryManager().StartScaleLayer(_attachedLayer);
    }

    public void Duplicate()
    {
        GeomObjectFactory.GetGeometryManager().DuplicateLayer(_attachedLayer);
    }

    public void Clear()
    {
        GeomObjectFactory.GetGeometryManager().ClearLayer(_attachedLayer);
    }

    public void Delete()
    {
        GeomObjectFactory.GetGeometryManager().DeleteLayer(_attachedLayer);
    }

    private void SetElevation(string elevationStr)
    {
        _attachedLayer.Elevation = float.Parse(elevationStr);
    }

    private LayerPlane _attachedLayer = null;
    public TMPro.TMP_InputField ElevationTextBox;
}
