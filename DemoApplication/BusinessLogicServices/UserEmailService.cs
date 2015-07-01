using System;
using System.Collections.Generic;
using Numero3.EntityFramework.Demo.DatabaseContext;
using Numero3.EntityFramework.Demo.DomainModel;
using Mehdime.Entity;

namespace Numero3.EntityFramework.Demo.BusinessLogicServices
{
	public class UserEmailService
	{
		private readonly IDbContextScopeFactory _dbContextScopeFactory;

		public UserEmailService(IDbContextScopeFactory dbContextScopeFactory)
		{
			if (dbContextScopeFactory == null) throw new ArgumentNullException("dbContextScopeFactory");
			_dbContextScopeFactory = dbContextScopeFactory;
		}

		public void SendWelcomeEmail(Guid userId)
		{
			/*
			 * Demo of forcing the creation of a new DbContextScope
			 * to ensure that changes made to the model in this service 
			 * method are persisted even if that method happens to get
			 * called within the scope of a wider business transaction
			 * that eventually fails for any reason.
			 * 
			 * This is an advanced feature that should be used as rarely 
			 * as possible (and ideally, never).
			 */

			// We're going to send a welcome email to the provided user
			// (if one hasn't been sent already). Once sent, we'll update
 			// that User entity in our DB to record that its Welcome email
			// has been sent.

			// Emails can't be rolled-back. Once they're sent, they're sent. 
			// So once the email has been sent successfully, we absolutely 
			// must persist this fact in our DB. Even if that method is called
			// by another busines logic service method as part of a wider 
			// business transaction and even if that parent business transaction
			// ends up failing for any reason, we still must ensure that
			// we have recorded the fact that the Welcome email has been sent.
			// Otherwise, we would risk spamming our users with repeated Welcome
			// emails. 

			// Force the creation of a new DbContextScope so that the changes we make here are
			// guaranteed to get persisted regardless of what happens after this method has completed.
			using (var dbContextScope = _dbContextScopeFactory.Create(DbContextScopeOption.ForceCreateNew))
			{
				var dbContext = dbContextScope.DbContexts.Get<UserManagementDbContext>();
				var user = dbContext.Users.Find(userId);

				if (user == null)
					throw new ArgumentException(String.Format("Invalid userId provided: {0}. Couldn't find a User with this ID.", userId));

				if (!user.WelcomeEmailSent)
				{
					SendEmail(user.Email);
					user.WelcomeEmailSent = true;
				}

				dbContextScope.SaveChanges();

				// When you force the creation of a new DbContextScope, you must force the parent
				// scope (if any) to reload the entities you've modified here. Otherwise, the method calling
				// you might not be able to see the changes you made here.
				dbContextScope.RefreshEntitiesInParentScope(new List<User> {user});
			}
		}

		private void SendEmail(string emailAddress)
		{
			// Send the email synchronously. Throw if any error occurs.
			// [...]
		}
	}
}
