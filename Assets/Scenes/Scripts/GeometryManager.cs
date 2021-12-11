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
            else if (_currState == UserActionState.EditCurve)
            {
                HandleEditStep();
            }
        }
    }

    private void HandleCreateStep()
    {
        if (_currTmpBzrCurve != null && _currCrvPtsLeft > 0)
        {
            Ray r = GeomObjectFactory.GetCameraControl().UserCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(r, 1000f, GlobalData.LayersLayerMask);
            if (hits != null)
            {
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.gameObject == _activeLayer.gameObject)
                    {

                        Transform ctlPt = GeomObjectFactory.CreateCtlPt(hit.point);
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

                        break;
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

        if (editingCtlPt!= null)
        {
            GeomObjectFactory.GetCtlPtEditPanel().AttachCtlPt(editingCtlPt);
        }
        else
        {
            GeomObjectFactory.GetCtlPtEditPanel().Release();
            _currState = UserActionState.SelectedCurve;
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
        _currState = UserActionState.EditCurve;
    }

    private string HelpString => string.Format("Creating a {0}.\n{1} points left.", _currCreatingObject, _currCrvPtsLeft);

    private UserActionState _currState = UserActionState.Default;

    private BezierCurveGeom _currTmpBzrCurve = null;
    private CircularArcGeom _currTmpCircArc = null;
    private CurveGeomBase _currSelectedCurve = null;
    private int _currCrvPtsLeft = 0;
    private string _currCreatingObject = "";

    private List<PlaneLayer> _layers = new List<PlaneLayer>();
    private PlaneLayer _activeLayer = null;

    private ControlPoint _selecedCtlPt = null;
    private Axis _dragAxis;
    private Vector3 _dragOrigin;
    private float _dragTime = 0f;
    private static readonly float _dragDelay = 0.5f;
}
