using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HouseGenerationScript : MonoBehaviour
{


    //Properties regarding the house's size, floor/room regions and so on.
    [SerializeField] private int houseX = 10;
    [SerializeField] private int houseY = 10;
    private List<List<int>> houseData = new List<List<int>>();

    //A list of materials and tiles resources.
    [SerializeField] private Transform Tiles;
    [SerializeField] private List<Material> roomTileMat = new List<Material>();

    //A list of furnitures to use.


    private void Start()
    {
        createHouse();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            createHouse();
    }

    //Generation code.
    void createHouse()
    {

        if (houseX < 8)
            houseX = 8;

        if (houseY < 8)
            houseY = 8;

        //First clear the house children and data.
        houseData.Clear();
        for (int i = transform.childCount - 1; i > -1; i--)
            Destroy(transform.GetChild(i).gameObject);

        //Fill the empty houseData with a new blank grid.
        for(int x = 0; x < houseX; x++)
        {
            List<int> column = new List<int>();
            for (int y = 0; y < houseY; y++)
                column.Add(-1);
            houseData.Add(column);
        }

        //Generate hallways.
        bool isMainHorizontal = (Random.Range(0, 1) == 1);
        if (isMainHorizontal)
        {
            //It is horizontal
            int hallY = Random.Range(3, houseY - 5);
            for (int hallX = 0; hallX < houseY; hallX++)
                houseData[hallX][hallY] = houseData[hallX][hallY + 1] = 0;

            //Now generate two more hallways branching off the main one.
            for (int i = 0; i < 2; i++)
            {
                int hallX = Random.Range(3, houseY - 5);
                if (i == 0)
                    for (int y = 0; y < hallY; y++)
                        houseData[hallX][y] = houseData[hallX + 1][y] = 0;
                else
                    for (int y = hallY; y < houseY; y++)
                        houseData[hallX][y] = houseData[hallX + 1][y] = 0;
            }
        }
        else
        {
            //It is vertical.
            int hallX = Random.Range(3, houseX - 5);
            for (int hallY = 0; hallY < houseY; hallY++)
                houseData[hallX][hallY] = houseData[hallX + 1][hallY] = 0;

            //Now generate two more hallways branching off the main one.
            for(int i = 0; i < 2; i++)
            {
                int hallY = Random.Range(3, houseY - 5);
                if (i == 0)
                    for (int x = 0; x < hallX; x++)
                        houseData[x][hallY] = houseData[x][hallY + 1] = 0;
                else
                    for (int x = hallX; x < houseX; x++)
                        houseData[x][hallY] = houseData[x][hallY + 1] = 0;
            }

        }

        //Hallways are now complete, perform a flood fill algorithm in each corner.
        //(The hallways will NEVER be up against a corner of the house, so we can safely just spawn in whatever corner is desired.
        //1 - Living room
        //2 - Kitchen
        //3 - Bedroom
        //4 - Bathroom
        List<int> roomsAvailable = new List<int>(){ 1,2,3,4};

        for(int i =0; i < 4; i++)
        {

            //Pick a random room type.
            int roomType = roomsAvailable[Random.Range(0, roomsAvailable.Count - 1)];

            //Perform flood fill algorithm in each corner with room type starting corner.
            if (i == 0)
                fillHouseGap(0, 0, roomType);
            else if (i == 1)
                fillHouseGap(houseX - 1, 0, roomType);
            else if (i == 2)
                fillHouseGap(houseX - 1, houseY - 1, roomType);
            else if (i == 3)
                fillHouseGap(0, houseY - 1, roomType);

            //Remove said type from the list.
            roomsAvailable.Remove(roomType);

        }

        //Begin creating all the objects.
        for(int x = 0; x < houseX; x++)
        {
            for(int y = 0; y < houseY; y++)
            {

                int tileData = houseData[x][y];

                if (tileData == -1)
                    continue; //Empty tile, ignore for now.

                Transform tileTemplate = Tiles.Find("Empty");
                if (tileTemplate != null) {
                    GameObject tile = GameObject.Instantiate(tileTemplate.gameObject, transform).gameObject;
                    tile.transform.localPosition = new Vector3((float)x, 0.0f, (float)y);

                    if (tileData < roomTileMat.Count)
                        tile.GetComponent<MeshRenderer>().material = roomTileMat[tileData];
                }
            }
        }

    }

    private void fillHouseGap(int currX, int currY, int roomType)
    {
        int nextX = currX + 1;
        int nextY = currY;
        if (nextX >= 0 && nextX < houseX && nextY >= 0 && nextY < houseY)
        {
            if (houseData[nextX][nextY] == -1)
            {
                houseData[nextX][nextY] = roomType;
                fillHouseGap(nextX, nextY, roomType);
            }
        }

        nextX = currX;
        nextY = currY + 1;
        if (nextX >= 0 && nextX < houseX && nextY >= 0 && nextY < houseY)
        {
            if (houseData[nextX][nextY] == -1)
            {
                houseData[nextX][nextY] = roomType;
                fillHouseGap(nextX, nextY, roomType);
            }
        }

        nextX = currX - 1;
        nextY = currY;
        if (nextX >= 0 && nextX < houseX && nextY >= 0 && nextY < houseY)
        {
            if (houseData[nextX][nextY] == -1)
            {
                houseData[nextX][nextY] = roomType;
                fillHouseGap(nextX, nextY, roomType);
            }
        }

        nextX = currX;
        nextY = currY - 1;
        if (nextX >= 0 && nextX < houseX && nextY >= 0 && nextY < houseY)
        {
            if (houseData[nextX][nextY] == -1)
            {
                houseData[nextX][nextY] = roomType;
                fillHouseGap(nextX, nextY, roomType);
            }
        }
    }

    //Network code for sending the house layout.

}
