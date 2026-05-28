// Copyright (c) 2025 - EcoSys
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using DigitaleDelta.Contracts;

namespace DigitaleDelta.CsdlParser.Tests;

public class CsdlFlattenerTests
{
    [Fact]
    public void FlattenEntityProperties_NullModel_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CsdlFlattener.FlattenEntityProperties(null!, "Product").ToList());
    }

    [Fact]
    public void FlattenEntityProperties_NullEntityTypeName_Throws()
    {
        var model = new CsdlModel();
        Assert.Throws<ArgumentNullException>(() =>
            CsdlFlattener.FlattenEntityProperties(model, null!).ToList());
    }

    [Fact]
    public void FlattenEntityProperties_EntityNotFound_ReturnsEmpty()
    {
        var model = new CsdlModel();
        var result = CsdlFlattener.FlattenEntityProperties(model, "DoesNotExist").ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void FlattenEntityProperties_PrimitivesOnly_ReturnsFlatList()
    {
        var model = new CsdlModel
        {
            EntityTypes =
            [
                new EntityType
                {
                    Name = "Product",
                    Properties =
                    [
                        new Property { Name = "Id",   Type = "Edm.Int32"  },
                        new Property { Name = "Name", Type = "Edm.String" }
                    ]
                }
            ]
        };

        var result = CsdlFlattener.FlattenEntityProperties(model, "Product").ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(("Id",   "Edm.Int32"),  result[0]);
        Assert.Equal(("Name", "Edm.String"), result[1]);
    }

    [Fact]
    public void FlattenEntityProperties_ComplexProperty_ExpandedWithSlashPath()
    {
        var model = new CsdlModel
        {
            EntityTypes =
            [
                new EntityType
                {
                    Name = "Person",
                    Properties =
                    [
                        new Property { Name = "Id",      Type = "Edm.Int32" },
                        new Property { Name = "Address", Type = "ODataDemo.Address" }
                    ]
                }
            ],
            ComplexTypes =
            [
                new ComplexType
                {
                    Name = "Address",
                    Properties =
                    [
                        new Property { Name = "Street", Type = "Edm.String" },
                        new Property { Name = "City",   Type = "Edm.String" }
                    ]
                }
            ]
        };

        var result = CsdlFlattener.FlattenEntityProperties(model, "Person").ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(("Id",             "Edm.Int32"),  result);
        Assert.Contains(("Address/Street", "Edm.String"), result);
        Assert.Contains(("Address/City",   "Edm.String"), result);
    }

    [Fact]
    public void FlattenEntityProperties_NestedComplexTypes_ProducesNestedPaths()
    {
        var model = new CsdlModel
        {
            EntityTypes =
            [
                new EntityType
                {
                    Name = "Person",
                    Properties =
                    [
                        new Property { Name = "Address", Type = "Address" }
                    ]
                }
            ],
            ComplexTypes =
            [
                new ComplexType
                {
                    Name = "Address",
                    Properties =
                    [
                        new Property { Name = "Street", Type = "Edm.String" },
                        new Property { Name = "Geo",    Type = "GeoPoint" }
                    ]
                },
                new ComplexType
                {
                    Name = "GeoPoint",
                    Properties =
                    [
                        new Property { Name = "Lat", Type = "Edm.Double" },
                        new Property { Name = "Lon", Type = "Edm.Double" }
                    ]
                }
            ]
        };

        var result = CsdlFlattener.FlattenEntityProperties(model, "Person").ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(("Address/Street",  "Edm.String"), result);
        Assert.Contains(("Address/Geo/Lat", "Edm.Double"), result);
        Assert.Contains(("Address/Geo/Lon", "Edm.Double"), result);
    }

    [Fact]
    public void FlattenEntityProperties_FilterByEdmType_ReturnsOnlyMatching()
    {
        var model = new CsdlModel
        {
            EntityTypes =
            [
                new EntityType
                {
                    Name = "Product",
                    Properties =
                    [
                        new Property { Name = "Id",    Type = "Edm.Int32"  },
                        new Property { Name = "Name",  Type = "Edm.String" },
                        new Property { Name = "Notes", Type = "Edm.String" }
                    ]
                }
            ]
        };

        var result = CsdlFlattener.FlattenEntityProperties(model, "Product", filterEdmType: "Edm.String").ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal("Edm.String", item.EdmType));
        Assert.Contains(("Name",  "Edm.String"), result);
        Assert.Contains(("Notes", "Edm.String"), result);
    }

    [Fact]
    public void FlattenEntityProperties_FilterByEdmType_NoMatches_ReturnsEmpty()
    {
        var model = new CsdlModel
        {
            EntityTypes =
            [
                new EntityType
                {
                    Name = "Product",
                    Properties = [new Property { Name = "Id", Type = "Edm.Int32" }]
                }
            ]
        };

        var result = CsdlFlattener.FlattenEntityProperties(model, "Product", filterEdmType: "Edm.Boolean").ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void FlattenEntityProperties_FilterCaseInsensitive_MatchesDifferentCasing()
    {
        var model = new CsdlModel
        {
            EntityTypes =
            [
                new EntityType
                {
                    Name = "Product",
                    Properties = [new Property { Name = "Name", Type = "Edm.String" }]
                }
            ]
        };

        var result = CsdlFlattener.FlattenEntityProperties(model, "Product", filterEdmType: "edm.string").ToList();
        Assert.Single(result);
    }

    [Fact]
    public void FlattenEntityProperties_ResolveByFullyQualifiedName()
    {
        var model = new CsdlModel
        {
            EntityTypes =
            [
                new EntityType
                {
                    Name = "Product",
                    Properties = [new Property { Name = "Id", Type = "Edm.Int32" }]
                }
            ]
        };

        var result = CsdlFlattener.FlattenEntityProperties(model, "ODataDemo.Product").ToList();

        Assert.Single(result);
        Assert.Equal(("Id", "Edm.Int32"), result[0]);
    }

    [Fact]
    public void FlattenEntityProperties_DefaultLookupIsCaseInsensitive()
    {
        var model = new CsdlModel
        {
            EntityTypes =
            [
                new EntityType
                {
                    Name = "Product",
                    Properties = [new Property { Name = "Id", Type = "Edm.Int32" }]
                }
            ]
        };

        var result = CsdlFlattener.FlattenEntityProperties(model, "product").ToList();
        Assert.Single(result);
    }

    [Fact]
    public void FlattenEntityProperties_OrdinalComparison_DoesNotMatchDifferentCasing()
    {
        var model = new CsdlModel
        {
            EntityTypes =
            [
                new EntityType
                {
                    Name = "Product",
                    Properties = [new Property { Name = "Id", Type = "Edm.Int32" }]
                }
            ]
        };

        var result = CsdlFlattener
            .FlattenEntityProperties(model, "product", nameComparison: StringComparison.Ordinal)
            .ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void FlattenEntityProperties_PropertyWithEmptyType_IsSkipped()
    {
        var model = new CsdlModel
        {
            EntityTypes =
            [
                new EntityType
                {
                    Name = "Product",
                    Properties =
                    [
                        new Property { Name = "Id",    Type = "Edm.Int32" },
                        new Property { Name = "Blank", Type = "   "       }
                    ]
                }
            ]
        };

        var result = CsdlFlattener.FlattenEntityProperties(model, "Product").ToList();

        Assert.Single(result);
        Assert.Equal("Id", result[0].Path);
    }

    [Fact]
    public void FlattenEntityProperties_UnresolvedComplexType_IsSkipped()
    {
        var model = new CsdlModel
        {
            EntityTypes =
            [
                new EntityType
                {
                    Name = "Product",
                    Properties =
                    [
                        new Property { Name = "Id",      Type = "Edm.Int32" },
                        new Property { Name = "Mystery", Type = "Ns.UnknownType" }
                    ]
                }
            ]
        };

        var result = CsdlFlattener.FlattenEntityProperties(model, "Product").ToList();

        Assert.Single(result);
        Assert.Equal(("Id", "Edm.Int32"), result[0]);
    }

    [Fact]
    public void FlattenEntityProperties_ComplexTypeResolvedByFullyQualifiedName()
    {
        var model = new CsdlModel
        {
            EntityTypes =
            [
                new EntityType
                {
                    Name = "Person",
                    Properties =
                    [
                        new Property { Name = "Address", Type = "ODataDemo.Address" }
                    ]
                }
            ],
            ComplexTypes =
            [
                new ComplexType
                {
                    Name = "Address",
                    Properties = [new Property { Name = "City", Type = "Edm.String" }]
                }
            ]
        };

        var result = CsdlFlattener.FlattenEntityProperties(model, "Person").ToList();

        Assert.Single(result);
        Assert.Equal(("Address/City", "Edm.String"), result[0]);
    }

    [Fact]
    public void FlattenEntityProperties_TypeWithWhitespace_IsTrimmedBeforeResolution()
    {
        var model = new CsdlModel
        {
            EntityTypes =
            [
                new EntityType
                {
                    Name = "Product",
                    Properties = [new Property { Name = "Id", Type = "  Edm.Int32  " }]
                }
            ]
        };

        var result = CsdlFlattener.FlattenEntityProperties(model, "Product").ToList();

        Assert.Single(result);
        Assert.Equal(("Id", "Edm.Int32"), result[0]);
    }
}
