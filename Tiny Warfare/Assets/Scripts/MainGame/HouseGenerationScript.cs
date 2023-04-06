using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This is to handle what floor type, but also what walls it should be occupying.
public class TileData
{

    public int room = -1;
    public int north = -1;
    public int east = -1;
    public int south = -1;
    public int west = -1;

    public bool isOccupied = false;

}

public class RoomData
{

    public int room = -1;

    public List<List<Vector2Int>> northWalls = new List<List<Vector2Int>>();
    public List<List<Vector2Int>> eastWalls = new List<List<Vector2Int>>();
    public List<List<Vector2Int>> southWalls = new List<List<Vector2Int>>();
    public List<List<Vector2Int>> westWalls = new List<List<Vector2Int>>();

    public RoomData(int roomType = -1)
    {
        room = roomType;
    }

}

public class HouseGenerationScript : MonoBehaviour
{


    //Properties regarding the house's size, floor/room regions and so on.
    [SerializeField] public int houseX = 10;
    [SerializeField] public int houseY = 10;
    public List<List<TileData>> houseData = new List<List<TileData>>();

    //A list of materials and tiles resources.
    [SerializeField] private List<Material> roomTileMat = new List<Material>();
    [SerializeField] private GameObject floorTemplate;

    //list containing the wall types prefabs.
    [SerializeField] private List<GameObject> wallTemplates = new List<GameObject>();
    [SerializeField] private List<Material> wallMat = new List<Material>();
    [SerializeField] private List<Material> railMat = new List<Material>();
    //ID table for each wall type.
    //0 - Wall
    //1 - Corner
    //2 - Doorway
    //3 - Window Single
    //4 - Window Left End
    //5 - Window Middle
    //6 - Window Right End

    //A list of furnitures to use.
    [SerializeField] private GameObject debugFurniture;

