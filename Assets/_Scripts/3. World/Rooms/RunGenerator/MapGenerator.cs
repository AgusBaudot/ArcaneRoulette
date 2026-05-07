using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    private int[] floorPlan; //Grid
    public int[] getFloorPlan => floorPlan;

    private int floorPlanCount; //Cuantas rooms spawnearon
    private int minRooms;
    private int maxRooms;
    private List<int> endRooms; //Dead End Room

    private int bossRoomIndex;
    private int secretRoomIndex;
    private int shopRoomIndex;
    private int itemRoomIndex;

    public Cell cellPrefab;
    private float cellSize; //separacion entre cells
    private Queue<int> cellQueue; //
    private List<Cell> spawnedCells; //Guardar las habitaciones de la escena

    public List<Cell> getSpawnedCells => spawnedCells;

    private List<int> bigRoomIndexes;


    [SerializeField] private Material item;
    [SerializeField] private Material shop;
    [SerializeField] private Material boss;
    [SerializeField] private Material secret;

    public void Start()
    {
        minRooms = 15;
        maxRooms = 20;
        cellSize = 2f;
        spawnedCells = new List<Cell>();

        SetupDungeon();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) 
        {
            SetupDungeon();
        }
    }
    private void SetupDungeon() 
    {
        for (int i = 0; i < spawnedCells.Count; i++) 
        {
            Destroy(spawnedCells[i].gameObject);
        }

        spawnedCells.Clear();

        floorPlan = new int[100];
        floorPlanCount = default;
        cellQueue = new Queue<int>();
        endRooms = new List<int>();

        VisitCell(45); // Centro del mapa

        GenerateDungeon();
    }
    private void GenerateDungeon() 
    {
        while (cellQueue.Count > 0) 
        {
            int index = cellQueue.Dequeue();
            int x = index % 10;

            bool created = false;

            if (x > 1) created |= VisitCell(index - 1);
            if (x < 9) created |= VisitCell(index + 1);
            if (index > 20) created |= VisitCell(index - 10);
            if (index < 70) created |= VisitCell(index + 10);

            if (created == false)
                endRooms.Add(index);
        }

        if (floorPlanCount < minRooms) 
        {
            SetupDungeon();
            return;
        }

        SetupSpecialRooms();
    }
    private void SetupSpecialRooms() 
    {
        bossRoomIndex = endRooms.Count > 0 ? endRooms[endRooms.Count - 1] : -1;

        if(bossRoomIndex != -1) 
        {
            endRooms.RemoveAt(endRooms.Count - 1);
        }

        itemRoomIndex = RandomEndRoom();
        shopRoomIndex = RandomEndRoom();
        secretRoomIndex = PickSecretRoom();

        if(itemRoomIndex == -1 || shopRoomIndex == -1 || bossRoomIndex == -1 || secretRoomIndex == -1) 
        {
            SetupDungeon();
            return;
        }

        SpawnRoom(secretRoomIndex);

        UpdateSpecialRoomVisual();
    }
    private void UpdateSpecialRoomVisual() 
    {
        foreach(var cell in spawnedCells) 
        {
            if (cell.index == itemRoomIndex) 
            {
                cell.SetSpecialRoomMaterial(item);
            }
            if(cell.index == shopRoomIndex)
            {
                cell.SetSpecialRoomMaterial(shop);
            }
            if (cell.index == bossRoomIndex) 
            {
                cell.SetSpecialRoomMaterial(boss);
            }
            if (cell.index == secretRoomIndex) 
            {
                cell.SetSpecialRoomMaterial(secret);
            }
        }
    }

    private int RandomEndRoom() 
    {
        if (endRooms.Count == 0)
            return -1;

        int randomRoom = Random.Range(0, endRooms.Count);
        int index = endRooms[randomRoom];

        endRooms.RemoveAt(randomRoom);

        return index;
    }

    private int PickSecretRoom() 
    {
        for (int attempt = 0; attempt < 900; attempt++)
        {
            int x = Mathf.FloorToInt(Random.Range(0f, 1f) * 9) + 1;
            int y = Mathf.FloorToInt(Random.Range(0f, 1f) * 8) + 2;

            int index = y * 10 + x;

            if (floorPlan[index] != 0)
            {
                continue;
            }

            if (bossRoomIndex == index - 1 || bossRoomIndex == index + 1 || bossRoomIndex == index + 10 || bossRoomIndex == index - 10)
            {
                continue;
            }

            if (index - 1 < 0 || index + 1 > floorPlan.Length || index - 10 < 0 || index + 10 > floorPlan.Length)
            {
                continue;
            }

            int neighbours = GetNeighbourCount(index);

            if (neighbours >= 3 || (attempt > 300 && neighbours >= 2) || (attempt > 600 && neighbours >= 1))
            {
                return index;
            }
        }

        return -1;
    }

    private int GetNeighbourCount(int index) 
    {
        return floorPlan[index - 10] + floorPlan[index - 1] + floorPlan[index + 10] + floorPlan[index + 1]; // Result between 0 - 4
    }

    private bool VisitCell (int index) 
    {
        if (floorPlan[index] != 0 || GetNeighbourCount(index) > 1 || floorPlanCount > maxRooms || Random.value < 0.5f)
            return false;

        cellQueue.Enqueue(index);
        floorPlan[index] = 1;
        floorPlanCount++;

        SpawnRoom(index);

        return true;
    }

    private void SpawnRoom(int index) 
    {
        int x = index % 10;
        int z = index / 10;

        Vector3 position = new Vector3(x * cellSize, 0, -z * cellSize);
        
        Cell newCell = Instantiate(cellPrefab, position, Quaternion.identity);
        newCell.value = 1;
        newCell.index = index;

        spawnedCells.Add(newCell);
    }
}
