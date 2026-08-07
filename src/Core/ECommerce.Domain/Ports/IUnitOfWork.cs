public interface IUnitOfWork
{
  Task SaveChangesAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Runs <paramref name="operation"/> inside a single database transaction.
  /// Compatible with retrying execution strategies (e.g. Npgsql's
  /// <c>EnableRetryOnFailure()</c>), which do not support user-initiated
  /// transactions opened directly on <c>DbContext.Database</c>.
  /// The transaction is committed on success and rolled back on exception.
  /// </summary>
  Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}