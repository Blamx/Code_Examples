using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;

using Sirenix.OdinInspector;

public class TileMapUtility : MonoBehaviour
{

    [SerializeField]
    TileObjectList tileObjectList;

    [SerializeField]
    GameObject replacementObject;

    [SerializeField]
    Transform ObjectParent;

    [SerializeField]
    float tileSize = 1;

    Dictionary<Vector3Int, GameObject> tilelist;

    public static TileMapUtility instance;

    private void Awake()
    {
        instance = this;
    }

    [Button]
    void Generate()
    {
        tilelist = new Dictionary<Vector3Int, GameObject>();

        Tilemap map = GetComponent<Tilemap>();

        float tileOffset = tileSize * 0.5f;

        GameObject objParent = GameObject.Instantiate<GameObject>(new GameObject(), ObjectParent);

        foreach (var pos in map.cellBounds.allPositionsWithin)
        {
            Vector3Int gridPlace = new Vector3Int(pos.x, pos.y, pos.z);
            TileBase tile = map.GetTile(gridPlace);
            if (tile == null)
                continue;

            if (!tileObjectList.TileObjects.ContainsKey(tile))
                continue;

            GameObject temp = Instantiate<GameObject>(tileObjectList.TileObjects[tile], new Vector3(pos.x + tileOffset, pos.y + tileOffset, pos.z), Quaternion.identity, objParent.transform);


            tilelist.Add(pos, temp);
        }

        objParent.AddComponent<CompositeCollider2D>();
        Rigidbody2D rigidbody2D = objParent.GetComponent<Rigidbody2D>();
        rigidbody2D.useAutoMass = true;
        objParent.AddComponent<ConnectedParent>();
        objParent.tag = "Platform";

        foreach (var pos in map.cellBounds.allPositionsWithin)
        {
            Vector3Int gridPlace = new Vector3Int(pos.x, pos.y, pos.z);
            TileBase tile = map.GetTile(gridPlace);
            if (tile == null)
                continue;

            if (!tileObjectList.TileObjects.ContainsKey(tile))
                continue;

            try
            {

                TileBase tileAbove = map.GetTile(new Vector3Int(pos.x, pos.y + 1, pos.z));

                if (tileAbove != null)
                {
                    Vector3Int otherGridPlace = new Vector3Int(pos.x, pos.y + 1, pos.z);
                    tilelist[gridPlace].GetComponent<ConnectedTiles>().TileAbove = tilelist[otherGridPlace].gameObject;
                    tilelist[otherGridPlace].GetComponent<ConnectedTiles>().TileBelow = tilelist[gridPlace].gameObject;
                }
                TileBase tileBelow = map.GetTile(new Vector3Int(pos.x, pos.y - 1, pos.z));

                if (tileBelow != null)
                {
                    Vector3Int otherGridPlace = new Vector3Int(pos.x, pos.y - 1, pos.z);
                    tilelist[gridPlace].GetComponent<ConnectedTiles>().TileBelow = tilelist[otherGridPlace].gameObject;
                    tilelist[otherGridPlace].GetComponent<ConnectedTiles>().TileAbove = tilelist[gridPlace].gameObject;
                }

                TileBase tileRight = map.GetTile(new Vector3Int(pos.x + 1, pos.y, pos.z));

                if (tileRight != null)
                {
                    Vector3Int otherGridPlace = new Vector3Int(pos.x + 1, pos.y, pos.z);
                    tilelist[gridPlace].GetComponent<ConnectedTiles>().TileRight = tilelist[otherGridPlace].gameObject;
                    tilelist[otherGridPlace].GetComponent<ConnectedTiles>().TileLeft = tilelist[gridPlace].gameObject;
                }

                TileBase tileLeft = map.GetTile(new Vector3Int(pos.x - 1, pos.y, pos.z));

                if (tileLeft != null)
                {
                    Vector3Int otherGridPlace = new Vector3Int(pos.x - 1, pos.y, pos.z);
                    tilelist[gridPlace].GetComponent<ConnectedTiles>().TileLeft = tilelist[otherGridPlace].gameObject;
                    tilelist[otherGridPlace].GetComponent<ConnectedTiles>().TileRight = tilelist[gridPlace].gameObject;
                }


            }
            catch (System.Exception)
            {

                Debug.Log("Null ref");
            }

            map.SetTile(gridPlace, null);
            //above
        }

        tilelist = new Dictionary<Vector3Int, GameObject>();
    }

    public void createBranch(List<GameObject> objs)
    {
        GameObject objParent = new GameObject();

        objParent.transform.parent = ObjectParent;
      
        foreach (var item in objs)
        {
            item.transform.parent = objParent.transform;
        }

        objParent.AddComponent<CompositeCollider2D>();
        Rigidbody2D rigidbody2D = objParent.GetComponent<Rigidbody2D>();
        rigidbody2D.useAutoMass = true;
        objParent.AddComponent<ConnectedParent>();
        objParent.tag = "Platform";
    }

}
