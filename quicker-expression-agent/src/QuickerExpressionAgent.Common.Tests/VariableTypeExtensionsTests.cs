using System;
using System.Collections.Generic;
using System.Text.Json;
using QuickerExpressionAgent.Common;
using Xunit;

namespace QuickerExpressionAgent.Common.Tests;

/// <summary>
/// Tests for VariableTypeExtensions conversion methods
/// </summary>
public class VariableTypeExtensionsTests
{
    #region ConvertValueToString Tests

    [Theory]
    [InlineData(VariableType.String, "test string", "test string")]
    [InlineData(VariableType.Int, 42, "42")]
    [InlineData(VariableType.Double, 3.14, "3.14")]
    [InlineData(VariableType.Bool, true, "true")]
    [InlineData(VariableType.Bool, false, "false")]
    public void ConvertValueToString_BasicTypes_ReturnsExpectedString(VariableType varType, object value, string expected)
    {
        // Act
        var result = varType.ConvertValueToString(value);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertValueToString_DateTime_ReturnsString()
    {
        // Arrange
        var value = new DateTime(2024, 1, 1, 12, 0, 0);

        // Act
        var result = VariableType.DateTime.ConvertValueToString(value);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("2024", result);
    }

    [Fact]
    public void ConvertValueToString_ListString_ReturnsNewlineSeparated()
    {
        // Arrange
        var value = new List<string> { "item1", "item2", "item3" };

        // Act
        var result = VariableType.ListString.ConvertValueToString(value);

        // Assert
        Assert.Equal("item1\nitem2\nitem3", result);
    }

    [Fact]
    public void ConvertValueToString_Dictionary_ReturnsJson()
    {
        // Arrange
        var value = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 42 }
        };

