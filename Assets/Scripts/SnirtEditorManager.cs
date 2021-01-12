﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;

public class SnirtEditorManager : MonoBehaviour
{
    public string snirtName;
    public string[] randomName;

    [Header("Snirt Game Object Parts")]
    public GameObject[] PartGameObjects;

    [Header("Part Lists")]
    public PartListSO[] partLists;

    private int[] activeParts = new int[4];

    [Header("UI Elements")]
    public TMP_InputField nameInput;
    public ModPart[] PartDropdowns;
    public ModColor[] ColorSliders;
    public ScaleUIToChildren SavedSnirtsMenu;

    [Header("Required Prefabs")]
    public GameObject SaveFileUI;

    [Header("TESTING")]
    public Action lastAction;

    [System.Serializable]
    public struct Action
    {
        public ActionType type;
        public int PartModified;
        public int PartChangedTo;
        public Color ColorChangedTo;

        public enum ActionType { PART, COLOR/*, NAME*/ };
    }

    private void Awake()
    {
        // Set part buttons to have the proper sprites and function.
        for (int i = 0; i < partLists.Length; i++)
        {
            for (int j = 0; j < partLists[i].Parts.Length; j++)
            {
                PartDropdowns[i].AddUIButton(partLists[i], j);
            }
        }

        LoadFile();
        LoadSnirt(0);
    }

    #region Save/Load
    public void LoadFile()
    {
        // Load from file. 
        SnirtSaveLoader.LoadFile();

        PopulateSaveUI();
    }

    public void LoadSnirt(int index)
    {
        string snirtData = SnirtSaveLoader.savedSnirts[index];

        // Use the array created by the inital loading of the file.
        // Set all values to the snirt at the index and update all ui.
        string[] snirtTraits = snirtData.Split(',');

        ChangeName(snirtTraits[0]);

        for (int i = 0; i < activeParts.Length; i++)
        {
            if (int.TryParse(snirtTraits[i + 1], out int part))
            {
                ChangePart(part, i);
            }
            else
            {
                ChangePart(0, i);
            }

        }

        for (int i = 0; i < PartGameObjects.Length; i++)
        {
            ChangeColorViaHex(snirtTraits[i + 5], i);
        }
    }

    public void DeleteSnirt(int index)
    {
        // Just a wrapper function.
        SnirtSaveLoader.DeleteSnirt(index);

        // Reload UI.
        PopulateSaveUI();
    }

    public void SaveSnirt()
    {   
        // Make a string out of all the snirt properties.
        string snirtTraits = snirtName.Replace(',', ' '); // Just in case there are any commas, replace them with spaces.

        if (snirtTraits == "")
        {
            snirtTraits = "Unnamed";
        }

        for (int i = 0; i < activeParts.Length; i++)
        {
            snirtTraits += "," + activeParts[i].ToString();
        }

        for (int i = 0; i < PartGameObjects.Length; i++)
        {
            if (PartGameObjects[i].TryGetComponent(out MeshRenderer meshR))
            {
                snirtTraits += "," + ColorUtility.ToHtmlStringRGB(meshR.sharedMaterial.GetColor("_BaseColor"));
            }
            else
            {
                if (PartGameObjects[i].TryGetComponent(out SkinnedMeshRenderer skinnedMeshR))
                {
                    snirtTraits += "," + ColorUtility.ToHtmlStringRGB(skinnedMeshR.sharedMaterial.GetColor("_BaseColor"));
                }
            }
        }

        SnirtSaveLoader.SaveSnirt(snirtTraits);

        PopulateSaveUI();
    }

    private void PopulateSaveUI()
    {
        SavedSnirtsMenu.ClearChildren();

        // Create Save UI as many times as there are lines.
        for (int i = 0; i < SnirtSaveLoader.savedSnirts.Count; i++)
        {
            CreateSaveUI(SnirtSaveLoader.savedSnirts[i], i);
        }
    }

    public void CreateSaveUI(string snirtData, int index)
    {
        // Create a new snirtSave UI Element
        GameObject newSaveSlot = Instantiate(SaveFileUI, SavedSnirtsMenu.gameObject.transform);

        // Child the object to the Saved Snirts window.
        SavedSnirtsMenu.AddChild(newSaveSlot);

        // Update its UI with the proper values.
        string[] snirtTraits = snirtData.Split(',');

        List<Color> snirtColors = new List<Color>();

        for (int i = 0; i < PartGameObjects.Length; i++)
        {
            ColorUtility.TryParseHtmlString("#" + snirtTraits[i + 5], out Color newCol);
            snirtColors.Add(newCol);
        }

        newSaveSlot.GetComponent<SavedSnirtUI>().UpdateUI(snirtTraits[0], index, snirtColors.ToArray(), this);
    }
    #endregion

    #region Name
    public void ChangeName(string newName)
    {
        snirtName = newName;
        nameInput.text = newName;
    }

    public void RandomizeName()
    {
        int randy = UnityEngine.Random.Range(0, randomName.Length);
        nameInput.text = randomName[randy];
    }
    #endregion

    #region Part
    public void ChangePart(int changeTo, int part)
    {
        activeParts[part] = changeTo;
        if (PartGameObjects[part].TryGetComponent(out MeshFilter meshF))
        {
            meshF.sharedMesh = partLists[part].Parts[activeParts[part]].partMesh;
        }

        PartDropdowns[part].UpdateUI(activeParts[part], partLists[part].Parts[activeParts[part]].partName);
        ChangeLastAction(Action.ActionType.PART, part, changeTo, Color.clear);
    }
    #endregion

    #region Color
    public void ChangeColor(Color changeTo, int part)
    {
        if (PartGameObjects[part].TryGetComponent(out MeshRenderer meshR))
        {
            meshR.sharedMaterial.SetColor("_BaseColor", changeTo);
        }
        else
        {
            if (PartGameObjects[part].TryGetComponent(out SkinnedMeshRenderer skinnedMeshR))
            {
                skinnedMeshR.sharedMaterial.SetColor("_BaseColor", changeTo);
            }
        }

        ColorSliders[part].UpdateUI(changeTo);
        ChangeLastAction(Action.ActionType.COLOR, part, -1, changeTo);
    }

    public void ChangeColorViaHex(string changeTo, int part)
    {
        bool success = ColorUtility.TryParseHtmlString("#" + changeTo, out Color newCol);
        if (success)
        {
            ChangeColor(newCol, part);
        }
    }
    #endregion

    #region UndoRedo
    public void ChangeLastAction(Action.ActionType type, int part, int changedPart, Color changedColor)
    {
        lastAction.type = type;
        lastAction.PartModified = part;
        lastAction.PartChangedTo = changedPart;
        lastAction.ColorChangedTo = changedColor;
    }

    #endregion
}
