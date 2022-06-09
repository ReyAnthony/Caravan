using System; 
using System.Collections.Generic;
using CaravanSerialization;
using UnityEngine;

#pragma warning disable CS0414 // Remove warnings
[CreateAssetMenu(menuName = "Game/GameManager")]
[Saved("GameManager")]
public class GameManager : CaravanScriptableObject
{
	//basic types
	[SaveThat, SerializeField] private string _s;
	[SaveThat, SerializeField] private float f;
	[SaveThat, SerializeField] private double d;
	[SaveThat, SerializeField] private int _i = 1;
	[SaveThat, SerializeField] private bool _b = false;
	[SaveThat, SerializeField] private char _c = 'd';

	//byte
	//uint
	//short
	//ulong
	//long
	//decimal
	//etc..

	//TODO test with wrapper types
	[SaveThat, SerializeField] private Boolean _bb = false;

	//List of AsIs
	[SaveThat, SerializeField] private List<string> _li = new List<string>{ "a", "a", "a" };

	//Unity
	[SaveThat, SerializeField] private Vector3 _vec3 = Vector3.back;
	[SaveThat, SerializeField] private Quaternion q = Quaternion.identity;

	//Custom type
	[SaveThat, SerializeField] private TestStruct testStruct;

	//Should be handleded automatically since we have defined TestStruct
	[SaveThat, SerializeField] private List<TestStruct> testStructsList;

    //Nested

    [SaveThat, SerializeField] private NeedNested _needNested = new NeedNested();

    protected override void SaveCallback(ISaver saver)
    {
		saver.Save<Vector3>(Vector3.back, "_ss");
		saver.Save(new List<Vector3> { Vector3.down, Vector3.up }, "_allPlayerPositions");
		saver.Save(new List<string> { "Ninja", "Berserk" }, "_allPlayerClasses");
	}

    protected override void LoadCallback(ILoader loader)
    {
		var v = loader.Load<Vector3>("_ss");
		Debug.Log(v);

		var pp = loader.Load<List<Vector3>>("_allPlayerPositions");
		foreach (var pos in pp)
		{
			Debug.Log($"{pos.x}, {pos.y}, {pos.z}");
		}

		var classes = loader.Load<List<string>>("_allPlayerClasses");
		foreach (var c in classes)
		{
			Debug.Log(c);
		}
	}
}

[Nested, Serializable]
public class NeedNested
{
	[SaveThat, SerializeField] private int aaa = 0;
	[SaveThat, SerializeField] private Vector3 abb = Vector3.up;
	[SaveThat, SerializeField] private string abc = "aaaa";

	[SaveThat, SerializeField] private NeedNested2 _nnn = new NeedNested2();
	[SaveThat, SerializeField] private NeedNested2 _nnn2 = new NeedNested2();
	[SaveThat, SerializeField] private NeedNested2 _nnn3 = new NeedNested2();

	[SaveCallback]
	private void SaveCallback(ISaver saver)
	{
		Debug.Log("Test callbacks in Nested");
	}

	[LoadCallback]
	private void LoadCallback(ILoader saver)
	{
		Debug.Log("Test callbacks in Nested");
	}

}

[Nested, Serializable]
public class NeedNested2
{
	[SaveThat, SerializeField] private int aaa = 0;
	[SaveThat, SerializeField] private Vector3 abb = Vector3.up;
	[SaveThat, SerializeField] private string abc = "aaaa";
}

[Serializable]
public struct TestStruct
{
	public int a;
	public string b;
}

//Defines custom User type that I want to get serialized with Caravan
public class UserTypesMapper : IUserMapper
{
	public ITypeMapper FindUserTypeMapper(Type t, bool isAList)
	{
		dynamic mapper = null;

		if (t == typeof(TestStruct)) mapper = new AsIsMapper<TestStruct>();

		if (isAList && mapper != null)
			mapper = MapperHelpers.HandleLists(t, mapper);

		if (mapper != null) return NonGenericTypeMapper.CreateFrom(mapper);
		return null;
	}
}
#pragma warning restore CS0414