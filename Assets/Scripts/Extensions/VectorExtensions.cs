using UnityEngine;

public static class VectorExtensions{

    public static Vector2 Horizontal3dTo2d(this Vector3 vec){
        return new Vector2(vec.x, vec.z);
    }

    public static Vector3 Horizontal2dTo3d(this Vector2 vec, float y = 0){
        return new Vector3(vec.x, y, vec.y);
    }

}