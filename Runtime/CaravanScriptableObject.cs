using System;
using NaughtyAttributes;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CaravanSerialization
{
	[RequireSavedInInheritors]
	public abstract class CaravanScriptableObject : ScriptableObject
	{
        [CaravanId, SerializeField, ReadOnly] private string _id;

        [SaveCallback]
		protected abstract void SaveCallback(ISaver saver);

		[LoadCallback]
		protected abstract void LoadCallback(ILoader loader);

//TODO use partial class ?
#if UNITY_EDITOR
		[Button]
		private void GenerateUUID()
		{
			if(!string.IsNullOrEmpty(_id))
            {
				if(!EditorUtility.DisplayDialog("Warning", "You have already an ID !\nThis will most likely break the saves !", "k", "Cancel"))
                {
					return;
                }
            }
			_id = Guid.NewGuid().ToString();
		}
#endif
	}
}