    public void clearHouse()
    {
        //First clear the house children and data.
        houseData.Clear();
        for (int i = transform.childCount - 1; i > -1; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    //Generation code.
    public void createHouse()
    {

        if (houseX < 8)
            houseX = 8;

        if (houseY < 8)
            houseY = 8;

        clearHouse();

        //Fill the empty houseData with a new blank grid.
        for (int x = 0; x < houseX; x++)
        {
            List<TileData> column = new List<TileData>();
            for (int y = 0; y < houseY; y++)
            {
                TileData newTile = new TileData();
                column.Add(newTile);
            }
            houseData.Add(column);
        }

        //FIRST STAGE OF GENERATION: HALLWAYS & ROOM REGIONS.
        List<RoomData> Rooms = new List<RoomData>() { 
            new RoomData(0), //Hallway
            new RoomData(1), //Living Room
            new RoomData(2), //Kitchen
            new RoomData(3), //Bedroom
            new RoomData(4) //Bathroom
        };

        //Generate hallways.
        bool isMainHorizontal = (Random.Range(0.0f, 1.0f) <= 0.5f);
        if (isMainHorizontal)
        {
            //It is horizontal
            int hallY = Random.Range(3, houseY - 5);
            for (int hallX = 0; hallX < houseY; hallX++)
                houseData[hallX][hallY].room = houseData[hallX][hallY + 1].room = 0;

            //Now generate two more hallways branching off the main one.
            for (int i = 0; i < 2; i++)
            {
                int hallX = Random.Range(3, houseY - 5);
                if (i == 0)
                    for (int y = 0; y < hallY; y++)
                        houseData[hallX][y].room = houseData[hallX + 1][y].room = 0;
                else
                    for (int y = hallY; y < houseY; y++)
                        houseData[hallX][y].room = houseData[hallX + 1][y].room = 0;
            }
        }
        else
        {
            //It is vertical.
            int hallX = Random.Range(3, houseX - 5);
            for (int hallY = 0; hallY < houseY; hallY++)
                houseData[hallX][hallY].room = houseData[hallX + 1][hallY].room = 0;

            //Now generate two more hallways branching off the main one.
            for (int i = 0; i < 2; i++)
            {
                int hallY = Random.Range(3, houseY - 5);
                if (i == 0)
                    for (int x = 0; x < hallX; x++)
                        houseData[x][hallY].room = houseData[x][hallY + 1].room = 0;
                else
                    for (int x = hallX; x < houseX; x++)
                        houseData[x][hallY].room = houseData[x][hallY + 1].room = 0;
            }

        }

        //Hallways are now complete, perform a flood fill algorithm in each corner.
        //(The hallways will NEVER be up against a corner of the house, so we can safely just spawn in whatever corner is desired.
        //1 - Living room
        //2 - Kitchen
        //3 - Bedroom
        //4 - Bathroom
        List<int> roomsAvailable = new List<int>() { 1, 2, 3, 4 };

        for (int i = 0; i < 4; i++)
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

        //SECOND STAGE OF GENERATION: EDGE DETECTION/WALL PLACING.

        //Before we place any walls, we need to scan horizontally and vertically for splits/edges between boundaries/rooms.
        List<List<Vector2Int>> northEdges = getEdges(false, false);
        List<List<Vector2Int>> eastEdges = getEdges(false, true);
        List<List<Vector2Int>> southEdges = getEdges(true, false);
        List<List<Vector2Int>> westEdges = getEdges(true, true);

        bool isVerticalDoors = (Random.Range(0.0f, 1.0f) <= 0.5f);

        //With a list of edges and splits, we can generate along them.
        foreach (List<Vector2Int> edge in northEdges)
        {

            //See if we shall generate a window here?
            if (edge[0].y == 0 && edge.Count > 2 && Random.Range(0.0f, 1.0f) <= 0.6667f)
            {
                //Get a starting and ending position for the window.
                int start = 1;
                int end = edge.Count - 2;

                if (start == end)
                {
                    Vector2Int tilePos = edge[start];
                    houseData[tilePos.x][tilePos.y].north = 3;
                }
                else
                {
                    for (int i = start; i <= end; i++)
                    {
                        Vector2Int tilePos = edge[i];
                        int windowType = 5;
                        if (i == start)
                            windowType = 6;
                        else if (i == end)
                            windowType = 4;
                        houseData[tilePos.x][tilePos.y].north = windowType;
                    }
                }
            }
            else if (edge[0].y > 0 && (!isVerticalDoors || Random.Range(0.0f, 1.0f) <= 0.5f))
            {
                Vector2Int doorPos = edge[Random.Range(0, edge.Count - 1)];
                houseData[doorPos.x][doorPos.y].north = houseData[doorPos.x][doorPos.y - 1].south = 2;
                houseData[doorPos.x][doorPos.y].isOccupied = houseData[doorPos.x][doorPos.y - 1].isOccupied = true;
            }

            //Fill in the rest with just normal walls.
            foreach (Vector2Int tilePos in edge)
                if (houseData[tilePos.x][tilePos.y].north == -1)
                    houseData[tilePos.x][tilePos.y].north = 0;

            //Now add this wall to the appropriate room region.
            Rooms[houseData[edge[0].x][edge[0].y].room].northWalls.Add(edge);

        }

        foreach (List<Vector2Int> edge in eastEdges)
        {

            //See if we shall generate a window here?
            if (edge[0].x == 0 && edge.Count > 2 && Random.Range(0.0f, 1.0f) <= 0.6667f)
            {
                //Get a starting and ending position for the window.
                int start = 1;
                int end = edge.Count - 2;

                if (start == end)
                {
                    Vector2Int tilePos = edge[start];
                    houseData[tilePos.x][tilePos.y].east = 3;
                }
                else
                {
                    for (int i = start; i <= end; i++)
                    {
                        Vector2Int tilePos = edge[i];
                        int windowType = 5;
                        if (i == start)
                            windowType = 4;
                        else if (i == end)
                            windowType = 6;
                        houseData[tilePos.x][tilePos.y].east = windowType;
                    }
                }
            }
            else if (edge[0].x > 0 && (isVerticalDoors || Random.Range(0.0f, 1.0f) <= 0.5f))
            {
                Vector2Int doorPos = edge[Random.Range(0,edge.Count - 1)];
                houseData[doorPos.x][doorPos.y].east = houseData[doorPos.x - 1][doorPos.y].west = 2;
                houseData[doorPos.x][doorPos.y].isOccupied = houseData[doorPos.x - 1][doorPos.y].isOccupied = true;
            }

            //Fill in the rest with just normal walls.
            foreach (Vector2Int tilePos in edge)
                if (houseData[tilePos.x][tilePos.y].east == -1)
                    houseData[tilePos.x][tilePos.y].east = 0;

            //Now add this wall to the appropriate room region.
            Rooms[houseData[edge[0].x][edge[0].y].room].eastWalls.Add(edge);
        }

        foreach (List<Vector2Int> edge in southEdges)
        {


            //See if we shall generate a window here?
            if (edge[0].y == houseY - 1 && edge.Count > 2 && Random.Range(0.0f, 1.0f) <= 0.6667f)
            {
                //Get a starting and ending position for the window.
                int start = 1;
                int end = edge.Count - 2;

                if (start == end)
                {
                    Vector2Int tilePos = edge[start];
                    houseData[tilePos.x][tilePos.y].south = 3;
                }
                else
                {
                    for (int i = start; i <= end; i++)
                    {
                        Vector2Int tilePos = edge[i];
                        int windowType = 5;
                        if (i == start)
                            windowType = 4;
                        else if (i == end)
                            windowType = 6;
                        houseData[tilePos.x][tilePos.y].south = windowType;
                    }
                }
            }

            //Fill in the rest with just normal walls.
            foreach (Vector2Int tilePos in edge)
                if (houseData[tilePos.x][tilePos.y].south == -1)
                    houseData[tilePos.x][tilePos.y].south = 0;

            //Now add this wall to the appropriate room region.
            Rooms[houseData[edge[0].x][edge[0].y].room].southWalls.Add(edge);
        }

        foreach (List<Vector2Int> edge in westEdges)
        {

            //See if we shall generate a window here?
            if (edge[0].x == houseX - 1 && edge.Count > 2 && Random.Range(0.0f, 1.0f) <= 0.6667f)
            {
                //Get a starting and ending position for the window.
                int start = 1;
                int end = edge.Count - 2;

                if (start == end)
                {
                    Vector2Int tilePos = edge[start];
                    houseData[tilePos.x][tilePos.y].west = 3;
                }
                else
                {
                    for (int i = start; i <= end; i++)
                    {
                        Vector2Int tilePos = edge[i];
                        int windowType = 5;
                        if (i == start)
                            windowType = 6;
                        else if (i == end)
                            windowType = 4;
                        houseData[tilePos.x][tilePos.y].west = windowType;
                    }
                }
            }

            //Fill in the rest with just normal walls.
            foreach (Vector2Int tilePos in edge)
                if (houseData[tilePos.x][tilePos.y].west == -1)
                    houseData[tilePos.x][tilePos.y].west = 0;

            //Now add this wall to the appropriate room region.
            Rooms[houseData[edge[0].x][edge[0].y].room].westWalls.Add(edge);
        }

        //Now luckily for inner corners, two walls can meet together via two edges.
        //However, the outer corner is not met at all, so must be manually filled in.
        //Thankfully it is just a single tile, so we can just brute-force a scan.
        //Also outer corners can never be at the boundaries of the house, so we can eliminate two rows/columns from being tested.
        for(int x = 1; x < houseX - 1; x++)
        {
            for(int y = 1; y < houseY - 1; y++)
            {

                TileData tileData = houseData[x][y];
                if (houseData[x + 1][y].north != -1 && houseData[x][y - 1].west != -1)
                    tileData.north = 1;

                if (houseData[x - 1][y].north != -1 && houseData[x][y - 1].east != -1)
                    tileData.east = 1;

                if (houseData[x - 1][y].south != -1 && houseData[x][y + 1].east != -1)
                    tileData.south = 1;

                if (houseData[x + 1][y].south != -1 && houseData[x][y + 1].west != -1)
                    tileData.west = 1;

            }
        }

        //THIRD STAGE OF GENERATION:: FURNITURE/DECORATION.
        for (int r = 0; r < Rooms.Count; r++)
            for (int i = 0; i < 3; i++)
                placeFurniture(Rooms[r], debugFurniture);

    }

    private List<List<Vector2Int>> getEdges(bool direction, bool isVertical)
    {

        List<List<Vector2Int>> Edges = new List<List<Vector2Int>>();
        List<Vector2Int> Edge = new List<Vector2Int>();

        if (!isVertical)
        {
            for (int y = 0; y < houseY; y++)
            {

                //Get the identifier for which room type we are edge sampling.
                int targetType = houseData[0][y].room;

                for (int x = 0; x < houseX; x++)
                {

                    if (houseData[x][y].room != targetType)
                    {
                        //A new type of flooring is found, add our current edge and start afresh.
                        if (Edge.Count > 0) 
                            Edges.Add(Edge);
                        Edge = new List<Vector2Int>();
                        targetType = houseData[x][y].room;
                    }

                    //Now see if we can add this as an "edge"
                    if (y == (direction ? houseY - 1 : 0) || houseData[x][y + (direction ? 1 : -1)].room != targetType)
                    {
                        Edge.Add(new Vector2Int(x, y));
                    }
                    else
                    {
                        //A new type of flooring is found, add our current edge and start afresh.
                        if (Edge.Count > 0)
                            Edges.Add(Edge);
                        Edge = new List<Vector2Int>();
                        targetType = houseData[x][y].room;
                    }

                }

                //Moving onto the next row, clear out the buffer after adding what potentially is the last edge.
                if (Edge.Count > 0) 
                    Edges.Add(Edge);
                Edge = new List<Vector2Int>();

            }
        }
        else
        {
            for (int x = 0; x < houseX; x++)
            {

                //Get the identifier for which room type we are edge sampling.
                int targetType = houseData[x][0].room;

                for (int y = 0; y < houseY; y++)
                {

                    if (houseData[x][y].room != targetType)
                    {
                        //A new type of flooring is found, add our current edge and start afresh.
                        if (Edge.Count > 0) 
                            Edges.Add(Edge);
                        Edge = new List<Vector2Int>();
                        targetType = houseData[x][y].room;
                    }

                    //Now see if we can add this as an "edge"
                    if (x == (direction ? houseX - 1 : 0) || houseData[x + +(direction ? 1 : -1)][y].room != targetType)
                    {
                        Edge.Add(new Vector2Int(x, y));
                    }
                    else
                    {
                        //A new type of flooring is found, add our current edge and start afresh.
                        if (Edge.Count > 0)
                            Edges.Add(Edge);
                        Edge = new List<Vector2Int>();
                        targetType = houseData[x][y].room;
                    }

                }

                //Moving onto the next row, clear out the buffer after adding what potentially is the last edge.
                if(Edge.Count > 0)
                    Edges.Add(Edge);
                Edge = new List<Vector2Int>();

            }
        }

        return Edges;

    }

    private void fillHouseGap(int currX, int currY, int roomType)
    {
        int nextX = currX + 1;
        int nextY = currY;
        if (nextX >= 0 && nextX < houseX && nextY >= 0 && nextY < houseY)
        {
            if (houseData[nextX][nextY].room == -1)
            {
                houseData[nextX][nextY].room = roomType;
                fillHouseGap(nextX, nextY, roomType);
            }
        }

        nextX = currX;
        nextY = currY + 1;
        if (nextX >= 0 && nextX < houseX && nextY >= 0 && nextY < houseY)
        {
            if (houseData[nextX][nextY].room == -1)
            {
                houseData[nextX][nextY].room = roomType;
                fillHouseGap(nextX, nextY, roomType);
            }
        }

        nextX = currX - 1;
        nextY = currY;
        if (nextX >= 0 && nextX < houseX && nextY >= 0 && nextY < houseY)
        {
            if (houseData[nextX][nextY].room == -1)
            {
                houseData[nextX][nextY].room = roomType;
                fillHouseGap(nextX, nextY, roomType);
            }
        }

        nextX = currX;
        nextY = currY - 1;
        if (nextX >= 0 && nextX < houseX && nextY >= 0 && nextY < houseY)
        {
            if (houseData[nextX][nextY].room == -1)
            {
                houseData[nextX][nextY].room = roomType;
                fillHouseGap(nextX, nextY, roomType);
            }
        }
    }

    //This function places a tile.
    public void placeHouseTile(int x, int y, int room, int north, int east, int south, int west)
    {

        if (room == -1)
            return;

        Vector3 tilePos = new Vector3((float)x, 0.0f, (float)y);

        //Handle floor tile.
        GameObject tile = GameObject.Instantiate(floorTemplate, transform).gameObject;
        tile.transform.localPosition = tilePos;

        tile.transform.GetComponent<MeshRenderer>().material = roomTileMat[room];

        if (north != -1)
        {
            tile = GameObject.Instantiate(wallTemplates[north], transform).gameObject;
            tile.transform.localPosition = tilePos;
            tile.transform.localRotation = Quaternion.Euler(new Vector3(0.0f, -90.0f, 0.0f));

            MeshRenderer tileRender = tile.GetComponent<MeshRenderer>();
            Material[] wallMaterials = tileRender.materials;

            if (north != 2)
                wallMaterials[wallMaterials.Length - 1] = railMat[room];
            wallMaterials[wallMaterials.Length - 2] = wallMat[room];

            tileRender.materials = wallMaterials;

        }

        if (east != -1)
        {
            tile = GameObject.Instantiate(wallTemplates[east], transform).gameObject;
            tile.transform.localPosition = tilePos;
            tile.transform.localRotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, 0.0f));

            MeshRenderer tileRender = tile.GetComponent<MeshRenderer>();
            Material[] wallMaterials = tileRender.materials;

            if (east != 2)
                wallMaterials[wallMaterials.Length - 1] = railMat[room];
            wallMaterials[wallMaterials.Length - 2] = wallMat[room];

            tileRender.materials = wallMaterials;

        }

        if (south != -1)
        {
            tile = GameObject.Instantiate(wallTemplates[south], transform).gameObject;
            tile.transform.localPosition = tilePos;
            tile.transform.localRotation = Quaternion.Euler(new Vector3(0.0f, 90.0f, 0.0f));

            MeshRenderer tileRender = tile.GetComponent<MeshRenderer>();
            Material[] wallMaterials = tileRender.materials;

            if (south != 2)
                wallMaterials[wallMaterials.Length - 1] = railMat[room];
            wallMaterials[wallMaterials.Length - 2] = wallMat[room];

            tileRender.materials = wallMaterials;

        }

        if (west != -1)
        {
            tile = GameObject.Instantiate(wallTemplates[west], transform).gameObject;
            tile.transform.localPosition = tilePos;
            tile.transform.localRotation = Quaternion.Euler(new Vector3(0.0f, 180.0f, 0.0f));

            MeshRenderer tileRender = tile.GetComponent<MeshRenderer>();
            Material[] wallMaterials = tileRender.materials;

            if (west != 2)
                wallMaterials[wallMaterials.Length - 1] = railMat[room];
            wallMaterials[wallMaterials.Length - 2] = wallMat[room];

            tileRender.materials = wallMaterials;
        }
    }

