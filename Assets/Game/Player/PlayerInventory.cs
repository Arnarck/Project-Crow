﻿using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [HideInInspector] public InventoryItem[] inventory; // Total Inventory capacity;
    [HideInInspector] public Weapon[] weapons;
    [HideInInspector] public int current_slot_selected_on_item_menu;
    [HideInInspector] public WeaponType equiped_weapon = WeaponType.NONE;

    public int slots_expanded_after_collecting_expansion_item;
    public int total_capacity;
    public bool combine_option_enabled;
    public GameObject slot_prefab;
    public Transform weapon_holster;

    void Awake()
    {
        GI.player_inventory = this;
        inventory = new InventoryItem[total_capacity];

        // @Arnarck maybe turn InventoryItem into a struct
        for (int i = 0; i < inventory.Length; i++) // Creates the inventory, and disable all slots
        {
            inventory[i] = new InventoryItem();
            inventory[i].ui = Instantiate(slot_prefab).GetComponent<InventorySlotUI>();
            inventory[i].ui.index = i;
            inventory[i].ui.gameObject.SetActive(false);
            inventory[i].ui.transform.SetParent(GI.hud.inventory_handler.transform, false);
        }

        weapons = new Weapon[weapon_holster.childCount];
        // Disable all weapons at the start of the game
        for (int i = 0; i < weapon_holster.childCount; i++)
        {
            weapons[i] = weapon_holster.GetChild(i).GetComponent<Weapon>();
            weapons[i].gameObject.SetActive(false);
        }
    }

    void Update()
    {
        { // Toggle inventory ui
            if (Input.GetKeyDown(KeyCode.I))
            {
                GI.pause_game.toggle_pause_game();
                GI.hud.inventory_screen.SetActive(GI.pause_game.game_paused);
                if (GI.pause_game.game_paused == false)
                {
                    disable_item_menu();
                    combine_option_enabled = false;
                    Debug.Log("Combine disabled!");
                }
            }   
        }

        { // Close current menu / option active
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                if (combine_option_enabled)
                {
                    combine_option_enabled = false;
                    toggle_all_slot_buttons(true);
                    Debug.Log("Combine disabled!");
                }
                else if (GI.hud.item_menu.gameObject.activeInHierarchy)
                {
                    disable_item_menu();
                }
                else if (GI.hud.inventory_screen.activeInHierarchy)
                {
                    GI.pause_game.toggle_pause_game();
                    GI.hud.inventory_screen.SetActive(false);
                    toggle_all_slot_buttons(true);
                }
            }
        }
    }

    public void disable_item_menu()
    {
        GI.hud.item_menu.gameObject.SetActive(false);
        toggle_all_slot_buttons(true);
    }

    public bool store_item(ItemType item_type)
    {
        if (is_cumulative(item_type)) return false;

        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].is_avaliable && inventory[i].type.Equals(ItemType.NONE)) // Tries to find an empty and avaliable inventory slot
            {
                ItemDetails item_details = GI.items_in_game.get_details_of(item_type);
                set_slot_data(i, item_details.type, 1, item_details.sprite);
                Debug.Log($"Slot {i} filled with {inventory[i].type}!");
                //Destroy(item.gameObject);

                return true;
            }
        }

        Debug.LogError("No inventory slots avaliable!");
        return false;
    }

    void set_slot_data(int i, ItemType type, int stored_amount, Sprite sprite)
    {
        inventory[i].type = type;
        inventory[i].stored_amount = stored_amount;
        inventory[i].ui.index = i;
        inventory[i].ui.button.gameObject.SetActive(stored_amount > 0 ? true : false);
        inventory[i].ui.count_text.text = is_cumulative(type) ? stored_amount.ToString() : null;
        inventory[i].ui.button.image.sprite = sprite;

        Debug.Log($"Added {inventory[i].type} to slot {i} and amount {inventory[i].stored_amount}");
    }

    public bool store_cumulative_item(ItemType item_type, int item_amount)
    {
        if (!is_cumulative(item_type)) return false;

        int type = (int)item_type;
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].is_avaliable)
            {
                if (inventory[i].type.Equals(ItemType.NONE)) // Add item to an empty slot
                {
                    ItemDetails item_details = GI.items_in_game.get_details_of(item_type);
                    if (InventoryData.max_capacity[type] >= item_amount) // avaliable space on inventory is greater than item amount
                    {
                        set_slot_data(i, item_details.type, item_amount, item_details.sprite);
                        //Destroy(item.gameObject);
                        return true;
                    }
                    else // item amount is greater than inventory space
                    {
                        int remaining_amount_on_item = item_amount - InventoryData.max_capacity[type];
                        set_slot_data(i, item_details.type, InventoryData.max_capacity[type], item_details.sprite);
                        item_amount = remaining_amount_on_item;
                    }
                }
                else if (inventory[i].type.Equals(item_type) && inventory[i].stored_amount < InventoryData.max_capacity[type]) // Add item to an existing slot
                {
                    int avaliable_space = InventoryData.max_capacity[type] - inventory[i].stored_amount;

                    if (avaliable_space >= item_amount) // avaliable space on inventory is greater than item amount
                    {
                        inventory[i].stored_amount += item_amount;
                        inventory[i].ui.count_text.text = inventory[i].stored_amount.ToString();
                        item_amount = 0;
                        //Destroy(item.gameObject);
                        return true;
                    }
                    else // item amount is greater than inventory space
                    {
                        int remaining_amount_on_item = item_amount - avaliable_space;
                        inventory[i].stored_amount = InventoryData.max_capacity[type];
                        inventory[i].ui.count_text.text = inventory[i].stored_amount.ToString();
                        item_amount = remaining_amount_on_item;
                    }
                }
            }
        }

        Debug.LogError("No inventory slots avaliable!");
        return false;
    }

    public void remove_item(int i)
    {

        if (!inventory[i].is_avaliable || inventory[i].type.Equals(ItemType.NONE) || inventory[i].stored_amount < 1)
        {
            Debug.LogError("Trying to remove item from an invalid inventory slot!");
            return;
        }

        Debug.Log($"Removed 1 {inventory[i].type} from slot {i}.");

        inventory[i].stored_amount--;

        if (inventory[i].stored_amount < 1) // Reset inventory slot
        {
            set_slot_data(i, ItemType.NONE, 0, null);
            return;
        }
        
        // Should only get here with cumulative items
        inventory[i].ui.count_text.text = inventory[i].stored_amount.ToString();
    }

    public void remove_item(ItemType item, int amount_to_remove)
    {
        for (int i = 0; i < inventory.Length; i++)
        {
            if (item.Equals(inventory[i].type))
            {
                if (inventory[i].stored_amount > amount_to_remove)
                {
                    inventory[i].stored_amount -= amount_to_remove;
                    inventory[i].ui.count_text.text = inventory[i].stored_amount.ToString();
                    return;
                }
                else
                {
                    amount_to_remove -= inventory[i].stored_amount;
                    set_slot_data(i, ItemType.NONE, 0, null);
                    if (amount_to_remove < 1) return;
                }
            }
        }

        Debug.LogError($"Could not remove {amount_to_remove} items!");
    }

    public void expand_inventory_capacity()
    {
        int amount_to_expand = slots_expanded_after_collecting_expansion_item;

        for (int i = 0; i < inventory.Length; i++)
        {
            if (!inventory[i].is_avaliable)
            {
                amount_to_expand--;
                inventory[i].is_avaliable = true;
                inventory[i].type = ItemType.NONE;
                inventory[i].ui.gameObject.SetActive(true);
                inventory[i].ui.button.gameObject.SetActive(false);

                if (amount_to_expand < 1) return;
            }
        }

        Debug.LogError("All inventory slots unlocked. Couldn't expand inventory by " + amount_to_expand + " slots.");
    }

    public bool can_expand_capacity()
    {
        for (int i = 0; i < inventory.Length; i++)
        {
            if (!inventory[i].is_avaliable) return true;
        }

        return false;
    }

    public bool is_cumulative(ItemType item)
    {
        if ((int)item >= 6 && (int)item <= 9) return true; // Ammo
        if (item == ItemType.GUN_REPAIR_KIT) return true;

        return false;
    }

    public bool is_weapon(ItemType item)
    {
        if ((int)item >= 1 && (int)item <= 5) return true;
        return false;
    }

    public bool is_ammo(ItemType item)
    {
        int current_index = (int)item;
        int first_ammo_index = (int)ItemType.PISTOL_AMMO;
        int last_ammo_index = (int)ItemType.SUBMACHINE_GUN_AMMO;

        return current_index >= first_ammo_index && current_index <= last_ammo_index;
    }

    public bool is_consumable(ItemType item)
    {
        if ((int)item >= 10 && (int)item <= 13) return true;
        return false;
    }

    // @Arnarck Create 4 slots that will work as shortcuts for weapons?
    // @Arnarck prevents player from accessing other inventory slots while the item menu is active
    // @Arnarck (Later) Change "item menu" position based on which slot activated it
    public void toggle_item_menu(int i)
    {
        ItemType item = inventory[i].type;
        current_slot_selected_on_item_menu = i;

        toggle_all_slot_buttons(false, i);

        if (is_weapon(item)) // Weapons
        {
            Debug.Log("WEAPON selected");
            toggle_item_menu_options(false, true, false);
        }
        else if (is_cumulative(item)) // Ammo (or cumulative items)
        {
            Debug.Log("CUMULATIVE ITEM selected");
            toggle_item_menu_options(false, false, true);
        }
        else if (is_consumable(item)) // Consumable items
        {
            Debug.Log("CONSUMABLE ITEM selected");
            toggle_item_menu_options(true, false, false);
        }
        GI.hud.item_menu.activate_item_menu();
    }

    public void toggle_item_menu_options(bool option1_active, bool option2_active, bool option3_active)
    {
        GI.hud.item_menu.options[0].SetActive(option1_active);
        GI.hud.item_menu.options[1].SetActive(option2_active);
        GI.hud.item_menu.options[2].SetActive(option3_active);
    }

    // @Arnarck Disable option of equiping with the weapon is already equiped
    // @Arnarck Create 4 slots to serve as shortcuts?
    public void equip_weapon()
    {
        if (!is_weapon(inventory[current_slot_selected_on_item_menu].type))
        {
            Debug.LogError($"{inventory[current_slot_selected_on_item_menu].type} is not a weapon!");
            return;
        }

        WeaponType weapon = get_weapon_type_of(inventory[current_slot_selected_on_item_menu].type);
        if (weapon.Equals(equiped_weapon))
        {
            Debug.LogError($"{weapon} already equiped!");
            return;
        }
        if (weapon.Equals(WeaponType.NONE) || weapon.Equals(WeaponType.COUNT))
        {
            Debug.LogError($"Trying to equip {weapon} to weapon slot!");
            return;
        }

        // Enables the selected weapon and disable the others
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].type.Equals(weapon))
            {
                weapons[i].gameObject.SetActive(true);
                equiped_weapon = weapons[i].type;
            }
            else weapons[i].gameObject.SetActive(false);
        }

        display_total_ammo_of_equiped_weapon();
        GI.hud.ammo_display.SetActive(is_equiped_with_a_gun()); // Enables the ammo display if player equips a gun, otherwise disables it
        GI.hud.display_crosshair_of_equiped_weapon();
        disable_item_menu();
    }

    public void display_total_ammo_of_equiped_weapon()
    {
        if (equiped_weapon.Equals(WeaponType.KNIFE) || equiped_weapon.Equals(WeaponType.NONE) || equiped_weapon.Equals(WeaponType.COUNT))
        {
            Debug.LogError($"{equiped_weapon} does not have an ammo count");
            return;
        }

        ItemType ammo_type = get_ammo_type_of(equiped_weapon);
        int total_ammo_count = get_total_item_count(ammo_type);
        GI.hud.display_ammo_in_holster(total_ammo_count);
    }

    public ItemType get_ammo_type_of(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.PISTOL:
                return ItemType.PISTOL_AMMO;

            case WeaponType.SHOTGUN:
                return ItemType.SHOTGUN_AMMO;

            case WeaponType.ASSAULT_RIFLE:
                return ItemType.ASSAULT_RIFLE_AMMO;

            case WeaponType.SUBMACHINE_GUN:
                return ItemType.SUBMACHINE_GUN_AMMO;

            default:
                Debug.Assert(false);
                break;
        }

        Debug.LogError($"{weapon} is a invalid weapon type!");
        return ItemType.NONE;
    }

    public WeaponType get_weapon_type_of(ItemType item)
    {
        switch (item)
        {
            case ItemType.PISTOL:
            case ItemType.PISTOL_AMMO:
                return WeaponType.PISTOL;

            case ItemType.SHOTGUN:
            case ItemType.SHOTGUN_AMMO:
                return WeaponType.SHOTGUN;

            case ItemType.ASSAULT_RIFLE:
            case ItemType.ASSAULT_RIFLE_AMMO:
                return WeaponType.ASSAULT_RIFLE;

            case ItemType.SUBMACHINE_GUN:
            case ItemType.SUBMACHINE_GUN_AMMO:
                return WeaponType.SUBMACHINE_GUN;

            case ItemType.KNIFE:
                return WeaponType.KNIFE;

            default:
                //Debug.Assert(false);
                break;
        }

        Debug.LogError($"{item} is a invalid weapon type!");
        return WeaponType.NONE;
    }

    public int get_total_item_count(ItemType item)
    {
        int item_count = 0;
        for (int i = 0; i < inventory.Length; i++)
        {
            if (item.Equals(inventory[i].type)) item_count += inventory[i].stored_amount;
        }

        return item_count;
    }

    public void reset_slot_data()
    {
        bool result = check_if_item_is_ammo_and_corresponds_to_equiped_weapon(inventory[current_slot_selected_on_item_menu].type);
        set_slot_data(current_slot_selected_on_item_menu, ItemType.NONE, 0, null);
        if (result) display_total_ammo_of_equiped_weapon();
        disable_item_menu();
    }

    public bool check_if_item_is_ammo_and_corresponds_to_equiped_weapon(ItemType item)
    {
        if (!is_ammo(item)) return false;
        if (!equiped_weapon.Equals(get_weapon_type_of(item))) return false; // checks if item is an ammo of the equiped weapon

        return true;
    }

    public void use_item()
    {
        if (inventory[current_slot_selected_on_item_menu].stored_amount < 1)
        {
            Debug.LogError("Trying to consume an item from an empty slot");
            return;
        }

        GI.player.use_item(inventory[current_slot_selected_on_item_menu].type);
        remove_item(current_slot_selected_on_item_menu);
        disable_item_menu();
    }

    
    public void clicked_combine_button()
    {
        combine_option_enabled = true;
        GI.hud.item_menu.gameObject.SetActive(false);
        disable_all_slots_not_combinable_with_current_selected_slot();
        Debug.Log("Combine enabled!");
    }

    public void try_to_combine_item_slots(int i)
    {
        if (i == current_slot_selected_on_item_menu)
        {
            Debug.LogError("Trying to combine an inventory with itself");
            return;
        }

        if (inventory[i].type == inventory[current_slot_selected_on_item_menu].type && is_cumulative(inventory[i].type))
        {
            combine_cumulative_itens_of_same_type(i);
        }
        else if (is_weapon(inventory[i].type) && inventory[current_slot_selected_on_item_menu].type == ItemType.GUN_REPAIR_KIT)
        {
            combine_gun_with_repair_kit(i);
        }

        combine_option_enabled = false;
        toggle_all_slot_buttons(true);
        Debug.Log("Combine disabled!");
    }

    //@TODO: Currently the Knife is displaying when clicking to combine repair kit with gun. Add condition to disable the knife
    void combine_gun_with_repair_kit(int index)
    {
        remove_item(current_slot_selected_on_item_menu);
        WeaponType weapon_type = get_weapon_type_of(inventory[index].type);

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapon_type == weapons[i].type) weapons[i].increase_integrity(GI.items_in_game.gun_integrity_restored_by_repair_kit);
        }
    }

    void combine_cumulative_itens_of_same_type(int i)
    {
        int avaliable_space = InventoryData.max_capacity[(int)inventory[i].type] - inventory[i].stored_amount;
        int space_needed = inventory[current_slot_selected_on_item_menu].stored_amount;

        if (space_needed > avaliable_space) // The first slot won't be reseted
        {
            // Remove item count from the first slot to send it to the second slot
            inventory[current_slot_selected_on_item_menu].stored_amount = space_needed - avaliable_space;
            inventory[current_slot_selected_on_item_menu].ui.count_text.text = inventory[current_slot_selected_on_item_menu].stored_amount.ToString();

            // Fills the slot that will receive the item
            inventory[i].stored_amount = InventoryData.max_capacity[(int)inventory[i].type];
            inventory[i].ui.count_text.text = inventory[i].stored_amount.ToString();
        }
        else // The first slot will be reseted
        {
            set_slot_data(current_slot_selected_on_item_menu, ItemType.NONE, 0, null);
            inventory[i].stored_amount += space_needed;
            inventory[i].ui.count_text.text = inventory[i].stored_amount.ToString();
        }
    }

    public void toggle_all_slot_buttons(bool interactable, int exception = -1)
    {
        for (int i = 0; i < inventory.Length; i++)
        {
            if (i == exception) continue;
            inventory[i].ui.button.interactable = interactable;
        }
    }

    public void disable_all_slots_not_combinable_with_current_selected_slot()
    {
        ItemType item = inventory[current_slot_selected_on_item_menu].type;
        for (int i = 0; i < inventory.Length; i++)
        {
            if (can_combine_slot_with_cumulative_item(item, i)) inventory[i].ui.button.interactable = true;
            else if (can_combine_slot_with_repair_kit(item, i)) inventory[i].ui.button.interactable = true;
            else inventory[i].ui.button.interactable = false;
        }
    }

    public bool can_combine_slot_with_cumulative_item(ItemType item, int index)
    {
        if (!is_cumulative(item)) return false;

        InventoryItem slot = inventory[index];
        bool is_current_slot_selected = index == current_slot_selected_on_item_menu;
        bool has_avaliable_space_on_slot = slot.stored_amount < InventoryData.max_capacity[(int)slot.type];

        return item == slot.type && has_avaliable_space_on_slot && !is_current_slot_selected;
    }

    public bool can_combine_slot_with_repair_kit(ItemType item, int index)
    {
        if (!is_weapon(inventory[index].type)) return false;

        bool is_current_slot_selected = index == current_slot_selected_on_item_menu;
        bool is_weapon_integrity_full = false;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].type == get_weapon_type_of(inventory[index].type))
            {
                is_weapon_integrity_full = weapons[i].integrity >= weapons[i].max_integrity;
                break;
            }
        }

        return item == ItemType.GUN_REPAIR_KIT && !is_current_slot_selected && !is_weapon_integrity_full;
    }

    public bool is_equiped_with_a_gun()
    {
        if (equiped_weapon == WeaponType.KNIFE) return false;
        else if (equiped_weapon == WeaponType.NONE) return false;
        else return true;
    }

    public Weapon get_equiped_weapon()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].type == equiped_weapon) return weapons[i];
        }

        //Debug.LogError("Equiped weapon not found");
        return null;
    }
}
