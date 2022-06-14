# Caravan
Unity quick and dirty serialization. 

- Saves Scriptable Objects and allow to restore them back
- Uses mostly C# attributes + custom classes to inherit from (as Helpers)
- Supports nested objects (tagged as [Nested])
- You can also define custom mappers for complex types
- You can use Callback to define custom saving/loading strategies for certains variable (eg : dumping/loading gameobjects) 
- Events to know when the save/load finished
- Should play along pretty well with [DItan, A lite DI framework for Unity](https://github.com/ReyAnthony/DItan).


Dependencies :
- JSON.Net (Meaning the saves will be in JSON format)
	- I just wanted something without the complications of most serializations libs. Goal is to abstract the code enough to use any serialization backend (but i'm lazy eh, it will work for my games as-is) 
- [Naughty Attributes](https://github.com/dbrizov/NaughtyAttributes) (Used in some UIs, unless the project is using Odin Inspector)
	- Embeds .dll of the tool in editor mode unless ODIN_INSPECTOR is defined in the project.

Example usage : 
```
[Saved("GameManager")]
public class GameManager : CaravanScriptableObject
{
	[SaveThat, SerializeField] private string _string;
	[SaveThat] private Vector3 _vec3 = Vector3.back;
	[SaveThat, SerializeField] private NeedNested _needNested = new NeedNested();

	//You can do it explicitely
	protected override void SaveCallback(ISaver saver)
	{
		saver.Save<Vector3>(Vector3.back, "_AnotherVec");
		saver.Save(new List<Vector3> { Vector3.down, Vector3.up }, "_allPlayerPositions");
		saver.Save(new List<string> { "Ninja", "Berserk" }, "_allPlayerClasses");
	}

	protected override void LoadCallback(ILoader loader)
	{
		var v = loader.Load<Vector3>("_AnotherVec");
		var pp = loader.Load<List<Vector3>>("_allPlayerPositions");
		var classes = loader.Load<List<string>>("_allPlayerClasses");
	}
}

[Nested]
[Serializable]
public class NeedNested
{
	[SaveThat, SerializeField] private int anInt = 0;

	[SaveCallback]
	private void SaveCallback(ISaver saver)
	{

	}

	[LoadCallback]
	private void LoadCallback(ILoader saver)
	{

	}
}

```
