using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayDropdownClick : MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler
{
    private TMP_Dropdown _dropdown;

    private void Awake()
    {
        _dropdown = GetComponent<TMP_Dropdown>();

        // Optional: sound when selection changes
        _dropdown.onValueChanged.AddListener(_ =>
        {
            AudioManager.Instance.PlayButtonClick();
        });
    }

    /// <summary>
    /// Plays hover sound when pointer enters dropdown.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.Instance.PlayButtonHover();
    }

    /// <summary>
    /// Plays click sound when dropdown is clicked/opened.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Instance.PlayButtonClick();
    }
}
