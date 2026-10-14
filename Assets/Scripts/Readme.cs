using System; // Imports basic C# types and attributes, including Serializable for saved data classes.
using UnityEngine; // Imports Unity types such as MonoBehaviour, GameObject, Transform, Vector3, and Time.

public class Readme : ScriptableObject // Defines a data asset for project documentation; ScriptableObject stores data separately from scene components.
{ // Begins this block; the statements inside belong to the class, method, or condition above.
    public Texture2D icon; // Stores the image displayed as the documentation asset's icon.
    public string title; // Stores the documentation title as text.
    public Section[] sections; // Stores an ordered array of documentation sections, using the nested Section class below.
    public bool loadedLayout; // Records whether the readme editor has already loaded its suggested window layout.

    [Serializable] // Allows Unity to serialize Section objects inside this asset, retaining their fields when saved.
    public class Section // Defines the nested data class used for each documentation section.
    { // Begins this block; the statements inside belong to the class, method, or condition above.
        public string heading, text, linkText, url; // Stores the section heading, body text, clickable link label, and destination URL.
    } // Ends the block opened by the matching opening brace.
} // Ends the block opened by the matching opening brace.
