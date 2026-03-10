using MkWMS.API.DTOs;
using MkWMS.Desktop.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace MkWMS.Desktop.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiClient(AuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:7000/api/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        private void AddAuth()
        {
            if (!string.IsNullOrEmpty(_authService.Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authService.Token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<(bool Success, string? Message, bool RequiresChange, string? Token, UserDto? User)> LoginAsync(string login, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("auth/login", new { Login = login, Password = password });
                // Для отладки — смотри в Output Window Visual Studio
                var rawJson = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine("RAW LOGIN RESPONSE: " + rawJson);

                // Пытаемся десериализовать как успешный ответ
                var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>(_jsonOptions);

                if (result == null)
                {
                    return (false, "Сервер вернул пустой или некорректный ответ", false, null, null);
                }

                // Если есть Message и нет Token/User — это ошибка
                if (!string.IsNullOrEmpty(result.Message) && string.IsNullOrEmpty(result.Token))
                {
                    return (false, result.Message, result.RequiresPasswordChange, null, null);
                }

                // Фикс десериализации: если Roles пришло как List<string> — преобразуем в List<RoleDto>
                if (result.User?.Roles != null && result.User.Roles.Any() && result.User.Roles[0].Id == 0 && string.IsNullOrEmpty(result.User.Roles[0].Name) == false)
                {
                    // Уже RoleDto — ничего не делаем
                }
                else if (result.User?.Roles != null && result.User.Roles.Any() && result.User.Roles[0].Id == 0)
                {
                    // Если пришло как strings (старый формат) — фиксим
                    var stringRoles = result.User.Roles.Select(r => r.Name).ToList();
                    result.User.Roles = stringRoles.Select(name => new RoleDto { Id = 0, Name = name }).ToList();
                }

                // Успех
                return (
                    true,
                    result.Message, // обычно null при успехе
                    result.RequiresPasswordChange,
                    result.Token,
                    result.User
                );
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка соединения с сервером: {ex.Message}", "Сеть", MessageBoxButton.OK, MessageBoxImage.Error);
                return (false, $"Сетевая ошибка: {ex.Message}", false, null, null);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Сервер вернул некорректный формат данных: {ex.Message}", "Ошибка JSON", MessageBoxButton.OK, MessageBoxImage.Error);
                return (false, $"Ошибка формата ответа: {ex.Message}", false, null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка при входе: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return (false, $"Неизвестная ошибка: {ex.Message}", false, null, null);
            }
        }

        public async Task<bool> ChangePasswordAsync(string oldPass, string newPass)
        {
            AddAuth();
            try
            {
                var dto = new { OldPassword = oldPass, NewPassword = newPass };
                var response = await _httpClient.PostAsJsonAsync("auth/change-password", dto);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ====================== ОБЩИЙ PAGINATION ======================
        private async Task<PagedResult<T>?> GetPaged<T>(string endpoint, PagedRequestDto req)
        {
            AddAuth();
            try
            {
                var query = $"?Page={req.Page}&PageSize={req.PageSize}";
                if (!string.IsNullOrWhiteSpace(req.Search))
                    query += $"&Search={Uri.EscapeDataString(req.Search)}";
                var response = await _httpClient.GetAsync(endpoint + query);
                if (!response.IsSuccessStatusCode)
                    return null;
                return await response.Content.ReadFromJsonAsync<PagedResult<T>>(_jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private async Task<List<T>?> GetList<T>(string endpoint)
        {
            AddAuth();
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                if (!response.IsSuccessStatusCode)
                    return null;
                return await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        // ====================== КОНКРЕТНЫЕ МЕТОДЫ ======================
        public Task<PagedResult<ProductDto>?> GetProductsAsync(PagedRequestDto req)
            => GetPaged<ProductDto>("products", req);
        public Task<PagedResult<BatchDto>?> GetBatchesAsync(PagedRequestDto req)
            => GetPaged<BatchDto>("batches", req);
        public Task<PagedResult<SerialNumberDto>?> GetSerialNumbersAsync(PagedRequestDto req)
            => GetPaged<SerialNumberDto>("serialnumbers", req);
        public Task<PagedResult<WarehouseDto>?> GetWarehousesAsync(PagedRequestDto req)
            => GetPaged<WarehouseDto>("warehouses", req);
        public Task<PagedResult<DepartmentDto>?> GetDepartmentsAsync(PagedRequestDto req)
            => GetPaged<DepartmentDto>("departments", req);
        public Task<List<DocumentTypeDto>?> GetDocumentTypesAsync()
            => GetList<DocumentTypeDto>("documenttypes");
        public Task<PagedResult<DocumentDto>?> GetDocumentsAsync(PagedRequestDto req)
            => GetPaged<DocumentDto>("documents", req);
        public async Task<DocumentDto?> GetDocumentByIdAsync(int id)
        {
            AddAuth();
            try
            {
                var response = await _httpClient.GetAsync($"documents/{id}");
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<DocumentDto>(_jsonOptions);
            }
            catch
            {
                return null;
            }
        }
        public async Task<int?> CreateDocumentAsync(CreateDocumentDto dto)
        {
            AddAuth();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("documents", dto);
                if (!response.IsSuccessStatusCode) return null;
                var idStr = await response.Content.ReadAsStringAsync();
                return int.TryParse(idStr, out int id) ? id : null;
            }
            catch
            {
                return null;
            }
        }
        public async Task<bool> PostDocumentAsync(int id)
        {
            AddAuth();
            try
            {
                var response = await _httpClient.PostAsync($"documents/{id}/post", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> UnpostDocumentAsync(int id)
        {
            AddAuth();
            try
            {
                var response = await _httpClient.PostAsync($"documents/{id}/unpost", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public async Task<List<RoleDto>?> GetAllRolesAsync()
        {
            AddAuth();
            return await _httpClient.GetFromJsonAsync<List<RoleDto>>("api/roles");
        }
        public async Task<List<RoleDto>?> GetUserRolesAsync(int userId)
        {
            AddAuth();
            return await _httpClient.GetFromJsonAsync<List<RoleDto>>($"api/users/{userId}/roles");
        }
        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            AddAuth();
            var response = await _httpClient.PutAsJsonAsync($"api/users/{id}", dto);
            return response.IsSuccessStatusCode;
        }
        public async Task<bool> AssignRolesAsync(int userId, List<int> roleIds)
        {
            AddAuth();
            var dto = new AssignRolesDto
            {
                UserId = userId,
                RoleIds = roleIds
            };
            var response = await _httpClient.PostAsJsonAsync($"api/users/{userId}/roles", dto);
            return response.IsSuccessStatusCode;
        }
        public async Task<UserDto?> CreateUserAsync(CreateUserWithRolesDto dto)
        {
            AddAuth();
            try
            {
                var response = await _httpClient.PostAsJsonAsync("users", dto);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<UserDto>(_jsonOptions);
            }
            catch
            {
                return null;
            }
        }
        public Task<PagedResult<StockBalanceReportDto>?> GetStockBalancesReportAsync(PagedRequestDto req)
            => GetPaged<StockBalanceReportDto>("reports/stock-balances", req);
        public Task<PagedResult<StockMovementReportDto>?> GetStockMovementsReportAsync(PagedRequestDto req)
            => GetPaged<StockMovementReportDto>("reports/movements", req);
        public Task<PagedResult<UserDto>?> GetUsersAsync(PagedRequestDto req)
            => GetPaged<UserDto>("users", req);
        public Task<List<RoleDto>?> GetRolesAsync()
            => GetList<RoleDto>("roles");
        public Task<PagedResult<AuditLogDto>?> GetAuditLogsAsync(PagedRequestDto req)
            => GetPaged<AuditLogDto>("auditlogs", req);
        public async Task<bool> DeleteUserAsync(int id)
        {
            AddAuth();
            try
            {
                var response = await _httpClient.DeleteAsync($"users/{id}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}