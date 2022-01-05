using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CtlPtEditPanel : MonoBehaviour
{
    public void AttachCtlPt(ControlPoint ctlpt)
    {
        _ctlPt = ctlpt;
        _crv = _ctlPt.ContainingCurve;
        UpdateValuesFromCtlPt();
        gameObject.SetActive(true);
    }

    public void Release()
    {
        _ctlPt = null;
        _crv = null;
        gameObject.SetActive(false);
    }

    void Awake()
    {
        AbsXInput.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<string>(AbsValueChanged));
        AbsZInput.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<string>(AbsValueChanged));
        RelativeXInput.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<string>(RelativeValueChanged));
        RelativeZInput.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<string>(RelativeValueChanged));
    }

    private void AbsValueChanged(string val)
    {
        Vector3 absolutePos = new Vector3(float.Parse(AbsXInput.text), _ctlPt.transform.position.y, float.Parse(AbsZInput.text));
        GeomObjectFactory.GetGeometryManager().TryChangeCtlPtPosition(_ctlPt, absolutePos);
        _crv.UpdateControlPoint(_ctlPt);
        _crv.TryRender();
        Vector3 relativePos = _ctlPt.transform.position - _crv.CtlPts[0].position;
        RelativeXInput.SetTextWithoutNotify(relativePos.x.ToString());
        RelativeZInput.SetTextWithoutNotify(relativePos.z.ToString());
    }

    private void RelativeValueChanged(string val)
    {
        Vector3 relativePos = new Vector3(float.Parse(RelativeXInput.text), 0f, float.Parse(RelativeZInput.text));
        Vector3 absolutePos = _crv.CtlPts[0].position + relativePos;
        GeomObjectFactory.GetGeometryManager().TryChangeCtlPtPosition(_ctlPt, absolutePos);
        _crv.UpdateControlPoint(_ctlPt);
        _crv.TryRender();
        AbsXInput.SetTextWithoutNotify(_ctlPt.transform.position.x.ToString());
        AbsZInput.SetTextWithoutNotify(_ctlPt.transform.position.z.ToString());
    }

    public void UpdateValuesFromCtlPt()
    {
        AbsXInput.SetTextWithoutNotify(_ctlPt.transform.position.x.ToString());
        AbsZInput.SetTextWithoutNotify(_ctlPt.transform.position.z.ToString());
        Vector3 relativePos = _ctlPt.transform.position - _crv.CtlPts[0].position;
        RelativeXInput.SetTextWithoutNotify(relativePos.x.ToString());
        RelativeZInput.SetTextWithoutNotify(relativePos.z.ToString());
    }

    public TMPro.TMP_InputField AbsXInput;
    public TMPro.TMP_InputField AbsZInput;
    public TMPro.TMP_InputField RelativeXInput;
    public TMPro.TMP_InputField RelativeZInput;

    private ControlPoint _ctlPt;
    private CurveGeomBase _crv;
}
