using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DevTeleport : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private List<Transform> teleportLocations;
    [SerializeField] private TMP_Dropdown teleportDropDown;
    private int currentTeleportOption = 0;

    private PlayerActions _actions;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _actions = _player.GetComponent<PlayerActions>();

        teleportDropDown.ClearOptions();

        List<string> teleportOptions = new List<string>();

        foreach (Transform t in teleportLocations)
        {
            teleportOptions.Add(t.name);
        }

        teleportDropDown.AddOptions(teleportOptions);

        teleportDropDown.onValueChanged.AddListener(OnDropDownValuesChanged);
    }

    // Update is called once per frame
    void Update()
    {
        if (_actions.ControlHeld)
        {
            if(_actions.RespawnDown)
            {
                TeleportToLocation();
            }
        }
    }

    private void OnDropDownValuesChanged(int index)
    {
        currentTeleportOption = index;
        TeleportToLocation();
    }

    public void TeleportToLocation()
    {
        _player.transform.position = teleportLocations[currentTeleportOption].position;
        _player.transform.rotation = teleportLocations[currentTeleportOption].rotation;
    }
}
