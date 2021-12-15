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

    }

    public void OnRotateAction()
    {

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
