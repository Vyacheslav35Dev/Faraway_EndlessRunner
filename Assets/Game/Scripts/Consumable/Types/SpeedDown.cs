using Cysharp.Threading.Tasks;
using Game.Scripts.Consumable;
using Game.Scripts.Consumable.Types;
using UnityEngine;
using CharacterController = Game.Scripts.Character.CharacterController;

public class SpeedDown : Consumable
{
    public int Value;
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
    }

    public override void Started(CharacterController controller)
    {
        base.Started(controller);
        controller.SetSpedDown(duration, Value).Forget();
    }

    public override void Ended(CharacterController controller)
    {
        base.Ended(controller);
    }
}
