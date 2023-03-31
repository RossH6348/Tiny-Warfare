using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{

    private Rigidbody rigid;
    private Animator animator;

    private bool isGrounded = true;
    private bool isJumping = false;

    // Start is called before the first frame update
    void Start()
    {

        rigid = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

    }

    // Update is called once per frame
    void Update()
    {

        Vector2 moveVelocity = Vector2.zero;
        if (Input.GetKey(KeyCode.W))
            moveVelocity.y += 1.0f;

        if (Input.GetKey(KeyCode.S))
            moveVelocity.y -= 1.0f;

        if (Input.GetKey(KeyCode.D))
            moveVelocity.x += 1.0f;

        if (Input.GetKey(KeyCode.A))
            moveVelocity.x -= 1.0f;

        moveVelocity = moveVelocity.normalized;

        transform.position += transform.right * moveVelocity.x * 5.0f * Time.deltaTime;
        transform.position += transform.forward * moveVelocity.y * 5.0f * Time.deltaTime;

        float angleTurn = Input.GetAxis("Mouse X") * 10.0f;
        transform.localRotation = Quaternion.Euler(0.0f, transform.localRotation.eulerAngles.y + angleTurn, 0.0f);

        //-1 - Not moving.
        //0 - Forward
        //1 - Backward
        //2 - Sidestep (Right)
        //3 - Sidestep (Left)
        int moveDirection = -1;

        isGrounded = isOnGround();

        if (isGrounded && !isJumping && Input.GetKeyDown(KeyCode.Space))
        {
            isJumping = true;
            rigid.velocity = Physics.gravity * -0.5f;
            transform.position += new Vector3(0.0f, 0.1f, 0.0f); //So they ain't immediately touching the ground.
            isGrounded = false;
        }

        if (moveVelocity.magnitude > 0.1f)
        {
            float forwardDirection = Vector2.Dot(moveVelocity, Vector2.up);
            if (forwardDirection < -0.8f)
                moveDirection = 1;
            else if (forwardDirection < 0.8f)
                moveDirection = (Vector2.Dot(moveVelocity, Vector2.right) > 0.0f ? 2 : 3);
            else
                moveDirection = 0;
        }

        animator.SetInteger("moveDirection", moveDirection);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isJumping", isJumping);

        isJumping = false;

    }

    private bool isOnGround()
    {

        if (isGrounded)
        {
            isGrounded = false;
            return true;
        }

        return false;

    }

    private void OnCollisionStay(Collision collision)
    {

        isGrounded = true;

    }

}
