﻿using UnityEngine;
using System.Collections;

public class AmmoClip : MonoBehaviour
{

    int _currentAmmo;
    bool _isReloading, _hasAmmo, _hasStarted;

    [SerializeField] float reloadTime = 1f;

    [Header("Ammo Settings")]
    [SerializeField] AmmoType ammoType;
    [SerializeField] int maxAmount = 30;
    [SerializeField] int initialAmount = 30;

    public bool HasAmmo { get => _hasAmmo; }
    public bool IsReloading { get => _isReloading; }

    void OnEnable()
    {
        if (_hasStarted)
        {
            AmmoDisplay.Instance.UpdateAmmoInClip(_currentAmmo);
            AmmoDisplay.Instance.UpdateAmmoInHolster(GI.Instance.ammoHolster.GetCurrentAmmo(ammoType));
        }
    }

    void OnDisable()
    {
        // Cancel reload process so the gun won't reenable in reloading state.
        if (_isReloading)
        {
            _isReloading = false;
        }
    }

    void Start()
    {
        _hasStarted = true;
        _currentAmmo = Mathf.Clamp(initialAmount, 0, maxAmount); // Clamps the initial ammo amount.
        _hasAmmo = _currentAmmo > 0 ? true : false;

        AmmoDisplay.Instance.UpdateAmmoInClip(_currentAmmo);
        AmmoDisplay.Instance.UpdateAmmoInHolster(GI.Instance.ammoHolster.GetCurrentAmmo(ammoType));
    }

    public void ReduceAmmo()
    {
        _currentAmmo--;
        if (_currentAmmo < 1)
        {
            _currentAmmo = 0;
            _hasAmmo = false;
        }
        AmmoDisplay.Instance.UpdateAmmoInClip(_currentAmmo);
    }

    public void Reload()
    {
        StartCoroutine(CooldownToReload());
    }

    // Create an "waitForSeconds" variable when the final reload time be defined.
    IEnumerator CooldownToReload()
    {
        _isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        _isReloading = false;
        _hasAmmo = true;
        ReloadClip();
    }

    void ReloadClip()
    {
        int spentAmmo = maxAmount - _currentAmmo;
        int ammoInHolster = GI.Instance.ammoHolster.GetCurrentAmmo(ammoType);

        if (ammoInHolster >= spentAmmo)
        {
            _currentAmmo = maxAmount;
            GI.Instance.ammoHolster.ReduceAmmo(ammoType, spentAmmo);
        }
        else
        {
            _currentAmmo += ammoInHolster;
            GI.Instance.ammoHolster.ReduceAmmo(ammoType, ammoInHolster);
        }

        AmmoDisplay.Instance.UpdateAmmoInClip(_currentAmmo);
    }
}
