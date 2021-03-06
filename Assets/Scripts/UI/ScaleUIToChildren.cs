﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleUIToChildren : MonoBehaviour
{
    public bool scaleHorizontally;
    public bool scaleVertically;
    public float horizontalPadding;
    public float verticalPadding;

    // Instead of just grabbing all children and their children we just want
    // a list of the children we want actively influencing our dimentions.
    public List<GameObject> children;

    private void Awake()
    {
        Resize();
    }

    public void Resize()
    {
        if (scaleVertically)
        {
            float totalNewHeight = 0f;

            foreach (GameObject child in children)
            {
                if (child.activeInHierarchy)
                {
                    RectTransform rt = child.transform.GetComponent<RectTransform>();
                    totalNewHeight += rt.sizeDelta.y * rt.localScale.y;
                }
            }

            totalNewHeight += verticalPadding * 2;

            GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalNewHeight);
        }

        if (scaleHorizontally)
        {
            float totalNewWidth = 0f;

            foreach (GameObject child in children)
            {
                if (child.activeInHierarchy)
                {
                    RectTransform rt = child.transform.GetComponent<RectTransform>();
                    totalNewWidth += rt.sizeDelta.x * rt.localScale.x;
                }
            }

            totalNewWidth += horizontalPadding * 2;

            GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalNewWidth);
        }
    }

    public void AddChild(GameObject newChild)
    {
        children.Add(newChild);

        Resize();
    }

    public void ClearChildren()
    {
        for (int i = 0; i < children.Count; i++)
        {
            Destroy(children[i]);
        }

        children.Clear();
    }
}
