using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class LayerPlane : MonoBehaviour
{
    public void AddCurve(CurveGeomBase crv)
    {
        _curves.Add(crv);
    }

    public void DeleteCurve(CurveGeomBase crv)
    {
        if (_curves.Contains(crv))
        {
            _curves.Remove(crv);
            Destroy(crv.gameObject);
        }
    }

    public bool ContainsCurve(CurveGeomBase crv)
    {
        return _curves.Contains(crv);
    }

    public bool ContainsCtlPt(ControlPoint ctlPt)
    {
        if (ctlPt.ContainingCurve == null)
        {
            return false;
        }
        return ContainsCurve(ctlPt.ContainingCurve);
    }

    public void CheckConnectivity()
    {
        _chainBuf.Clear();

        bool prevAxisActive = AxisObj.gameObject.activeInHierarchy;
        AxisObj.gameObject.SetActive(true);
        bool fullyConnected = false;
        CurveGeomBase currCrv = null;
        Vector3 currPt = Vector3.zero;
        LayerAxis axis = GetComponentInChildren<LayerAxis>(true);
        CapsuleCollider axisCapsule = axis.GetComponentInChildren<CapsuleCollider>(true);
        Vector3 axisVector = axis.transform.forward;
        Vector3 pt1 = axis.transform.position - axisVector * (axisCapsule.height / 2f);
        Vector3 pt2 = axis.transform.position + axisVector * (axisCapsule.height / 2f);
        int numHits = Physics.OverlapCapsuleNonAlloc(pt1, pt2, axisCapsule.radius, _colliderBuf, GlobalData.ControlPtsLayerMask);
        float maxZ = 0f;
        ControlPoint firstCtlPt = null;
        for (int i  = 0; i < numHits; ++i)
        {
            ControlPoint ctlPt = _colliderBuf[i].GetComponent<ControlPoint>();
            if (ContainsCtlPt(ctlPt))
            {
                if (Mathf.Abs(ctlPt.transform.position.x) < GlobalData.CurveConnectionTolerance)
                {
                    if (firstCtlPt == null || ctlPt.transform.position.z > maxZ)
                    {
                        firstCtlPt = ctlPt;
                        maxZ = ctlPt.transform.position.z;
                    }
                }
            }
        }

        if (firstCtlPt != null)
        {
            currCrv = firstCtlPt.ContainingCurve;
            if (currCrv.CtlPts[0].gameObject == firstCtlPt.gameObject)
            {
                _chainBuf.Add((currCrv, false));
                currPt = currCrv.EvalEnd();
            }
            else
            {
                _chainBuf.Add((currCrv, true));
                currPt = currCrv.EvalStart();
            }
        }
        else
        {
            AxisObj.gameObject.SetActive(prevAxisActive);
            return;
        }

        while (true)
        {
            numHits = Physics.OverlapSphereNonAlloc(currPt, axisCapsule.radius, _colliderBuf, GlobalData.ControlPtsLayerMask | GlobalData.LayersLayerMask);
            bool chainSuccess = false;
            for (int i = 0; i < numHits; ++i)
            {
                ControlPoint ctlPt = _colliderBuf[i].GetComponent<ControlPoint>();
                LayerAxis hitAxis = _colliderBuf[i].GetComponent<LayerAxis>();
                if (ctlPt != null && ctlPt.ContainingCurve != currCrv && ContainsCtlPt(ctlPt) && !_chainBuf.Exists(x => x.Item1 == ctlPt.ContainingCurve))
                {
                    if ((ctlPt.transform.position - currPt).sqrMagnitude < GlobalData.CurveConnectionTolerance * GlobalData.CurveConnectionTolerance)
                    {
                        currCrv = ctlPt.ContainingCurve;
                        if (currCrv.CtlPts[0].gameObject == ctlPt.gameObject)
                        {
                            _chainBuf.Add((currCrv, false));
                            currPt = currCrv.EvalEnd();
                        }
                        else
                        {
                            _chainBuf.Add((currCrv, true));
                            currPt = currCrv.EvalStart();
                        }
                        chainSuccess = true;
                        break;
                    }
                }
                else if (hitAxis != null)
                {
                    if (Mathf.Abs(currPt.x) < GlobalData.CurveConnectionTolerance)
                    {
                        fullyConnected = true;
                        chainSuccess = true;
                        break;
                    }
                }
            }

            if (fullyConnected || !chainSuccess)
            {
                break;
            }
        }

        if (fullyConnected)
        {
            Debug.Log(string.Format("Found a fully connected chain of {0} curves: {1}", _chainBuf.Count, DbgPrintCurveChain(_chainBuf)));
        }
        else
        {
            Debug.Log(string.Format("Layer is not fully connected. Max chain of {0} curves: {1}", _chainBuf.Count, DbgPrintCurveChain(_chainBuf)));
        }

        AxisObj.gameObject.SetActive(prevAxisActive);
    }

    public List<ValueTuple<CurveGeomBase, bool>> GetConnectedChain()
    {
        CheckConnectivity();
        return new List<(CurveGeomBase, bool)>(_chainBuf);
    }

    private string DbgPrintCurveChain(List<ValueTuple<CurveGeomBase, bool>> chain)
    {
        StringBuilder sb = new StringBuilder();

        bool first = true;
        foreach (ValueTuple<CurveGeomBase, bool> item in chain)
        {
            if (!first)
            {
                sb.Append(" => ");
            }
            first = false;
            sb.AppendFormat("({0}-point curve ", item.Item1.Order);
            sb.Append(item.Item2 ? " reversed)" : ")");
        }

        return sb.ToString();
    }

    public void SetSelected(bool selected)
    {
        PlaneObject.gameObject.SetActive(selected);
        UpArrow.gameObject.SetActive(selected);
        DownArrow.gameObject.SetActive(selected);
        AxisObj.gameObject.SetActive(selected);
    }

    public float Elevation
    {
        get
        {
            return transform.position.y;
        }
        set
        {
            transform.position = new Vector3(transform.position.z, value, transform.position.x);
            foreach (CurveGeomBase crv in _curves)
            {
                foreach (Transform ctlPtTr in crv.CtlPts)
                {
                    ctlPtTr.position = new Vector3(ctlPtTr.position.x, value, ctlPtTr.position.z);
                    crv.UpdateControlPoint(ctlPtTr.GetComponent<ControlPoint>());
                }
                crv.TryRender();
            }
        }
    }

    private HashSet<CurveGeomBase> _curves = new HashSet<CurveGeomBase>();
    private Collider[] _colliderBuf = new Collider[10000];
    private List<ValueTuple<CurveGeomBase, bool>> _chainBuf = new List<(CurveGeomBase, bool)>();

    public LayerPlane Duplicate(float elevation)
    {
        LayerPlane copy = GeomObjectFactory.CreateLayer(elevation);

        foreach (CurveGeomBase crv in _curves)
        {
            CurveGeomBase crvCopy = crv.Copy();
            foreach (Transform ctlPtTr in crvCopy.CtlPts)
            {
                ctlPtTr.position = new Vector3(ctlPtTr.position.x, elevation, ctlPtTr.position.z);
                crvCopy.UpdateControlPoint(ctlPtTr.GetComponent<ControlPoint>());
            }
            crvCopy.TryRender();
            copy.AddCurve(crvCopy);
        }

        return copy;
    }

    public void Clear()
    {
        foreach (CurveGeomBase crv in _curves)
        {
            Destroy(crv.gameObject);
        }
        _curves.Clear();
    }

    public void StartUniformScale()
    {
        bool anyRemoved;
        do
        {
            anyRemoved = false;
            foreach (CurveGeomBase crv in _backupControlPoints.Keys)
            {
                if (!_curves.Contains(crv))
                {
                    _backupControlPoints.Remove(crv);
                    anyRemoved = true;
                    break;
                }
            }
        } while (anyRemoved);

        foreach (CurveGeomBase crv in _curves)
        {
            if (_backupControlPoints.ContainsKey(crv))
            {
                _backupControlPoints[crv].Clear();
                _backupControlPoints[crv].AddRange(crv.CtlPts.Select(p => p.position));
            }
            else
            {
                _backupControlPoints.Add(crv, crv.CtlPts.Select(p => p.position).ToList());
            }
            
        }
    }

    public void UniformScale(float factor)
    {
        foreach (CurveGeomBase crv in _curves)
        {
            for (int i = 0; i < crv.CtlPts.Count; ++i)
            {
                crv.CtlPts[i].position = new Vector3(_backupControlPoints[crv][i].x * factor, Elevation, _backupControlPoints[crv][i].z * factor);
                crv.UpdateControlPoint(crv.CtlPts[i].GetComponent<ControlPoint>());
            }
            crv.TryRender();
        }
    }

    public IReadOnlyCollection<CurveGeomBase> Curves => _curves;

    private Dictionary<CurveGeomBase, List<Vector3>> _backupControlPoints = new Dictionary<CurveGeomBase, List<Vector3>>();

    public Transform PlaneObject;
    public Transform UpArrow;
    public Transform DownArrow;
    public Transform AxisObj;
}
