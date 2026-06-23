using MkWMS.API.DTOs;
using MkWMS.Desktop.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.IO;

namespace MkWMS.Desktop.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        private readonly AuthService _auth;

        private readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiClient(AuthService auth)
        {
            _auth = auth;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };

            _http = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:7000/api/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        private void AddAuth()
        {
            if (!string.IsNullOrEmpty(_auth.Token))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);
            else
                _http.DefaultRequestHeaders.Authorization = null;
        }

        // =========================================================
        // AUTH
        // =========================================================
        public async Task<(bool Success, string? Message, bool RequiresPasswordChange, string? Token, UserDto? User)>
            LoginAsync(string login, string password)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(ApiEndpoints.Login, new { Login = login, Password = password });
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var dto = JsonSerializer.Deserialize<LoginResponseDto>(json, _json);
                    return (true, dto?.Message, dto?.RequiresPasswordChange ?? false, dto?.Token, dto?.User);
                }

                var err = JsonSerializer.Deserialize<LoginResponseDto>(json, _json);
                return (false, err?.Message ?? "Ошибка сервера", false, null, null);
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка сети: {ex.Message}", false, null, null);
            }
        }

        public async Task<bool> ChangePasswordAsync(string oldPass, string newPass)
        {
            AddAuth();
            var dto = new { OldPassword = oldPass, NewPassword = newPass };
            var response = await _http.PostAsJsonAsync(ApiEndpoints.ChangePassword, dto);
            return response.IsSuccessStatusCode;
        }

        // =========================================================
        // UNIVERSAL CRUD
        // =========================================================
        public async Task<PagedResult<T>?> GetPagedAsync<T>(string endpoint, PagedRequestDto req)
        {
            AddAuth();
            try
            {
                var url = $"{endpoint}?Page={req.Page}&PageSize={req.PageSize}";

                if (!string.IsNullOrWhiteSpace(req.Search))
                    url += $"&Search={Uri.EscapeDataString(req.Search)}";
                if (!string.IsNullOrWhiteSpace(req.SortBy))
                    url += $"&SortBy={req.SortBy}";
                if (!string.IsNullOrWhiteSpace(req.SortDirection))
                    url += $"&SortDirection={req.SortDirection}";

                var response = await _http.GetAsync(url);
                return response.IsSuccessStatusCode
                    ? await response.Content.ReadFromJsonAsync<PagedResult<T>>(_json)
                    : null;
            }
            catch { return null; }
        }

        public async Task<T?> GetByIdAsync<T>(string endpoint, int id)
        {
            AddAuth();
            try
            {
                var response = await _http.GetAsync($"{endpoint}/{id}");
                return response.IsSuccessStatusCode
                    ? await response.Content.ReadFromJsonAsync<T>(_json)
                    : default;
            }
            catch { return default; }
        }

        public async Task<T?> CreateAsync<T>(string endpoint, T dto)
        {
            AddAuth();
            try
            {
                var response = await _http.PostAsJsonAsync(endpoint, dto);
                return response.IsSuccessStatusCode
                    ? await response.Content.ReadFromJsonAsync<T>(_json)
                    : default;
            }
            catch { return default; }
        }

        public async Task<bool> UpdateAsync<T>(string endpoint, int id, T dto)
        {
            AddAuth();
            try
            {
                var response = await _http.PutAsJsonAsync($"{endpoint}/{id}", dto);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteAsync(string endpoint, int id)
        {
            AddAuth();
            try
            {
                var response = await _http.DeleteAsync($"{endpoint}/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // =========================================================
        // DOCUMENTS
        // =========================================================
        public async Task<DocumentDto?> GetDocumentByIdAsync(int id)
            => await GetByIdAsync<DocumentDto>(ApiEndpoints.Documents, id);
        private class CreateResponse { public int Id { get; set; } }
        public async Task<int?> CreateDocumentAsync(CreateDocumentDto dto)
        {
            AddAuth();
            try
            {
                var response = await _http.PostAsJsonAsync(ApiEndpoints.Documents, dto);
                if (!response.IsSuccessStatusCode) return null;

                // Десериализуем именно в маленький объект
                var created = await response.Content.ReadFromJsonAsync<CreateResponse>(_json);
                return created?.Id;
            }
            catch { return null; }
        }

        public async Task<bool> PostDocumentAsync(int id)
        {
            AddAuth();
            var response = await _http.PostAsync($"{ApiEndpoints.Documents}/{id}/post", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UnpostDocumentAsync(int id)
        {
            AddAuth();
            var response = await _http.PostAsync($"{ApiEndpoints.Documents}/{id}/unpost", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<PagedResult<DocumentDto>?> GetDocumentsAsync(PagedRequestDto req)
            => await GetPagedAsync<DocumentDto>(ApiEndpoints.Documents, req);

        public async Task<List<DocumentDto>?> GetDocumentsByBaseIdAsync(int baseId)
        {
            AddAuth();
            try
            {
                return await _http.GetFromJsonAsync<List<DocumentDto>>($"{ApiEndpoints.Documents}/by-base/{baseId}", _json);
            }
            catch { return null; }
        }

        // =========================================================
        // USERS & ROLES
        // =========================================================
        public async Task<List<RoleDto>?> GetRolesAsync()
        {
            var result = await GetPagedAsync<RoleDto>(ApiEndpoints.Roles, new PagedRequestDto { Page = 1, PageSize = 1000 });
            return result?.Items;
        }

        public async Task<List<RoleDto>?> GetUserRolesAsync(int userId)
        {
            AddAuth();
            try { return await _http.GetFromJsonAsync<List<RoleDto>>($"{ApiEndpoints.Users}/{userId}/roles"); }
            catch { return null; }
        }

        public async Task<bool> AssignRolesAsync(int userId, List<int> roleIds)
        {
            AddAuth();
            var dto = new AssignRolesDto { UserId = userId, RoleIds = roleIds };
            var response = await _http.PostAsJsonAsync($"{ApiEndpoints.Users}/{userId}/roles", dto);
            return response.IsSuccessStatusCode;
        }

        public async Task<UserDto?> CreateUserAsync(CreateUserWithRolesDto dto)
        {
            AddAuth();
            var response = await _http.PostAsJsonAsync(ApiEndpoints.Users, dto);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<UserDto>(_json)
                : null;
        }

        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            AddAuth();
            var response = await _http.PutAsJsonAsync($"{ApiEndpoints.Users}/{id}", dto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeactivateUserAsync(int id)
        {
            AddAuth();
            var response = await _http.PatchAsync($"{ApiEndpoints.Users}/{id}/deactivate", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ActivateUserAsync(int id)
        {
            AddAuth();
            var response = await _http.PatchAsync($"{ApiEndpoints.Users}/{id}/activate", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveRoleAsync(int userId, int roleId)
        {
            AddAuth();
            var response = await _http.DeleteAsync($"{ApiEndpoints.Users}/{userId}/roles/{roleId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RemoveAllRolesAsync(int userId)
        {
            AddAuth();
            var response = await _http.DeleteAsync($"{ApiEndpoints.Users}/{userId}/roles");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<UserDto>?> GetUsersByRoleAsync(int roleId)
        {
            AddAuth();
            try { return await _http.GetFromJsonAsync<List<UserDto>>($"{ApiEndpoints.Users}/role/{roleId}", _json); }
            catch { return null; }
        }

        // =========================================================
        // SPRAVOCHNIKI
        // =========================================================
        public Task<PagedResult<AuditLogDto>?> GetAuditLogsAsync(PagedRequestDto req)
            => GetPagedAsync<AuditLogDto>(ApiEndpoints.AuditLogs, req);

        public Task<PagedResult<BatchDto>?> GetBatchesAsync(PagedRequestDto req)
            => GetPagedAsync<BatchDto>(ApiEndpoints.Batches, req);

        public Task<PagedResult<DepartmentDto>?> GetDepartmentsAsync(PagedRequestDto req)
            => GetPagedAsync<DepartmentDto>(ApiEndpoints.Departments, req);

        public Task<PagedResult<ProductDto>?> GetProductsAsync(PagedRequestDto req)
            => GetPagedAsync<ProductDto>(ApiEndpoints.Products, req);

        public async Task<ProductDto?> GetProductByBarcodeAsync(string barcode)
        {
            AddAuth();
            try { return await _http.GetFromJsonAsync<ProductDto>($"{ApiEndpoints.Products}/by-barcode/{barcode}", _json); }
            catch { return null; }
        }

        public Task<PagedResult<SerialNumberDto>?> GetSerialNumbersAsync(PagedRequestDto req)
            => GetPagedAsync<SerialNumberDto>(ApiEndpoints.SerialNumbers, req);

        public Task<PagedResult<WarehouseDto>?> GetWarehousesAsync(PagedRequestDto req)
            => GetPagedAsync<WarehouseDto>(ApiEndpoints.Warehouses, req);

        public Task<PagedResult<StorageLocationDto>?> GetStorageLocationsAsync(PagedRequestDto req)
            => GetPagedAsync<StorageLocationDto>(ApiEndpoints.StorageLocations, req);

        public async Task<List<DocumentTypeDto>?> GetDocumentTypesAsync()
        {
            var result = await GetPagedAsync<DocumentTypeDto>(ApiEndpoints.DocumentTypes, new PagedRequestDto { Page = 1, PageSize = 1000 });
            return result?.Items;
        }

        // =========================================================
        // COUNTERPARTIES
        // =========================================================
        public Task<PagedResult<CounterpartyDto>?> GetCounterpartiesAsync(PagedRequestDto req)
            => GetPagedAsync<CounterpartyDto>(ApiEndpoints.Counterparties, req);

        public async Task<bool> CreateCounterpartyAsync(CounterpartyDto dto)
            => await CreateAsync(ApiEndpoints.Counterparties, dto) != null;

        public Task<bool> UpdateCounterpartyAsync(int id, CounterpartyDto dto)
            => UpdateAsync(ApiEndpoints.Counterparties, id, dto);

        public Task<bool> DeleteCounterpartyAsync(int id)
            => DeleteAsync(ApiEndpoints.Counterparties, id);

        // =========================================================
        // WAREHOUSES & DEPARTMENTS
        // =========================================================
        public async Task<List<WarehouseDto>?> GetAllWarehousesAsync()
        {
            var result = await GetPagedAsync<WarehouseDto>(ApiEndpoints.Warehouses, new PagedRequestDto { Page = 1, PageSize = 1000 });
            return result?.Items;
        }

        public async Task<List<DepartmentDto>?> GetDepartmentsByWarehouseAsync(int warehouseId)
        {
            AddAuth();
            try
            {
                var response = await _http.GetAsync($"{ApiEndpoints.Departments}?warehouseId={warehouseId}");
                if (!response.IsSuccessStatusCode) return null;

                var result = await response.Content.ReadFromJsonAsync<PagedResult<DepartmentDto>>(_json);
                return result?.Items;
            }
            catch { return null; }
        }

        // =========================================================
        // RFID & FILES & PRINT
        // =========================================================
        public async Task<RfidScanResultDto?> GetItemByRfidAsync(string rfid)
        {
            AddAuth();
            try { return await _http.GetFromJsonAsync<RfidScanResultDto>($"{ApiEndpoints.StorageLocations}/by-rfid/{rfid}", _json); }
            catch { return null; }
        }

        public async Task<byte[]?> GetPrintFormAsync(int documentId, string templateType)
        {
            AddAuth();
            var response = await _http.GetAsync($"{ApiEndpoints.Print}/{templateType}/{documentId}");
            return response.IsSuccessStatusCode
                ? await response.Content.ReadAsByteArrayAsync()
                : null;
        }

        public async Task<bool> UploadDocumentScanAsync(int documentId, string path)
        {
            AddAuth();
            try
            {
                using var content = new MultipartFormDataContent();
                using var fileStream = File.OpenRead(path); // using закроет поток при ошибке
                var file = new StreamContent(fileStream);
                file.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

                content.Add(file, "file", Path.GetFileName(path));

                var response = await _http.PostAsync($"{ApiEndpoints.Files}/documents/{documentId}/upload-scan", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<byte[]?> DownloadDocumentScanAsync(int documentId)
        {
            AddAuth();
            var response = await _http.GetAsync($"{ApiEndpoints.Files}/documents/{documentId}/download-scan");
            return response.IsSuccessStatusCode
                ? await response.Content.ReadAsByteArrayAsync()
                : null;
        }
        public async Task<byte[]?> DownloadDocumentPrintAsync(int documentId, string printType)
        {
            AddAuth();
            // API endpoint должен соответствовать: api/documents/{id}/print/torg12 (или upd, inv3)
            var response = await _http.GetAsync($"documents/{documentId}/print/{printType}");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsByteArrayAsync();
            return null;
        }

        public async Task<bool> ImportProductsFromExcelAsync(string path)
        {
            AddAuth();
            using var content = new MultipartFormDataContent();
            var file = new StreamContent(File.OpenRead(path));

            content.Add(file, "file", Path.GetFileName(path));

            var response = await _http.PostAsync($"{ApiEndpoints.Excel}/import-products", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<byte[]?> ExportStockBalancesExcelAsync(int? warehouseId, int? productId)
        {
            AddAuth();
            var url = $"{ApiEndpoints.Reports}/stock-balances/excel?";
            if (warehouseId.HasValue) url += $"warehouseId={warehouseId.Value}&";
            if (productId.HasValue) url += $"productId={productId.Value}";

            var response = await _http.GetAsync(url);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadAsByteArrayAsync()
                : null;
        }

        // =========================================================
        // REPORTS
        // =========================================================
        public Task<PagedResult<StockBalanceReportDto>?> GetStockBalancesReportAsync(PagedRequestDto req)
            => GetPagedAsync<StockBalanceReportDto>($"{ApiEndpoints.Reports}/stock-balances", req);

       
        public async Task<PagedResult<StockMovementReportDto>?> GetStockMovementsReportAsync(PagedRequestDto req, DateTime? from = null, DateTime? to = null)
        {
            AddAuth();
            try
            {
                var url = $"{ApiEndpoints.Reports}/movements?Page={req.Page}&PageSize={req.PageSize}";
                if (!string.IsNullOrWhiteSpace(req.Search)) url += $"&Search={Uri.EscapeDataString(req.Search)}";
                if (!string.IsNullOrWhiteSpace(req.SortBy)) url += $"&SortBy={req.SortBy}";
                if (!string.IsNullOrWhiteSpace(req.SortDirection)) url += $"&SortDirection={req.SortDirection}";

                // Добавляем даты в запрос
                if (from.HasValue) url += $"&from={from.Value:yyyy-MM-dd}";
                if (to.HasValue) url += $"&to={to.Value:yyyy-MM-dd}";

                var response = await _http.GetAsync(url);
                return response.IsSuccessStatusCode
                    ? await response.Content.ReadFromJsonAsync<PagedResult<StockMovementReportDto>>(_json)
                    : null;
            }
            catch { return null; }
        }
        // =========================================================
        // INTERNAL DTO
        // =========================================================
       
        public class RfidScanResultDto
        {
            public string Type { get; set; } = string.Empty;
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Number { get; set; } = string.Empty;
            public int? ProductId { get; set; }
        }

        public async Task<byte[]?> ExportAnyReportToExcelAsync(string reportEndpoint, PagedRequestDto req)
        {
            AddAuth();
            var url = $"{reportEndpoint}/excel?Page={req.Page}&PageSize={req.PageSize}";
            if (!string.IsNullOrEmpty(req.Search)) url += $"&Search={Uri.EscapeDataString(req.Search)}";

            var response = await _http.GetAsync(url);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadAsByteArrayAsync()
                : null;
        }
    }

    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly AuthService _auth;
        public AuthHeaderHandler(AuthService auth) => _auth = auth;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (!string.IsNullOrEmpty(_auth.Token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);

            return await base.SendAsync(request, ct);
        }
    
    
    }



}