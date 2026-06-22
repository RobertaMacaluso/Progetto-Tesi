//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//[System.Serializable]
//public class StorageContainer
//{
//    //[PrimaryKey, AutoIncrement]
//    public int id;
//    public string name;
//    public string worldTransform;
//    public int parentShelfId;
//    public bool isShelf = false;


//    // Start is called before the first frame update
//    void Start()
//    {

//    }

//    // Update is called once per frame
//    void Update()
//    {

//    }

//    public void SetIsShelf(bool value)
//    {
//        isShelf = value;
//    }

//    public bool GetIsShelf()
//        { return isShelf; }
//}
using System;

[Serializable]
public class StorageContainer
{
    public int id;
    public string name;
    public string worldTransform;

    public int parentShelfId;

    public bool isShelf = false;

    // indica se è una stanza (con marker)
    public bool isRoom = false;

    // riferimento al marker ArUco
    public int markerId;

    // dimensioni stanza per collider
    public float roomWidth;
    public float roomHeight;
    public float roomDepth;

    // coordinate centro stanza
    public string roomCenterPose;

    public void SetIsShelf(bool value)
    {
        isShelf = value;
    }

    public bool GetIsShelf()
    {
        return isShelf;
    }

    public void SetIsRoom(bool value)
    {
        isRoom = value;
    }

    public bool GetIsRoom()
    {
        return isRoom;
    }
}
