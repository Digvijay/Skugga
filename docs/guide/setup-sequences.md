# Setup Sequences

Configure methods to return different values on consecutive calls -- perfect for testing retry logic, pagination, and stateful scenarios.

## Basic Sequences

```csharp
mock.SetupSequence(x => x.GetNext())
   .Returns(1)
   .Returns(2)
   .Returns(3);
   
mock.GetNext(); // Returns 1
mock.GetNext(); // Returns 2
mock.GetNext(); // Returns 3
mock.GetNext(); // Returns 3 (repeats last value)
```

## Mixing Returns and Exceptions

Perfect for testing retry logic:

```csharp
mock.SetupSequence(x => x.FetchData())
   .Throws(new TimeoutException())
   .Throws(new TimeoutException())
   .Returns("success");
   
// First two calls throw, third succeeds
try { mock.FetchData(); } catch { /* retry */ }
try { mock.FetchData(); } catch { /* retry */ }
var data = mock.FetchData(); // "success"
```

## Property Sequences

```csharp
mock.SetupSequence(x => x.Counter)
   .Returns(0)
   .Returns(1)
   .Returns(2);
```

## Use Cases

- **Retry logic testing**: Fail N times, then succeed
- **Pagination**: Return different pages of data
- **State machines**: Simulate state transitions
- **Circuit breakers**: Test failure thresholds
