using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;

var builder = new ODataConventionModelBuilder();
builder.EntitySet<MyModel>(nameof(MyModel));

var edmModel = builder.GetEdmModel();
var oDataContext = new ODataQueryContext(edmModel, typeof(MyModel), new ODataPath());


string[] workingQueryParts = [
    "knownString eq 'test'",
    "knownComplexTypeArray/any(x: x/fileName in ('test.txt', 'test2.txt'))",
    "unknownString eq 'test'",
    "unknownInt eq 123",
    "unknownDateTime eq 2024-01-01T00:00:00Z",
    "knownString in ('a', 'b', 'c')",
    "knownInt in (1, 2, 3)",
];

string[] notWorkingQueryParts = [
    "unknownComplexTypeArray/any(x: x/fileName eq 'test.txt')",
    "unknownString IN ('a, 'b', 'c')",
    "unknownInt in (1, 2, 3)"
];

// The working query parts succesfully compile
CreateQuery<MyModel>(oDataContext, string.Join(" and ", workingQueryParts));


// The not working query parts throw exceptions
foreach (var notWorkingQueryPart in notWorkingQueryParts)
{
    try
    {
        CreateQuery<MyModel>(oDataContext, notWorkingQueryPart);
        Debug.Fail("This should not happen");
    }
    catch (ArgumentNullException e)
    {
    }
    catch (ODataException e)
    {
    }
}


return;

void CreateQuery<TModel>(ODataQueryContext oDataContext, string rule)
{
    var queryOptionParser = new ODataQueryOptionParser(oDataContext.Model, oDataContext.ElementType,
        oDataContext.NavigationSource,
        new Dictionary<string, string> { { "$filter", rule } });

    var filter = new FilterQueryOption(rule, oDataContext, queryOptionParser);

    var baseQueryable = new List<TModel>().AsQueryable();
    filter.ApplyTo(baseQueryable, new ODataQuerySettings());
}

[DataContract]
public class MyModel : Dictionary<string, object>
{
    [Key]
    [DataMember(Name = "id")]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [DataMember(Name = "knownString")]
    [JsonPropertyName("knownString")]
    public string KnownString { get; set; } = null!;

    [Required]
    [DataMember(Name = "knownInt")]
    [JsonPropertyName("knownInt")]
    public int KnownInt { get; set; }

    [DataMember(Name = "knownComplexTypeArray")]
    [JsonPropertyName("knownComplexTypeArray")]
    public KnownComplexType[] KnownComplexTypeArray { get; set; } = null!;

    [JsonExtensionData]
    public Dictionary<string, object> OpenProperties => this;
}

[DataContract]
public class KnownComplexType
{
    [DataMember(Name = "fileName")]
    public string FileName { get; set; } = null!;
}
