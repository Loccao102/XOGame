using System.Reflection;
using NUnit.Framework;

// Runs every [Test] method found in this assembly (the repo's Unity EditMode tests plus the
// regression tests in RegressionTests.cs) and exits non-zero on any failure, so it can gate CI.
public static class Runner
{
    public static int Main(string[] args)
    {
        string[] filters = args.Length > 0 ? args[0].Split(',', StringSplitOptions.RemoveEmptyEntries) : null;
        int pass = 0, fail = 0;
        var failures = new List<string>();

        foreach (var type in typeof(Runner).Assembly.GetTypes().OrderBy(t => t.FullName))
        {
            foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                                  .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
                                  .OrderBy(m => m.Name))
            {
                string name = type.Name + "." + m.Name;
                if (filters != null && !filters.Any(f => name.Contains(f))) continue;
                try
                {
                    object inst = m.IsStatic ? null : Activator.CreateInstance(type);
                    m.Invoke(inst, null);
                    pass++;
                    Console.WriteLine("[PASS] " + name);
                }
                catch (TargetInvocationException tie) when (tie.InnerException is SuccessException)
                {
                    pass++;
                    Console.WriteLine("[PASS] " + name);
                }
                catch (TargetInvocationException tie)
                {
                    fail++;
                    string msg = $"[FAIL] {name}: {tie.InnerException?.GetType().Name}: {tie.InnerException?.Message}";
                    failures.Add(msg);
                    Console.WriteLine(msg);
                }
            }
        }

        Console.WriteLine($"\n{pass} passed, {fail} failed");
        return fail == 0 ? 0 : 1;
    }
}
