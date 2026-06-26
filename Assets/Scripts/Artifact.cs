using System.Collections;
using System.Collections.Generic;
//using Unity.VisualScripting.Dependencies.Sqlite; //poi con SQLite4Unity3d ???
using UnityEngine;

[System.Serializable]
public class Artifact
{
    //[PrimaryKey, AutoIncrement]
    public int id;
    public string name;
    public string textDescription;
    public int shelvingUnit = -1;
    public int lastShelvingUnit = -1;
    public string containerLocalPose = "";
    public float artifactWidth;
    public float artifactHeight;
    public float artifactDepth;

    public override string ToString()
    {
        return string.Format("[Artifact: Id={0}, Name={1},  TextDescription={2},  ShelvingUnit={3}, LastShelvingUnit={4}, ContainerLocalPose={5}]", 
            id, name, textDescription, shelvingUnit.ToString(), lastShelvingUnit.ToString(), containerLocalPose);
    }

    public int GetShelfID()
        { return shelvingUnit; }

    public void SetShelfID(int id)
    { 
        shelvingUnit = id;
        lastShelvingUnit = id;
    }

    public void SetName(string nome)
    { this.name = nome; }
}
