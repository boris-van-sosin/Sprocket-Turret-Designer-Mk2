using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class GeometryManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        _layersLayerMask = LayerMask.GetMask("PlaneLayers");
        _activeLayer = GeomObjectFactory.CreateLayer(0f);
        _layers.Add(_activeLayer);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (_currTmpBzrCurve != null && _currCrvPtsLeft > 0)
            {
                Ray r = GeomObjectFactory.GetCameraControl().UserCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit[] hits = Physics.RaycastAll(r, 1000f, _layersLayerMask);
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
        }
    }


    private string HelpString => string.Format("Creating a {0}.\n{1} points left.", _currCreatingObject, _currCrvPtsLeft);

    private BezierCurveGeom _currTmpBzrCurve = null;
    private CircularArcGeom _currTmpCircArc = null;
    private int _currCrvPtsLeft = 0;
    private string _currCreatingObject = "";

    private List<PlaneLayer> _layers = new List<PlaneLayer>();
    private PlaneLayer _activeLayer = null;

    private ControlPoint _selecedCtlPt = null;
    private Axis _dragAxis;
    private Vector3 _dragOrigin;
    private float _dragTime = 0f;
    private static readonly float _dragDelay = 0.5f;
    private static int _layersLayerMask;
}
