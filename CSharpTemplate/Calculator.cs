namespace CSharpTemplate;

/// <summary>
/// Basic arithmetic operations, used as a target for the example unit tests in <c>CSharpTemplate.Tests</c>.
/// </summary>
public static class Calculator
{
    /// <summary>Adds two numbers.</summary>
    /// <param name="a">The first number.</param>
    /// <param name="b">The second number.</param>
    /// <returns>The sum of <paramref name="a"/> and <paramref name="b"/>.</returns>
    public static int Add(int a, int b) => a + b;

    /// <summary>Divides one number by another.</summary>
    /// <param name="a">The dividend.</param>
    /// <param name="b">The divisor.</param>
    /// <returns>The result of dividing <paramref name="a"/> by <paramref name="b"/>.</returns>
    /// <exception cref="DivideByZeroException"><paramref name="b"/> is zero.</exception>
    public static int Divide(int a, int b)
    {
        if (b == 0)
        {
            throw new DivideByZeroException("Cannot divide by zero.");
        }

        return a / b;
    }

    /// <summary>Determines whether a number is even.</summary>
    /// <param name="a">The number to check.</param>
    /// <returns><see langword="true"/> if <paramref name="a"/> is even; otherwise, <see langword="false"/>.</returns>
    public static bool IsEven(int a) => a % 2 == 0;
}
