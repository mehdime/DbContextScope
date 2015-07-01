DbContextScope
==============

A simple and flexible way to manage your Entity Framework DbContext instances.

`DbContextScope` was created out of the need for a better way to manage DbContext instances in Entity Framework-based applications. 

The commonly advocated method of injecting DbContext instances works fine for single-threaded web applications where each web request implements exactly one business transaction. But it breaks down quite badly when console apps, Windows Services, parallelism and requests that need to implement multiple independent business transactions make their appearance.

The alternative of manually instantiating DbContext instances and manually passing them around as method parameters is (speaking from experience) more than cumbersome. 

`DbContextScope` implements the ambient context pattern for DbContext instances. It's something that NHibernate users or anyone who has used the `TransactionScope` class to manage ambient database transactions will be familiar with.

It doesn't force any particular design pattern or application architecture to be used. It works beautifully with dependency injection. And it works beautifully without. It of course works perfectly with async execution flows, including with the new async / await support introduced in .NET 4.5 and EF6. 

And most importantly, at the time of writing, `DbContextScope` has been battle-tested in a large-scale application for over two months and has performed without a hitch. 

#Using DbContextScope

The repo contains a demo application that demonstrates the most common (and a few more advanced) use-cases.

I would highly recommend reading the following blog post first. It examines in great details the most commonly used approaches to manage DbContext instances and explains how `DbContextScope` addresses their shortcomings and simplifies DbContext management: [Managing DbContext the right way with Entity Framework 6: an in-depth guide](http://mehdi.me/ambient-dbcontext-in-ef6/). 

###Overview

This is the `DbContextScope` interface:

```C#
public interface IDbContextScope : IDisposable
{
    void SaveChanges();
    Task SaveChangesAsync();

    void RefreshEntitiesInParentScope(IEnumerable entities);
    Task RefreshEntitiesInParentScopeAsync(IEnumerable entities);

    IDbContextCollection DbContexts { get; }
}
```

The purpose of a `DbContextScope` is to create and manage the `DbContext` instances used within a code block. A `DbContextScope` therefore effectively defines the boundary of a business transaction. 

Wondering why DbContextScope wasn't called "UnitOfWork" or "UnitOfWorkScope"? The answer is here: [Why DbContextScope and not UnitOfWork?](http://mehdi.me/ambient-dbcontext-in-ef6/#whydbcontextscopeandnotunitofwork)

You can instantiate a `DbContextScope` directly. Or you can take a dependency on `IDbContextScopeFactory`, which provides convenience methods to create a `DbContextScope` with the most common configurations:

```C#
public interface IDbContextScopeFactory
{
    IDbContextScope Create(DbContextScopeOption joiningOption = DbContextScopeOption.JoinExisting);
    IDbContextReadOnlyScope CreateReadOnly(DbContextScopeOption joiningOption = DbContextScopeOption.JoinExisting);

    IDbContextScope CreateWithTransaction(IsolationLevel isolationLevel);
    IDbContextReadOnlyScope CreateReadOnlyWithTransaction(IsolationLevel isolationLevel);

    IDisposable SuppressAmbientContext();
}
```

###Typical usage
With `DbContextScope`, your typical service method would look like this:

```C#
public void MarkUserAsPremium(Guid userId)
{
    using (var dbContextScope = _dbContextScopeFactory.Create())
    {
        var user = _userRepository.Get(userId);
        user.IsPremiumUser = true;
        dbContextScope.SaveChanges();
    }
}
```

Within a `DbContextScope`, you can access the `DbContext` instances that the scope manages in two ways. You can get them via the `DbContextScope.DbContexts` property like this:

```C#
public void SomeServiceMethod(Guid userId)
{
    using (var dbContextScope = _dbContextScopeFactory.Create())
    {
        var user = dbContextScope.DbContexts.Get<MyDbContext>.Set<User>.Find(userId);
        [...]
        dbContextScope.SaveChanges();
    }
}
```

But that's of course only available in the method that created the `DbContextScope`. If you need to access the ambient `DbContext` instances anywhere else (e.g. in a repository class), you can just take a dependency on `IAmbientDbContextLocator`, which you would use like this:

```C#
public class UserRepository : IUserRepository
{
    private readonly IAmbientDbContextLocator _contextLocator;

    public UserRepository(IAmbientDbContextLocator contextLocator)
    {
        if (contextLocator == null) throw new ArgumentNullException("contextLocator");
        _contextLocator = contextLocator;
    }

    public User Get(Guid userId)
    {
        return _contextLocator.Get<MyDbContext>.Set<User>().Find(userId);
    }
}
```

Those `DbContext` instances are created lazily and the `DbContextScope` keeps track of them to ensure that only one instance of any given DbContext type is ever created within its scope. 

You'll note that the service method doesn't need to know which type of `DbContext` will be required during the course of the business transaction. It only needs to create a `DbContextScope` and any component that needs to access the database within that scope will request the type of `DbContext` they need. 

###Nesting scopes
A `DbContextScope` can of course be nested. Let's say that you already have a service method that can mark a user as a premium user like this:

```C#
public void MarkUserAsPremium(Guid userId)
{
    using (var dbContextScope = _dbContextScopeFactory.Create())
    {
        var user = _userRepository.Get(userId);
        user.IsPremiumUser = true;
        dbContextScope.SaveChanges();
    }
}
```

You're implementing a new feature that requires being able to mark a group of users as premium within a single business transaction. You can easily do it like this:

```C#
public void MarkGroupOfUsersAsPremium(IEnumerable<Guid> userIds)
{
    using (var dbContextScope = _dbContextScopeFactory.Create())
    {
        foreach (var userId in userIds)
        {
        	// The child scope created by MarkUserAsPremium() will
            // join our scope. So it will re-use our DbContext instance(s)
            // and the call to SaveChanges() made in the child scope will
            // have no effect.
        	MarkUserAsPremium(userId);
        }
        
        // Changes will only be saved here, in the top-level scope,
        // ensuring that all the changes are either committed or
        // rolled-back atomically.
        dbContextScope.SaveChanges();
    }
}
```

(this would of course be a very inefficient way to implement this particular feature but it demonstrates the point)

This makes creating a service method that combines the logic of multiple other service methods trivial. 

###Read-only scopes
If a service method is read-only, having to call `SaveChanges()` on its `DbContextScope` before returning can be a pain. But not calling it isn't an option either as: 

1. It will make code review and maintenance difficult (did you intend not to call `SaveChanges()` or did you forget to call it?) 
2. If you requested an explicit database transaction to be started (we'll see later how to do it), not calling `SaveChanges()` will result in the transaction being rolled back. Database monitoring systems will usually interpret transaction rollbacks as an indication of an application error. Having spurious rollbacks is not a good idea.

The `DbContextReadOnlyScope` class addresses this issue. This is its interface:

```C#
public interface IDbContextReadOnlyScope : IDisposable
{
    IDbContextCollection DbContexts { get; }
}
```

And this is how you use it:

```C#
public int NumberPremiumUsers()
{
    using (_dbContextScopeFactory.CreateReadOnly())
    {
        return _userRepository.GetNumberOfPremiumUsers();
    }
}
```

###Async support
`DbContextScope` works with async execution flows as you would expect:

```C#
public async Task RandomServiceMethodAsync(Guid userId)
{
    using (var dbContextScope = _dbContextScopeFactory.Create())
    {
        var user = await _userRepository.GetAsync(userId);
        var orders = await _orderRepository.GetOrdersForUserAsync(userId);

        [...]

        await dbContextScope.SaveChangesAsync();
    }
}
```

In the example above, the `OrderRepository.GetOrdersForUserAsync()` method will be able to see and access the ambient DbContext instance despite the fact that it's being called in a separate thread than the one where the `DbContextScope` was initially created.

This is made possible by the fact that `DbContextScope` stores itself in the CallContext. The CallContext automatically flows through async points. If you're curious about how it all works behind the scenes, Stephen Toub has written [an excellent blog post about it](http://blogs.msdn.com/b/pfxteam/archive/2012/06/15/executioncontext-vs-synchronizationcontext.aspx). But if all you want to do is use `DbContextScope`, you just have to know that: it just works.

**WARNING**: There is one thing that you *must* always keep in mind when using any async flow with `DbContextScope`. Just like `TransactionScope`, `DbContextScope` only supports being used within a single logical flow of execution. 

I.e. if you attempt to start multiple parallel tasks within the context of a `DbContextScope` (e.g. by creating multiple threads or multiple TPL `Task`), you will get into big trouble. This is because the ambient `DbContextScope` will flow through all the threads your parallel tasks are using. If code in these threads need to use the database, they will all use the same ambient `DbContext` instance, resulting the same the `DbContext` instance being used from multiple threads simultaneously. 

In general, parallelizing database access within a single business transaction has little to no benefits and only adds significant complexity. Any parallel operation performed within the context of a business transaction should not access the database.

However, if you really need to start a parallel task within a `DbContextScope` (e.g. to perform some out-of-band background processing independently from the outcome of the business transaction), then you **must** suppress the ambient context before starting the parallel task. Which you can easily do like this:

```C#
public void RandomServiceMethod()
{
    using (var dbContextScope = _dbContextScopeFactory.Create())
    {
        // Do some work that uses the ambient context
        [...]
        
        using (_dbContextScopeFactory.SuppressAmbientContext())
        {
            // Kick off parallel tasks that shouldn't be using the
            // ambient context here. E.g. create new threads,
            // enqueue work items on the ThreadPool or create 
            // TPL Tasks. 
            [...]
        }

		// The ambient context is available again here.
        // Can keep doing more work as usual.
        [...]

        dbContextScope.SaveChanges();
    }
}
```

###Creating a non-nested DbContextScope
This is an advanced feature that I would expect most applications to never need. Tread carefully when using this as it can create tricky issues and quickly lead to a maintenance nightmare. 

Sometimes, a service method may need to persist its changes to the underlying database regardless of the outcome of overall business transaction it may be part of. This would be the case if:

- It needs to record cross-cutting concern information that shouldn't be rolled-back even if the business transaction fails. A typical example would be logging or auditing records.
- It needs to record the result of an operation that cannot be rolled back. A typical example would be service methods that interact with non-transactional remote services or APIs. E.g. if your service method uses the Facebook API to post a new status update on Facebook and then records the newly created status update in the local database, that record must be persisted even if the overall business transaction fails because of some other error occurring after the Facebook API call. The Facebook API isn't transactional - it's impossible to "rollback" a Facebook API call. The result of that API call should therefore never be rolled back. 

In that case, you can pass a value of `DbContextScopeOption.ForceCreateNew` as the `joiningOption` parameter when creating a new `DbContextScope`. This will create a `DbContextScope` that will not join the ambient scope even if one exists:

```C#
public void RandomServiceMethod()
{
    using (var dbContextScope = _dbContextScopeFactory.Create(DbContextScopeOption.ForceCreateNew))
    {
        // We've created a new scope. Even if that service method
        // was called by another service method that has created its 
        // own DbContextScope, we won't be joining it. 
        // Our scope will create new DbContext instances and won't
        // re-use the DbContext instances that the parent scope uses.
        [...]
        
		// Since we've forced the creation of a new scope,
        // this call to SaveChanges() will persist
        // our changes regardless of whether or not the
        // parent scope (if any) saves its changes or rolls back.
        dbContextScope.SaveChanges();
    }
}
```

The major issue with doing this is that this service method will use separate `DbContext` instances than the ones used in the rest of that business transaction. Here are a few basic rules to always follow in that case in order to avoid weird bugs and maintenance nightmares:

####1. Persistent entity returned by a service method must always be attached to the ambient context

If you force the creation of a new `DbContextScope` (and therefore of new `DbContext` instances) instead of joining the ambient one, your service method must **never** return persistent entities that were created / retrieved within that new scope. This would be completely unexpected and will lead to humongous complexity.

The client code calling your service method may be a service method itself that created its own `DbContextScope` and therefore expects all service methods it calls to use that same ambient scope (this is the whole point of using an ambient context). It will therefore expect any persistent entity returned by your service method to be attached to the ambient `DbContext`.

Instead, either:

- Don't return persistent entities. This is the easiest, cleanest, most foolproof method. E.g. if your service creates a new domain model object, don't return it. Return its ID instead and let the client load the entity in its own `DbContext` instance if it needs the actual object.
- If you absolutely need to return a persistent entity, switch back to the ambient context, load the entity you want to return in the ambient context and return that.

####2. Upon exit, a service method must make sure that all modifications it made to persistent entities have been replicated in the parent scope

If your service method forces the creation of a new `DbContextScope` and then modifies persistent entities in that new scope, it must make sure that the parent ambient scope (if any) can "see" those modification when it returns. 

I.e. if the `DbContext` instances in the parent scope had already loaded the entities you modified in their first-level cache (ObjectStateManager), your service method must force a refresh of these entities to ensure that the parent scope doesn't end up working with stale versions of these objects.

The `DbContextScope` class has a handy helper method that makes this fairly painless:

```C#
public void RandomServiceMethod(Guid accountId)
{
	// Forcing the creation of a new scope (i.e. we'll be using our 
	// own DbContext instances)
    using (var dbContextScope = _dbContextScopeFactory.Create(DbContextScopeOption.ForceCreateNew))
    {
        var account = _accountRepository.Get(accountId);
        account.Disabled = true;
        
        // Since we forced the creation of a new scope,
        // this will persist our changes to the database
        // regardless of what the parent scope does.
        dbContextScope.SaveChanges();

        // If the caller of this method had already
        // loaded that account object into their own
        // DbContext instance, their version
        // has now become stale. They won't see that
        // this account has been disabled and might
        // therefore execute incorrect logic.
        // So make sure that the version our caller
        // has is up-to-date.
        dbContextScope.RefreshEntitiesInParentScope(new[] { account });
    }
}
```




