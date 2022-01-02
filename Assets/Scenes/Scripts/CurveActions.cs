using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurveActions : MonoBehaviour
{
    public void AttachCurve(CurveGeomBase crv)
    {
        gameObject.SetActive(true);
        _currCrv = crv;
    }

    public void Release()
    {
        _currCrv = null;
        gameObject.SetActive(false);
    }

    public void OnMoveAction()
    {
        GeomObjectFactory.GetGeometryManager().StartMoveCurve(_currCrv);
    }

    public void OnRotateAction()
    {
        GeomObjectFactory.GetGeometryManager().StartRotateCurve(_currCrv);
    }

    public void OnScaleAction()
    {
        GeomObjectFactory.GetGeometryManager().StartScaleCurve(_currCrv);
    }

    public void OnEditAction()
    {
        GeomObjectFactory.GetGeometryManager().StartEditCurve(_currCrv);
    }

    public void OnDeleteAction()
    {
        GeomObjectFactory.GetGeometryManager().DeleteCurve(_currCrv);
    }

    private CurveGeomBase _currCrv = null;
}
