using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace SCHQ_Blazor.Classes;

/// <summary>
/// Intercepts GET_LOCK / RELEASE_LOCK scalar results and replaces DBNull with 1
/// to work around a bug in MySql.EntityFrameworkCore where a NULL return value
/// from MySQL causes an InvalidCastException in AcquireDatabaseLockAsync.
/// </summary>
public class MySqlGetLockInterceptor : DbCommandInterceptor {

  public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    => FixGetLockResult(command, result) ?? base.ScalarExecuted(command, eventData, result);

  public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    => ValueTask.FromResult(FixGetLockResult(command, result) ?? result);

  private static object? FixGetLockResult(DbCommand command, object? result) {
    if (result is DBNull && (command.CommandText.Contains("GET_LOCK") || command.CommandText.Contains("RELEASE_LOCK"))) {
      return 1L;
    }
    return null;
  }
}
