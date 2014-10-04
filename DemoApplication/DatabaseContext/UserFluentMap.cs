using System.Data.Entity.ModelConfiguration;
using Numero3.EntityFramework.Demo.DomainModel;

namespace Numero3.EntityFramework.Demo.DatabaseContext
{
	/// <summary>
	/// Defines the convention-based mapping overrides for the User model. 
	/// </summary>
	public class UserFluentMap : EntityTypeConfiguration<User>
	{
		public UserFluentMap()
		{
			Property(m => m.Name).IsRequired();
			Property(m => m.Email).IsRequired();
		}
	}
}
