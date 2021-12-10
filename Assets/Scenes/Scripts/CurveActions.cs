using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurveActions : MonoBehaviour
{
    public void AssignCurve(CurveGeomBase crv)
    {
        gameObject.SetActive(true);
        _currCrv = crv;
    }

    public void Release()
    {
        _currCrv = null;
        gameObject.SetActive(false);
    }

    private CurveGeomBase _currCrv = null;
}
