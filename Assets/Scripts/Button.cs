using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonBehavior : MonoBehaviour
{
    [Header("Menus")]
    [SerializeField] private GameObject startMenu;
    [SerializeField] private GameObject settingsParent;
    [SerializeField] private GameObject settingsOptions;
    [SerializeField] private GameObject[] subMenus;

    [Header("Voltar")]
    [SerializeField] private GameObject backToMenuButton;
    [SerializeField] private GameObject backToSettingsButton;

    private GameObject currentSubMenu;

    public void ShowSubMenu(int index)
    {
        if (index >= 0 && index < subMenus.Length)
        {
            settingsOptions.SetActive(false);

            foreach (var menu in subMenus)
                menu.SetActive(false);

            subMenus[index].SetActive(true);
            currentSubMenu = subMenus[index];

            backToMenuButton.SetActive(false);
            backToSettingsButton.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Índice inválido para submenu.");
        }
    }

    public void BackToSettings()
    {
        if (currentSubMenu != null)
        {
            currentSubMenu.SetActive(false);
            currentSubMenu = null;
        }

        settingsOptions.SetActive(true);
        backToSettingsButton.SetActive(false);
        backToMenuButton.SetActive(true);
    }

    public void backToStartMenu()
    {
        startMenu.SetActive(true);
        settingsParent.SetActive(false);
        backToMenuButton.SetActive(false);
        backToSettingsButton.SetActive(false);

        foreach (var menu in subMenus)
            menu.SetActive(false);
    }

    public void hideStartMenu()
    {
        startMenu.SetActive(false);
        settingsParent.SetActive(true);
        backToMenuButton.SetActive(true);
        backToSettingsButton.SetActive(false);
    }




}
