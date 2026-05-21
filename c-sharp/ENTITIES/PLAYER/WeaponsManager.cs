using Godot;
using System;
using System.Collections.Generic;
using System.Threading;

public partial class WeaponsManager : Node3D
{
	private List<Node> weapons = new();
	
	private int currentWeaponIndex = 0;

	
	private float scrollCooldown = 0.2f; // seconds
	private float scrollTimer = 0f;

	public override void _Process(double delta)
	{
		if(PlayerStats.playerHealth <= 0)
            return;
		// Count down the scroll cooldown
		if (scrollTimer > 0)
			scrollTimer -= (float)delta;

	}

	public override void _Ready()
    {
		AddToGroup("WeaponsManager");
		foreach (Node child in GetChildren())
        {
            weapons.Add(child);
            child.SetProcess(false);  
            CenterContainer cc = child.GetNode<CenterContainer>("CenterContainer");
			cc.Visible = false;
        }

		EquipWeapon(0);
    }
	public string GetCurrentWeaponName()
	{
		if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
		{
			return weapons[currentWeaponIndex].Name;
		}
		return "Unknown";
	}

	public int GetCurrentWeaponAmmo()
	{
		if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
		{
			string weaponName = weapons[currentWeaponIndex].Name;
			if (weaponName == "PISTOL" || weaponName == "MINIGUN")
				return PlayerStats.pistolAmmo;
			else if (weaponName == "SHOTGUN" || weaponName == "SUPERSHOTGUN")
				return PlayerStats.shotgunAmmo;			
		}
		return 0;
	}
	public override void _UnhandledInput(InputEvent @event)
    {
		if(PlayerStats.playerHealth <= 0)
            return;
			
        if (weapons[currentWeaponIndex].GetNodeOrNull<AnimatedSprite2D>("CenterContainer/GUN") is AnimatedSprite2D anim)
        {
			if (anim.Animation == "IDLE")
			{
				for (int i = 0; i < weapons.Count; i++)
				{
					if (Input.IsActionJustPressed("weapon_" + (i + 1)))
					{
						EquipWeapon(i);
						return;
					}
				}
				if (scrollTimer <= 0)
    			{
					if (@event is InputEventMouseButton mb)
					{
						if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
						{
							NextWeapon();
							scrollTimer = scrollCooldown;
						}
							

						if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
						{
							PreviousWeapon();
							scrollTimer = scrollCooldown;
						}		
					}
				}
			}
        }    
    }

	private void EquipWeapon(int index)
	{
		if (index < 0 || index >= weapons.Count) return;

		// Disable previous weapon
		weapons[currentWeaponIndex].SetProcess(false);

		// Get CenterContainer of previous weapon and hide it
		if (weapons[currentWeaponIndex].GetNodeOrNull<CenterContainer>("CenterContainer") is CenterContainer prevCC)
			prevCC.Visible = false;

		// Enable new weapon
		currentWeaponIndex = index;
		weapons[currentWeaponIndex].SetProcess(true);

		// Get CenterContainer of new weapon and show it
		if (weapons[currentWeaponIndex].GetNodeOrNull<CenterContainer>("CenterContainer") is CenterContainer newCC)
			newCC.Visible = true;
	}
	private void NextWeapon()
    {
        int next = (currentWeaponIndex + 1) % weapons.Count;
        EquipWeapon(next);
    }

    private void PreviousWeapon()
    {
        int prev = (currentWeaponIndex - 1 + weapons.Count) % weapons.Count;
        EquipWeapon(prev);
    }
}
