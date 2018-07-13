using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class ThreadRandom {
	private ThreadLocal<System.Random> rand;
	public ThreadRandom(int seed){
		rand = new ThreadLocal<System.Random> (() => new System.Random (seed));
	}
	public int Range(int min,int max){
		return rand.Value.Next (min, max);
	}
	public float RangeFloat(float minimum, float maximum){ 
		return (float) rand.Value.NextDouble() * (maximum - minimum) + minimum;
	}
	public float Float(){ 
		return (float) rand.Value.NextDouble();
	}
	public int Integer(){ 
		return rand.Value.Next();
	}
}