        // Act
        var result = VariableType.Dictionary.ConvertValueToString(value);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("key1", result);
        Assert.Contains("value1", result);
        Assert.Contains("key2", result);
    }

    [Fact]
    public void ConvertValueToString_Null_ReturnsEmpty()
    {
        // Act
        var result = VariableType.String.ConvertValueToString(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("42", VariableType.Int, "42")]
    [InlineData("\"test\"", VariableType.String, "test")]
    [InlineData("true", VariableType.Bool, "true")]
    [InlineData("3.14", VariableType.Double, "3.14")]
    public void ConvertValueToString_JsonElement_ConvertsCorrectly(string json, VariableType varType, string expected)
    {
        // Arrange
        var jsonDoc = JsonDocument.Parse(json);
        var jsonElement = jsonDoc.RootElement;

        // Act
        var result = varType.ConvertValueToString(jsonElement);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region ConvertValueFromString Tests

    [Theory]
    [InlineData(VariableType.String, "test", "test")]
    [InlineData(VariableType.Int, "42", 42)]
    [InlineData(VariableType.Double, "3.14", 3.14)]
    [InlineData(VariableType.Bool, "true", true)]
    [InlineData(VariableType.Bool, "false", false)]
    public void ConvertValueFromString_BasicTypes_ReturnsExpectedValue(VariableType varType, string input, object expected)
    {
        // Act
        var result = varType.ConvertValueFromString(input);

        // Assert
        if (expected is double expectedDouble)
        {
            Assert.Equal(expectedDouble, (double)result, 2);
        }
        else
        {
            Assert.Equal(expected, result);
        }
    }

    [Fact]
    public void ConvertValueFromString_DateTime_ReturnsDateTime()
    {
        // Arrange
        var dateStr = "2024-01-01 12:00:00";

        // Act
        var result = VariableType.DateTime.ConvertValueFromString(dateStr);

        // Assert
        Assert.IsType<DateTime>(result);
    }

    [Theory]
    [InlineData("[\"item1\", \"item2\", \"item3\"]", 3, "item1", "item2", "item3")]
    [InlineData("item1\nitem2\nitem3", 3, null, null, null)] // Newline format, just check count
    public void ConvertValueFromString_ListString_ReturnsList(string input, int expectedCount, string? item1, string? item2, string? item3)
    {
        // Act
        var result = VariableType.ListString.ConvertValueFromString(input);

        // Assert
        var list = Assert.IsType<List<string>>(result);
        Assert.Equal(expectedCount, list.Count);
        
        if (item1 != null)
        {
            Assert.Equal(item1, list[0]);
            Assert.Equal(item2, list[1]);
            Assert.Equal(item3, list[2]);
        }
    }

    [Fact]
    public void ConvertValueFromString_Dictionary_JsonObject_ReturnsDictionary()
    {
        // Arrange
        var jsonObject = "{\"key1\":\"value1\",\"key2\":42}";

        // Act
        var result = VariableType.Dictionary.ConvertValueFromString(jsonObject);

        // Assert
        var dict = Assert.IsType<Dictionary<string, object>>(result);
        Assert.Equal(2, dict.Count);
        Assert.Equal("value1", dict["key1"]);
    }

    [Fact]
    public void ConvertValueFromString_Null_ReturnsDefault()
    {
        // Act
        var result = VariableType.String.ConvertValueFromString(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region ConvertValueFromJson Tests

    [Theory]
    [InlineData("42", VariableType.Int, 42)]
    [InlineData("3.14", VariableType.Double, 3.14)]
    [InlineData("true", VariableType.Bool, true)]
    [InlineData("false", VariableType.Bool, false)]
    [InlineData("\"test\"", VariableType.String, "test")]
    public void ConvertValueFromJson_BasicTypes_ReturnsExpectedValue(string json, VariableType varType, object expected)
    {
        // Arrange
        var jsonDoc = JsonDocument.Parse(json);
        var jsonElement = jsonDoc.RootElement;

        // Act
        var result = varType.ConvertValueFromJson(jsonElement);

        // Assert
        if (expected is double expectedDouble)
        {
            Assert.Equal(expectedDouble, (double)result, 2);
        }
        else
        {
            Assert.Equal(expected, result);
        }
    }

    [Fact]
    public void ConvertValueFromJson_ListString_ReturnsList()
    {
        // Arrange
        var json = JsonDocument.Parse("[\"item1\", \"item2\"]");
        var jsonElement = json.RootElement;

        // Act
        var result = VariableType.ListString.ConvertValueFromJson(jsonElement);

        // Assert
        var list = Assert.IsType<List<string>>(result);
        Assert.Equal(2, list.Count);
        Assert.Equal("item1", list[0]);
        Assert.Equal("item2", list[1]);
    }

    [Fact]
    public void ConvertValueFromJson_Dictionary_ReturnsDictionary()
    {
        // Arrange
        var json = JsonDocument.Parse("{\"key1\":\"value1\",\"key2\":42}");
        var jsonElement = json.RootElement;

        // Act
        var result = VariableType.Dictionary.ConvertValueFromJson(jsonElement);

        // Assert
        var dict = Assert.IsType<Dictionary<string, object>>(result);
        Assert.Equal(2, dict.Count);
        Assert.Equal("value1", dict["key1"]);
    }

    #endregion

    #region ConvertToVariableType Tests

    [Theory]
    [InlineData(typeof(string), VariableType.String)]
    [InlineData(typeof(int), VariableType.Int)]
    [InlineData(typeof(double), VariableType.Double)]
    [InlineData(typeof(bool), VariableType.Bool)]
    [InlineData(typeof(DateTime), VariableType.DateTime)]
    public void ConvertToVariableType_BasicTypes_ReturnsExpectedType(Type type, VariableType expected)
    {
        // Act
        var result = type.ConvertToVariableType();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertToVariableType_ListString_ReturnsListString()
    {
        // Act
        var result = typeof(List<string>).ConvertToVariableType();

        // Assert
        Assert.Equal(VariableType.ListString, result);
    }

    [Fact]
    public void ConvertToVariableType_Dictionary_ReturnsDictionary()
    {
        // Act
        var result = typeof(Dictionary<string, object>).ConvertToVariableType();

        // Assert
        Assert.Equal(VariableType.Dictionary, result);
    }

    [Fact]
    public void ConvertToVariableType_Null_ReturnsObject()
    {
        // Act
        var result = ((Type?)null).ConvertToVariableType();

        // Assert
        Assert.Equal(VariableType.Object, result);
    }

    #endregion

    #region FromTypeName Tests

    [Theory]
    [InlineData("string", VariableType.String)]
    [InlineData("int", VariableType.Int)]
    [InlineData("System.String", VariableType.String)]
    [InlineData("System.Int32", VariableType.Int)]
    public void FromTypeName_BasicTypes_ReturnsExpectedType(string typeName, VariableType expected)
    {
        // Act
        var result = VariableTypeExtensions.FromTypeName(typeName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("List<string>", VariableType.ListString)]
    [InlineData("List`1[System.String]", VariableType.ListString)]
    [InlineData("System.Collections.Generic.List`1[System.String]", VariableType.ListString)]
    public void FromTypeName_ListTypes_ReturnsListString(string typeName, VariableType expected)
    {
        // Act
        var result = VariableTypeExtensions.FromTypeName(typeName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Dictionary<string, object>", VariableType.Dictionary)]
    [InlineData("Dictionary`2[System.String,System.Object]", VariableType.Dictionary)]
    [InlineData("System.Collections.Generic.Dictionary`2[System.String,System.Object]", VariableType.Dictionary)]
    public void FromTypeName_DictionaryTypes_ReturnsDictionary(string typeName, VariableType expected)
    {
        // Act
        var result = VariableTypeExtensions.FromTypeName(typeName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromTypeName_Null_ReturnsObject()
    {
        // Act
        var result = VariableTypeExtensions.FromTypeName(null);

        // Assert
        Assert.Equal(VariableType.Object, result);
    }

    #endregion

    #region GetDefaultValue Tests

    [Theory]
    [InlineData(VariableType.String, "")]
    [InlineData(VariableType.Int, 0)]
    [InlineData(VariableType.Double, 0.0)]
    [InlineData(VariableType.Bool, false)]
    public void GetDefaultValue_BasicTypes_ReturnsExpectedDefault(VariableType varType, object expected)
    {
        // Act
        var result = varType.GetDefaultValue();

        // Assert
        if (expected is double expectedDouble)
        {
            Assert.Equal(expectedDouble, (double)result, 2);
        }
        else
        {
            Assert.Equal(expected, result);
        }
    }

    [Fact]
    public void GetDefaultValue_ListString_ReturnsEmptyList()
    {
        // Act
        var result = VariableType.ListString.GetDefaultValue();

        // Assert
        var list = Assert.IsType<List<string>>(result);
        Assert.Empty(list);
    }

    [Fact]
    public void GetDefaultValue_Dictionary_ReturnsEmptyDictionary()
    {
        // Act
        var result = VariableType.Dictionary.GetDefaultValue();

        // Assert
        var dict = Assert.IsType<Dictionary<string, object>>(result);
        Assert.Empty(dict);
    }

    [Theory]
    [InlineData(VariableType.Int, "42", 42)]
    [InlineData(VariableType.String, "test", "test")]
    [InlineData(VariableType.Bool, "true", true)]
    public void GetDefaultValue_WithProvidedValue_ReturnsParsedValue(VariableType varType, string providedValue, object expected)
    {
        // Act
        var result = varType.GetDefaultValue(providedValue);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetDefaultValue_WithNullProvidedValue_ReturnsDefault()
    {
        // Act
        var result = VariableType.Int.GetDefaultValue(null);

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region GetTypeDeclaration Tests

    [Theory]
    [InlineData(VariableType.String, "string")]
    [InlineData(VariableType.Int, "int")]
    [InlineData(VariableType.Double, "double")]
    [InlineData(VariableType.Bool, "bool")]
    [InlineData(VariableType.DateTime, "DateTime")]
    [InlineData(VariableType.ListString, "List<string>")]
    [InlineData(VariableType.Dictionary, "Dictionary<string, object>")]
    [InlineData(VariableType.Object, "object")]
    public void GetTypeDeclaration_AllTypes_ReturnsExpectedDeclaration(VariableType varType, string expected)
    {
        // Act
        var result = varType.GetTypeDeclaration();

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion
}
