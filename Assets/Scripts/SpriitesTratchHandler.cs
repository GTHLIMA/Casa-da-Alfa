using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriitesTratchHandler : MonoBehaviour
{
    //Flag to check aspect ratio of sprite
    public bool isAspectRatio;


    void Start()
    {
        var topRightCorner = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
        var worldSpaceWidth = topRightCorner.x * 2;
        var worldSpaceHeight = topRightCorner.y * 2;

        var spriteSize = GetComponent<SpriteRenderer>().bounds.size;

        var ScaleFactorX = worldSpaceWidth / spriteSize.x;
        var ScaleFactorY = worldSpaceHeight / spriteSize.y;

        if (isAspectRatio)
        {
            if (ScaleFactorX > ScaleFactorY) ScaleFactorY = ScaleFactorX;

            else ScaleFactorX = ScaleFactorY;

            transform.localScale = new Vector3(ScaleFactorX, ScaleFactorY, 1f);
        }
    }

}
