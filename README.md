This repository attempts to show a bug in `Microsoft.AspNetCore.OData` Version `9.0.0` (and 8.0.0).

### Context
We have an Open EDM type, which means we can add custom properties without defining them beforehand.
We want to perform OData queries on the known and unknown properties.

It seems that the OData queries fail on unknown "complex type" elements.

I have defined the following class:

## Set-up

```cs
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
```

## Outcome

The following OData queries work:

```
knownString eq 'test'
knownComplexTypeArray/any(x: x/fileName in ('test.txt', 'test2.txt'))
unknownString eq 'test'
unknownInt eq 123
unknownDateTime eq 2024-01-01T00:00:00Z
knownString in ('a', 'b', 'c')
knownInt in (1, 2, 3)
```

The following queries do not work, and throw exceptions:

```
unknownComplexTypeArray/any(x: x/fileName eq 'test.txt')
```
<details>
<summary>System.ArgumentNullException: Value cannot be null. (Parameter 'edmType')</summary>

```
   at Microsoft.AspNetCore.OData.Edm.IODataTypeMapperExtensions.GetClrType(IODataTypeMapper mapper, IEdmModel edmModel, IEdmTypeReference edmType, IAssemblyResolver assembliesResolver)
   at Microsoft.AspNetCore.OData.Edm.EdmClrTypeMapExtensions.GetClrType(IEdmModel edmModel, IEdmTypeReference edmTypeReference, IAssemblyResolver assembliesResolver)
   at Microsoft.AspNetCore.OData.Query.Expressions.QueryBinderContext.HandleLambdaParameters(IEnumerable`1 rangeVariables)
   at Microsoft.AspNetCore.OData.Query.Expressions.QueryBinder.BindAnyNode(AnyNode anyNode, QueryBinderContext context)
   at Microsoft.AspNetCore.OData.Query.Expressions.QueryBinder.BindSingleValueNode(SingleValueNode node, QueryBinderContext context)
   at Microsoft.AspNetCore.OData.Query.Expressions.QueryBinder.Bind(QueryNode node, QueryBinderContext context)
   at Microsoft.AspNetCore.OData.Query.Expressions.FilterBinder.BindFilter(FilterClause filterClause, QueryBinderContext context)
   at Microsoft.AspNetCore.OData.Query.Expressions.BinderExtensions.ApplyBind(IFilterBinder binder, IQueryable query, FilterClause filterClause, QueryBinderContext context)
   at Microsoft.AspNetCore.OData.Query.FilterQueryOption.ApplyTo(IQueryable query, ODataQuerySettings querySettings)
   at Program.<<Main>$>g__CreateQuery|0_0[TModel](ODataQueryContext oDataContext, String rule) in C:\temp\ODataOpenTypeRepro\ODataOpenTypeRepro\Program.cs:line 65
   at Program.<Main>$(String[] args) in C:\temp\ODataOpenTypeRepro\ODataOpenTypeRepro\Program.cs:line 42
```
</details>

```
unknownString in ('a', 'b', 'c')
```
<details>
<summary>System.NullReferenceException: Object reference not set to an instance of an object.</summary>

```
   at Microsoft.AspNetCore.OData.Query.Expressions.QueryBinder.BindCollectionConstantNode(CollectionConstantNode node, QueryBinderContext context)
   at Microsoft.AspNetCore.OData.Query.Expressions.QueryBinder.BindCollectionNode(CollectionNode node, QueryBinderContext context)
   at Microsoft.AspNetCore.OData.Query.Expressions.QueryBinder.Bind(QueryNode node, QueryBinderContext context)
   at Microsoft.AspNetCore.OData.Query.Expressions.QueryBinder.BindInNode(InNode inNode, QueryBinderContext context)
   at Microsoft.AspNetCore.OData.Query.Expressions.QueryBinder.BindSingleValueNode(SingleValueNode node, QueryBinderContext context)
   at Microsoft.AspNetCore.OData.Query.Expressions.QueryBinder.Bind(QueryNode node, QueryBinderContext context)
   at Microsoft.AspNetCore.OData.Query.Expressions.FilterBinder.BindFilter(FilterClause filterClause, QueryBinderContext context)
   at Microsoft.AspNetCore.OData.Query.Expressions.BinderExtensions.ApplyBind(IFilterBinder binder, IQueryable query, FilterClause filterClause, QueryBinderContext context)
   at Microsoft.AspNetCore.OData.Query.FilterQueryOption.ApplyTo(IQueryable query, ODataQuerySettings querySettings)
   at Program.<<Main>$>g__CreateQuery|0_0[TModel](ODataQueryContext oDataContext, String rule)
   at Program.<Main>$(String[] args)
```
</details>

```
unknownInt in (1, 2, 3)
```
<details>
<summary>Microsoft.OData.ODataException: String item should be single/double quoted: '1'.</summary>

```
   at Microsoft.OData.UriParser.InBinder.NormalizeStringCollectionItems(String literalText)
   at Microsoft.OData.UriParser.InBinder.GetCollectionOperandFromToken(QueryToken queryToken, IEdmTypeReference expectedType, IEdmModel model)
   at Microsoft.OData.UriParser.InBinder.BindInOperator(InToken inToken, BindingState state)
   at Microsoft.OData.UriParser.MetadataBinder.BindIn(InToken inToken)
   at Microsoft.OData.UriParser.MetadataBinder.Bind(QueryToken token)
   at Microsoft.OData.UriParser.FilterBinder.BindFilter(QueryToken filter)
   at Microsoft.OData.UriParser.ODataQueryOptionParser.ParseFilterImplementation(String filter, ODataUriParserConfiguration configuration, ODataPathInfo odataPathInfo)
   at Microsoft.OData.UriParser.ODataQueryOptionParser.ParseFilter()
   at Microsoft.AspNetCore.OData.Query.FilterQueryOption.get_FilterClause()
   at Microsoft.AspNetCore.OData.Query.FilterQueryOption.ApplyTo(IQueryable query, ODataQuerySettings querySettings)
   at Program.<<Main>$>g__CreateQuery|0_0[TModel](ODataQueryContext oDataContext, String rule)
   at Program.<Main>$(String[] args)
```
</details>
