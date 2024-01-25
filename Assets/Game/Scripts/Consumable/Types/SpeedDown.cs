using Cysharp.Threading.Tasks;
using Game.Scripts.Consumable;
using Game.Scripts.Consumable.Types;
using UnityEngine;
using CharacterController = Game.Scripts.Character.CharacterController;

public class SpeedDown : Consumable
{
    public override string GetConsumableName()
    {
        return "SpeedDown";
    }

    public override ConsumableType GetConsumableType()
    {
        return ConsumableType.SPEED_DOWN;
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
        Debug.Log("SpeedDown::Tick");
    }

    public override void Started(CharacterController controller)
    {
        base.Started(controller);
        Debug.Log("SpeedDown::Started");
    }

    public override void Ended(CharacterController controller)
    {
        base.Ended(controller);
        Debug.Log("SpeedDown::Ended");
    }
}
