namespace StateMaker.Tests;

public class FilterDefinitionLoaderTests
{
    #region Valid Definitions

    [Fact]
    public void LoadFromJson_SingleFilter_ReturnsDefinition()
    {
        var json = @"{
            ""filters"": [
                {
                    ""condition"": ""[Status] == 'Approved'"",
                    ""attributes"": { ""ranking"": ""high"" }
                }
            ]
        }";

        var definition = FilterDefinitionLoader.LoadFromJson(json);

        Assert.Single(definition.Filters);
        Assert.Equal("[Status] == 'Approved'", definition.Filters[0].Condition);
        Assert.Single(definition.Filters[0].Attributes);
        Assert.Equal("high", definition.Filters[0].Attributes["ranking"]);
    }

    [Fact]
    public void LoadFromJson_MultipleFilters_ReturnsAll()
    {
        var json = @"{
            ""filters"": [
                {
                    ""condition"": ""[Status] == 'Approved'"",
                    ""attributes"": { ""ranking"": ""high"" }
                },
                {
                    ""condition"": ""[IsComplete] == true"",
                    ""attributes"": { ""ranking"": ""low"" }
                }
            ]
        }";

        var definition = FilterDefinitionLoader.LoadFromJson(json);

        Assert.Equal(2, definition.Filters.Count);
        Assert.Equal("[Status] == 'Approved'", definition.Filters[0].Condition);
        Assert.Equal("[IsComplete] == true", definition.Filters[1].Condition);
    }

    [Fact]
    public void LoadFromJson_FilterWithoutAttributes_DefaultsToEmpty()
    {
        var json = @"{
            ""filters"": [
                {
                    ""condition"": ""[Status] == 'Approved'""
                }
            ]
        }";

        var definition = FilterDefinitionLoader.LoadFromJson(json);

        Assert.Single(definition.Filters);
        Assert.Empty(definition.Filters[0].Attributes);
    }

    [Fact]
    public void LoadFromJson_EmptyFiltersArray_ReturnsEmptyDefinition()
    {
        var json = @"{ ""filters"": [] }";

        var definition = FilterDefinitionLoader.LoadFromJson(json);

        Assert.Empty(definition.Filters);
    }

    [Fact]
    public void LoadFromJson_AttributesSupportMultipleTypes()
    {
        var json = @"{
            ""filters"": [
                {
                    ""condition"": ""true"",
                    ""attributes"": {
                        ""label"": ""important"",
                        ""priority"": 1,
                        ""flagged"": true,
                        ""score"": 9.5,
                        ""notes"": null
                    }
                }
            ]
        }";

        var definition = FilterDefinitionLoader.LoadFromJson(json);

        var attrs = definition.Filters[0].Attributes;
        Assert.Equal(5, attrs.Count);
        Assert.Equal("important", attrs["label"]);
        Assert.Equal(1, attrs["priority"]);
        Assert.Equal(true, attrs["flagged"]);
        Assert.Equal(9.5, attrs["score"]);
        Assert.Null(attrs["notes"]);
    }

    #endregion

    #region Validation and Errors

    [Fact]
    public void LoadFromJson_InvalidJson_ThrowsJsonParseException()
    {
        Assert.Throws<JsonParseException>(() =>
            FilterDefinitionLoader.LoadFromJson("not valid json {{{"));
    }

    [Fact]
    public void LoadFromJson_MissingFiltersArray_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            FilterDefinitionLoader.LoadFromJson(@"{ ""other"": 1 }"));
        Assert.Contains("filters", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromJson_FilterMissingCondition_Throws()
    {
        var json = @"{
            ""filters"": [
                {
                    ""attributes"": { ""ranking"": ""high"" }
                }
            ]
        }";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            FilterDefinitionLoader.LoadFromJson(json));
        Assert.Contains("condition", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromJson_NullJson_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilterDefinitionLoader.LoadFromJson(null!));
    }

    #endregion

    #region File Loading

    [Fact]
    public void LoadFromFile_ValidFile_LoadsDefinition()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, @"{
                ""filters"": [
                    {
                        ""condition"": ""[x] > 5"",
                        ""attributes"": { ""category"": ""high"" }
                    }
                ]
            }");

            var definition = FilterDefinitionLoader.LoadFromFile(tempFile);

            Assert.Single(definition.Filters);
            Assert.Equal("[x] > 5", definition.Filters[0].Condition);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadFromFile_FileNotFound_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            FilterDefinitionLoader.LoadFromFile("/nonexistent/path/filter.json"));
    }

    #endregion
}
