using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Sirenix.OdinInspector;

public class ConnectedTiles : MonoBehaviour
{
    [SerializeField]
    public GameObject TileAbove;
    [SerializeField]
    public GameObject TileBelow;
    [SerializeField]
    public GameObject TileRight;
    [SerializeField]
    public GameObject TileLeft;

    public List<GameObject> CurrentBranch;

    public List<GameObject> ExistingBranch;

    //find connected Tiles
    void FindBranches(GameObject obj, ConnectedTiles CurrentBranch)
    {
        ConnectedTiles tile = obj.GetComponent<ConnectedTiles>();

        if (CurrentBranch.ExistingBranch.Contains(obj))
            return;

        CurrentBranch.CurrentBranch.Add(obj);
        CurrentBranch.ExistingBranch.Add(obj);

        if (tile.TileAbove != null)
        {
            FindBranches(tile.TileAbove, CurrentBranch);
        }
        if (tile.TileBelow != null)
        {
            FindBranches(tile.TileBelow, CurrentBranch);
        }
        if (tile.TileLeft != null)
        {
            FindBranches(tile.TileLeft, CurrentBranch);
        }
        if (tile.TileRight != null)
        {
            FindBranches(tile.TileRight, CurrentBranch);
        }
    }

    private void OnDestroy()
    {
        if (TileAbove != null)
        {
            TileAbove.GetComponent<ConnectedTiles>().TileBelow = null;


            if (!ExistingBranch.Contains(TileAbove))
            {
                FindBranches(TileAbove, this);

                if (CurrentBranch.Count != 0)
                    TileMapUtility.instance.createBranch(CurrentBranch);
                CurrentBranch = new List<GameObject>();
            }

        }
        if (TileBelow != null)
        {
            TileBelow.GetComponent<ConnectedTiles>().TileAbove = null;

            if (!ExistingBranch.Contains(TileBelow))
            {
                FindBranches(TileBelow, this);

                if (CurrentBranch.Count != 0)
                    TileMapUtility.instance.createBranch(CurrentBranch);
                CurrentBranch = new List<GameObject>();
            }
        }
        if (TileLeft != null)
        {
            TileLeft.GetComponent<ConnectedTiles>().TileRight = null;

            if (!ExistingBranch.Contains(TileLeft))
            {
                FindBranches(TileLeft, this);

                if (CurrentBranch.Count != 0)
                    TileMapUtility.instance.createBranch(CurrentBranch);
                CurrentBranch = new List<GameObject>();
            }
        }
        if (TileRight != null)
        {

            TileRight.GetComponent<ConnectedTiles>().TileLeft = null;
            if (!ExistingBranch.Contains(TileRight))
            {
                FindBranches(TileRight, this);

                if (CurrentBranch.Count != 0)
                    TileMapUtility.instance.createBranch(CurrentBranch);
                CurrentBranch = new List<GameObject>();
            }
        }
    }
}
