using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace WebAppExperimental266.Models.Main_Objects
{
    // REF:  https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/enumeration-classes-over-enum-types#implement-an-enumeration-base-class

    // THIS CODE TAKEN VERBATIM FROM ABOVE REFERENCE
    // Code needed substantial editing to clear about 10 warnings.

    /// <summary>
    /// Code to allow enumeration of strings in C#
    /// </summary>
    public abstract class Enumeration : IComparable
    {

        /// <summary>
        /// String held in enumeration
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Number used to coordinate enumeration of string
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Class to return the id,name objects.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        protected Enumeration(int id, string name) => (Id, Name) = (id, name);

        /// <summary>
        /// Ability to report the enum as the string in Name.
        /// </summary>
        /// <returns>string from Name.</returns>
        public override string ToString() => Name;

        /// <summary>
        /// Ability to enumerate over all the defined T
        /// </summary>
        /// <typeparam name="T">Class to extend this</typeparam>
        /// <returns>Collection of the values.</returns>
        public static IEnumerable<T> GetAll<T>() where T : Enumeration =>
            typeof(T).GetFields(BindingFlags.Public |
                                BindingFlags.Static |
                                BindingFlags.DeclaredOnly)
                     .Select(f => f.GetValue(null))
                     .Cast<T>();

        /// <summary>
        /// Ability to check if two instances of these values are equal.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>true if mathches</returns>

        public override bool Equals([AllowNull] object obj)
        {
            if (obj is not Enumeration otherValue)
            {
                return false;
            }

            var typeMatches = GetType().Equals(obj.GetType());
            var valueMatches = Id.Equals(otherValue.Id);

            return typeMatches && valueMatches;
        }

        /// <summary>
        /// Simple comparator
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        /// <returns>true if Id matches</returns>
        int IComparable.CompareTo(object? obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            return Id.CompareTo(((Enumeration)obj).Id);
        }

        /// <summary>
        /// Required override for this object.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
