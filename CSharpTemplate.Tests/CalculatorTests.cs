using Xunit.Abstractions;

namespace CSharpTemplate.Tests;

// [Fact]: a test that takes no parameters and checks a single case.
public class CalculatorTests
{
    [Fact]
    public void Add_ReturnsSumOfTwoPositiveNumbers()
    {
        int result = Calculator.Add(1, 2);

        Assert.Equal(3, result);
    }

    // [Theory] + [InlineData]: same test body run once per data row, values supplied inline.
    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(-1, 1, 0)]
    [InlineData(0, 0, 0)]
    public void Add_ReturnsSumOfTwoNumbers(int a, int b, int expected)
    {
        int result = Calculator.Add(a, b);

        Assert.Equal(expected, result);
    }

    // [Theory] + [MemberData]: data comes from a property/method on this class (or another, via MemberType).
    [Theory]
    [MemberData(nameof(DivisionCases))]
    public void Divide_ReturnsQuotient(int a, int b, int expected)
    {
        int result = Calculator.Divide(a, b);

        Assert.Equal(expected, result);
    }

    public static TheoryData<int, int, int> DivisionCases =>
        new()
        {
            { 10, 2, 5 },
            { 9, 3, 3 },
            { 5, 2, 2 },
        };

    // [Theory] + [ClassData]: data comes from a separate class implementing IEnumerable<object[]>.
    // Useful when the data set is large or reused across multiple test classes.
    [Theory]
    [ClassData(typeof(EvenNumberTestData))]
    public void IsEven_IdentifiesEvenNumbers(int value, bool expected)
    {
        bool result = Calculator.IsEven(value);

        Assert.Equal(expected, result);
    }

    // Assert.Throws: asserting that an operation raises a specific exception.
    [Fact]
    public void Divide_ByZero_ThrowsDivideByZeroException()
    {
        Assert.Throws<DivideByZeroException>(() => Calculator.Divide(1, 0));
    }

    // [Trait]: attaches free-form metadata (category, owner, ticket id, ...) usable to filter
    // test runs, e.g. `dotnet test --filter Category=Arithmetic`.
    [Trait("Category", "Arithmetic")]
    [Fact]
    public void Add_IsCommutative()
    {
        Assert.Equal(Calculator.Add(2, 3), Calculator.Add(3, 2));
    }

    // Skip: temporarily disables a test without deleting it. Shows up as "skipped" in results.
    [Fact(Skip = "Example of a skipped test - remove Skip to enable it.")]
    public void Add_ExampleOfASkippedTest()
    {
        Assert.Equal(0, Calculator.Add(int.MaxValue, 1));
    }

    // Async test methods are supported directly - xUnit awaits the returned Task.
    [Fact]
    public async Task Add_WorksInAsyncContext()
    {
        int result = await Task.Run(() => Calculator.Add(4, 5));

        Assert.Equal(9, result);
    }
}

public class EvenNumberTestData : TheoryData<int, bool>
{
    public EvenNumberTestData()
    {
        Add(2, true);
        Add(3, false);
        Add(0, true);
    }
}

// xUnit creates a new instance of the test class for every test method, so the constructor
// (setup) and IDisposable.Dispose (teardown) run once per test - state is never shared between them.
public class SetupAndTeardownExampleTests : IDisposable
{
    private readonly int _initialValue;

    public SetupAndTeardownExampleTests()
    {
        _initialValue = 10;
    }

    public void Dispose()
    {
        // Release per-test resources here (files, connections, temp state, ...).
    }

    [Fact]
    public void Add_UsesValueFromConstructor()
    {
        int result = Calculator.Add(_initialValue, 5);

        Assert.Equal(15, result);
    }
}

// IClassFixture<T>: shares one instance of T across all tests in the class - use for
// expensive-to-create state (e.g. a database connection) that's safe to reuse.
// A collection fixture (ICollectionFixture<T>) extends this sharing across multiple classes.
public class CalculatorFixture
{
    public int SharedSeedValue { get; } = 100;
}

public class CalculatorFixtureTests(CalculatorFixture fixture) : IClassFixture<CalculatorFixture>
{
    [Fact]
    public void Add_UsesSharedFixtureValue()
    {
        int result = Calculator.Add(fixture.SharedSeedValue, 1);

        Assert.Equal(101, result);
    }
}

// ITestOutputHelper: writes diagnostic output associated with the running test
// (visible in `dotnet test` with -v n/detailed, and in most IDE test runners).
public class OutputExampleTests(ITestOutputHelper output)
{
    [Fact]
    public void Add_LogsInputsAndResult()
    {
        int a = 2, b = 3;
        int result = Calculator.Add(a, b);

        output.WriteLine($"Add({a}, {b}) = {result}");

        Assert.Equal(5, result);
    }
}
