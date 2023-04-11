[![](https://img.shields.io/nuget/v/Soenneker.Extensions.Enumerable.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Extensions.Enumerable/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.enumerable/publish.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.enumerable/actions/workflows/publish.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Extensions.Enumerable.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Extensions.Enumerable/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Extensions.Enumerable
### A collection of helpful enumerable extension methods

## Installation

```
Install-Package Soenneker.Extensions.Enumerable
```

## Usage

### `IEnumerable` should have `IsNullOrEmpty()` too

```csharp
var populatedList = new List<string>{"foo", "bar", "foo"};

populatedList.IsNullOrEmpty() // false

populatedList.Populated() // true
populatedList.None() // false
```

## One call checking for null and contains any elements

```csharp
List<string>? nullList = null;

nullList.IsNullOrEmpty() // true
nullList.Populated() // false
```

### Duplicate handling

```csharp
var containsDuplicates = populatedList.ContainsDuplicates(); // true

var deduped = populatedList.RemoveDuplicates(); // {"foo", "bar"}
```

### Recursive flattening

```csharp
public class Node 
{
    public string Name {get; set;}
    public List<Node> Children {get; set;}
}

void Example()
{
    var node = new Node(){ Name = "Node1" };
    node.Children = new List()
    {
        new Node() 
        {
            Name = "Node2"
        }
    }

    List<Node>? children = node.Children.ToFlattenedFromRecursive(c => c.Children);

    // Results in flattened List:
    // { Node1, Node2 }
}
```