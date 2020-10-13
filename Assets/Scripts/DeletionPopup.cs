using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 0649
public class DeletionPopup : MonoBehaviour // Script for the prompt
{
    [SerializeField] private Text pathText;
    [SerializeField] private Text pathName;

    [HideInInspector] public FileListing popupForListing;

    public void OnClick_DeleteFile()
    {
        DeleteProjFiles.Instance.DeleteFile(popupForListing);
        OnClick_ClosePrompt();
        Destroy(gameObject);
    }

    public void OnClick_ClosePrompt()
    {
        Destroy(gameObject);
    }
}
#pragma warning restore 0649