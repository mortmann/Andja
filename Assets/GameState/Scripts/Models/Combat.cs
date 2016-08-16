using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// Damage types.
/// Projectile - arrow/bullet
/// Blade - Sword/eg
/// Pike - Pike :D
/// Artillery - Cannons (only thing that can damage buildings)
/// </summary>
public enum DamageType {Projectile,Blade,Pike,Artillery}
/// <summary>
/// Armor types.
/// Leather - worst used by smaller units
/// Metal - better but for slower units
/// Wood - Only gets hurt by artillery
/// </summary>
public enum ArmorType {Leather,Metal,Wood}

public class Combat {
	//TODO LOAD THIS IN
	static float[,] reduction = {
		//leather
		{ 1, 1, 1, 2 },
		//Metal
		{ 0.5f, 0.7f, 1, 1 },
		//Wood
		{ 0, 0, 0, .9f }
	};

//	public static float TakeDamage(DamageType type, float damageAmount){
//		switch (type) {
//		case DamageType.Projectile:
//			
//		case DamageType.Blade:
//
//		case DamageType.Pike:
//
//		case DamageType.Artillery:
//
//		default:
//			throw new ArgumentOutOfRangeException ();
//		}
//
//	}
	public static float ArmorDamageReduction(ArmorType armor,DamageType dt){
		switch (armor) {
		case ArmorType.Leather:
			return reduction [0, (int)dt];
		case ArmorType.Metal:
			return reduction [1, (int)dt];
		case ArmorType.Wood:
			return reduction [2, (int)dt];
		default:
			throw new ArgumentOutOfRangeException ();
		}
	}
}
