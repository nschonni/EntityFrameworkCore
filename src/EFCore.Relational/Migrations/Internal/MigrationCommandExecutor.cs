﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.Migrations.Internal
{
    /// <summary>
    ///     <para>
    ///         This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///         the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///         any release. You should only use it directly in your code with extreme caution and knowing that
    ///         doing so can result in application failures when updating to a new Entity Framework Core release.
    ///     </para>
    ///     <para>
    ///         The service lifetime is <see cref="ServiceLifetime.Singleton"/>. This means a single instance
    ///         is used by many <see cref="DbContext"/> instances. The implementation must be thread-safe.
    ///         This service cannot depend on services registered as <see cref="ServiceLifetime.Scoped"/>.
    ///     </para>
    /// </summary>
    public class MigrationCommandExecutor : IMigrationCommandExecutor
    {
        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual void ExecuteNonQuery(
            IEnumerable<MigrationCommand> migrationCommands,
            IRelationalConnection connection)
        {
            Check.NotNull(migrationCommands, nameof(migrationCommands));
            Check.NotNull(connection, nameof(connection));

            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            {
                connection.Open();

                try
                {
                    IDbContextTransaction transaction = null;

                    try
                    {
                        foreach (var command in migrationCommands)
                        {
                            if (transaction == null
                                && !command.TransactionSuppressed)
                            {
                                transaction = connection.BeginTransaction();
                            }

                            if (transaction != null
                                && command.TransactionSuppressed)
                            {
                                transaction.Commit();
                                transaction.Dispose();
                                transaction = null;
                            }

                            command.ExecuteNonQuery(connection);
                        }

                        transaction?.Commit();
                    }
                    finally
                    {
                        transaction?.Dispose();
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual async Task ExecuteNonQueryAsync(
            IEnumerable<MigrationCommand> migrationCommands,
            IRelationalConnection connection,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(migrationCommands, nameof(migrationCommands));
            Check.NotNull(connection, nameof(connection));

            var transactionScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
            try
            {
                await connection.OpenAsync(cancellationToken);

                try
                {
                    IDbContextTransaction transaction = null;

                    try
                    {
                        foreach (var command in migrationCommands)
                        {
                            if (transaction == null
                                && !command.TransactionSuppressed)
                            {
                                transaction = await connection.BeginTransactionAsync(cancellationToken);
                            }

                            if (transaction != null
                                && command.TransactionSuppressed)
                            {
                                transaction.Commit();
                                await transaction.DisposeAsync();
                                transaction = null;
                            }

                            await command.ExecuteNonQueryAsync(connection, cancellationToken: cancellationToken);
                        }

                        transaction?.Commit();
                    }
                    finally
                    {
                        if (transaction != null)
                        {
                            await transaction.DisposeAsync();
                        }
                    }
                }
                finally
                {
                    await connection.CloseAsync(cancellationToken);
                }
            }
            finally
            {
                await transactionScope.DisposeAsyncIfAvailable();
            }
        }
    }
}
