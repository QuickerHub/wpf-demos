using Microsoft.SemanticKernel;

namespace QuickerExpressionAgent.Demo.Plugins;

/// <summary>
/// Simple calculator plugin for demonstration
/// </summary>
public class CalculatorPlugin
{
    [KernelFunction]
    [System.ComponentModel.Description("Add two numbers")]
    public double Add(
        [System.ComponentModel.Description("First number")] double a,
        [System.ComponentModel.Description("Second number")] double b)
    {
        return a + b;
    }
    
    [KernelFunction]
    [System.ComponentModel.Description("Subtract two numbers")]
    public double Subtract(
        [System.ComponentModel.Description("First number")] double a,
        [System.ComponentModel.Description("Second number")] double b)
    {
        return a - b;
    }
    
    [KernelFunction]
    [System.ComponentModel.Description("Multiply two numbers")]
    public double Multiply(
        [System.ComponentModel.Description("First number")] double a,
        [System.ComponentModel.Description("Second number")] double b)
    {
        return a * b;
    }
    
    [KernelFunction]
    [System.ComponentModel.Description("Divide two numbers")]
    public double Divide(
        [System.ComponentModel.Description("Dividend")] double a,
        [System.ComponentModel.Description("Divisor")] double b)
    {
        if (b == 0)
        {
            throw new DivideByZeroException("Cannot divide by zero");
        }
        return a / b;
    }
}

