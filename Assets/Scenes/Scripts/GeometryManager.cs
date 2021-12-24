using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;

public class GeometryManager : MonoBehaviour
{
    private enum UserActionState
    {
        Default,
        CreateCurve,
        SelectedCurve,
        EditCurve,
        MoveCurve,
        RotateCurve
    }

    // Start is called before the first frame update
    void Start()
    {
        CreateEmptyLayer();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _dragStartTime = Time.time;

            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (_currState == UserActionState.CreateCurve)
            {
                HandleCreateStep();
            }
            else if (_currState == UserActionState.Default)
            {
                HandleDefaultClick();
            }
            else if (_currState == UserActionState.SelectedCurve)
            {
                HandleDefaultClick(); //TODO: verify this
                _activeLayer.CheckConnectivity(); //TODO: right now this is only for verification.
            }
            else if (_currState == UserActionState.EditCurve)
            {
                HandleEditStep();
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (_currState == UserActionState.EditCurve)
            {
                HandleDragCtlPt();
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            _dragStartTime = Time.time;
        }
    }

    private void HandleCreateStep()
    {
        if (_currTmpBzrCurve != null && _currCrvPtsLeft > 0)
        {
            Ray r = GeomObjectFactory.GetCameraControl().UserCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(r, 1000f, GlobalData.LayersLayerMask | GlobalData.ControlPtsLayerMask);
            if (hits != null)
            {
                RaycastHit? bestHit = null;
                ControlPoint snapCtlPt = null;
                LayerAxis snapAxis = null;
                foreach (RaycastHit hit in hits)
                {
                    ControlPoint hitCtlpt;
                    LayerAxis hitAxis;
                    PlaneLayer hitLayer;
                    if ((hitCtlpt = hit.collider.GetComponent<ControlPoint>()) != null && _activeLayer.ContainsCtlPt(hitCtlpt))
                    {
                        snapCtlPt = hitCtlpt;
                        bestHit = hit;
                    }
                    else if (snapCtlPt == null && (hitAxis = hit.collider.GetComponent<LayerAxis>()) && hitAxis.ContainingLayer == _activeLayer)
                    {
                        snapAxis = hitAxis;
                        bestHit = hit;
                    }
                    else if (snapCtlPt == null && (hitLayer = hit.collider.GetComponentInParent<PlaneLayer>()) == _activeLayer)
                    {
                        bestHit = hit;
                    }
                }

                if (bestHit.HasValue)
                {
                    Vector3 placePt = snapCtlPt != null ? snapCtlPt.transform.position : (snapAxis != null ? new Vector3(0f, snapAxis.ContainingLayer.Elevation, bestHit.Value.point.z) : bestHit.Value.point);
                    Transform ctlPt = GeomObjectFactory.CreateCtlPt(snapCtlPt != null ? snapCtlPt.transform.position : bestHit.Value.point);
                    ControlPoint ctlPtObj = ctlPt.GetComponent<ControlPoint>();
                    ctlPt.SetParent(_currTmpBzrCurve.transform);
                    _currTmpBzrCurve.AppendCtlPt(ctlPtObj);
                    --_currCrvPtsLeft;

                    if (_currCrvPtsLeft == 0)
                    {
                        _currTmpBzrCurve.TryRender();
                        _activeLayer.AddCurve(_currTmpBzrCurve);
                        _currTmpBzrCurve = null;
                        GeomObjectFactory.GetHelpPanel().SetText();
                        _currState = UserActionState.Default;
                    }
                    else
                    {
                        GeomObjectFactory.GetHelpPanel().SetText(HelpString);
                    }
                }

            }
        }
        else if (_currTmpCircArc != null && _currCrvPtsLeft > 0)
        {
            Debug.Log("Circular arc not implemented yet.");
        }
        else
        {
            Debug.LogWarning("Curve empty or no points left in create state.");
            _currState = UserActionState.Default;
        }
    }

    private void HandleDefaultClick()
    {
        Ray r = GeomObjectFactory.GetCameraControl().UserCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(r, 1000f, GlobalData.CurvesLayerMask | GlobalData.ControlPtsLayerMask | GlobalData.GizmosLayerMask);
        bool selectedCrv = false;
        PlaneLayer bestHitLayer = null;
        foreach (RaycastHit hit in hits)
        {
            CurveGeomBase hitCrv;
            PlaneLayer hitLayer;
            if ((hitCrv = hit.collider.GetComponentInParent<CurveGeomBase>()) != null &&
                _activeLayer.ContainsCurve(hitCrv))
            {
                if (hitCrv == _currSelectedCurve)
                {
                    return;
                }
                else
                {
                    if (_currSelectedCurve != null)
                    {
                        _currSelectedCurve.Selected = false;
                    }
                    _currSelectedCurve = hitCrv;
                    _currSelectedCurve.Selected = true;
                }
                if (_currSelectedCurve == null)
                {
                    return;
                }
                _currState = UserActionState.SelectedCurve;
                
                CurveActions actionsPanel = GeomObjectFactory.GetCurveActionPanel();
                SetPanelPos(actionsPanel.GetComponent<RectTransform>(), hit.point);
                
                actionsPanel.AttachCurve(_currSelectedCurve);
                selectedCrv = true;
                break; //TODO: verify this
            }
            else if ((hitLayer = hit.collider.GetComponentInParent<PlaneLayer>()) != null)
            {
                bestHitLayer = hitLayer;
            }
        }

        if (selectedCrv)
        {
            GeomObjectFactory.GetLayerActionPanel().Release();
        }
        else
        {
            GeomObjectFactory.GetCurveActionPanel().Release();
            if (_currSelectedCurve != null)
            {
                _currSelectedCurve.Selected = false;
            }
            _currSelectedCurve = null;
            _currState = UserActionState.Default;

            if (bestHitLayer != null)
            {
                if (bestHitLayer != _activeLayer)
                {
                    _activeLayer.SetSelected(false);
                    _activeLayer = bestHitLayer;
                    _activeLayer.SetSelected(true);
                }
                GeomObjectFactory.GetLayerActionPanel().AttachLayer(_activeLayer);
            }
        }
    }

    private void HandleEditStep()
    {
        Ray r = GeomObjectFactory.GetCameraControl().UserCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(r, 1000f, GlobalData.ControlPtsLayerMask);
        ControlPoint editingCtlPt = null;
        foreach (RaycastHit hit in hits)
        {
            ControlPoint hitCtlPt = null;
            if ((hitCtlPt = hit.collider.GetComponent<ControlPoint>()) != null)
            {
                if (hitCtlPt.ContainingCurve == _currSelectedCurve)
                {
                    editingCtlPt = hitCtlPt;
                    break;
                }
            }
        }

        if (editingCtlPt != null)
        {
            _currEditingCtlPt = editingCtlPt;
            CtlPtEditPanel ctlPtPanel = GeomObjectFactory.GetCtlPtEditPanel();
            ctlPtPanel.AttachCtlPt(editingCtlPt);
            SetPanelPos(ctlPtPanel.GetComponent<RectTransform>(), editingCtlPt.transform.position);
        }
        else
        {
            GeomObjectFactory.GetCtlPtEditPanel().Release();
            _currEditingCtlPt = null;
            _currState = UserActionState.SelectedCurve;

            foreach (Transform pt in _currSelectedCurve.CtlPts)
            {
                pt.GetComponent<MeshRenderer>().sharedMaterial = GeomObjectFactory.GetCtlPtMtlDefault();
            }

        }
    }
    
    private void HandleDragCtlPt()
    {
        if (Time.time - _dragStartTime > _dragDelay)
        {
            Ray r = GeomObjectFactory.GetCameraControl().UserCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(r, 1000f, GlobalData.LayersLayerMask | GlobalData.ControlPtsLayerMask);
            RaycastHit? bestHit = null;
            ControlPoint snapCtlPt = null;
            LayerAxis snapAxis = null;
            foreach (RaycastHit hit in hits)
            {
                ControlPoint hitCtlPt;
                LayerAxis hitAxis;
                PlaneLayer hitLayer;
                if ((hitCtlPt = hit.collider.GetComponent<ControlPoint>()) != null && hitCtlPt != _currEditingCtlPt && _activeLayer.ContainsCtlPt(hitCtlPt))
                {
                    snapCtlPt = hitCtlPt;
                    bestHit = hit;
                }
                else if (snapCtlPt == null && (hitAxis = hit.collider.GetComponent<LayerAxis>()) && hitAxis.ContainingLayer == _activeLayer)
                {
                    snapAxis = hitAxis;
                    bestHit = hit;
                }
                else if (snapCtlPt == null && (hitLayer = hit.collider.GetComponentInParent<PlaneLayer>()) == _activeLayer)
                {
                    bestHit = hit;
                }
            }

            if (bestHit.HasValue)
            {
                _currEditingCtlPt.transform.position = snapCtlPt != null ? snapCtlPt.transform.position : (snapAxis != null ? new Vector3(0f, snapAxis.ContainingLayer.Elevation, bestHit.Value.point.z) : bestHit.Value.point);
                _currSelectedCurve.UpdateControlPoint(_currEditingCtlPt);
                GeomObjectFactory.GetCtlPtEditPanel().UpdateValuesFromCtlPt();
                _currSelectedCurve.TryRender();
            }
        }
    }

    public void StartCreateLine()
    {
        if (_currTmpBzrCurve == null && _currTmpCircArc == null)
        {
            _currTmpBzrCurve = GeomObjectFactory.CreateBezierCurve();
            _currCrvPtsLeft = 2;
            _currCreatingObject = "2-point straight line";
            GeomObjectFactory.GetHelpPanel().SetText(HelpString);
            _currState = UserActionState.CreateCurve;
        }
    }

    public void StartCreate3PtBezier()
    {
        if (_currTmpBzrCurve == null && _currTmpCircArc == null)
        {
            _currTmpBzrCurve = GeomObjectFactory.CreateBezierCurve();
            _currCrvPtsLeft = 3;
            _currCreatingObject = "3-point curve";
            GeomObjectFactory.GetHelpPanel().SetText(HelpString);
            _currState = UserActionState.CreateCurve;
        }
    }

    public void StartCreate4PtBezier()
    {
        if (_currTmpBzrCurve == null && _currTmpCircArc == null)
        {
            _currTmpBzrCurve = GeomObjectFactory.CreateBezierCurve();
            _currCrvPtsLeft = 4;
            _currCreatingObject = "4-point curve";
            GeomObjectFactory.GetHelpPanel().SetText(HelpString);
            _currState = UserActionState.CreateCurve;
        }
    }

    public void StartEditCurve(CurveGeomBase crv)
    {
        if (crv != _currSelectedCurve)
        {
            Debug.LogWarning("Started editing curve other than selected curve");
        }

        foreach (Transform pt in crv.CtlPts)
        {
            pt.GetComponent<MeshRenderer>().sharedMaterial = GeomObjectFactory.GetCtlPtMtlEditing();
        }

        _currState = UserActionState.EditCurve;
    }

    public void DeleteCurve(CurveGeomBase crv)
    {
        if (crv != _currSelectedCurve)
        {
            Debug.LogWarning("Started editing curve other than selected curve");
        }

        GeomObjectFactory.GetCtlPtEditPanel().Release();
        GeomObjectFactory.GetCurveActionPanel().Release();
        _activeLayer.DeleteCurve(crv);
        _currState = UserActionState.Default;
    }

    public void CreateEmptyLayer()
    {
        if (_layers.Count == 0)
        {
            _activeLayer = GeomObjectFactory.CreateLayer(0f);
            _layers.Add(_activeLayer);
        }
        else
        {
            float maxH = _layers[0].Elevation;
            for (int i = 1; i < _layers.Count; ++i)
            {
                if (_layers[i].Elevation > maxH)
                {
                    maxH = _layers[i].Elevation;
                }
            }
            _activeLayer.SetSelected(false);
            _activeLayer = GeomObjectFactory.CreateLayer(maxH + 0.1f);
            _activeLayer.SetSelected(true);
            GeomObjectFactory.GetLayerActionPanel().AttachLayer(_activeLayer);
            _layers.Add(_activeLayer);
        }
    }

    public void StartScaleLayer(PlaneLayer l)
    {
        if (l != _activeLayer)
        {
            Debug.LogWarning("Tried to scale layer other than active layer");
        }
    }

    public void DuplicateLayer()
    {
        if (_layers.Count > 0)
        {
            int topLayer = 0;
            for (int i = 1; i < _layers.Count; ++i)
            {
                if (_layers[i].Elevation > _layers[topLayer].Elevation)
                {
                    topLayer = i;
                }
            }
            DuplicateLayer(_layers[topLayer]);
        }
    }

    public void DuplicateLayer(PlaneLayer l)
    {
        if (l != _activeLayer)
        {
            Debug.LogWarning("Tried to duplicate layer other than active layer");
        }

        float maxH = _layers[0].Elevation;
        for (int i = 1; i < _layers.Count; ++i)
        {
            if (_layers[i].Elevation > maxH)
            {
                maxH = _layers[i].Elevation;
            }
        }

        _activeLayer.SetSelected(false);
        PlaneLayer nextLayer = _activeLayer.Duplicate(maxH + 0.1f);
        nextLayer.SetSelected(true);
        GeomObjectFactory.GetLayerActionPanel().AttachLayer(nextLayer);
        _layers.Add(nextLayer);
        _activeLayer = nextLayer;
        _currState = UserActionState.Default;
    }

    public void ClearLayer(PlaneLayer l)
    {
        if (l != _activeLayer)
        {
            Debug.LogWarning("Tried to clear layer other than active layer");
        }

        _activeLayer.Clear();
        _currState = UserActionState.Default;
    }

    public void DeleteLayer(PlaneLayer l)
    {
        if (l != _activeLayer)
        {
            Debug.LogWarning("Tried to delete layer other than active layer");
        }

        if (_layers.Count <= 1)
        {
            Debug.LogWarning("Cannot delete last layer");
            return;
        }

        _activeLayer.Clear();
        _activeLayer.SetSelected(false);
        PlaneLayer toDelete = _activeLayer;

        int activeIdx = _layers.IndexOf(_activeLayer);
        if (activeIdx < 0)
        {
            Debug.LogError("Active layer not in list");
        }
        _layers.RemoveAt(activeIdx);
        int nextActiveLayer = -1;
        for (int i = 0; i < _layers.Count; ++i)
        {
            if (_layers[i] != _activeLayer && _layers[i].Elevation < _activeLayer.Elevation)
            {
                if (nextActiveLayer < 0 || _layers[i].Elevation > _layers[nextActiveLayer].Elevation)
                {
                    nextActiveLayer = i;
                }
            }
        }
        _activeLayer = _layers[nextActiveLayer];
        GeomObjectFactory.GetLayerActionPanel().AttachLayer(_activeLayer);
        _activeLayer.SetSelected(true);

        Destroy(toDelete.gameObject);
        _currState = UserActionState.Default;
    }

    public void GeneratePreview()
    {
        MeshGenerator.GenerateMesh(_layers);
    }

    private void SetPanelPos(RectTransform rtr, Vector3 rayCastPt)
    {
        Vector3 screenPt = GeomObjectFactory.GetCameraControl().UserCamera.WorldToScreenPoint(rayCastPt);
        rtr.anchoredPosition = new Vector2(screenPt.x, screenPt.y);
    }

    public void AssignGizmos(IEnumerable<(Vector3, Vector3, Color)> lines, IEnumerable<(Vector3, Color)> points)
    {
        _lineGizmos.Clear();
        if (lines != null) { _lineGizmos.AddRange(lines); }
        _pointGizmos.Clear();
        if (points != null) { _pointGizmos.AddRange(points); }
    }

    void OnDrawGizmos()
    {
        foreach (var line in _lineGizmos)
        {
            Gizmos.color = line.Item3;
            Gizmos.DrawLine(line.Item1, line.Item2);
        }

        foreach (var pt in _pointGizmos)
        {
            Gizmos.color = pt.Item2;
            Gizmos.DrawSphere(pt.Item1, 0.01f);
        }
    }

    private List<(Vector3, Vector3, Color)> _lineGizmos = new List<(Vector3, Vector3, Color)>();
    private List<(Vector3, Color)> _pointGizmos = new List<(Vector3, Color)>();

    private string HelpString => string.Format("Creating a {0}.\n{1} points left.", _currCreatingObject, _currCrvPtsLeft);

    private UserActionState _currState = UserActionState.Default;

    private BezierCurveGeom _currTmpBzrCurve = null;
    private CircularArcGeom _currTmpCircArc = null;
    private CurveGeomBase _currSelectedCurve = null;
    private ControlPoint _currEditingCtlPt = null;
    private int _currCrvPtsLeft = 0;
    private string _currCreatingObject = "";

    private List<PlaneLayer> _layers = new List<PlaneLayer>();
    private PlaneLayer _activeLayer = null;

    private Axis _dragAxis;
    private Vector3 _dragOrigin;
    private float _dragStartTime = 0f;
    private static readonly float _dragDelay = 0.5f;
}