    //This function will place a random furniture given a room's boundaries walls.
    private void placeFurniture(RoomData room, GameObject furniture)
    {

        if (room.northWalls.Count < 1)
            return; //There is no available room if a single wall have no length? Do not proceed to place furniture.

        //Get the furniture's properties.
        FurnitureDataScript furnitureData = furniture.GetComponent<FurnitureDataScript>();
        if (furnitureData == null)
            return; //Do not proceed to place furniture if no furniture data is available.

        //These will be our randomized range to generate the furniture's upcoming position.
        int minX = -1, maxX = -1;
        int minY = -1, maxY = -1;

        int rotation = Random.Range(0,3); //How is the furniture rotated?
        //0 - 0 degrees (North)
        //1 - 90 degrees (East)
        //2 - 180 degrees (South)
        //3 - 270 degrees (West)

        if (furnitureData.snapToEdge)
        {
            //This piece of furniture must be aligned against the wall, so pick a wall based on rotation that been picked.
            List<Vector2Int> edgeWall = new List<Vector2Int>();

            if (rotation == 0)
                edgeWall = room.northWalls[Random.Range(0, room.northWalls.Count - 1)];
            else if (rotation == 1)
                edgeWall = room.eastWalls[Random.Range(0, room.eastWalls.Count - 1)];
            else if (rotation == 2)
                edgeWall = room.southWalls[Random.Range(0, room.southWalls.Count - 1)];
            else if (rotation == 3)
                edgeWall = room.westWalls[Random.Range(0, room.westWalls.Count - 1)];

            minX = Mathf.Min(edgeWall[0].x, edgeWall[edgeWall.Count - 1].x);
            maxX = Mathf.Max(edgeWall[0].x, edgeWall[edgeWall.Count - 1].x);

            minY = Mathf.Min(edgeWall[0].y, edgeWall[edgeWall.Count - 1].y);
            maxY = Mathf.Max(edgeWall[0].y, edgeWall[edgeWall.Count - 1].y);

        }
        else
        {
            //This piece of furniture is free to be place anywhere, we just need to get two walls to generate a position inbetween them.
            //Pick a random north wall, (Picking north and south is the same as picking east and west.)
            List<Vector2Int> northWall = room.northWalls[Random.Range(0, room.northWalls.Count - 1)];

            //Sample the minX, maxX and minY from this chosen north wall.
            minX = northWall[0].x;
            maxX = northWall[northWall.Count - 1].x;
            minY = northWall[0].y;

            //Now pick a random south wall that is further away from the minY at least, and sample whatever is the smallest of maxX and largest of minX.
            while (true)
            {

                List<Vector2Int> potentialSouth = room.southWalls[Random.Range(0, room.southWalls.Count - 1)];

                if(potentialSouth[0].y > minY)
                {
                    //We found one! sample further!
                    maxY = potentialSouth[0].y;
                    minX = Mathf.Max(minX, potentialSouth[0].x);
                    maxX = Mathf.Min(maxX, potentialSouth[potentialSouth.Count - 1].x);
                    break; //Exit out of the loop.
                }

            }

        }


        //Get boundary of the furniture and change it based on rotation.
        Vector2Int furnitureBoundary = furnitureData.boundary + new Vector2Int(-1, -1);
        if (rotation == 1)
            furnitureBoundary = new Vector2Int(furnitureBoundary.y, -furnitureBoundary.x);
        else if(rotation == 2)
            furnitureBoundary = new Vector2Int(-furnitureBoundary.x,-furnitureBoundary.y);
        else if(rotation == 3)
            furnitureBoundary = new Vector2Int(-furnitureBoundary.y, furnitureBoundary.x);

        //Attempt to place the furniture.
        bool canPlace = true;
        Vector2Int furniturePos = new Vector2Int(Random.Range(minX, maxX), Random.Range(minY, maxY));
        int startX = Mathf.Min(furniturePos.x, furniturePos.x + furnitureBoundary.x);
        int startY = Mathf.Min(furniturePos.y, furniturePos.y + furnitureBoundary.y);

        int endX = Mathf.Max(furniturePos.x, furniturePos.x + furnitureBoundary.x);
        int endY = Mathf.Max(furniturePos.y, furniturePos.y + furnitureBoundary.y);

        for(int x = startX; x <= endX; x++)
        {
            for(int y = startY; y <= endY; y++)
            {
                if (!withinRoom(x, y, room.room))
                {
                    canPlace = false;
                    break;
                }
            }
        }

        if (canPlace)
        {
            GameObject newFurniture = GameObject.Instantiate(furniture, transform);
            newFurniture.transform.localPosition = new Vector3((float)furniturePos.x, 0.0f, (float)furniturePos.y);
            newFurniture.transform.localRotation = Quaternion.Euler(0.0f, (float)rotation * 90.0f, 0.0f);
            newFurniture.SetActive(true);

            //Update the tiles to say it is now occupied by a furniture
            for (int x = startX; x <= endX; x++)
                for (int y = startY; y <= endY; y++)
                    houseData[x][y].isOccupied = true;
        }

    }

    //This will check if a index position is within a room.
    //This is to do the boundary checking for a furniture.
    private bool withinRoom(int x, int y, int room)
    {
        if (x < 0 || x >= houseX || y < 0 || y >= houseY)
            return false; //Out of bounds, return false immediately.

        if (houseData[x][y].isOccupied)
            return false; //This area of space is occupied by another furniture or possibly a doorway.

        return (houseData[x][y].room == room); //Return if it is identical room.
    }

    //Network code for sending the house layout.

}
