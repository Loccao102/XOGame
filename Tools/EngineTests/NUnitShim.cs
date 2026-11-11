// Tiny NUnit stand-in so Assets/Tests/Editor/XOEngineTests.cs (written for the Unity Test Runner)
// also runs under plain `dotnet run`. Only the members that file uses are implemented.
using System;
using System.Collections;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestAttribute : Attribute { }

    public class AssertionException : Exception { public AssertionException(string m) : base(m) { } }
    public class SuccessException : Exception { public SuccessException(string m) : base(m) { } }

    public static class Assert
    {
        static string Fmt(string m) => string.IsNullOrEmpty(m) ? "" : " - " + m;
        public static void IsTrue(bool c, string m = null) { if (!c) throw new AssertionException("Expected true" + Fmt(m)); }
        public static void IsFalse(bool c, string m = null) { if (c) throw new AssertionException("Expected false" + Fmt(m)); }
        public static void AreEqual(object expected, object actual, string m = null)
        {
            if (expected is IConvertible && actual is IConvertible && expected.GetType() != actual.GetType()
                && !(expected is string) && !(actual is string))
            {
                try { if (Convert.ToDouble(expected) == Convert.ToDouble(actual)) return; } catch { }
            }
            if (!Equals(expected, actual))
                throw new AssertionException($"Expected <{expected}> but was <{actual}>" + Fmt(m));
        }
        public static void AreNotEqual(object notExpected, object actual, string m = null)
        { if (Equals(notExpected, actual)) throw new AssertionException($"Did not expect <{actual}>" + Fmt(m)); }
        public static void StartsWith(string expected, string actual, string m = null)
        { if (actual == null || !actual.StartsWith(expected, StringComparison.Ordinal)) throw new AssertionException($"Expected string starting with <{expected}> but was <{actual}>" + Fmt(m)); }
        public static void Contains(object expected, ICollection actual, string m = null)
        { foreach (var x in actual) if (Equals(x, expected)) return; throw new AssertionException($"Collection does not contain <{expected}>" + Fmt(m)); }
        public static void Pass(string m = null) => throw new SuccessException(m);
        public static void Fail(string m = null) => throw new AssertionException("Fail" + Fmt(m));
    }
}

namespace NUnit.Framework
{
    public static class StringAssert
    {
        public static void StartsWith(string expected, string actual, string m = null) => Assert.StartsWith(expected, actual, m);
        public static void Contains(string expected, string actual, string m = null)
        { if (actual == null || !actual.Contains(expected)) throw new AssertionException($"Expected string containing <{expected}> but was <{actual}>" + (string.IsNullOrEmpty(m) ? "" : " - " + m)); }
    }
}
