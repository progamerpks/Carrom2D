using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Striker : MonoBehaviour
{
    Rigidbody2D rb;

    [SerializeField] float senstivity = 10;
    [Space]

    [SerializeField] GameObject strikerBase;
    [SerializeField] GameObject tail;

    Vector3 startPos, endPos;
    private float rotationDegree;

    const float tailMinX = -3.5f, tailMaxX = -11, tailMinScale = 0.5f, tailMaxScale = 4;

    float magnitude;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.instance.isAiming)
        {

        }
    }

    

    private void OnMouseDown()
    {
        //Debug.Log("Clicked");
        if (!GameManager.instance.isAiming) { return; }

        startPos = transform.position;
        strikerBase.SetActive(true);
    }

    private void OnMouseDrag()
    {
        if (!GameManager.instance.isAiming) { return; }

        endPos = GameManager.instance.mainCamera.ScreenToWorldPoint(Input.mousePosition) - startPos;


        rotationDegree = Mathf.Atan2(-endPos.y, -endPos.x) * 180 / Mathf.PI;

        strikerBase.transform.eulerAngles = new Vector3(0, 0, rotationDegree);

        magnitude = endPos.magnitude - 10;

        //Debug.Log(magnitude);

        if (magnitude > 1.5f) { magnitude = 1.5f; }
        
        magnitude = magnitude / 1.5f;

        float alphaPos = Mathf.Lerp(tailMinX, tailMaxX, magnitude);
        float alphaScale = Mathf.Lerp(tailMinScale, tailMaxScale, magnitude);

        tail.transform.localPosition = new Vector3(alphaPos, 0, 0);
        tail.transform.localScale = new Vector3(alphaScale, alphaScale, 0);

    }

    private void OnMouseUp()
    {
        strikerBase.SetActive(false);

        if (!GameManager.instance.isAiming) { return; }

        if(magnitude < 0.01f) { return; }

        GameManager.instance.StrikerFired();
        rb.AddForce(new Vector3((-endPos.x * senstivity), (-endPos.y * senstivity), 0), ForceMode2D.Impulse);

    }
}
