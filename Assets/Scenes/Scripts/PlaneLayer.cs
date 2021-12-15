using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PlaneLayer : MonoBehaviour
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
        List<ValueTuple<CurveGeomBase, bool>> chain = new List<(CurveGeomBase, bool)>();

        bool fullyConnected = false;
        CurveGeomBase currCrv = null;
        Vector3 currPt = Vector3.zero;
        LayerAxis axis = GetComponentInChildren<LayerAxis>();
        CapsuleCollider axisCapsule = axis.GetComponentInChildren<CapsuleCollider>();
        Vector3 axisVector = axis.transform.forward;
        Vector3 pt1 = axisCapsule.center - axisVector * (axisCapsule.height / 2f);
        Vector3 pt2 = axisCapsule.center + axisVector * (axisCapsule.height / 2f);
        Collider[] candidateCtlPts = Physics.OverlapCapsule(pt1, pt2, axisCapsule.radius, GlobalData.ControlPtsLayerMask);
        float maxY = 0f;
        ControlPoint firstCtlPt = null;
        foreach (Collider c in candidateCtlPts)
        {
            ControlPoint ctlPt = c.GetComponent<ControlPoint>();
            if (ContainsCtlPt(ctlPt))
            {
                if (Mathf.Abs(ctlPt.transform.position.x) < GlobalData.CurveConnectionTolerance)
                {
                    if (firstCtlPt == null || ctlPt.transform.position.y > maxY)
                    {
                        firstCtlPt = ctlPt;
                        maxY = ctlPt.transform.position.y;
                    }
                }
            }
        }

        if (firstCtlPt != null)
        {
            currCrv = firstCtlPt.ContainingCurve;
            if (currCrv.CtlPts[0].gameObject == firstCtlPt.gameObject)
            {
                chain.Add((currCrv, false));
                currPt = currCrv.EvalEnd();
            }
            else
            {
                chain.Add((currCrv, true));
                currPt = currCrv.EvalStart();
            }
        }
        else
        {
            return;
        }

        while (true)
        {
            candidateCtlPts = Physics.OverlapSphere(currPt, axisCapsule.radius, GlobalData.ControlPtsLayerMask | GlobalData.LayersLayerMask);
            bool chainSuccess = false;
            foreach (Collider c in candidateCtlPts)
            {
                ControlPoint ctlPt = c.GetComponent<ControlPoint>();
                LayerAxis hitAxis = c.GetComponent<LayerAxis>();
                if (ctlPt != null && ctlPt.ContainingCurve != currCrv && ContainsCtlPt(ctlPt) && !chain.Exists(x => x.Item1 == ctlPt.ContainingCurve))
                {
                    if ((ctlPt.transform.position - currPt).sqrMagnitude < GlobalData.CurveConnectionTolerance * GlobalData.CurveConnectionTolerance)
                    {
                        currCrv = ctlPt.ContainingCurve;
                        if (currCrv.CtlPts[0].gameObject == ctlPt.gameObject)
                        {
                            chain.Add((currCrv, false));
                            currPt = currCrv.EvalEnd();
                        }
                        else
                        {
                            chain.Add((currCrv, true));
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
            Debug.Log(string.Format("Found a fully connected chain of {0} curves: {1}", chain.Count, DbgPrintCurveChain(chain)));
        }
        else
        {
            Debug.Log(string.Format("Layer is not fully connected. Max chain of {0} curves: {1}", chain.Count, DbgPrintCurveChain(chain)));
        }
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
        }
    }
    private HashSet<CurveGeomBase> _curves = new HashSet<CurveGeomBase>();

    public Transform PlaneObject;
    public Transform UpArrow;
    public Transform DownArrow;
    public Transform AxisObj;
}
