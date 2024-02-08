using Microsoft.AspNetCore.Identity;

namespace MovieAnalyticsWeb.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            this.FilePaths = new HashSet<FilePath>();
        }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string FullName
        {
            get { return $"{FirstName} {LastName}"; }
        }

        public virtual ICollection<FilePath> FilePaths { get; set; }
    }
}
