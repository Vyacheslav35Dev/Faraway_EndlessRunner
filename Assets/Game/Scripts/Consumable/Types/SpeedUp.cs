using Cysharp.Threading.Tasks;
using Game.Scripts.Consumable;
using Game.Scripts.Consumable.Types;
using UnityEngine;
using CharacterController = Game.Scripts.Character.CharacterController;

public class SpeedUp : Consumable
{
    public override string GetConsumableName()
    {
        return "SpeedUp";
    }

    public override ConsumableType GetConsumableType()
    {
        return ConsumableType.SPEED_UP;
    }

    public override int GetPrice()
    {
        return 1500;
    }

	public override int GetPremiumCost()
	{
		return 5;
	}

	public override void Tick(CharacterController controller)
    {
        base.Tick(controller);
        Debug.Log("SpeedUp::Tick");
    }

    public override void Started(CharacterController controller)
    {
        base.Started(controller);
        Debug.Log("SpeedUp::Started");
    }

    public override void Ended(CharacterController controller)
    {
        base.Ended(controller);
        Debug.Log("SpeedUp::Ended");
    }
}
