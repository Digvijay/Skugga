# Argument Matchers

Match method arguments with flexible predicates, value sets, null checks, and regex patterns.

## It.IsAny\<T>()

Match any value of type `T`:

```csharp
mock.Setup(x => x.GetEmail(It.IsAny<int>())).Returns("default@test.com");
mock.GetEmail(1);   // Returns "default@test.com"
mock.GetEmail(999); // Returns "default@test.com"
```

## It.Is\<T>(predicate)

Match with a custom predicate:

```csharp
mock.Setup(x => x.Process(It.Is<int>(n => n > 0))).Returns("positive");
mock.Process(5);   // Returns "positive"
mock.Process(-1);  // Returns null (no match)
```

## It.IsIn(values)

Match values within a set:

```csharp
mock.Setup(x => x.Handle(It.IsIn("red", "green", "blue"))).Returns("color");
mock.Handle("red");     // Returns "color"
mock.Handle("yellow");  // Returns null (no match)
```

## It.IsNotNull\<T>()

Match any non-null value:

```csharp
mock.Setup(x => x.ValidateObject(It.IsNotNull<object>())).Returns(true);
mock.ValidateObject(new object()); // Returns true
mock.ValidateObject(null);         // Returns false (no match)
```

## It.IsRegex(pattern)

Match strings against regex patterns:

```csharp
mock.Setup(x => x.ValidateEmail(It.IsRegex(@"^[\w\.-]+@[\w\.-]+\.\w+$"))).Returns(true);
mock.ValidateEmail("test@example.com"); // Returns true
mock.ValidateEmail("invalid");          // Returns false (no match)
```

## Combining Matchers

Use multiple matchers in a single setup:

```csharp
mock.Setup(x => x.ProcessTwo(
   It.Is<int>(n => n > 0), 
   It.IsNotNull<string>()
)).Returns("valid");
```

## Using in Verify

All matchers work with `Verify`:

```csharp
mock.Verify(x => x.Process(It.Is<int>(n => n > 10)), Times.AtLeast(2));
```

## Custom Matchers

Create reusable custom matchers:

```csharp
mock.Setup(x => x.Process(Match.Create<int>(n => n % 2 == 0)))
   .Returns("even");
```
