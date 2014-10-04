using System;

namespace Numero3.EntityFramework.Demo.DomainModel
{
	// Anemic model to keep this demo application simple.
	public class User
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		public int CreditScore { get; set; }
		public bool WelcomeEmailSent { get; set; }
		public DateTime CreatedOn { get; set; }

		public override string ToString()
		{
			return String.Format("Id: {0} | Name: {1} | Email: {2} | CreditScore: {3} | WelcomeEmailSent: {4} | CreatedOn (UTC): {5}", Id, Name, Email, CreditScore, WelcomeEmailSent, CreatedOn.ToString("dd MMM yyyy - HH:mm:ss"));
		}
	}
}
