using UnityEngine;
using UnityEngine.UI;

public class BuildingButton : MonoBehaviour
{
    [SerializeField] private string buildingName;
    [TextArea]
    [SerializeField] private string buildingDescription;

    private Button button;
    private MapUIManager mapManager;

    private void Start()
    {
        button = GetComponent<Button>();
        mapManager = FindObjectOfType<MapUIManager>();

        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        mapManager.ShowBuildingInfo(buildingName, buildingDescription);
    }
}
