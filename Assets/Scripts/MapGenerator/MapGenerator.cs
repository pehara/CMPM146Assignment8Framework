using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEditor.Experimental.GraphView;
using System.Linq;
using UnityEngine.UI;
using System.Data.Common;
using NUnit.Framework;
using System;

public class MapGenerator : MonoBehaviour
{
    public List<Room> rooms;
    public Hallway vertical_hallway;
    public Hallway horizontal_hallway;
    public Room start;
    public Room target;

    // Constraint: How big should the dungeon be at most
    // this will limit the run time (~10 is a good value 
    // during development, later you'll want to set it to 
    // something a bit higher, like 25-30)
    public int MAX_SIZE;

    // set this to a high value when the generator works
    // for debugging it can be helpful to test with few rooms
    // and, say, a threshold of 100 iterations
    public int THRESHOLD;

    // keep the instantiated rooms and hallways here 
    private List<GameObject> generated_objects;
    
    int iterations;

    List<T> ShuffleList<T>(List<T> l)
    {
        List<int> orderlist = new();
        for (int i = 0; i < l.Count; i++)
        {
            orderlist.Add(i);
        }
        List<T> nl = new();
        for (int i = 0; i < l.Count; i++)
        {
            int r = UnityEngine.Random.Range(0, orderlist.Count);
            int ri = orderlist[r];
            orderlist.RemoveAt(r);
            nl.Add(l[ri]);
        }
        return nl;
    }

    List<Door> CleanMatchingDoors(List<Door> ld)
    {
        List<Door> result = new();
        foreach (Door d1 in ld)
        {
            bool matched = false;
            foreach (Door d2 in ld)
            {
                if (d1.IsMatching(d2))
                {
                    matched = true;
                    break;
                }
            }
            if (!matched)
            {
                result.Add(d1);
            }
        }
        return result;
    }

    bool CheckStateValidity(List<Door> ld, List<Vector2Int> occ)
    {
        //assume ld cleaned
        foreach (Door d in ld)
        {
            Vector2Int matching_pos = d.GetMatching().GetGridCoordinates();
            foreach (Vector2Int v in occ)
            {
                if (v == matching_pos)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public void Generate()
    {
        // dispose of game objects from previous generation process
        foreach (var go in generated_objects)
        {
            Destroy(go);
        }
        generated_objects.Clear();

        generated_objects.Add(start.Place(new Vector2Int(0, 0)));
        List<Door> doors = start.GetDoors();
        List<Vector2Int> occupied = new List<Vector2Int>();
        occupied.Add(new Vector2Int(0, 0));
        iterations = 0;
        rooms.Add(target);
        GenerateWithBacktracking(occupied, doors, 1);
    }


    bool GenerateWithBacktracking(List<Vector2Int> occupied, List<Door> doors, int depth, bool ended = false, int cl = 0, int cr = 0, int cu = 0, int cd = 0)
    {
        iterations++;
        if (iterations > THRESHOLD) throw new System.Exception("Iteration limit exceeded");
        if (depth > MAX_SIZE)
        {
            return false;
        }
        if (Math.Abs((cr - cl) - (cd - cu)) > 2)
        {
            return false;
        }
        if (doors.Count == 0)
        {
            if (depth > 5)
            {
                return true;
            }
            return false;
        }
        List<Door> shuffled_doors = ShuffleList<Door>(doors);
        List<Room> shuffled_rooms = ShuffleList<Room>(rooms);
        foreach (Door target_d in shuffled_doors)
        {
            Door.Direction matching_dir = target_d.GetMatchingDirection();
            foreach (Room rm in shuffled_rooms)
            {
                if (rm == target && ended)
                {
                    continue;
                }
                if (rm.HasDoorOnSide(matching_dir))
                {
                    //attemp to place and call backtracking
                    List<Vector2Int> occ_copy = new(occupied);
                    List<Door> d_copy = new(doors);
                    Vector2Int place_pos = target_d.GetMatching().GetGridCoordinates();
                    occ_copy.Add(place_pos);
                    foreach (Door d in rm.GetDoors(place_pos))
                    {
                        d_copy.Add(d);
                    }
                    List<Door> bt_copy = CleanMatchingDoors(d_copy);
                    if (CheckStateValidity(bt_copy, occ_copy))
                    {
                        cl = Math.Min(cl, place_pos.x);
                        cr = Math.Max(cr, place_pos.x);
                        cu = Math.Min(cu, place_pos.y);
                        cd = Math.Max(cd, place_pos.y);
                        bool suc = GenerateWithBacktracking(occ_copy, bt_copy, depth + 1, ended | rm == target, cl, cr, cu, cd);
                        if (suc)
                        {
                            rm.Place(place_pos);
                            foreach (Door rd in rm.GetDoors(place_pos))
                            {
                                if (rd.GetDirection() == Door.Direction.SOUTH)
                                {
                                    vertical_hallway.Place(rd);
                                }
                                else if (rd.GetDirection() == Door.Direction.WEST)
                                {
                                    horizontal_hallway.Place(rd);
                                }
                            }
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MAX_SIZE = 15;
        THRESHOLD = 10000;
        generated_objects = new List<GameObject>();
        Generate();
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.gKey.wasPressedThisFrame)
            Generate();
    }
}
