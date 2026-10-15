using System;
using System.Collections.Generic;
using LSW._02._Code.Common;
using LSW._02._Code.Entity;
using LSW._02._Code.Entity.Players;
using LSW._02._Code.Entity.Players.Components;
using LSW._02._Code.Sos;
using LSW._02._Code.System;
using LSW._02._Code.System.HandlePlayerSystems;
using UnityEngine;
using UnityEngine.UI;

namespace LSW._02._Code.UI
{
    public class InGameUI : MonoBehaviour
    {
        private PlayerController _player;
        private PlayerInventory _inventory;
        private RoomController _room;
        private GameObject _inventoryPanel;
        private Button _inventoryToggleButton;
        private Text _inventoryTitle;
        private readonly List<Button> _weaponButtons = new();
        private bool _isBound;

        private void Start()
        {
            BindPlayer();
        }

        private void Update()
        {
            if (!_isBound)
                BindPlayer();

            if (_inventoryPanel != null && _inventoryPanel.activeSelf &&
                (_room == null || !_room.CanOpenInventory))
            {
                _inventoryPanel.SetActive(false);
            }

            if (_inventoryToggleButton != null && _room != null)
                _inventoryToggleButton.gameObject.SetActive(_room.CanOpenInventory);
        }

        private void BindPlayer()
        {
            _player = GetComponentInParent<PlayerController>();

            if (_player == null && SystemHandler.Instance != null &&
                SystemHandler.Instance.GetSystem(out HandlePlayerSystem playerSystem))
            {
                _player = playerSystem.Player;
            }

            if (_player == null || _player.inputData == null ||
                !_player.GetCompo(out _inventory))
                return;

            _room = _player.GetComponent<RoomController>();
            _player.inputData.OnSwitchPressed += ToggleInventory;
            _inventory.OnInventoryChanged += RefreshInventory;
            BuildInventoryPanel();
            _inventoryToggleButton = AddScreenButton("INVENTORY", ToggleInventory);
            _inventoryToggleButton.gameObject.SetActive(_room != null && _room.CanOpenInventory);
            _isBound = true;
        }

        private void BuildInventoryPanel()
        {
            _inventoryPanel = new GameObject("Inventory Panel", typeof(RectTransform), typeof(Image));
            _inventoryPanel.transform.SetParent(transform, false);

            RectTransform panelRect = _inventoryPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(460f, 320f);

            _inventoryPanel.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.1f, 0.95f);
            _inventoryTitle = CreateText("Title", _inventoryPanel.transform, 105f);
            RefreshInventory();

            AddButton("Close", _inventoryPanel.transform, -115f,
                () => _inventoryPanel.SetActive(false));
            _inventoryPanel.SetActive(false);
        }

        private void RefreshInventory()
        {
            if (_inventoryPanel == null)
                return;

            foreach (Button button in _weaponButtons)
                Destroy(button.gameObject);
            _weaponButtons.Clear();

            _inventoryTitle.text =
                $"INVENTORY - EQUIPPED: {GetWeaponName(_inventory.EquippedWeapon)}";

            HashSet<WeaponData> displayedWeapons = new();
            float yPosition = 35f;
            foreach (WeaponData weapon in _inventory.Weapons)
            {
                if (weapon == null || !displayedWeapons.Add(weapon))
                    continue;

                string label = weapon == _inventory.EquippedWeapon
                    ? $"{GetWeaponName(weapon)} (EQUIPPED)"
                    : GetWeaponName(weapon);
                Button button = AddButton(label, _inventoryPanel.transform, yPosition,
                    () => _inventory.Equip(weapon));
                _weaponButtons.Add(button);
                yPosition -= 55f;
            }
        }

        private static string GetWeaponName(WeaponData weapon)
        {
            return weapon != null ? weapon.displayName : "NONE";
        }

        private void ToggleInventory()
        {
            if (_room == null || !_room.CanOpenInventory)
                return;

            _inventoryPanel.SetActive(!_inventoryPanel.activeSelf);
        }

        private Text CreateText(string objectName, Transform parent, float yPosition)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.5f);
            rect.anchorMax = new Vector2(0.95f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, yPosition);
            rect.sizeDelta = new Vector2(0f, 42f);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return text;
        }

        private Button AddButton(string label, Transform parent, float yPosition, Action onClick)
        {
            GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.5f);
            rect.anchorMax = new Vector2(0.8f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, yPosition);
            rect.sizeDelta = new Vector2(0f, 44f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.27f, 0.4f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());

            Text text = CreateText("Label", buttonObject.transform, 0f);
            text.text = label;
            text.fontSize = 18;
            return button;
        }

        private Button AddScreenButton(string label, Action onClick)
        {
            Button button = AddButton(label, transform, 0f, onClick);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -24f);
            rect.sizeDelta = new Vector2(180f, 54f);
            return button;
        }

        private void OnDestroy()
        {
            if (_player != null && _player.inputData != null)
                _player.inputData.OnSwitchPressed -= ToggleInventory;

            if (_inventory != null)
                _inventory.OnInventoryChanged -= RefreshInventory;
        }
    }
}
