using QuickerExpressionAgent.Common;

namespace QuickerExpressionAgent.Server.Services.VariableTypeFormatters;

/// <summary>
/// Factory for creating variable type formatters
/// </summary>
public class VariableTypeFormatterFactory
{
    private readonly Dictionary<VariableType, IVariableTypeFormatter> _formatters;

    public VariableTypeFormatterFactory()
    {
        _formatters = new Dictionary<VariableType, IVariableTypeFormatter>
        {
            { VariableType.String, new StringVariableFormatter() },
            { VariableType.Int, new IntVariableFormatter() },
            { VariableType.Double, new DoubleVariableFormatter() },
            { VariableType.Bool, new BoolVariableFormatter() },
            { VariableType.DateTime, new DateTimeVariableFormatter() },
            { VariableType.ListString, new ListStringVariableFormatter() },
            { VariableType.Object, new ObjectVariableFormatter() }
        };

        // Dictionary formatter needs factory reference for nested formatting
        _formatters[VariableType.Dictionary] = new DictionaryVariableFormatter(this);
    }

    /// <summary>
    /// Get formatter for a specific variable type
    /// </summary>
    public IVariableTypeFormatter GetFormatter(VariableType variableType)
    {
        return _formatters.TryGetValue(variableType, out var formatter)
            ? formatter
            : _formatters[VariableType.Object];
    }

    /// <summary>
    /// Get formatter for a value (infer type from value)
    /// </summary>
    public IVariableTypeFormatter GetFormatterForValue(object? value)
    {
        if (value == null)
        {
            return _formatters[VariableType.Object];
        }

        return value switch
        {
            string => _formatters[VariableType.String],
            int or long or short or byte => _formatters[VariableType.Int],
            double or float or decimal => _formatters[VariableType.Double],
            bool => _formatters[VariableType.Bool],
            DateTime => _formatters[VariableType.DateTime],
            System.Collections.IDictionary => _formatters[VariableType.Dictionary],
            System.Collections.IEnumerable => _formatters[VariableType.ListString],
            _ => _formatters[VariableType.Object]
        };
    }
}

