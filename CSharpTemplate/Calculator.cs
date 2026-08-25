namespace CSharpTemplate;

public static class Calculator
{
    public static int Add(int a, int b) => a + b;

    public static int Divide(int a, int b)
    {
        if (b == 0)
        {
            throw new DivideByZeroException("Cannot divide by zero.");
        }

        return a / b;
    }

    public static bool IsEven(int a) => a % 2 == 0;
}
