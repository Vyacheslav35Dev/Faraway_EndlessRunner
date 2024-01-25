using UnityEngine;
using System.Collections.Generic;
using Game.Scripts.Consumable;
using Game.Scripts.Consumable.Types;

[CreateAssetMenu(fileName="Consumables", menuName = "FarawayRunner/Consumables Database")]
public class ConsumableDatabase : ScriptableObject
{
    public Consumable[] consumbales;

    private Dictionary<ConsumableType, Consumable> _consumablesDict;

    public void Load()
    {
        if (_consumablesDict == null)
        {
            _consumablesDict = new Dictionary<ConsumableType, Consumable>();

            for (int i = 0; i < consumbales.Length; ++i)
            {
                _consumablesDict.Add(consumbales[i].GetConsumableType(), consumbales[i]);
            }
        }
    }

    public Consumable GetConsumbale(ConsumableType type)
    {
        Consumable c;
        return _consumablesDict.TryGetValue (type, out c) ? c : null;
    }
}
