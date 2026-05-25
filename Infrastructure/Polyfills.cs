using System;
using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}

namespace System
{
    /// <summary>
    /// Represents a type that can be used as an index to a collection.
    /// </summary>
    internal readonly struct Index : IEquatable<Index>
    {
        private readonly int _value;

        /// <summary>
        /// Construct an Index using a value and indicating if the index is from the start or from the end.
        /// </summary>
        /// <param name="value">The index value. If "fromEnd" is true, this represents the distance from the end.</param>
        /// <param name="fromEnd">Indicates if the index is from the start (false) or from the end (true).</param>
        public Index(int value, bool fromEnd = false)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative.");

            _value = fromEnd ? ~value : value;
        }

        /// <summary>
        /// Create an Index pointing at the first element (0).
        /// </summary>
        public static Index Start => new Index(0, false);

        /// <summary>
        /// Create an Index pointing at the last element (^1).
        /// </summary>
        public static Index End => new Index(0, true);

        /// <summary>
        /// Create an Index from a non-negative integer value, counting from the start.
        /// </summary>
        public static Index FromStart(int value) => new Index(value, false);

        /// <summary>
        /// Create an Index from a non-negative integer value, counting from the end.
        /// </summary>
        public static Index FromEnd(int value) => new Index(value, true);

        /// <summary>
        /// Get the underlying integer value of the Index.
        /// </summary>
        public int Value => _value < 0 ? ~_value : _value;

        /// <summary>
        /// Indicates if the Index is from the end.
        /// </summary>
        public bool IsFromEnd => _value < 0;

        /// <summary>
        /// Calculate the offset from the start based on the length of the collection.
        /// </summary>
        public int GetOffset(int length)
        {
            return _value < 0 ? length - (~_value) : _value;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current Index.
        /// </summary>
        public override bool Equals(object? obj) => obj is Index other && Equals(other);

        /// <summary>
        /// Determines whether the specified Index is equal to the current Index.
        /// </summary>
        public bool Equals(Index other) => _value == other._value;

        /// <summary>
        /// Returns the hash code for this Index.
        /// </summary>
        public override int GetHashCode() => _value;

        /// <summary>
        /// Implicitly converts an integer to an Index from the start.
        /// </summary>
        public static implicit operator Index(int value) => FromStart(value);

        /// <summary>
        /// Returns a string representation of the Index.
        /// </summary>
        public override string ToString() => IsFromEnd ? $"^{Value}" : Value.ToString();
    }

    /// <summary>
    /// Represents a range that can be used to access a sequence of elements.
    /// </summary>
    internal readonly struct Range : IEquatable<Range>
    {
        /// <summary>
        /// The start of the range.
        /// </summary>
        public Index Start { get; }

        /// <summary>
        /// The end of the range.
        /// </summary>
        public Index End { get; }

        /// <summary>
        /// Constructs a range with the specified start and end.
        /// </summary>
        public Range(Index start, Index end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Creates a range starting from the specified index to the end.
        /// </summary>
        public static Range All => new Range(Index.Start, Index.End);

        /// <summary>
        /// Determines whether the specified object is equal to the current Range.
        /// </summary>
        public override bool Equals(object? obj) => obj is Range other && Equals(other);

        /// <summary>
        /// Determines whether the specified Range is equal to the current Range.
        /// </summary>
        public bool Equals(Range other) => Start.Equals(other.Start) && End.Equals(other.End);

        /// <summary>
        /// Returns the hash code for this Range.
        /// </summary>
        public override int GetHashCode() => Start.GetHashCode() ^ End.GetHashCode();

        /// <summary>
        /// Returns a string representation of the Range.
        /// </summary>
        public override string ToString() => $"{Start}..{End}";
    }
}
