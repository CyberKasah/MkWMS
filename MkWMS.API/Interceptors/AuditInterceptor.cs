using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MkWMS.Data.Entities;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;

namespace MkWMS.API.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;


    private static readonly ConditionalWeakTable<DbContext, List<AuditPendingEntry>> _pendingAudits = new();

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }




    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
        int userId = userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid) ? uid : 1;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added ||
                         e.State == EntityState.Modified ||
                         e.State == EntityState.Deleted)
            .ToList();

        var pendingList = new List<AuditPendingEntry>();

        foreach (var entry in entries)
        {

            if (entry.Entity is AuditLog) continue;

            var entityName = entry.Entity.GetType().Name;
            var auditDetails = new List<object>();

            if (entry.State == EntityState.Modified)
            {
                foreach (var prop in entry.Properties.Where(p => p.IsModified))
                {
                    auditDetails.Add(new
                    {
                        Field = prop.Metadata.Name,
                        Old = prop.OriginalValue?.ToString() ?? "—",
                        New = prop.CurrentValue?.ToString() ?? "—"
                    });
                }
            }
            else if (entry.State == EntityState.Added)
            {
                foreach (var prop in entry.Properties)
                {
                    auditDetails.Add(new
                    {
                        Field = prop.Metadata.Name,
                        Old = "—",
                        New = prop.CurrentValue?.ToString() ?? "—"
                    });
                }
            }

            if (auditDetails.Any() || entry.State == EntityState.Deleted)
            {
                pendingList.Add(new AuditPendingEntry
                {
                    Entry = entry,
                    UserId = userId,
                    EntityName = entityName,
                    State = entry.State,
                    ChangesJson = auditDetails.Any() ? JsonSerializer.Serialize(auditDetails) : null
                });
            }
        }

        if (pendingList.Any())
        {

            _pendingAudits.AddOrUpdate(context, pendingList);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }




    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;


        if (context != null && _pendingAudits.TryGetValue(context, out var pendingList))
        {

            _pendingAudits.Remove(context);

            foreach (var pending in pendingList)
            {

                var entityId = pending.Entry.Properties
     .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "0";


                string entityNameRu = pending.EntityName switch
                {
                    "Product" => "Товар",
                    "Document" => "Документ",
                    "Warehouse" => "Склад",
                    "Counterparty" => "Контрагент",
                    "Batch" => "Партия",
                    "SerialNumber" => "Серийный номер",
                    "User" => "Пользователь",
                    "Role" => "Роль",
                    "Department" => "Подразделение",
                    "StorageLocation" => "Ячейка хранения",
                    "StockBalance" => "Остаток на складе",
                    "StockMovement" => "Движение товара",
                    "DocumentItem" => "Строка документа",
                    _ => pending.EntityName
                };


                var actionStr = pending.State switch
                {
                    EntityState.Added => $"Создал запись: {entityNameRu}",
                    EntityState.Modified => $"Изменил запись: {entityNameRu}",
                    EntityState.Deleted => $"Удалил запись: {entityNameRu} (ID: {entityId})",
                    _ => "Неизвестное действие"
                };

                context.Add(new AuditLog
                {
                    UserId = pending.UserId,
                    ActionDate = DateTime.UtcNow,
                    Action = actionStr,
                    EntityName = entityNameRu,
                    EntityId = entityId,
                    ChangesJson = pending.ChangesJson
                });
            }



            await context.SaveChangesAsync(cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }


    private class AuditPendingEntry
    {
        public EntityEntry Entry { get; set; } = null!;
        public int UserId { get; set; }
        public string EntityName { get; set; } = null!;
        public EntityState State { get; set; }
        public string? ChangesJson { get; set; }
    }
}