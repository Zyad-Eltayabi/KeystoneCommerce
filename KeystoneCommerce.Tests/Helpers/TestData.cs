namespace KeystoneCommerce.Tests.Helpers;

public class TestData
{
    public static IEnumerable<object[]> InvalidStrings =>
     new[]
     {
            new object[] { null },
            new object[] { "" },
            new object[] { "   " }
     };
}