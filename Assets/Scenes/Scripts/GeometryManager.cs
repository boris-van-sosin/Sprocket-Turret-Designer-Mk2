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
        _activeLayer = GeomObjectFactory.CreateLayer(0f);
        _layers.Add(_activeLayer);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
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
                _dragStartTime = Time.time;
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (_currState == UserActionState.EditCurve)
            {
                HandleDrag();
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
                    if (snapCtlPt == null && hit.collider.gameObject == _activeLayer.gameObject)
                    {
                        bestHit = hit;
                    }
                    else if ((hitCtlpt = hit.collider.GetComponent<ControlPoint>()) != null && _activeLayer.ContainsCtlPt(hitCtlpt))
                    {
                        snapCtlPt = hitCtlpt;
                        bestHit = hit;
                    }
                    else if (snapCtlPt == null && (hitAxis = hit.collider.GetComponent<LayerAxis>()) && hitAxis.ContainingLayer == _activeLayer)
                    {
                        snapAxis = hitAxis;
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
        RaycastHit hit;
        if (Physics.Raycast(r, out hit, 1000f, GlobalData.CurvesLayerMask | GlobalData.ControlPtsLayerMask))
        {
            CurveGeomBase hitCrv = hit.transform.parent.GetComponent<CurveGeomBase>();
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
            Vector3 screenPt = GeomObjectFactory.GetCameraControl().UserCamera.WorldToScreenPoint(hit.point);
            CurveActions actionsPanel = GeomObjectFactory.GetCurveActionPanel();
            RectTransform rt = actionsPanel.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(screenPt.x, screenPt.y);
            actionsPanel.AssignCurve(_currSelectedCurve);
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
            GeomObjectFactory.GetCtlPtEditPanel().AttachCtlPt(editingCtlPt);
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
    
    private void HandleDrag()
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
                if (snapCtlPt == null && hit.collider.gameObject == _activeLayer.gameObject)
                {
                    bestHit = hit;
                }
                else if ((hitCtlPt = hit.collider.GetComponent<ControlPoint>()) != null && hitCtlPt != _currEditingCtlPt)
                {
                    snapCtlPt = hitCtlPt;
                    bestHit = hit;
                }
                else if (snapCtlPt == null && (hitAxis = hit.collider.GetComponent<LayerAxis>()) && hitAxis.ContainingLayer == _activeLayer)
                {
                    snapAxis = hitAxis;
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
    }

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
