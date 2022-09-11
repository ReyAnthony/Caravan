using System;
using System.Collections.Generic;
using CaravanSerialization.Attributes;
using CaravanSerialization.ObjectModel;
using UnityEngine;

namespace CaravanSerialization.Demo.TypeChecks
{
    [Saved("BasicTypeCheck")]
    [CreateAssetMenu(menuName = "Caravan/Demo/Basic Type Check")]
    public class BasicTypeCheck : CaravanScriptableObject
    {
	    [SaveThat, SerializeField] private string _string = "str";
        [SaveThat, SerializeField] private bool _bool = false;
        [SaveThat, SerializeField] private char _char = 'c';
        
        [SaveThat, SerializeField] private int _int = 10;

        //Not handled for now
        //[SaveThat, SerializeField] private long _long = 5;
        //[SaveThat, SerializeField] private byte _byte = 0;

        //Boxed types
        [SaveThat, SerializeField] private Boolean _bb = false;

        //Lists
        [SaveThat, SerializeField] private List<string> _listString = new() { "a", "a", "a" };
        [SaveThat, SerializeField] private List<int> _listInt = new() { 1, 2, 3 };
        
        //Unity
        [SaveThat, SerializeField] private Vector3 _vector3 = Vector3.back;
        [SaveThat, SerializeField] private Quaternion _quaternion = Quaternion.identity;
        
        //Nested
        [SaveThat, SerializeField] private NestedTest _nestedTest = new NestedTest();
        [SaveThat, SerializeField] private List<NestedTest2> _nestedTest2 = new () {new NestedTest2()};
        
        
        protected override void SaveCallback(ISaver saver)
        {
            Debug.Log($"Test callbacks in {this.GetType().Name}");
        }

        protected override void LoadCallback(ILoader loader)
        {
            Debug.Log($"Test callbacks in {this.GetType().Name}");
        }
    }
    
    [Nested, Serializable]
    public class NestedTest
    {
        [SaveThat, SerializeField] private int _int = 0;
        [SaveThat, SerializeField] private NestedTest2 _nestedTest2 = new NestedTest2();
        
        [SaveCallback]
        private void SaveCallback(ISaver saver)
        {
            Debug.Log($"Test callbacks in {this.GetType().Name}");
        }

        [LoadCallback]
        private void LoadCallback(ILoader saver)
        {
            Debug.Log($"Test callbacks in {this.GetType().Name}");
        }

    }

    [Nested, Serializable]
    public class NestedTest2
    {
        [SaveThat, SerializeField] private int aaa = 0;
    }
}