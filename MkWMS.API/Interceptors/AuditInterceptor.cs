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

    // Потокобезопасная таблица для хранения логов-черновиков между фазами Saving и Saved
    private static readonly ConditionalWeakTable<DbContext, List<AuditPendingEntry>> _pendingAudits = new();

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // =========================================================================
    // ФАЗА 1: Собираем изменения ДО сохранения в базу (когда ID еще отрицательные)
    // =========================================================================
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
                    Entry = entry, // Сохраняем ссылку на объект, чтобы потом забрать реальный ID
                    UserId = userId,
                    EntityName = entityName,
                    State = entry.State,
                    ChangesJson = auditDetails.Any() ? JsonSerializer.Serialize(auditDetails) : null
                });
            }
        }

        if (pendingList.Any())
        {
            // Сохраняем черновики в кэш для текущего контекста БД
            _pendingAudits.AddOrUpdate(context, pendingList);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // =========================================================================
    // ФАЗА 2: Пишем логи ПОСЛЕ сохранения, когда база выдала нормальные ID
    // =========================================================================
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        // Проверяем, есть ли для этого запроса отложенные логи
        if (context != null && _pendingAudits.TryGetValue(context, out var pendingList))
        {
            // Обязательно удаляем из кэша, иначе уйдем в рекурсию при следующем SaveChanges
            _pendingAudits.Remove(context);

            foreach (var pending in pendingList)
            {
                // 🔥 ВОТ ОНО! Теперь база отработала, и у Entry лежит нормальный сгенерированный ID
                var entityId = pending.Entry.Properties
     .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "0";

                // Переводим технические имена на русский
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

                // Формируем понятное русское действие
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

            // Вызываем сохранение логов. 
            // Это запустит SavingChangesAsync еще раз, но сработает "if (entry.Entity is AuditLog) continue;"
            await context.SaveChangesAsync(cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    // Вспомогательный класс для хранения данных между фазами
    private class AuditPendingEntry
    {
        public EntityEntry Entry { get; set; } = null!;
        public int UserId { get; set; }
        public string EntityName { get; set; } = null!;
        public EntityState State { get; set; }
        public string? ChangesJson { get; set; }
    }
}