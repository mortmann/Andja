﻿using UnityEngine;
using System.Collections;

public class Fertility {
	public int ID;
	public string name;
	public Climate[] climates;
	public Fertility(){
		
	}
	public Fertility(int ID, string name){
		this.ID = ID;
		this.name = name;
	}
	
}
