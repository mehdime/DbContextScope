using System;
using Numero3.EntityFramework.Demo.CommandModel;
using Numero3.EntityFramework.Demo.DomainModel;
using Numero3.EntityFramework.Demo.Repositories;
using Mehdime.Entity;

namespace Numero3.EntityFramework.Demo.BusinessLogicServices
{
	/*
	 * Example business logic service implementing command functionalities (i.e. create / update actions).
	 */
	public class UserCreationService
	{
		private readonly IDbContextScopeFactory _dbContextScopeFactory;
		private readonly IUserRepository _userRepository;

		public UserCreationService(IDbContextScopeFactory dbContextScopeFactory, IUserRepository userRepository)
		{
			if (dbContextScopeFactory == null) throw new ArgumentNullException("dbContextScopeFactory");
			if (userRepository == null) throw new ArgumentNullException("userRepository");
			_dbContextScopeFactory = dbContextScopeFactory;
			_userRepository = userRepository;
		}

		public void CreateUser(UserCreationSpec userToCreate)
		{
			if (userToCreate == null)
				throw new ArgumentNullException("userToCreate");

			userToCreate.Validate();

			/*
			 * Typical usage of DbContextScope for a read-write business transaction. 
			 * It's as simple as it looks.
			 */
			using (var dbContextScope = _dbContextScopeFactory.Create())
			{
				//-- Build domain model
				var user = new User()
				           {
							   Id = userToCreate.Id,
							   Name = userToCreate.Name,
							   Email = userToCreate.Email,
							   WelcomeEmailSent = false,
					           CreatedOn = DateTime.UtcNow
				           };

				//-- Persist
				_userRepository.Add(user);
				dbContextScope.SaveChanges();
			}
		}

		public void CreateListOfUsers(params UserCreationSpec[] usersToCreate)
		{
			/*
			 * Example of DbContextScope nesting in action. 
			 * 
			 * We already have a service method - CreateUser() - that knows how to create a new user
			 * and implements all the business rules around the creation of a new user 
			 * (e.g. validation, initialization, sending notifications to other domain model objects...).
			 * 
			 * So we'll just call it in a loop to create the list of new users we've 
			 * been asked to create.
			 * 
			 * Of course, since this is a business logic service method, we are making 
			 * an implicit guarantee to whoever is calling us that the changes we make to 
			 * the system will be either committed or rolled-back in an atomic manner. 
			 * I.e. either all the users we've been asked to create will get persisted
			 * or none of them will. It would be disastrous to have a partial failure here
			 * and end up with some users but not all having been created.
			 * 
			 * DbContextScope makes this trivial to implement. 
			 * 
			 * The inner DbContextScope instance that the CreateUser() method creates
			 * will join our top-level scope. This ensures that the same DbContext instance is
			 * going to be used throughout this business transaction.
			 * 
			 */

			using (var dbContextScope = _dbContextScopeFactory.Create())
			{
				foreach (var toCreate in usersToCreate)
				{
					CreateUser(toCreate);
				}

				// All the changes will get persisted here
				dbContextScope.SaveChanges();
			}
		}

		public void CreateListOfUsersWithIntentionalFailure(params UserCreationSpec[] usersToCreate)
		{
			/*
			 * Here, we'll verify that inner DbContextScopes really join the parent scope and 
			 * don't persist their changes until the parent scope completes successfully. 
			 */

			var firstUser = true;

			using (var dbContextScope = _dbContextScopeFactory.Create())
			{
				foreach (var toCreate in usersToCreate)
				{
					if (firstUser)
					{
						CreateUser(toCreate);
						Console.WriteLine("Successfully created a new User named '{0}'.", toCreate.Name);
						firstUser = false;
					}
					else
					{
						// OK. So we've successfully persisted one user.
						// We're going to simulate a failure when attempting to 
						// persist the second user and see what ends up getting 
						// persisted in the DB.
						throw new Exception(String.Format("Oh no! An error occurred when attempting to create user named '{0}' in our database.", toCreate.Name));
					}
				}

				dbContextScope.SaveChanges();
			}
		}
	}
}

