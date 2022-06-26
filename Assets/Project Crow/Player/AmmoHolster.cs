﻿using UnityEngine;
public enum AmmoType
{
    Pistol,
    Shotgun,
    AssaultRifle,
    SubmachineGun
}

public class AmmoHolster : MonoBehaviour
{
    [SerializeField] AmmoSlot[] slots;

    // Shows the content of the class, in the inspector
    [System.Serializable]
    private class AmmoSlot
    {
        int currentAmount;
        public int ammo { get => currentAmount; set => currentAmount = value; }

        public AmmoType ammoType;
        [Range(0, 9999)] public int max_ammo;
        [Range(0, 9999)] public int initialAmount;
    }

    void Awake()
    {
        GI.ammo_holster = this;
    }

    void Start()
    {
        foreach (AmmoSlot slot in slots)
        {
            slot.initialAmount = Mathf.Clamp(slot.initialAmount, 0, slot.max_ammo);
            slot.ammo = slot.initialAmount;
        }
    }

    public int GetCurrentAmmo(AmmoType type)
    {
        AmmoSlot slot = FindAmmoSlot(type);

        if (slot != default)
        {
            return slot.ammo;
        }

        return 0;
    }

    public bool IncreaseAmmo(AmmoType type, int amount)
    {
        AmmoSlot slot = FindAmmoSlot(type);
        if (slot == default)
        {
            Debug.LogError(type + " ammo not found!");
            return false;
        }

        if (slot.ammo >= slot.max_ammo) return false;
        
        slot.ammo = Mathf.Clamp(slot.ammo + amount, 0, slot.max_ammo);

        if (GI.gun_switcher.get_current_ammo_type() == type) GI.ammo_display.display_ammo_in_holster(slot.ammo);

        return true;
    }

    public void ReduceAmmo(AmmoType type, int amount)
    {
        AmmoSlot slot = FindAmmoSlot(type);
        if (slot != default)
        {
            slot.ammo = Mathf.Clamp(slot.ammo - amount, 0, slot.max_ammo);
        }

        GI.ammo_display.display_ammo_in_holster(slot.ammo);
    }

    public bool IsAmmoInHolsterEmpty(AmmoType type)
    {
        AmmoSlot slot = FindAmmoSlot(type);

        if (slot == default) { return true; }

        if (slot.ammo < 1)
        {
            return true;
        }

        return false;
    }

    AmmoSlot FindAmmoSlot(AmmoType type)
    {
        foreach (AmmoSlot slot in slots)
        {
            if (slot.ammoType.Equals(type))
            {
                return slot;
            }
        }

        return default;
    }
}
