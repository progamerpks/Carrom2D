using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pocket : MonoBehaviour
{
    [SerializeField] float catchSpeedThreshold = 5;
    float thresholdSQ = 0;
    private void OnEnable()
    {
        thresholdSQ = Mathf.Pow(catchSpeedThreshold, 2);
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.attachedRigidbody != null)
        {
            var rb = collision.attachedRigidbody;
            if (rb.velocity.sqrMagnitude < thresholdSQ)
            {
                rb.velocity = Vector2.zero;
                rb.MovePosition(transform.position);
                rb.gameObject.SetActive(false);
                //GameObject.Destroy(rb.gameObject);  

                if (collision.tag == "White")
                {
                    GameManager.instance.WhiteAdd();
                }
                else if (collision.tag == "Black")
                {
                    GameManager.instance.BlackAdd();
                }
                else if (collision.tag == "Queen")
                {
                    GameManager.instance.QueenAdd();
                }
                else if(collision.tag == "Striker")
                {
                    GameManager.instance.StrikerAdd();
                }
            }
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
