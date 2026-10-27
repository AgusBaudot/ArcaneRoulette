namespace Foundation
{
	public interface IElementalResistance
	{
		Effectiveness GetEffectiveness(ElementType attackerElement);
	} 
}