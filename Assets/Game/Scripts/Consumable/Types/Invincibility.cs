using Cysharp.Threading.Tasks;
using Game.Scripts.Consumable;
using Game.Scripts.Consumable.Types;
using UnityEngine;
using CharacterController = Game.Scripts.Character.CharacterController;

public class Invincibility : Consumable
{
    public override string GetConsumableName()
    {
        return "Invincible";
    }

    public override ConsumableType GetConsumableType()
    {
        return ConsumableType.INVINCIBILITY;
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
        controller.characterCollider.SetInvincibleExplicit(true);
    }

    public override void Started(CharacterController controller)
    {
        base.Started(controller);
        controller.SetInvincible(duration).Forget();
    }

    public override void Ended(CharacterController controller)
    {
        base.Ended(controller);
        controller.characterCollider.SetInvincibleExplicit(false);
    }
}
