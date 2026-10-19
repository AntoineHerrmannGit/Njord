using Njord.Core.Extensions;

namespace Njord.Demo.Extensions;

internal static class Program
{
    #region Application

    private static void Main()
    {
        IEnumerable<int> closingPricesInCents = [18925, 19010, 18875, 19120];
        IEnumerable<int>? missingPrices = null;

        Console.WriteLine($"La série est vide : {closingPricesInCents.IsEmpty()}");
        Console.WriteLine($"La série contient des cours : {closingPricesInCents.IsNotEmpty()}");
        Console.WriteLine($"Aucune valeur négative : {closingPricesInCents.None(price => price < 0)}");
        Console.WriteLine($"La série facultative est absente ou vide : {missingPrices.IsNullOrEmpty()}");
    }

    #endregion
}
