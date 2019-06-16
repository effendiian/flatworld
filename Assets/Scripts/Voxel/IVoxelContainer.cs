using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IVoxelContainer {
    IVoxel GetVoxel(Vector3Int pos);
    void AddVoxel(Vector3Int pos);
    void RemoveVoxel(Vector3Int pos);
}
