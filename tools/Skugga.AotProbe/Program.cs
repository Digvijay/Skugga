using Skugga.Core;

namespace Skugga.AotProbe;

/// <summary>
/// A Native AOT probe for Skugga's shipping surface.
/// </summary>
/// <remarks>
/// <para>
/// Skugga claims that mocks are generated at compile time and therefore survive Native AOT, where
/// proxy-based libraries do not. This program is the evidence for that claim: CI publishes it with
/// <c>PublishAot=true</c>, asserts that no trim or AOT warning originates in Skugga, and then runs
/// the native binary.
/// </para>
/// <para>
/// Running it is the part that matters. The reflective fallbacks in <c>Skugga.Core</c> are wrapped
/// in <c>try/catch</c> and return <c>null</c> when they fail, so a path that silently degrades
/// under AOT would not throw — it would return the wrong value. Only assertions catch that, which
/// is why every probe below checks the value it got rather than merely calling the method.
/// </para>
/// </remarks>
internal static class Program
{
    private static int _failures;

    private static int Main()
    {
        Console.WriteLine("Skugga Native AOT probe");
        Console.WriteLine("=======================");
        Console.WriteLine();

        ProbeEmptyDefaults();
        ProbeSetupAndReturns();
        ProbeVerify();
        ProbeRecursiveMocking();

        Console.WriteLine();
        if (_failures == 0)
        {
            Console.WriteLine("All AOT probes passed.");
            return 0;
        }

        Console.Error.WriteLine($"{_failures} AOT probe(s) failed.");
        return 1;
    }

    /// <summary>
    /// Covers every branch of <c>EmptyDefaultValueProvider</c> that uses reflection.
    /// </summary>
    /// <remarks>
    /// Arrays go through <c>Array.CreateInstance</c>, and the generic collections go through
    /// <c>Type.MakeGenericType</c> plus <c>Activator.CreateInstance</c>. Those are the calls that
    /// Native AOT cannot generate code for, so if the compile-time path has not replaced them these
    /// assertions fail — or, worse, quietly receive <c>null</c>, which is why null is checked
    /// explicitly rather than assumed.
    /// </remarks>
    private static void ProbeEmptyDefaults()
    {
        var mock = Mock.Create<IProbeService>(DefaultValue.Empty);

        Check("string default is empty, not null", mock.GetName() == string.Empty);
        Check("int default is zero", mock.GetCount() == 0);

        var items = mock.GetItems();
        Check("List<string> default is a non-null empty list", items is { Count: 0 });

        var numbers = mock.GetNumbers();
        Check("IEnumerable<int> default is non-null and empty", numbers is not null && !numbers.Any());

        var array = mock.GetArray();
        Check("int[] default is a non-null empty array", array is { Length: 0 });

        var dictionary = mock.GetDictionary();
        Check("Dictionary<string,int> default is non-null and empty", dictionary is { Count: 0 });
    }

    /// <summary>
    /// Confirms the generator intercepted <c>Setup</c> rather than leaving it to runtime expression
    /// compilation, which Native AOT cannot perform.
    /// </summary>
    private static void ProbeSetupAndReturns()
    {
        var mock = Mock.Create<IProbeService>();

        mock.Setup(x => x.GetName()).Returns("configured");
        Check("Setup(...).Returns(...) returns the configured value", mock.GetName() == "configured");

        mock.Setup(x => x.Process(21)).Returns(42);
        Check("Setup matches a literal argument", mock.Process(21) == 42);

        // A captured local is the case that would otherwise be read by compiling a closure
        // expression at runtime.
        var captured = 7;
        mock.Setup(x => x.Process(captured)).Returns(70);
        Check("Setup matches a captured local", mock.Process(7) == 70);
    }

    /// <summary>
    /// Confirms call recording and verification survive AOT.
    /// </summary>
    private static void ProbeVerify()
    {
        var mock = Mock.Create<IProbeService>();
        mock.Setup(x => x.Process(1)).Returns(1);

        _ = mock.Process(1);
        _ = mock.Process(1);

        var verified = true;
        try
        {
            mock.Verify(x => x.Process(1), Times.Exactly(2));
        }
        catch (Exception ex)
        {
            verified = false;
            Console.Error.WriteLine($"        {ex.GetType().Name}: {ex.Message}");
        }

        Check("Verify(..., Times.Exactly(2)) succeeds after two calls", verified);
    }

    /// <summary>
    /// Covers <c>MockDefaultValueProvider</c>, whose reflective fallback calls
    /// <c>MethodInfo.MakeGenericMethod</c>.
    /// </summary>
    /// <remarks>
    /// This is the branch most likely to degrade silently: the fallback catches every exception and
    /// returns <c>null</c>, so under AOT a missing generated factory shows up as a null property
    /// rather than as a crash.
    /// </remarks>
    private static void ProbeRecursiveMocking()
    {
        var mock = Mock.Create<IProbeService>(DefaultValue.Mock);

        var logger = mock.GetLogger();
        Check("Recursive mocking returns a non-null nested mock", logger is not null);

        if (logger is not null)
        {
            Check("The nested mock is usable", logger.GetLevel() is not null);
        }
        else
        {
            Check("The nested mock is usable", false);
        }
    }

    private static void Check(string description, bool condition)
    {
        if (condition)
        {
            Console.WriteLine($"  PASS  {description}");
            return;
        }

        _failures++;
        Console.Error.WriteLine($"  FAIL  {description}");
    }
}

/// <summary>The interface the probe mocks. Each member targets a distinct default-value branch.</summary>
public interface IProbeService
{
    /// <summary>Exercises the string default.</summary>
    string GetName();

    /// <summary>Exercises the value-type default.</summary>
    int GetCount();

    /// <summary>Exercises the concrete generic collection default.</summary>
    List<string> GetItems();

    /// <summary>Exercises the collection-interface default.</summary>
    IEnumerable<int> GetNumbers();

    /// <summary>Exercises the array default, which uses <c>Array.CreateInstance</c>.</summary>
    int[] GetArray();

    /// <summary>Exercises the dictionary default.</summary>
    Dictionary<string, int> GetDictionary();

    /// <summary>Exercises recursive mocking.</summary>
    IProbeLogger GetLogger();

    /// <summary>Exercises argument matching.</summary>
    int Process(int value);
}

/// <summary>A nested interface used to exercise recursive mocking.</summary>
public interface IProbeLogger
{
    /// <summary>Returns a string so the probe can assert the nested mock works.</summary>
    string GetLevel();
}
