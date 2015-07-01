/* 
 * Copyright (C) 2014 Mehdi El Gueddari
 * http://mehdi.me
 *
 * This software may be modified and distributed under the terms
 * of the MIT license.  See the LICENSE file for details.
 */
using System.Data.Entity;

namespace Mehdime.Entity
{
    /// <summary>
    /// Factory for DbContext-derived classes that don't expose 
    /// a default constructor.
    /// </summary>
    /// <remarks>
	/// If your DbContext-derived classes have a default constructor, 
	/// you can ignore this factory. DbContextScope will take care of
	/// instanciating your DbContext class with Activator.CreateInstance() 
	/// when needed.
	/// 
	/// If your DbContext-derived classes don't expose a default constructor
	/// however, you must impement this interface and provide it to DbContextScope
	/// so that it can create instances of your DbContexts.
	/// 
	/// A typical situation where this would be needed is in the case of your DbContext-derived 
	/// class having a dependency on some other component in your application. For example, 
	/// some data in your database may be encrypted and you might want your DbContext-derived
	/// class to automatically decrypt this data on entity materialization. It would therefore 
	/// have a mandatory dependency on an IDataDecryptor component that knows how to do that. 
	/// In that case, you'll want to implement this interface and pass it to the DbContextScope
	/// you're creating so that DbContextScope is able to create your DbContext instances correctly. 
    /// </remarks>
    public interface IDbContextFactory
    {
		TDbContext CreateDbContext<TDbContext>() where TDbContext : DbContext;
    }
}
