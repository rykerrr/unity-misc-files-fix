using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 0649
[System.Serializable]
public class Settings
{
    [Header("References")]
    public Canvas mainCanvas;
    public Text popupSetter;
    public Text showPathText;
    public Transform fileScrollContent;
    [Header("Prefabs")]
    public GameObject popupWindowPrefab;
    public FileListing fileListingPrefab;
}
#pragma warning restore 0649