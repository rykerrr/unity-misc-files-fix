using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// shift-click implementation

#pragma warning disable 0649
public class DeleteProjFiles : InheritableSingleton<DeleteProjFiles>
{
    // add a singleton

    [SerializeField] private Settings settings = new Settings();

    // button + multiselect
    // place it in projects folder, check if its in assets or project folder cause debil

    private List<FileListing> existingListings = new List<FileListing>();
    private List<FileListing> selectionList = new List<FileListing>();
    private FileListing curSelectedFileListing;

    private bool showFullPath = false;
    private bool forceDelete = true;

    private void Awake()
    {
        OnClick_SetForceDelete();

        CheckLocation();
        ClearSelectionList();
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.A)) // ctrl A, selects entire list
        {
            ClearSelectionList();

            foreach (FileListing listing in existingListings)
            {
                ForceSelect(listing, true);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space)) // Basically checks its location + searches for the files again
        {
            CheckLocation();
            ClearSelectionList();
        }

        if (Input.GetKeyDown(KeyCode.Delete)) // Same as clicking the "Delet this" button
        {
            OnClick_DeleteDis();
        }
    }

    private void CreateFileListing(string fullPath) // Creates a file listing and places it in the content, it now exists in existingListings
    {
        FileListing fileListingClone = Instantiate(settings.fileListingPrefab, settings.fileScrollContent) as FileListing;
        Button cloneButton = fileListingClone.GetComponent<Button>();
        Text cloneText = cloneButton.GetComponentInChildren<Text>();

        fileListingClone.FullPath = fullPath;
        fileListingClone.FileName = Path.GetFileName(fullPath);

        existingListings.Add(fileListingClone);

        if (showFullPath)
        {
            cloneText.text = fileListingClone.FullPath;
        }
        else
        {
            cloneText.text = fileListingClone.FileName;
        }


        cloneButton.onClick.AddListener(() => OnClick_SelectFile(fileListingClone));
        // You could probably do the above in the FileListing script as well, this just felt easier for me
    }

    private void CheckLocation() // Checks current location, tries to find the project folder
    {
        DirectoryInfo currentDirectory = new DirectoryInfo(Application.dataPath); // gives assets folder of this

        DirectoryInfo info = currentDirectory;

        while (info.Parent != null) // Climb up the file system tree until it "gets out", aka trying to access a Disk's parent
        {
            info = info.Parent;

            List<string> directories = Directory.GetDirectories(info.FullName, "*", SearchOption.TopDirectoryOnly).ToList();

            for (int i = 0; i < directories.Count; i++)
            {
                string name = Path.GetFileName(directories[i]);
                directories[i] = name;
            }

            bool isProjectFolder = directories.Find(x => x == "Assets") != null
                || directories.Find(x => x == "Library") != null || directories.Find(x => x == "Logs") != null
                || directories.Find(x => x == "Packages") != null || directories.Find(x => x == "ProjectSettings") != null
                || directories.Find(x => x == "Temp") != null; // these exist whenever a new project is made


            if (isProjectFolder) // If we found the project folder, epic success
            {
                List<string> filesToDelete = GetProjFileList(info.FullName);

                filesToDelete.ForEach(x => CreateFileListing(x));

                break;
            }
        }
    }

    private List<string> GetProjFileList(string path) // Finds all the required files that need to be deleted and returns them
    {
        List<string> returnFileList = new List<string>();

        List<string> fileList = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly).Where(x => !x.Contains(".meta")).ToList();

        for (int i = 0; i < fileList.Count; i++)
        {
            if (fileList[i].Contains(".sln") || fileList[i].Contains(".user") || fileList[i].Contains(".csproj"))
            {
                string filePath = fileList[i];
                returnFileList.Add(filePath);
            }
        }

        return returnFileList;
    }

    private void ForceSelect(FileListing listing, bool addToSelectedList = false, bool setAsSelected = true)
    {   // Used mainly in the script, while OnClick_SelectFile is used as the OnClick event for the listings...
        if (setAsSelected) // We don't always want the last one to be selected, such as in shift-click selection
        {
            curSelectedFileListing = listing;
            curSelectedFileListing.GetComponent<Image>().color = Color.green;
        }

        if (addToSelectedList) // You could probably do without this as in all method calls the 2nd
        {   // bool parameter is set to true
            selectionList.Add(listing);

            listing.GetComponent<Image>().color = Color.green;
        }
        else
        {
            ClearSelectionList();

            if (curSelectedFileListing != null)
            {
                curSelectedFileListing.GetComponent<Image>().color = Color.white;
            }

            if (curSelectedFileListing)
            {
                listing.GetComponent<Image>().color = Color.green;
            }
        }
    }

    private void ClearSelectionList() // Clears it and resets their color
    {
        if (selectionList.Count > 0)
        {
            foreach (FileListing listing in selectionList)
            {
                listing.GetComponent<Image>().color = Color.white;
            }

            selectionList.Clear();
        }
    }

    private void TryDeleteFile(FileListing listing)
    {
        // Prompt depending on forceDelete (set by UI button)

        if (forceDelete)
        {
            DeleteFile(listing);
        }
        else
        {
            CreatePromptInstance(listing);
        }
    }

    private void CreatePromptInstance(FileListing listing) // Creates an instance of the prompt, in case of multiple selection,
    {   // calling this multiple times is handled outside of this method
        DeletionPopup popupClone = Instantiate(settings.popupWindowPrefab, settings.mainCanvas.transform).GetComponent<DeletionPopup>();

        popupClone.popupForListing = listing;
        popupClone.transform.SetAsLastSibling();
    }

    public void DeleteFile(FileListing listing)
    {
        if (listing != null) // very rare cases
        {
            string path = listing.FullPath;

            Destroy(listing.gameObject);
            File.Delete(path);
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("Listing doesn't exist, perhaps it got deleted by something else beforehand?");
#endif
        }
    }

    public void OnClick_SetForceDelete()
    {
        forceDelete = !forceDelete;

        settings.popupSetter.text = "Force Delete: " + forceDelete + (forceDelete == true ? "(No prompt)" : "(Prompt will show)");

        if (forceDelete)
        {
            settings.popupSetter.color = Color.red;
        }
        else
        {
            settings.popupSetter.color = Color.green;
        }
    }

    public void OnClick_SelectFile(FileListing listing)
    {
        if (Input.GetKey(KeyCode.LeftControl)) // Clicking on a file while holding ctrl, adds it to selection unless it already is in
        { // otherwise deselects it
            if (selectionList.Contains(listing))
            {
                if (curSelectedFileListing)
                {
                    curSelectedFileListing.GetComponent<Image>().color = Color.white;

                    if (curSelectedFileListing.GetInstanceID() == listing.GetInstanceID())
                    {
                        curSelectedFileListing = null;
                    }
                }

                selectionList.Remove(listing);
            }
            else
            {
                curSelectedFileListing = listing;

                selectionList.Add(listing);

                curSelectedFileListing.GetComponent<Image>().color = Color.green;
            }
        }
        else if (Input.GetKey(KeyCode.LeftShift)) // // Clicking on a file while holding lshift, selects it from A to B
        {
            ClearSelectionList();

            int start;
            int end;

            if (curSelectedFileListing == null)
            {
                start = 0;
                end = existingListings.IndexOf(listing);
            }
            else
            {
                int ind1 = existingListings.IndexOf(curSelectedFileListing);
                int ind2 = existingListings.IndexOf(listing);

                start = ind1 > ind2 ? ind2 : ind1;
                end = ind1 > ind2 ? ind1 : ind2;
            }

            for (int i = start; i <= end; i++)
            {
                if (existingListings[i] == null)
                {
#if UNITY_EDITOR
                    Debug.LogWarning("isnull");
#endif
                    continue;
                }

                Debug.Log(existingListings[i]);
                ForceSelect(existingListings[i], true, false); // No matter where you shift-click, it always starts from the currently selected
                // listing, or 0 if none selected
            }
        }
        else
        {
            ClearSelectionList(); // Just clicking clears the list and selects the listing by itself

            if (curSelectedFileListing != null)
            {
                curSelectedFileListing.GetComponent<Image>().color = Color.white;
            }

            curSelectedFileListing = listing;
            selectionList.Add(listing);

            curSelectedFileListing.GetComponent<Image>().color = Color.green;
        }
    }

    // Terrible method name...didn't know what else to place lol
    public void OnClick_ChangePathVisibility() // Not really visibility, just what's shown in text
    {
        showFullPath = !showFullPath;

        if (showFullPath)
        {
            settings.showPathText.text = "Show name only";

            foreach (FileListing listing in existingListings)
            {
                listing.GetComponentInChildren<Text>().text = listing.FullPath;
            }
        }
        else
        {
            settings.showPathText.text = "Show full path";

            foreach (FileListing listing in existingListings)
            {
                listing.GetComponentInChildren<Text>().text = listing.FileName;
            }
        }

    }

    public void OnClick_SearchForFiles()
    {
        CheckLocation();
        ClearSelectionList();
    }

    public void OnClick_DeleteDis() // deletes with curlisting
    {
        if (selectionList.Count > 0)
        {
            int a = 1;

#if UNITY_EDITOR
            Debug.Log(selectionList.Count);
#endif

            foreach (FileListing listing in selectionList)
            {
                TryDeleteFile(listing);
            }

            ClearSelectionList();
        }
        else if (curSelectedFileListing)
        {
            TryDeleteFile(curSelectedFileListing);
        }
    }
}
#pragma warning restore 0649