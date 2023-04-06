using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MainGameScript : NetworkBehaviour
{

    [SerializeField] private HouseGenerationScript houseGenerator;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private GameObject PlayerPrefab;


    public void InitializeGame()
    {
        //Move the title screen camera to a position.
        gameCamera.transform.position = new Vector3(5.0f, 15.0f, 5.0f);
        gameCamera.transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));

        PlayerReadyServerRpc();
    }

    private void spawnPlayer(ulong clientId)
    {

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        setSpectateCameraClientRpc(false, clientRpcParams);

        GameObject Soldier = GameObject.Instantiate(PlayerPrefab, transform);
        Soldier.transform.localPosition = new Vector3((float)Random.Range(0, houseGenerator.houseX - 1) + 0.5f, 0.0f, (float)Random.Range(0, houseGenerator.houseY - 1) + 0.5f);
        Soldier.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

    }

    //Network related stuff here.
    [ServerRpc(RequireOwnership = false)]
    private void PlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {

        ClientGroupScript.clientIsReady[serverRpcParams.Receive.SenderClientId] = true;

        Debug.Log(ClientGroupScript.clientToName[serverRpcParams.Receive.SenderClientId] + " is ready!");

        //Check if all clients are ready?
        bool isEveryoneReady = true;
        foreach(ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            if (!ClientGroupScript.clientIsReady[clientId])
            {
                isEveryoneReady = false;
                break;
            }
        }

        if (isEveryoneReady)
        {
            //BEGIN HOUSE GENERATION! (On the Host side at least.)
            houseGenerator.createHouse();
            
            //Begin compressing each column of the house and send it to all clients.
            for(int x = 0; x < houseGenerator.houseX; x++)
            {

                string column = "";

                for(int y =0; y < houseGenerator.houseY; y++)
                {

                    TileData tile = houseGenerator.houseData[x][y];

                    string newTile = x.ToString();
                    newTile = newTile + "," + y.ToString();
                    newTile = newTile + "," + tile.room.ToString();
                    newTile = newTile + "," + tile.north.ToString();
                    newTile = newTile + "," + tile.east.ToString();
                    newTile = newTile + "," + tile.south.ToString();
                    newTile = newTile + "," + tile.west.ToString();

                    column = column + newTile;
                    if (y < houseGenerator.houseY - 1)
                        column = column + " ";

                }

                PlaceHouseColumnClientRpc(column);

            }

            foreach(ulong clientId in NetworkManager.ConnectedClientsIds)
                spawnPlayer(clientId);

        }

    }

    [ClientRpc]
    private void PlaceHouseColumnClientRpc(string column)
    {
        string[] tiles = column.Split(" ");
        foreach(string tile in tiles)
        {

            string[] tileData = tile.Split(",");
            houseGenerator.placeHouseTile(
                int.Parse(tileData[0]),
                int.Parse(tileData[1]),
                int.Parse(tileData[2]),
                int.Parse(tileData[3]),
                int.Parse(tileData[4]),
                int.Parse(tileData[5]),
                int.Parse(tileData[6])
            );

        }

    }

    [ClientRpc]
    private void setSpectateCameraClientRpc(bool status, ClientRpcParams clientRpcParams = default)
    {
        gameCamera.enabled = status;
    }

}
