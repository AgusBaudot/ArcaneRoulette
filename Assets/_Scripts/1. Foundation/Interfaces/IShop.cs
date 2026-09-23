using System.Collections.Generic;

namespace Foundation
{
    public interface IShop
    {
        IReadOnlyList<RuneDefinitionSO> StockRunes { get; }
        IReadOnlyList<bool> RunePurchasedState { get; }

        void MarkRunePurchased(int index);
        void GenerateStock(bool isReroll);
    }
}