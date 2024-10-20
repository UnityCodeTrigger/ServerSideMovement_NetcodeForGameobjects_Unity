using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    public float speed = 3;
    Vector3 moveDirection;

    Vector3 clientPosition;
    Vector3 serverposition;
    float serverCorrectionThreshold = 0.1f;
    float reconciliationSpeed = 5f;

    void Start()
    {
        clientPosition = transform.position;
    }

    void Update()
    {
        if (IsOwner && IsClient)
        {
            Vector3 inputAxis = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0);
            SetDirectionServerRpc(inputAxis);

            MoveLocalClient(inputAxis);

            Vector3 interpolatedPosition = Vector3.Lerp(clientPosition, serverposition, Time.deltaTime * speed);
            transform.position = interpolatedPosition;
        }

        if (IsServer)
        {
            Move(moveDirection);
            CheckClientPosition();
        }
    }

    [ClientRpc]
    public void UpdateClientPositionClientRpc(Vector3 serverPosition)
    {
        float dist = Vector3.Distance(clientPosition, serverPosition);
        if (dist > serverCorrectionThreshold)
        {
            clientPosition = Vector3.Lerp(clientPosition, serverPosition, Time.deltaTime * reconciliationSpeed);
            if (!IsOwner)
                transform.position = clientPosition;
        }
    }

    [ServerRpc]
    public void SetDirectionServerRpc(Vector3 dir)
    {
        moveDirection = dir;
    }

    void MoveLocalClient(Vector3 dir)
    {
        Vector3 desiredPos = transform.position + (dir * speed * Time.deltaTime);

        if (desiredPos.x >= 8 || desiredPos.x <= -8)
            dir.x = 0;
        if (desiredPos.y >= 4 || desiredPos.y <= -4)
            dir.y = 0;

        clientPosition += dir * speed * Time.deltaTime;
    }

    void Move(Vector3 dir)
    {
        if (!IsServer)
            return;

        Vector3 desiredPos = transform.position + (dir * speed * Time.deltaTime);

        if (desiredPos.x >= 8 || desiredPos.x <= -8)
            dir.x = 0;
        if (desiredPos.y >= 4 || desiredPos.y <= -4)
            dir.y = 0;

        transform.Translate(dir * speed * Time.deltaTime);
        UpdateServerPositionClientRpc(transform.position);
    }

    [ClientRpc]
    void UpdateServerPositionClientRpc(Vector3 position)
    {
        serverposition = position;
    }

    void CheckClientPosition()
    {
        if (Vector3.Distance(transform.position, clientPosition) > serverCorrectionThreshold)
        {
            UpdateClientPositionClientRpc(transform.position);
        }
    }
}