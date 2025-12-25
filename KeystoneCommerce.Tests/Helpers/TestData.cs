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

    public static IEnumerable<object[]> WhiteSpaceTestData =>
     new[]
     {
            new object[] { "   " },
            new object[] {"\t"},
            new object[] { "\n" }
     };
}