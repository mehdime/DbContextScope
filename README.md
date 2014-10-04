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





