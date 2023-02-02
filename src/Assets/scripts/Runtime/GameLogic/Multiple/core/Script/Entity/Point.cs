using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Point : Entity
{
    public enum Status {
        Normal,
        Brick,
        PathPoint,
        Ban
    }
    public int x, y;
    Status status;

    public void SetStatus(Status val)
    {
        status = val;
    }
}
