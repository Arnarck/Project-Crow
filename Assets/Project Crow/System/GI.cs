﻿using UnityEngine;

// Game Instances
public class GI : MonoBehaviour
{
    public static GI Instance { get; private set; }
    public Player player;
    public PlayerHunger hunger;
    public PlayerThirst thirst;
    public Camera FPCamera;
    public AmmoHolster ammoHolster;

    private void Awake()
    {
        Instance = this;
    }
}