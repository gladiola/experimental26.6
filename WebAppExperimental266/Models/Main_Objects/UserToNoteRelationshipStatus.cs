namespace WebAppExperimental266.Models.Main_Objects
{


    // REF:  https://josipmisko.com/posts/string-enums-in-c-sharp-everything-you-need-to-know

    // Used in conjunction with Enumeration abstract class from 
    // Microsoft docs, quoted verbatim.

    /// <summary>
    /// Class to describe how users and notes relate to each other.
    /// </summary>
    public class UserToNoteRelationshipStatus : Enumeration
    {

        
        /// <summary>
        /// Show the user created the Note
        /// </summary>
        public static UserToNoteRelationshipStatus Created => new(1, "created");

        /// <summary>
        /// Show the user updated the Note
        /// </summary>
        public static UserToNoteRelationshipStatus Updated => new(2, "updated");


        /// <summary>
        /// Show the user deleted the Note
        /// </summary>
        public static UserToNoteRelationshipStatus Deleted => new(3, "deleted");

        /// <summary>
        /// Show the user viewed the Note
        /// </summary>
        public static UserToNoteRelationshipStatus Viewed => new(1, "viewed");


        /// <summary>
        /// Show the user listed the Note
        /// </summary>
        public static UserToNoteRelationshipStatus Listed=> new(1, "Listed");


        /// <summary>
        /// Code to enumerate over strings.
        /// </summary>
        /// <param name="id">Number in enumeration.</param>
        /// <param name="name">String associated with id.</param>
        public UserToNoteRelationshipStatus(int id, string name) : base(id, name)
        {
        }



    }
}
