using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        MoveCtlPtViaTrihedron,
        StartMoveCurve,
        MoveCurve,
        StartRotateCurve,
        StartRotateCurveSelectPoint,
        RotateCurve,
        StartScaleCurve,
        StartScaleCurveSelectPoint,
        ScaleCurve
    }

    private enum TransformType
    {
        Move,
        Rotate,
        Scale
    }

    private enum TransformPhase
    {
        Start,
        PivotSelected,
        InTransform,
        Finish
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

            if (_receiveStructureDefHandle != null || _receiveTankBlueprintHandle != null || EventSystem.current.IsPointerOverGameObject())
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
                _enableDrag = true;
                HandleEditStep();
            }
            else if (_currState == UserActionState.StartMoveCurve)
            {
                HandleTransformCurve(TransformType.Move, TransformPhase.Start);
            }
            else if (_currState == UserActionState.StartRotateCurve)
            {
                HandleTransformCurve(TransformType.Rotate, TransformPhase.Start);
            }
            else if (_currState == UserActionState.StartRotateCurveSelectPoint)
            {
                HandleTransformCurve(TransformType.Rotate, TransformPhase.PivotSelected);
            }
            else if (_currState == UserActionState.StartScaleCurve)
            {
                HandleTransformCurve(TransformType.Scale, TransformPhase.Start);
            }
            else if (_currState == UserActionState.StartScaleCurveSelectPoint)
            {
                HandleTransformCurve(TransformType.Scale, TransformPhase.PivotSelected);
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (_receiveStructureDefHandle != null || _receiveTankBlueprintHandle != null)
            {
                return;
            }

            if (_currState == UserActionState.EditCurve)
            {
                HandleDragCtlPt();
            }
            else if (_currState == UserActionState.MoveCtlPtViaTrihedron)
            {
                HandleMoveCtlPtViaTrihedron();
            }
            else if (_currState == UserActionState.MoveCurve)
            {
                HandleTransformCurve(TransformType.Move, TransformPhase.InTransform);
            }
            else if (_currState == UserActionState.RotateCurve)
            {
                HandleTransformCurve(TransformType.Rotate, TransformPhase.InTransform);
            }
            else if (_currState == UserActionState.ScaleCurve)
            {
                HandleTransformCurve(TransformType.Scale, TransformPhase.InTransform);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            _dragStartTime = Time.time;
            _enableDrag = false;

            if (_receiveStructureDefHandle != null || _receiveTankBlueprintHandle != null)
            {
                return;
            }

            if (_currState == UserActionState.MoveCtlPtViaTrihedron)
            {
                HandleFinishMoveCtlPtViaTrihedron();
            }
            else if (_currState == UserActionState.MoveCurve)
            {
                HandleTransformCurve(TransformType.Move, TransformPhase.Finish);
            }
            else if (_currState == UserActionState.RotateCurve)
            {
                HandleTransformCurve(TransformType.Rotate, TransformPhase.Finish);
            }
            else if (_currState == UserActionState.ScaleCurve)
            {
                HandleTransformCurve(TransformType.Scale, TransformPhase.Finish);
            }
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
                    LayerPlane hitLayer;
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
                    else if (snapCtlPt == null && (hitLayer = hit.collider.GetComponentInParent<LayerPlane>()) == _activeLayer)
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
        LayerPlane bestHitLayer = null;
        foreach (RaycastHit hit in hits)
        {
            CurveGeomBase hitCrv;
            LayerPlane hitLayer;
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
            else if ((hitLayer = hit.collider.GetComponentInParent<LayerPlane>()) != null)
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
        RaycastHit[] hits = Physics.RaycastAll(r, 1000f, GlobalData.ControlPtsLayerMask | GlobalData.GizmosLayerMask);
        ControlPoint editingCtlPt = null;
        Trihedron editingTri = null;
        foreach (RaycastHit hit in hits)
        {
            ControlPoint hitCtlPt = null;
            Trihedron hitTri = null;
            if ((hitCtlPt = hit.collider.GetComponent<ControlPoint>()) != null)
            {
                if (hitCtlPt.ContainingCurve == _currSelectedCurve)
                {
                    editingCtlPt = hitCtlPt;
                    break;
                }
            }
            else if ((hitTri = hit.collider.GetComponentInParent<Trihedron>()) != null)
            {
                _dragOrigin = new Vector3(hit.point.x, _currEditingCtlPt.transform.position.y, hit.point.z);
                if (hit.collider == _moveTrihedron.XTool)
                {
                    _dragAxis = Axis.X;
                }
                else if (hit.collider == _moveTrihedron.ZTool)
                {
                    _dragAxis = Axis.Z;
                }
                editingTri = hitTri;
                break;
            }
        }

        if (editingCtlPt != null)
        {
            _currEditingCtlPt = editingCtlPt;
            CtlPtEditPanel ctlPtPanel = GeomObjectFactory.GetCtlPtEditPanel();
            ctlPtPanel.AttachCtlPt(editingCtlPt);
            SetPanelPos(ctlPtPanel.GetComponent<RectTransform>(), editingCtlPt.transform.position);
            _moveTrihedron = GeomObjectFactory.GetMoveTridehron(_currEditingCtlPt.transform.position);
        }
        else if (editingTri != null)
        {
            if (_currEditingCtlPt == null)
            {
                Debug.LogError("Tried to move control point via trihedron without selected control point.");
            }

            _dragCtlPtOldPos = _currEditingCtlPt.transform.position;
            _currState = UserActionState.MoveCtlPtViaTrihedron;
        }
        else
        {
            GeomObjectFactory.GetCtlPtEditPanel().Release();
            if (_moveTrihedron != null) { _moveTrihedron.ReleaseTrihedron(); }
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
        if (_enableDrag && Time.time - _dragStartTime > _dragDelay)
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
                LayerPlane hitLayer;
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
                else if (snapCtlPt == null && (hitLayer = hit.collider.GetComponentInParent<LayerPlane>()) == _activeLayer)
                {
                    bestHit = hit;
                }
            }

            if (bestHit.HasValue)
            {
                Vector3 resPt = snapCtlPt != null ? snapCtlPt.transform.position : (snapAxis != null ? new Vector3(0f, snapAxis.ContainingLayer.Elevation, bestHit.Value.point.z) : bestHit.Value.point);

                if (_symmetricMode)
                {
                    if (resPt.x < 0f)
                    {
                        resPt.x = 0f;
                    }
                }

                _currEditingCtlPt.transform.position = resPt;

                if (_moveTrihedron != null)
                {
                    _moveTrihedron.transform.position = _currEditingCtlPt.transform.position;
                }
                _currSelectedCurve.UpdateControlPoint(_currEditingCtlPt);
                GeomObjectFactory.GetCtlPtEditPanel().UpdateValuesFromCtlPt();
                _currSelectedCurve.TryRender();
            }
        }
    }

    private void HandleMoveCtlPtViaTrihedron()
    {
        Ray r = GeomObjectFactory.GetCameraControl().UserCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(r, 1000f, GlobalData.LayersLayerMask);
        RaycastHit? bestHit = null;
        ControlPoint snapCtlPt = null;
        LayerAxis snapAxis = null;
        foreach (RaycastHit hit in hits)
        {
            LayerPlane hitLayer;
            if ((hitLayer = hit.collider.GetComponentInParent<LayerPlane>()) == _activeLayer)
            {
                bestHit = hit;
            }
        }
        if (bestHit.HasValue)
        {
            Vector3 resPoint = snapCtlPt != null ? snapCtlPt.transform.position : (snapAxis != null ? new Vector3(0f, snapAxis.ContainingLayer.Elevation, bestHit.Value.point.z) : bestHit.Value.point);
            if (_dragAxis == Axis.X)
            {
                resPoint.y = _dragOrigin.y;
                resPoint.z = _dragOrigin.z;
            }
            else if (_dragAxis == Axis.Z)
            {
                resPoint.x = _dragOrigin.x;
                resPoint.y = _dragOrigin.y;
            }

            Vector3 newPtPos = _dragCtlPtOldPos + resPoint - _dragOrigin;
            if (_symmetricMode)
            {
                if (newPtPos.x < 0f)
                {
                    newPtPos.x = 0f;
                }
            }

            _currEditingCtlPt.transform.position = newPtPos;
            _moveTrihedron.transform.position = _currEditingCtlPt.transform.position;
            _currSelectedCurve.UpdateControlPoint(_currEditingCtlPt);
            GeomObjectFactory.GetCtlPtEditPanel().UpdateValuesFromCtlPt();
            _currSelectedCurve.TryRender();
        }
    }

    private void HandleFinishMoveCtlPtViaTrihedron()
    {
        _currState = UserActionState.EditCurve;
    }

    private void HandleTransformCurve(TransformType tr, TransformPhase phase)
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
            LayerPlane hitLayer;
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
            else if (snapCtlPt == null && (hitLayer = hit.collider.GetComponentInParent<LayerPlane>()) == _activeLayer)
            {
                bestHit = hit;
            }
        }

        if (bestHit.HasValue)
        {
            Vector3 pt = snapCtlPt != null ? snapCtlPt.transform.position : (snapAxis != null ? new Vector3(0f, snapAxis.ContainingLayer.Elevation, bestHit.Value.point.z) : bestHit.Value.point);
            if (tr == TransformType.Move)
            {
                if (phase == TransformPhase.Start)
                {
                    _dragOrigin = pt;
                    _transformCurveOrigPts.Clear();
                    _transformCurveOrigPts.AddRange(_currSelectedCurve.CtlPts.Select(p => p.position));
                }
                else
                {
                    Vector3 moveVec = pt - _dragOrigin;

                    bool symmetryFail = false;
                    if (_symmetricMode)
                    {
                        float minX = _transformCurveOrigPts[0].x;
                        foreach (Vector3 origPt in _transformCurveOrigPts)
                        {
                            if (origPt.x < minX)
                            {
                                minX = origPt.x;
                            }
                        }
                        if (minX + moveVec.x < 0f)
                        {
                            moveVec.x = -minX;
                        }
                    }

                    if (!symmetryFail)
                    {
                        for (int i = 0; i < _currSelectedCurve.CtlPts.Count; ++i)
                        {
                            _currSelectedCurve.CtlPts[i].position = _transformCurveOrigPts[i] + moveVec;
                            _currSelectedCurve.UpdateControlPoint(_currSelectedCurve.CtlPts[i].GetComponent<ControlPoint>());
                        }
                        _currSelectedCurve.TryRender();
                    }
                }
            }
            else if (tr == TransformType.Rotate)
            {
                if (phase == TransformPhase.Start)
                {
                    _scaleRotatePoint = pt;
                    _transformCurveOrigPts.Clear();
                    _transformCurveOrigPts.AddRange(_currSelectedCurve.CtlPts.Select(p => p.position));
                }
                else if (phase == TransformPhase.PivotSelected)
                {
                    _dragOrigin = pt;
                }
                else
                {
                    Vector3 origVec = _dragOrigin - _scaleRotatePoint;
                    Vector3 vec = pt - _scaleRotatePoint;
                    float angle = Vector3.SignedAngle(origVec, vec, Vector3.up);
                    Quaternion q = Quaternion.AngleAxis(angle, Vector3.up);

                    bool symmetryFail = false;
                    if (_symmetricMode)
                    {
                        float minX = _transformCurveOrigPts[0].x;
                        foreach (Vector3 origPt in _transformCurveOrigPts)
                        {
                            if (((q * (origPt - _scaleRotatePoint)) + _scaleRotatePoint).x < 0f)
                            {
                                symmetryFail = true;
                                break;
                            }
                        }
                    }

                    if (!symmetryFail)
                    {
                        for (int i = 0; i < _currSelectedCurve.CtlPts.Count; ++i)
                        {
                            _currSelectedCurve.CtlPts[i].position = (q * (_transformCurveOrigPts[i] - _scaleRotatePoint)) + _scaleRotatePoint;
                            _currSelectedCurve.UpdateControlPoint(_currSelectedCurve.CtlPts[i].GetComponent<ControlPoint>());
                        }
                        _currSelectedCurve.TryRender();
                    }
                }
            }
            else if (tr == TransformType.Scale)
            {
                if (phase == TransformPhase.Start)
                {
                    _scaleRotatePoint = pt;
                    _transformCurveOrigPts.Clear();
                    _transformCurveOrigPts.AddRange(_currSelectedCurve.CtlPts.Select(p => p.position));
                }
                else if (phase == TransformPhase.PivotSelected)
                {
                    _dragOrigin = pt;
                }
                else
                {
                    Vector3 origVec = _dragOrigin - _scaleRotatePoint;
                    Vector3 vec = pt - _scaleRotatePoint;
                    float factor = Mathf.Sqrt(vec.sqrMagnitude / origVec.sqrMagnitude);

                    bool symmetryFail = false;
                    if (_symmetricMode)
                    {
                        float minX = _transformCurveOrigPts[0].x;
                        foreach (Vector3 origPt in _transformCurveOrigPts)
                        {
                            if ((((origPt - _scaleRotatePoint) * factor) + _scaleRotatePoint).x < 0f)
                            {
                                symmetryFail = true;
                                break;
                            }
                        }
                    }

                    if (!symmetryFail)
                    {
                        for (int i = 0; i < _currSelectedCurve.CtlPts.Count; ++i)
                        {
                            _currSelectedCurve.CtlPts[i].position = ((_transformCurveOrigPts[i] - _scaleRotatePoint) * factor) + _scaleRotatePoint;
                            _currSelectedCurve.UpdateControlPoint(_currSelectedCurve.CtlPts[i].GetComponent<ControlPoint>());
                        }
                        _currSelectedCurve.TryRender();
                    }
                }
            }
        }

        if (phase != TransformPhase.Finish)
        {
            switch (tr)
            {
                case TransformType.Move:
                    _currState = UserActionState.MoveCurve;
                    break;
                case TransformType.Rotate:
                    _currState = _currState == UserActionState.StartRotateCurve ? UserActionState.StartRotateCurveSelectPoint : UserActionState.RotateCurve;
                    break;
                case TransformType.Scale:
                    _currState = _currState == UserActionState.StartScaleCurve ? UserActionState.StartScaleCurveSelectPoint : UserActionState.ScaleCurve;
                    break;
                default:
                    break;
            }
        }
        else
        {
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

        foreach (Transform pt in crv.CtlPts)
        {
            pt.GetComponent<MeshRenderer>().sharedMaterial = GeomObjectFactory.GetCtlPtMtlEditing();
        }

        _currState = UserActionState.EditCurve;
    }

    public void StartMoveCurve(CurveGeomBase crv)
    {
        if (crv != _currSelectedCurve)
        {
            Debug.LogWarning("Started editing curve other than selected curve");
        }

        foreach (Transform pt in crv.CtlPts)
        {
            pt.GetComponent<MeshRenderer>().sharedMaterial = GeomObjectFactory.GetCtlPtMtlDefault();
        }

        _currState = UserActionState.StartMoveCurve;
    }

    public void StartRotateCurve(CurveGeomBase crv)
    {
        if (crv != _currSelectedCurve)
        {
            Debug.LogWarning("Started editing curve other than selected curve");
        }

        foreach (Transform pt in crv.CtlPts)
        {
            pt.GetComponent<MeshRenderer>().sharedMaterial = GeomObjectFactory.GetCtlPtMtlDefault();
        }

        _currState = UserActionState.StartRotateCurve;
    }

    public void StartScaleCurve(CurveGeomBase crv)
    {
        if (crv != _currSelectedCurve)
        {
            Debug.LogWarning("Started editing curve other than selected curve");
        }

        foreach (Transform pt in crv.CtlPts)
        {
            pt.GetComponent<MeshRenderer>().sharedMaterial = GeomObjectFactory.GetCtlPtMtlDefault();
        }

        _currState = UserActionState.StartScaleCurve;
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

    public void MirrorCurve(CurveGeomBase crv)
    {
        if (crv != _currSelectedCurve)
        {
            Debug.LogWarning("Started editing curve other than selected curve");
        }

        float minX = _currSelectedCurve.CtlPts[0].position.x,
            maxX = _currSelectedCurve.CtlPts[0].position.x;
        foreach (Transform pt in _currSelectedCurve.CtlPts)
        {
            if (pt.position.x < minX)
            {
                minX = pt.position.x;
            }
            if (pt.position.x > maxX)
            {
                maxX = pt.position.x;
            }
        }

        float mirrorX = minX + maxX;
        foreach (Transform pt in _currSelectedCurve.CtlPts)
        {
            pt.position = new Vector3(mirrorX - pt.position.x, pt.position.y, pt.position.z);
            _currSelectedCurve.UpdateControlPoint(pt.GetComponent<ControlPoint>());
        }
        _currSelectedCurve.TryRender();
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

    public void StartScaleLayer(LayerPlane l)
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

    public void DuplicateLayer(LayerPlane l)
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
        LayerPlane nextLayer = _activeLayer.Duplicate(maxH + 0.1f);
        nextLayer.SetSelected(true);
        GeomObjectFactory.GetLayerActionPanel().AttachLayer(nextLayer);
        _layers.Add(nextLayer);
        _activeLayer = nextLayer;
        _currState = UserActionState.Default;
    }

    public void ClearLayer(LayerPlane l)
    {
        if (l != _activeLayer)
        {
            Debug.LogWarning("Tried to clear layer other than active layer");
        }

        _activeLayer.Clear();
        _currState = UserActionState.Default;
    }

    public void DeleteLayer(LayerPlane l)
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
        LayerPlane toDelete = _activeLayer;

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
        MeshGenerator.QuadMesh quads = MeshGenerator.GenerateQuadMesh(_layers, 5);
        Transform previewObj = GeomObjectFactory.GetPreviewObject();
        previewObj.gameObject.SetActive(true);
        previewObj.Find("PreviewCore").gameObject.SetActive(false);
        MeshFilter mf = previewObj.GetComponent<MeshFilter>();
        mf.mesh = MeshGenerator.AssignToMesh(quads);
    }

    public void GenerateHexPreview()
    {
        MeshGenerator.HexMesh hexes = MeshGenerator.GenerateHexMesh(_layers, 5);
        Transform previewObj = GeomObjectFactory.GetPreviewObject();
        previewObj.gameObject.SetActive(true);
        Transform previewCore = previewObj.Find("PreviewCore");
        previewCore.gameObject.SetActive(true);
        previewCore.transform.localScale = hexes.CoreSize;
        previewCore.transform.position = new Vector3(0f, hexes.CoreElevation, 0f);
        MeshFilter mf = previewObj.GetComponent<MeshFilter>();
        mf.mesh = MeshGenerator.AssignToMesh(hexes, 0f);
    }

    public void DownloadStructureDef()
    {
        JavascripAdapter.DownloadData(SerializationUtils.Serialize(_layers), "txt", "MyTurret");
    }

    public void UploadStructureDef()
    {
        _receiveStructureDefHandle = GeomObjectFactory.GetFileReceiver().StartUploadFile();
        _receiveStructureDefHandle.OnDataReceived += OnReceiveStructureDef;
        //Debug.Log("Started receiving structure definition");
        if (_receiveStructureDefHandle.ReceivedData)
        {
            //Debug.Log(string.Format("Receiving structure definition immediately. Success={0} Data={1}", _receiveStructureDefHandle.Success, _receiveStructureDefHandle.Data));
            OnReceiveStructureDef(_receiveStructureDefHandle.Data, _receiveStructureDefHandle.Success);
        }
    }

    private void OnReceiveStructureDef(string structureDefData, bool success)
    {
        _receiveStructureDefHandle.OnDataReceived -= OnReceiveStructureDef;
        try
        {
            if (success)
            {
                SetStructureFromUpload(structureDefData);
            }
        }
        catch (System.Exception exc)
        {
            Debug.LogError(string.Format("Failed to load structure definition. Exception: {0}", exc));
        }
        finally
        {
            _receiveStructureDefHandle = null;
        }
    }

    public void UploadTankDesign()
    {
        _receiveTankBlueprintHandle = GeomObjectFactory.GetFileReceiver().StartUploadFile();
        _receiveTankBlueprintHandle.OnDataReceived += OnReceiveTankDesign;
        //Debug.Log("Started receiving tank design");
        if (_receiveStructureDefHandle.ReceivedData)
        {
            OnReceiveTankDesign(_receiveTankBlueprintHandle.Data, _receiveTankBlueprintHandle.Success);
        }
    }

    private void OnReceiveTankDesign(string tankDesignData, bool success)
    {
        _receiveTankBlueprintHandle.OnDataReceived -= OnReceiveTankDesign;
        _receiveTankBlueprintHandle = null;
    }

    private void SetStructureFromUpload(string structureDef)
    {
        GeomObjectFactory.GetLayerActionPanel().Release();
        GeomObjectFactory.GetCtlPtEditPanel().Release();

        StructureDef def = SerializationUtils.LoadFromJson(structureDef);
        for (int i = 0; i < _layers.Count; i++)
        {
            _layers[i].Clear();
            if (i != 0)
            {
                Destroy(_layers[i].gameObject);
            }
        }
        _layers.RemoveRange(1, _layers.Count - 1);

        for (int i = 0; i < def.Layers.Length; ++i)
        {
            LayerPlane layer;
            if (i == 0)
            {
                layer = _layers[0];
                layer.Elevation = def.Layers[i].Elevation;
                _activeLayer = layer;
                _activeLayer.SetSelected(true);
            }
            else
            {
                _layers.Add(layer = GeomObjectFactory.CreateLayer(def.Layers[i].Elevation));
                layer.SetSelected(false);
            }

            for (int j = 0; j < def.Layers[i].Curves.Length; ++j)
            {
                if (def.Layers[i].Curves[j].CurveType == SerializationUtils.SplineCurveString)
                {
                    BezierCurveGeom crv = GeomObjectFactory.CreateBezierCurve();
                    foreach (Vector3 pt in def.Layers[i].Curves[j].Points)
                    {
                        ControlPoint ctlPt = GeomObjectFactory.CreateCtlPt(pt).GetComponent<ControlPoint>();
                        crv.AppendCtlPt(ctlPt);
                    }
                    crv.TryRender();
                    layer.AddCurve(crv);
                }
                else if (def.Layers[i].Curves[j].CurveType == SerializationUtils.CircuarArcString)
                {

                }
            }
        }
        
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
    private Trihedron _moveTrihedron = null;
    private int _currCrvPtsLeft = 0;
    private string _currCreatingObject = "";
    private UploadFileReceiver.DataReceiveHandle _receiveStructureDefHandle = null;
    private UploadFileReceiver.DataReceiveHandle _receiveTankBlueprintHandle = null;

    private List<LayerPlane> _layers = new List<LayerPlane>();
    private LayerPlane _activeLayer = null;

    private Axis _dragAxis;
    private Vector3 _dragOrigin;
    private Vector3 _dragCtlPtOldPos;
    private Vector3 _scaleRotatePoint;
    private List<Vector3> _transformCurveOrigPts = new List<Vector3>();
    private float _dragStartTime = 0f;
    private bool _enableDrag = false;
    private bool _symmetricMode = true;
    private static readonly float _dragDelay = 0.5f;
}
