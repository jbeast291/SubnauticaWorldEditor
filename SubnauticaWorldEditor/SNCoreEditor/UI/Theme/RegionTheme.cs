using UnityEngine;

namespace SNCoreEditor.UI.Theme;

public class RegionTheme : MonoBehaviour
{
    [SerializeField] private string regionName;
    public string RegionName => regionName;
}