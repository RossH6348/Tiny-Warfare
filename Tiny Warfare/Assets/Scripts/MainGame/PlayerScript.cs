using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerScript : NetworkBehaviour
{

    [SerializeField] private GameObject playerCamera;

    private Rigidbody rigid;
    private Animator animator;

    private bool isGrounded = true;
    private bool isJumping = false;

    //Shotgun parameters.
    [SerializeField] public Transform muzzlePoint;
    [SerializeField] private Animator shotgunAnimator;

    [SerializeField] private float moveSpeed = 2.0f;

    //public NetworkVariable<string> PlayerName = new NetworkVariable<string>("Tiny Soldier");
    public NetworkVariable<int> Health = new NetworkVariable<int>(100);
    public NetworkVariable<float> Recoil = new NetworkVariable<float>(0.0f);
    public ulong lastShot = 0; //Who was the person that landed the finishing shot?


    // Start is called before the first frame update
    void Start()
    {

        rigid = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

    }

    // Update is called once per frame
    void Update()
    {

        if (IsServer)
        {
            Recoil.Value -= Time.deltaTime; //Lower the amount of recoil of the shotgun over time.
            if (Recoil.Value < 0.0f)
                Recoil.Value = 0.0f;
        }

        playerCamera.SetActive(IsOwner);

        if (!IsOwner)
            return;

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

        transform.position += transform.right * moveVelocity.x * moveSpeed * Time.deltaTime;
        transform.position += transform.forward * moveVelocity.y * moveSpeed * Time.deltaTime;

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

        bool mousePressed = Input.GetMouseButtonDown(0);

        if (mousePressed)
            FireShotgunServerRpc();

        shotgunAnimator.SetBool("triggerPulled", mousePressed);

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



    //All network related stuff here
    [SerializeField] private GameObject trailPrefab;

    [ServerRpc(RequireOwnership = false)]
    private void FireShotgunServerRpc(ServerRpcParams serverRpcParams = default)
    {

        //Get the player object if we could.
        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[serverRpcParams.Receive.SenderClientId].PlayerObject;
        if (playerObject != null)
        {

            //They got an object, attempt to get their playerscript.
            PlayerScript playerScript = playerObject.GetComponent<PlayerScript>();
            if (playerScript != null)
            {

                Transform muzzle = playerScript.muzzlePoint;

                Vector3 muzzlePos = muzzle.position;
                Vector3 muzzleRight = muzzle.right;
                Vector3 muzzleVec = muzzle.up;

                for (int i = 0; i < 5; i++)
                {
                    GameObject trail = GameObject.Instantiate(trailPrefab, null);

                    if (trail != null)
                    {

                        //Get a random range.
                        Vector3 velocity = Quaternion.AngleAxis((4.0f + (6.0f * Recoil.Value)) * Random.Range(0.0f, 1.0f), muzzleRight) * muzzleVec;
                        velocity = Quaternion.AngleAxis(360.0f * Random.Range(0.0f, 1.0f), muzzleVec) * velocity;

                        float range = 100.0f;

                        RaycastHit hit;
                        if (Physics.Raycast(muzzlePos, velocity, out hit, range))
                        {
                            range = (hit.point - muzzlePos).magnitude;

                            PlayerScript victim = hit.collider.GetComponent<PlayerScript>();
                            if (victim != null)
                            {
                                victim.lastShot = serverRpcParams.Receive.SenderClientId; //Record who was dealing the shot.
                                victim.Health.Value -= 20; //Deal 20 damage to the victim.
                            }

                        }

                        trail.transform.position = muzzlePos + (velocity * range * 0.5f);
                        trail.transform.rotation = Quaternion.LookRotation(velocity);
                        trail.transform.localScale = new Vector3(trail.transform.localScale.x * 0.2f, trail.transform.localScale.y * 0.2f, trail.transform.localScale.z * range);

                        trail.GetComponent<NetworkObject>().SpawnWithOwnership(serverRpcParams.Receive.SenderClientId);

                        Destroy(trail, 0.5f);

                    }

                }

                //After firing, increase recoil of the shotgun for 1.5 seconds, you gain 4 degrees of spread per seconds remaining, so at worse it is 10 degrees spread compared to 4.
                Recoil.Value += 1.5f;
                if (Recoil.Value > 1.5f)
                    Recoil.Value = 1.5f;

            }

        }

    }


}
